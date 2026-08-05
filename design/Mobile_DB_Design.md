# Restaurant Mobile App — Database Design

**Stack:** Flutter (Riverpod + Freezed + Drift) ⇄ .NET 8 / ASP.NET Zero (ABP) ⇄ SQL Server
**Modules:** Menu · Order · KOT · BOT · Kitchen (KDS) · Review — offline-first.

This is a **three-layer** design. A mobile app that must keep working when the
Wi-Fi drops can't be a thin client over one database; it needs its own.

```
┌─────────────────────────┐   pull deltas (ChangeLog)   ┌──────────────────────────┐
│  ON-DEVICE  (SQLite)     │ ◀───────────────────────────│  SERVER  (SQL Server/ABP) │
│  local_db.sql            │                              │  Server_Schema.sql        │
│  • menu cache            │   push outbox (idempotent)   │  • source of truth        │
│  • orders/tickets/reviews│ ───────────────────────────▶ │  • ChangeLog change-feed  │
│  • outbox + sync_cursor  │                              │  • UploadBatches ledger   │
└─────────────────────────┘     FCM "new ticket/ready"   └──────────────────────────┘
                                ◀───────────────────────────
```

| Layer | File | Role |
|---|---|---|
| Server | `Server_Schema.sql` | 25 tables / 6 schemas. Canonical data + change-feed. ABP conventions. |
| Device | `local_db.sql` | 18 tables. Offline mirror, money as **integer paisa**, outbox queue. |
| Sync | (protocol below) | Pull via `ChangeLog.Seq` cursor; push via outbox keyed by `client_guid`. |

---

## 1. KOT vs BOT — it's one routing rule, not two systems

A **KOT** (Kitchen Order Ticket) and a **BOT** (Bar Order Ticket) are the *same
structure* sent to a different **station**. The deciding field is
`MenuItem.StationType` (1 = Kitchen → KOT, 2 = Bar → BOT), snapshotted onto the
order line as `StationTypeSnapshot` so re-routing the menu later never rewrites history.

**When the waiter taps "Send":**

1. Take all `OrderItems` with `Status = New` on the order.
2. Group them by their resolved **station** (item's `DefaultStationId`, else the
   first active station of the matching `StationType` at that outlet).
3. For each group create one `kds.Tickets` row — `TicketKind = 1 (KOT)` for a
   kitchen station, `2 (BOT)` for a bar station — with its `TicketItems`.
4. Flip those order items to `Status = Fired`, stamp `FiredAtAd`.
5. A later "Send" on the same order produces **Addon** tickets (`TicketType = 2`),
   so the kitchen sees "round 2" rather than a duplicate of round 1.

So three food items + two drinks fired together → **1 KOT to the kitchen + 1 BOT to
the bar**, both linked to the same order. The KDS board (`kds.vw_LiveBoard`) shows
each station only its own tickets.

---

## 2. Order lifecycle (the state machine)

```
Draft ─(add items)→ Draft ─(Send)→ Placed/InProgress ─(all items Ready)→ Served ─(bill)→ Billed → Closed
   │                                      │
   └──────────────(Cancel)───────────────┘
```

`sales.OrderStatusHistory` logs every transition. Ticket-level prep is tracked
separately on `kds.Tickets.Status` (Queued → Acknowledged → Preparing → Ready →
Bumped) with a full audit in `kds.TicketEvents` and an auto-computed `PrepSeconds`
column — that's your kitchen-speed analytics for free.

**Snapshots everywhere.** `OrderItems.ItemNameSnapshot/UnitPrice`,
`OrderItemModifiers.*Snapshot`, `TicketItems.NameSnapshot/ModifiersText`. Menu
prices and names drift; a fired ticket and a printed bill must show what was
actually ordered, not today's menu.

---

## 3. Offline-first sync protocol

The whole design turns on two ideas: **client-generated GUIDs** for idempotency
and a **monotonic change-feed cursor** for deltas.

### Pull (server → device)
- Each device stores `sync_cursor.last_seq` per entity type.
- It calls `GET /sync/changes?since={last_seq}`; the server returns
  `sync.ChangeLog` rows where `Seq > last_seq` (scoped to the device's tenant +
  outlet), each carrying the operation and an optional `PayloadJson` snapshot.
- Device applies them to its local tables and advances `last_seq` to the max `Seq`.
- `ChangeLog.ChangedByDeviceId` lets a device **skip echoes of its own writes**.
- Menu, table status, and *other devices'* tickets all flow down this one pipe —
  which is how a bartender's BOT bump shows up on the waiter's phone seconds later.

### Push (device → server)
- Every local create/update/delete writes a row to the `outbox` with the row's
  `client_guid`, the operation, a JSON payload, and `base_version` (the
  server `RowVersion` it was edited from).
