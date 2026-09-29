# Busy restaurant UX seed

This deterministic SQL seed populates tenant `2` (`FutureStar`) with a coherent, high-volume restaurant operation for UI and report testing.

## Dataset size

| Area | Rows |
| --- | ---: |
| Dining tables | 120 |
| Menu items | 150 |
| Raw materials | 120 |
| Supplier mappings | 120 |
| Table sessions | 150 |
| Open POS orders | 100 |
| Billed/closed orders | 100 |
| Open KOT/BOT tickets | 150 |
| Stock adjustments | 120 |
| Channel menu items | 150 |
| Aggregator orders | 120 |
| Aggregator payouts | 100 |
| Menu sync logs | 150 |

Reference masters such as dining areas, kitchen stations, devices, suppliers, and channels are intentionally kept at realistic restaurant counts.

## Run

From the repository root:

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d RestroErp_db -E -C -b -i aspnet-core/tools/RestaurantBusyDemoSeed/seed-busy-restaurant.sql
```

The script uses stable IDs and `NOT EXISTS` guards, so it can be rerun without duplicating its records. It is additive and does not delete data created outside the seed.
