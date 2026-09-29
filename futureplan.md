# NeXtWave Restro: reliable launch and feature roadmap

Last reviewed: 2026-09-29

## Product goal

Launch a dependable product for Nepal cafés and restaurants with a browser cashier and Android waiter/kitchen app. The pilot is one outlet per restaurant account. Checkout stays in the browser; Android supports waiter and kitchen work. Cashier-verified card and QR payments come before automated gateway confirmation. Full offline billing, iPhone launch, and chain management are outside the first release.

## Current baseline

The repository already contains POS orders and billing, table transfer, split and merge orders, partial bills, kitchen tickets and KDS, recipes and inventory, guest table QR ordering, reservations, cash shifts, payroll, reports, and Windows print-agent routing, queueing, and retries. The presence of a module is not proof that its full workflow is production-ready. Each workflow still needs end-to-end validation, failure recovery, permission checks, and accounting reconciliation.

The main release gaps are:

| Area | Starting point | Required result |
| --- | --- | --- |
| Split tender | Cash, card, and QR allocations now post with the invoice; methods depend on tenant ledger configuration | Validate accounting reconciliation, concurrent settlement, tips, and cash-shift closing before pilot |
| Outage recovery | Browser recovery is extended; Android now stores encrypted drafts by tenant and user and restores them for review | Pass the 30-minute outage/restart scenario and verify refreshed prices, availability, table state, and order state before submission |
| Payment retries | Billing status lookup and stable request IDs are wired into browser checkout; Android order creation reuses its saved request ID | Exercise simultaneous submissions and lost responses against an isolated database; verify all endpoints preserve idempotency |
| Kitchen updates | Browser KDS reports stale/disconnected state and refreshes on reconnect or resume while retaining 15-second polling | Verify the full ticket lifecycle, overdue display, guest-order queue, and Android recovery during the pilot |
| Printing | Database claims now run serializably; the existing Windows agent retains queue and retry support | Verify restart durability, competing stations, paper failures, and explicit handling of uncertain physical output |
| Automatic digital settlement | No restaurant gateway confirmation integration | Add after pilot, beginning with Fonepay dynamic QR |

Code above is implemented in the current working tree; it is not a production-readiness claim. Refund workflows, order-version conflict checks, the combined daily closing view, a guided launch checklist, and restaurant-level rollout switches/client-capability discovery remain open release work. Automated accounting/race tests, the isolated busy-restaurant run, backup-restore drill, and ten-shift pilot have not been completed.

## Release sequence

### Milestone 1 — Billing and cash control

- Support cash, cashier-verified card/QR, and mixed payment allocations on one bill.
- Configure payment methods and accounting ledgers centrally; keep a method unavailable until its configuration is valid.
- Require tender allocations to equal the amount due. Calculate change from cash received only.
- Require an open cashier shift for settlement and reconcile opening cash, cash movements, refunds, closing count, and approved differences.
- Add full and partial refunds linked to the original invoice, backed by existing sales-return accounting. Require a reason and manager authorization. Keep refunding money separate from returning ingredients; prepared food does not replenish usable stock.
- Preserve finalized invoices. Record corrections as linked records.

### Milestone 2 — Safe work recovery

- Extend browser recovery and add durable Android drafts, scoped by restaurant and signed-in user; restore drafts after app restart.
- Show when a draft was saved, connection status, and whether it reached the server. Permit editing drafts during outages, but require the server for kitchen dispatch, payment confirmation, and final billing.
- On reconnect, refresh prices, availability, table state, and order status before submission. Show changed values for staff review.
- Persist one request ID for each attempted operation and reuse it on retry. After an uncertain bill response, retrieve its result before another payment attempt.
- Add order-version checks so competing device edits produce a clear conflict and show the latest server version.

### Milestone 3 — Kitchen and print reliability