- The sync worker drains the outbox FIFO into a batch (`batch_guid`) and posts it.
- The server records the batch in `sync.UploadBatches` (unique on `BatchGuid`) and
  upserts each entity **by `client_guid`** — so a retried upload is a no-op, not a
  duplicate order. It returns `{clientGuid → serverId, status}`; the device fills
  `server_id` and clears `sync_state` to 0.

### Conflict resolution
- If `base_version` ≠ the current server `RowVersion`, it's a conflict.
- Default policy: **last-write-wins on field groups**, except *append-only facts
  never conflict* — adding an order item, firing a ticket, or leaving a review are
  inserts keyed by new GUIDs, so two devices never collide. Conflicts only arise on
  shared mutable state (order header totals, ticket status); those flip the local
  row to `sync_state = 2` and surface a small "refresh" prompt rather than silently
  overwriting. Tune per entity.

### Bill numbers / order numbers
- The device works entirely on `client_guid`. The human-facing `OrderNumber` is
  **assigned by the server on first sync** and pulled back down — never minted
  offline — so you never get duplicate or gapped numbers across devices.

---

## 4. Push notifications (FCM)

`sync.Devices` + `sync.PushTokens` hold each install's FCM token (you've wired
FCM before on the campus ERP, same shape). Server fires:
- **KDS screens** ← "new KOT/BOT at your station" when a ticket is created.
- **Waiter phone** ← "Table 7 order is Ready" when all its tickets bump.
- **Manager** ← "1★ review just posted" when `review.Reviews` lands a low rating.

These are *hints* to sync now; the actual data still comes through the pull feed,
so a missed push never loses data.

---

## 5. Reviews

`review.Reviews` supports both **whole-visit** reviews (`MenuItemId` NULL) and
**per-dish** reviews (`MenuItemId` set), with sub-ratings (food/service/ambience/
value), optional photos (`ReviewMedia`), and staff replies (`ReviewResponses`).
`review.vw_ItemRatingRollup` recomputes per-item averages that you denormalize
back onto `MenuItems.AvgRating / RatingCount` for fast menu rendering. `Sentiment`
is a nullable slot if you later run the comment through an NLP pass.

On device, review photos are captured to `app_review_media.local_path` and
uploaded to Blob storage separately from the row sync (`upload_state`), so a big
photo never blocks the lightweight review record from syncing.

---

## 6. Drift mapping example (Flutter)

The local SQL maps cleanly to Drift. One table, for orientation:

```dart
class AppOrders extends Table {
  TextColumn get clientGuid => text()();
  IntColumn  get serverId => integer().nullable()();
  TextColumn get orderNumber => text().nullable()();
  IntColumn  get orderType => integer().withDefault(const Constant(1))();
  IntColumn  get tableId => integer().nullable()();
  IntColumn  get status => integer().withDefault(const Constant(1))();
  IntColumn  get grandTotal => integer().withDefault(const Constant(0))(); // paisa
  TextColumn get orderDateAd => text()();
  TextColumn get serverVersion => text().nullable()();
  IntColumn  get syncState => integer().withDefault(const Constant(1))();
  BoolColumn get isDeleted => boolean().withDefault(const Constant(false))();
  TextColumn get updatedAt => text()();

  @override
  Set<Column> get primaryKey => {clientGuid};
}
```

Wrap each user action (add item, fire, bump, review) in a Drift transaction that
(a) writes the domain row and (b) appends the matching `outbox` row, so the local
write and its sync intent are atomic. Expose orders/tickets as Riverpod
`StreamProvider`s over Drift's `watch()` queries — the KDS board then updates
reactively as sync applies remote bumps. Freezed DTOs are what you (de)serialize
into `outbox.payload_json` and the ABP API.

---

## 7. Things to confirm

1. **App audience** — I built this as a **staff ops app** (waiter + KDS + reviews).
   If you also want **customer self-order via QR** (`OrderType = 4` and the
   `dining_table.qr_code` hook are already there), the order-creation path opens to
   unauthenticated guests and needs rate-limiting + a lighter menu read API.
2. **KDS transport** — pull-on-push (FCM nudge → pull) is simplest and offline-safe.
   If you want truly instant boards, add a **SignalR** hub for tickets on top of the
   same `ChangeLog` (push the row, fall back to pull). ABP has SignalR built in.
3. **Conflict policy per entity** — confirm last-write-wins is acceptable for order
   header edits, or whether you want a stricter "kitchen owns ticket status, waiter
   owns order items" ownership split.

Tell me which way on #1 and #2 and I'll extend the schema + sketch the ABP
`SyncAppService` (pull/push endpoints) and the Drift sync worker to match.
