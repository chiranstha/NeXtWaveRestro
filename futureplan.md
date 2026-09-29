# NextWave Restaurant Mobile - Future Plan

Last reviewed: 2026-08-15

## Baseline delivered now

The first mobile release deliberately follows the existing ABP backend instead of inventing a second workflow. It is an online-first staff application with:

- tenant-aware login, secure token storage, automatic access-token refresh, two-factor verification, required-password reset, permission-based navigation, and logout;
- live menu categories, availability, variants, modifiers, recipe/cost visibility, table/takeaway/delivery contexts, existing open-order editing, unsaved-change protection, KOT/BOT dispatch/history/reprint, discounts/manager PIN, stock validation, accounting-ledger selection, and server-finalized billing;
- table transfer, order split/merge, item void with reason, and partial billing by saved item/quantity;
- station-filtered KDS with ticket/item status progression, cancellation reasons, and periodic refresh;
- low-stock suggestions, supplier mappings, draft purchase orders, stock adjustments/counts/wastage, recipe consumption/coverage, channel menu publishing, aggregator acceptance/status, payouts, editable restaurant setup/device health, and all current restaurant report endpoints;
- employee-first staff onboarding with optional login creation/linking, allowlisted primary restaurant roles, one-time temporary credentials, protected Admin access, and safe unlink/deactivation; plus self clock-in/out, manager attendance correction, monthly/hourly wages, overtime, fixed allowances/deductions, weighted tips and service-charge distribution, draft/approve/paid payroll runs, and role-private payslips;
- Android and iOS project runners, with the API origin configurable by `APP_BASE_URL`.

This baseline does not claim offline operation. Tax, service charge, recipe consumption, permissions, accounting entries, and final payable remain server-owned so the mobile app cannot drift from ERP rules.

## Competitor gap summary

The comparison below uses feature claims published by Nepal-market products. They are useful product signals, not proof of implementation quality; confirm behavior in demos before committing to parity.

