# Petpooja-Style Restaurant POS — Database Design

**Stack:** .NET 8 / ASP.NET Zero (ABP) · EF Core · SQL Server · Angular + Flutter clients
**66 tables / 10 schemas.** Localized to Nepal (VAT 13%, IRD-CBMS, NPR, Foodmandu/Pathao, eSewa/Khalti).

This isn't a generic POS schema — it's shaped around the four things Petpooja is
actually known for. Here's how each schema maps to a Petpooja capability.

| Schema | Tables | Petpooja capability it implements |
|---|---|---|
| `hq` | 6 | Brands → zones → outlets, head-office, **hub-and-spoke supply links**, SaaS licensing & module entitlements |
| `catalog` | 10 | Menu: categories, **variations, add-ons, combos**, item tags (veg/spicy), short-codes, kitchen stations |
| `channel` | 7 | **Aggregator integration** — per-channel menu mapping, online ON/OFF, order ingestion, **payout reconciliation** |
| `sales` | 11 | Billing, **category-wise KOT/BOT**, split/merge/move, payments, discounts, **void/anti-pilferage log** |
| `inventory` | 13 | Suppliers, PO (to supplier **or** central kitchen), **multi-stage recipes**, **FIFO** batches, wastage, food cost |
| `centralkitchen` | 5 | **Central kitchen**: production/item-conversion, indents, inter-outlet stock transfers, delivery route |
| `crm` | 6 | Customers, loyalty, feedback, **SMS/WhatsApp marketing campaigns** & segments |
| `finance` | 5 | Fiscal year, taxes, day-close (Z-report) with food cost, expenses, **IRD-CBMS sync** |
| `integration` | 2 | **Outbound ERP/Tally/SAP push** (data-lake outbox), peripheral device registry |
| `hr` | 1 | Outlet-wise **staff rights** (Bill.Void, Discount.Apply…) with PIN gating |

---

## Flow 1 — Aggregator integration (the signature feature)

Petpooja's pitch is "manage one menu, publish to every aggregator with its own
price and on/off, and ingest their orders into one screen." Three pieces:

**a) Publish menu out.** `channel.Channels` lists every sales channel (Dine-In,
Takeaway, plus each aggregator). `channel.ChannelItems` maps each catalog item to
a channel with its own `ChannelPrice` (delivery prices usually marked up to absorb
commission), an `ExternalItemId`, and a per-item `IsOnline` toggle. Flipping an
item off, or changing a price, sets `SyncStatus = PendingPush`; a job pushes the
delta to the platform API and logs it in `channel.MenuSyncLog`. `AggregatorAccounts.IsOnline`
is the master storefront switch for "go offline on Foodmandu right now."

**b) Ingest orders in.** A webhook/poll writes the raw platform order to
`channel.AggregatorOrders` (unique on `Provider + ExternalOrderId` → idempotent).
On accept, it's converted into a normal `sales.Orders` row (`ChannelId` = that
aggregator, `AggregatorOrderId` back-link) so it flows through the same KOT,
kitchen, and inventory-depletion path as a dine-in order. One kitchen, all channels.

**c) Reconcile the payout.** This is the part most POS systems skip.
`channel.AggregatorPayouts` + `AggregatorPayoutLines` ingest the settlement report
the platform sends each cycle. Each line is matched back to an `AggregatorOrders`
row (`MatchStatus`: Matched / Unmatched / Discrepancy) so you can see real
commission, restaurant-borne discount, cancellations, and **what was actually paid
vs. what the POS expected** — the "real cost of operating on aggregators" report.

---

## Flow 2 — Central kitchen (hub-and-spoke)

`hq.OutletSupplyLinks` wires each spoke outlet to its hub (a `core` outlet flagged
`IsCentralKitchen`). Then:

```
Outlet runs low ─▶ centralkitchen.Indents (request)        [spoke → hub]
Hub produces    ─▶ centralkitchen.ProductionOrders          (consume RM → produce
                                                              semi-finished material,
                                                              recording ConversionCost)
Hub dispatches  ─▶ centralkitchen.StockTransfers (+lines)   [hub → spoke, with route]
Spoke receives  ─▶ StockMovements TransferIn at the spoke    (stock + cost land locally)
```

A purchase order can target a supplier **or** the central kitchen
(`PurchaseOrders.SourceType`), exactly like Petpooja's "raise/accept PO tickets
from suppliers or your central kitchen." Production output is a *semi-finished
raw material* (`RawMaterials.IsSemiFinished = 1`), which can itself be an
ingredient in another recipe — that's what makes recipes multi-stage.

---

## Flow 3 — Multi-stage recipes & food costing

A `Recipe` outputs either a menu `Item` or a semi-finished `RawMaterial`. It has
ordered `RecipeStages` (Marination → Cooking → Plating), each with `RecipeLines`
of ingredients. Because an ingredient can be a semi-finished material that has its
*own* recipe, costs roll up through the tree. The `catalog.vw_ItemFoodCost` view
sums `qty × (1 + wastage%) × standardCost` to give per-item recipe cost; compared
against sale price that's your **food-cost %** and margin report. Sale-time
depletion walks the recipe, writes negative `StockMovements`, and consumes
`StockBatches` oldest-first for true **FIFO** costing. Low stock vs `ReorderLevel`
drives the "low stock" alert shown on the billing screen.

---

## Flow 4 — Outbound ERP / Tally integration

Petpooja exports sales/inventory/customer data to external accounting (Tally, SAP,
NAV). `integration.ErpOutbox` is an append-only outbound feed: every posted
invoice, purchase, stock movement, or customer writes a row tagged with `Target`
and `EntityType`. A connector job drains pending rows, maps them to the target's
format, posts them, and marks them Sent/Failed with retry — so your books stay in
sync without coupling the POS to any one accounting system.

---

## Nepal compliance (kept from your stack)

`sales.Invoices` are materialized and gapless per `finance.FiscalYears` (BS
2081/82, Shrawan–Ashar). Each issued invoice queues a `finance.CbmsSyncLog` row
that a Hangfire job pushes to the IRD CBMS endpoint with retry — bills issue even
offline and sync when connectivity returns. VAT 13% / service charge 10% default
per outlet, overridable per item. Buyer name + PAN captured for B2B tax invoices.

---

## Anti-pilferage (Petpooja "regulate staff rights to avoid fraud")

`hr.StaffRights` grants outlet-scoped rights (`Bill.Void`, `Discount.Apply`,
`Item.Cancel`, `Reports.View`) on top of ABP roles, with `RequiresPin` for
sensitive actions. Every void, item cancel, bill void, or discount override writes
to `sales.VoidLog` with the actor and approver — that's the fraud/pilferage report.

---

## What I'd confirm before going further

1. **Which aggregators** — Foodmandu, Pathao, Bhojdeals all have different API
   shapes. The `channel` schema is provider-agnostic (`Provider` string +
   `ApiCredentialsJson` + `RawPayloadJson`); naming the real targets lets me model
   exact field mappings and the ingestion service.
2. **Own online ordering** — if you want a Petpooja-style branded website/app
   (not just aggregators), `ChannelType = OwnOnline` is already there; it needs a
   customer-facing menu API + payment-gateway callback handling.
3. **Depth of food costing** — recipe-cost rollup is modeled; if you want
   theoretical-vs-actual variance (what *should* have been consumed per sales vs.
   what *was* per stock count), I can add the variance tables.

Say which aggregators and whether own-online is in scope, and I'll sketch the ABP
`AggregatorIntegrationAppService` (menu push + order ingest + payout import) and
the central-kitchen indent→transfer service to match.