- Verify order → kitchen ticket → preparation → ready → served, including modifiers and preparation notes.
- Show pending guest orders, overdue tickets, last refresh, and disconnected state on kitchen screens.
- Keep polling for the pilot; refresh immediately on reconnect and app resume.
- Claim print jobs atomically so two stations cannot process one job. Retain queued jobs over service restarts, show failed jobs, and support controlled retries.
- Distinguish confirmed print failure from uncertain physical output. Require an explicit reprint action when paper output cannot be confirmed.

### Milestone 4 — Stock and daily operations

- Validate recipe quantities, units, variants, and partial-bill consumption. A posted sale consumes ingredients once.
- Show missing recipes and unit mappings before service. Apply the restaurant's negative-stock policy consistently in each client.
- Provide one closing view for sales, collections by method, unpaid balances, discounts, voids, wastage, refunds, tips, and cash differences.
- Add setup checks for menu import, tables, kitchen stations, payment ledgers, staff roles, printers, and test receipts.
- Smoke-test guest ordering, reservations, attendance, and payroll; defer major expansion of these modules.

## Implementation boundaries

- Retain Angular, Flutter, ASP.NET Core/ABP, SQL Server, and the existing Windows print agent.
- Extend `RestaurantBilling/FinalizeBill` and existing tender records to post payment allocations atomically with the sale, inventory, and receipt job.
- Expose enabled payment methods, ledger mappings, and client capabilities from the server. Update Angular and Flutter contracts together.
- Add order-version checks, consistent request IDs, and operation-status lookup to order and billing contracts. Enforce permissions server-side.
- Add restaurant refunds linked to sales-return records, with explicit stock disposition and refund-settlement status.
- Use additive migrations. Preserve historical invoices and legacy single-payment records. Gate new capabilities with restaurant-level switches.

## Pilot gates

Do not release until these scenarios pass:

- Double-clicks, concurrent requests, and lost responses create no duplicate order, ticket, invoice, tender, or stock posting.
- Mixed tender, cash change, partial bills, tips, refunds, and shift closing reconcile with accounting.
- A 30-minute outage and browser/app restart preserve drafts and never claim a ticket was sent or a bill was paid when the server did not confirm it.
- Two devices editing, transferring, splitting, or billing one order produce controlled conflicts.
- Printer disconnects, service restart, paper failure, and competing stations leave recoverable, understandable job states.
- Guest QR tokens, staff permissions, and tenant boundaries prevent unauthorized access.
- Nepal date handling, invoice configuration, rounding, and applicable IRD/CBMS behavior are verified before production claims.
- Run the existing busy-restaurant dataset against an isolated test database, then pilot at one restaurant for ten consecutive shifts with no unexplained financial differences or lost accepted orders.
- Monitor failed billing, unresolved payment results, kitchen refresh failures, print failures, and reconciliation differences. Complete a backup-restore drill before the pilot.

## After the pilot

| Priority | Feature | Next increment |
| --- | --- | --- |
| 1 | Automatic Fonepay confirmation | Per-bill QR, server-side verification, delayed-payment handling, and settlement reconciliation |
| 2 | Live service updates | Push kitchen, table, and guest-order changes with polling fallback |
| 3 | Repeat-customer tools | Customer history, consent, loyalty points, simple coupons, and feedback |
| 4 | Guest service | Visual floor plan, waiter/bill requests, allergy information, and better waitlist handling |
| 5 | Food-cost control | Actual versus recipe consumption, supplier price changes, wastage trends, and item margins |
| Later | Expansion | Multiple outlets, central kitchen, delivery connectors, staff rostering, and full offline operation |

This ordering follows the published feature sets of [NRestro](https://www.nrestro.com/) and [Petpooja](https://www.petpooja.com/us/poss). Fonepay activation depends on access through a bank or provider, as described in its [merchant guidance](https://fonepay.com/blogs/fonepay-dynamic-qr-code). These market references are product signals, not proof of implementation quality.