| Capability commonly advertised in Nepal | Examples | Current NextWave state | Direction |
| --- | --- | --- | --- |
| Offline-first billing and automatic sync | [Vrestro](https://vrestro.com/), [Alphid](https://alphid.com/), [Mugu Shop](https://www.mugu.shop/) | Missing; mobile requires the ERP API | Highest priority: local operational store, durable command queue, conflict rules, and reconnect reconciliation |
| Table QR/self-ordering and call-waiter flow | [Vrestro](https://vrestro.com/), [RestroPasa](https://www.restropasa.com/), [NRestro](https://www.nrestro.com/) | Backend permission/channel foundations exist, but no guest journey | Add a separate guest web/PWA surface with signed table tokens and staff approval |
| Split bills, split tender, table move/merge | [RestroPasa](https://www.restropasa.com/), [Restronp](https://www.restronp.com/features), [Zaika](https://zaikax.com/) | Partial bills, order split/merge, and table transfer are delivered; split tender is missing | Add server-owned multiple payment allocations and settlement validation |
| Dynamic QR/wallet payments | [RestroPasa](https://www.restropasa.com/), [NRestro](https://www.nrestro.com/) | Mobile sends the backend payment enum only; no gateway confirmation | Add a backend payment-provider registry, Fonepay/eSewa/Khalti adapters, callback verification, and settlement reconciliation |
| Visual floor, reservations, waitlist, guest history | [Restronp](https://www.restronp.com/features), [SM Tech](https://www.smtechme.com/product/restaurant-pos/) | Basic areas/tables and live occupancy only | Add floor coordinates, reservations, walk-in queue, deposits, no-show handling, and guest notes/allergies |
| Loyalty, promotions, coupons, CRM | [Mugu Shop](https://www.mugu.shop/), [RestroPasa](https://www.restropasa.com/) | Missing | Add customer identity, earn/redeem ledger, promotion engine, consent, and targeted offers |
| Attendance, payroll, tips pooling | [Vrestro](https://vrestro.com/), [iFusion](https://www.ifusionapp.com/), [RestroPasa](https://www.restropasa.com/) | Core attendance, monthly/hourly payroll, OT, weighted tips/service charge, approval, payment state, and private payslips are delivered | Add roster planning, biometric/device sync, Nepal statutory calculations, accounting posting, and bank/export workflows |
| Printer/device routing and kiosk support | [Vrestro](https://vrestro.com/), [Mugu Shop](https://www.mugu.shop/) | KOT and billing APIs exist; mobile has no printer/device layer | Add printer discovery, station routing, retries, reprint audit, customer display, and kiosk mode |
| Multi-branch/store transfer and central kitchen | [Vrestro](https://vrestro.com/), [RestroPasa](https://www.restropasa.com/) | ERP is tenant-aware, but the mobile session has no explicit outlet context | Introduce outlet/device assignments, store-scoped stock, transfers, commissary production, and cross-branch reporting |
| Real-time event delivery | [Zaika](https://zaikax.com/) | KDS polls every 15 seconds | Add SignalR/WebSocket events with polling fallback and delivery acknowledgements |
| IRD/CBMS operational visibility | [IRD enlisted software list](https://www.ird.gov.np/public/pdf/1858543615.pdf), [Vrestro](https://vrestro.com/) | Billing remains backend-owned; no mobile queue/status dashboard | Verify the product's official IRD/CBMS requirements, then expose sync state, failures, retries, and immutable audit history |

## Recommended roadmap

### P0 - make the mobile POS dependable in daily service

1. **Offline-first order and billing continuity**
   - Cache menu, tables, stations, permissions, ledgers, open orders, and device/outlet settings in an encrypted local database.
   - Queue idempotent order/KOT/payment commands with a server-issued device identity and client request IDs.
   - Define field-level conflict rules: sent/billed items are immutable; draft-line edits can merge; payment/finalization conflicts always stop for staff review.
   - Show offline, pending, conflicted, and failed states. Never label a bill finalized before the server returns the posted sale ID.

2. **Production authentication and connectivity**
   - Access-token refresh and expiry retry are delivered; add forced logout/device revocation behavior for refresh-token expiry.
   - Provision API origins through managed build flavors or tenant discovery rather than asking production users to type a server URL.
   - Add certificate pinning/rotation policy, device registration, remote logout, and compromised-device revocation.

3. **Finish the controls not yet represented by current mobile/backend contracts**
   - Delivered now: table transfer, split/merge order, partial bill, item void with reason, KOT reprint, and manager-PIN checks supported by the backend.
   - Add explicit reopen rules, order-level void, guest count editing, waiter reassignment, structured order notes, and supervisor approval for split/transfer if product policy requires them.

4. **Receipts and restaurant hardware**
   - Bluetooth/LAN/USB printer abstraction, station-specific KOT/BOT templates, retry queue, paper-out recovery, duplicate-print warning, and reprint reason.
   - Cash drawer, barcode scanner, customer display, and Android kiosk/lock-task support.

5. **Payment correctness**
   - Replace the fixed mobile payment list with server-provided methods and ledger mappings.
   - Support split tender, exact/partial payments, credit limits, tips, refunds, and verified dynamic QR payments.
   - Reconcile gateway reference, bank ledger, aggregator payout, bill, refund, and chargeback end to end.

### P1 - close the most visible market gaps

1. **Floor, reservations, and waitlist**: visual floor editor/view, availability timeline, booking confirmation, walk-in queue, deposits, table assignment, no-show state, and turn-time metrics.
2. **Guest QR ordering**: signed table QR, tenant/outlet menu, modifiers, cart, waiter call, order approval, status, payment, multilingual content, and throttling/abuse controls.
3. **Customer and loyalty**: profiles, order history, preferences/allergies, points ledger, tiers, coupons, promotions, birthday offers, consent, and merge/forget flows.
4. **Inventory operations**: stock count, wastage with reason/photo, transfer, purchase approval, GRN, supplier mapping fixes, recipe variance, and central-kitchen requisition.
5. **Staff operations**: attendance, weighted tip/service pooling, payroll runs, approval, payment state, and payslips are delivered; add roster planning, leave requests, opening float, cash in/out, denomination count, handover variance, and biometric sync.
6. **Live operations**: SignalR-driven table/order/KDS/channel updates, notifications, SLA alerts, and polling fallback.

### P2 - scale, intelligence, and customer experience

1. Multi-branch command center, outlet switching, central menu/pricing with local overrides, consolidated purchasing, and cross-branch analytics.
2. Delivery dispatch, rider assignment/tracking, delivery zones/fees, proof of delivery, and marketplace-menu/availability synchronization.
3. Review/feedback capture, complaint recovery, NPS, and service-quality tasks linked to order/table/waiter.
4. Forecasting for sales, prep, purchase, waste, staffing, and stockout risk only after data quality and audit controls are proven.
5. Owner mobile experience with scheduled summaries, anomaly alerts, drill-down reports, exports, and approval inbox.

## Payroll improvements intentionally deferred

The imported payroll is restaurant-native and does not carry AcademicUpgrade's school-only grades, teacher logs, hostel deductions, or branch-school voucher coupling. The following remain later enhancements:

- Nepal statutory payroll rules including configurable income-tax slabs, SSF/PF/CIT employer and employee contributions, festival allowance, gratuity, and fiscal-year reconciliation;
- leave requests, holidays, weekly-off calendars, shift rosters, late/grace policies, overnight shifts, biometric/device imports, and manager exception approval;
- employee loans/advances with schedules, recurring pay heads, bonuses, commissions, cash-shortage recovery, and per-run adjustment editing;
- payroll accounting posting with wage, tax, liability, cash/bank ledgers, reversal rules, immutable posting references, and period locks;
- payslip PDF/download, bulk email, bank payment export, statutory reports, audit export, and multi-outlet payroll allocation;
- explicit rounding, minimum wage, overtime, service-charge, and tip-pooling policies reviewed by the restaurant's accountant/legal owner before production use.

## Architecture work required before P0 offline mode

- Add versioned mobile contracts rather than coupling releases indefinitely to generated Angular-facing DTO shapes.
- Add `/mobile/bootstrap` (or equivalent) returning the authenticated user's outlet, device policy, permissions, menu version, table/station snapshot, ledgers, payment methods, printer routes, and sync cursor.
- Add incremental pull endpoints/event cursors and an idempotency record for every mobile mutation.
- Add server time, tenant time zone, Nepali fiscal/calendar metadata, supported app version, maintenance mode, and forced-upgrade response.
- Model outlet/device scope explicitly. Tenant ID alone is not enough for multi-outlet stock, tables, printers, ledgers, and reports.
- Add structured audit events for login, discount, void, transfer, reprint, stock override, payment, offline replay, and conflict resolution.

## Release gates for later phases

- No duplicate order, KOT, sale, stock posting, or payment under retry/reconnect tests.
- A cashier can complete a full shift during a simulated internet outage and reconcile cleanly afterward.
- Permission tests cover manager, cashier, waiter, kitchen, and inventory roles on both UI and API.
- Real devices pass printer, scanner, QR, background/resume, low-memory, clock-change, and intermittent-network tests.
- Financial totals, Nepali dates, VAT/IRD/CBMS behavior, refunds, and fiscal-year boundaries are verified against the production backend and reviewed by the accounting/compliance owner.
- Security review covers token storage, tenant isolation, outlet/device scope, local database encryption, logs, screenshots, backups, and remote revocation.
