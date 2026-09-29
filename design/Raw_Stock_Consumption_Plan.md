# Raw stock consumption plan

## Recommendation

Use the existing raw material, recipe (BOM), and stock posting model. For restaurant and room service items, post recipe consumption **once when a KOT/BOT is sent to the kitchen or bar**. Keep the bill linked to that consumption, but do not deduct the same ingredients again at billing. This gives staff one simple action during service and keeps stock current while tables or room charges remain open.

Roll this out in stages. Until the kitchen posting change is ready, continue using the existing bill-time deduction. Never enable both deduction points for the same order item.

## Current ERP baseline

- `SaveRecipe` stores active BOM lines for a menu product. Each line has raw material, quantity, unit, wastage percentage, and cost rate.
- `FinalizeBill` calls the sales service, which creates raw material stock postings from the active BOM. Partial bills deduct the quantity being billed.
- Inventory already has stock adjustment, physical count, wastage, low stock, recipe coverage, and a consumption ledger.
- `SendToKitchen` creates KOT/BOT tickets and identifies the order items to consume. KDS tracks sent, preparing, ready, and served states.

Key gaps to fix: a kitchen item with no active recipe can be billed without raw material deduction; a missing unit conversion silently keeps the original number; product-level recipes do not reflect different ingredient quantities for variants or modifiers; and the current consumption ledger joins stock postings to sales lines, so it must change when posting moves to KOT/BOT.

## Daily staff workflow

1. **Set up raw materials:** Give each ingredient one stock unit, such as kg, litre, or piece. Record purchase-pack conversions, minimum stock, and opening balance. Reject a recipe or receipt with an unconvertible unit.
2. **Set up menu recipes:** Record the ingredient quantity for one serving, including normal preparation loss. Require a recipe for every kitchen item that should consume stock. A sealed stock item can instead deduct its own quantity directly.
3. **Receive goods:** Increase stock only when goods are physically received and accepted. Purchase orders alone do not increase available stock.
4. **Serve:** Sending a KOT/BOT posts the recipe quantities for each sent item once. Add-on tickets post only their new items. A retry or reprint does not post again.
5. **Handle exceptions:** A cancellation before preparation can restore the posted quantity. If preparation began, mark the existing consumption as wasted with a reason; do not deduct it a second time. Staff meals, samples, spoilage, and breakage without a KOT/BOT use a reasoned manual issue or wastage entry.
6. **Close the day:** Count fast-moving and expensive ingredients daily, then do a full count weekly. A count posts only the difference and requires a reason for a large variance.

Example: a serving uses 0.12 kg of chicken. Ten servings sent to the kitchen consume 1.20 kg. The later bill changes sales and payment records without another chicken issue.

## Posting rules for implementation

| Event | Stock result |
| --- | --- |
| Draft order or menu view | None |
| KOT/BOT sent for a new order item | One outward posting per recipe ingredient, or one for a direct-stock item |
| Add-on KOT/BOT | Post only newly sent order items |
| KOT reprint, repeated request, bill, or partial bill | No additional consumption |
| Void before preparation | Linked inward reversal, with reason |
| Void after preparation | Keep consumption; label it as waste or complimentary use without another stock issue |
| Customer refund after food was served | No automatic ingredient restoration |
| Verified return of an unopened stock item | Linked inward reversal |
| Physical count | Adjustment for counted minus expected quantity |

Use a server transaction for ticket creation and its stock postings. Give each consumption event a durable unique key based on tenant, order item, and posting event; give reversals their own link to the original event. Store the recipe quantities, units, rates, and cost used at the time of posting so later recipe edits do not change history. Use the existing `StockPosting` and `StockMaintain` structures as the stock source of truth rather than creating a second balance.

Keep the existing bill-time path for historical entries. New kitchen-posted items must skip the sales stock issue in `ProcessSalesStock`. The consumption ledger and material report should read both historical sales-linked postings and new order-item-linked postings without double counting. Show order number, KOT/BOT number, bill number when available, raw material, quantity, cost, and reversal or waste reason.

## Build order

### Phase 1: make current consumption trustworthy

1. Audit active menu items with the recipe coverage screen. Block kitchen items with missing recipes from being sent to the kitchen until the recipe is set up; any exceptional manual issue must be explicit and recorded.
2. Make unit conversion validation strict. Test purchase packs, recipe units, and physical count units against the ingredient's stock unit.
3. Confirm that negative-stock policy and user permissions work for every stock issue. Show missing ingredients before KOT send.
4. Pilot with a small menu and compare one week of expected stock against physical counts.

### Phase 2: move automatic deduction to KOT/BOT

1. Add the durable consumption event/reference and recipe snapshot.
2. Post on `SendToKitchen` atomically and make retries idempotent.
3. Skip the bill-time stock issue for items already consumed at KOT/BOT; retain bill-time behavior for historical and exceptional unsent items during migration.
4. Add cancellation/reversal choices tied to KDS preparation state and manager permission.
5. Update the ledger, inventory report, and mobile/Angular views to show kitchen-origin consumption.

### Phase 3: improve accuracy without adding daily work

1. Add recipe overrides for size variants and ingredient-changing modifiers. Keep price-only modifiers out of stock calculations.
2. Show theoretical use, recorded waste, expected closing stock, counted stock, and variance by ingredient. Formula: `expected closing = opening + receipts + other increases - KOT consumption - separate waste issues - other issues`. Waste classified from an existing KOT issue stays inside KOT consumption and is not subtracted again.
3. Add alerts for missing recipes, invalid units, negative stock, duplicate posting attempts, and unusually large count differences.
4. Add outlet and kitchen/bar stock locations only when the hotel operates separate stock rooms or branches; transfers then need paired out/in entries.

## Acceptance checks

- Sending the same KOT request twice, reprinting, and billing once or in parts produces exactly one ingredient issue per order item.
- An add-on consumes only the added item. A draft or cancelled unsent item consumes nothing.
- Cancellation before preparation makes a linked reversal; cancellation after preparation remains visible as waste.
- Changing a recipe after service does not change earlier consumption or cost.
- Missing recipe or unit conversion cannot silently create incorrect stock quantities.
- Daily expected stock reconciles to receipts, consumption, waste, adjustments, and counted stock.
