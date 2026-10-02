# Busy restaurant UX seed

This deterministic, additive seeder populates tenant `2` with a coherent restaurant operation for UI and report testing. It includes the current restaurant workflows: POS, kitchen, inventory, delivery channels, guest reservations, cashier closeout, printing, offline sync, and payroll. The runner creates the tenant as FutureStar only when tenant 2 is missing and the next available tenant ID is 2. It then creates tenant 2's baseline roles and restaurant setup, bootstraps missing ERP reference masters, and applies the dataset.

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
| Payroll employees | 12 |
| Payroll attendance rows | 516 |
| Payroll runs and lines | 2 / 24 |
| Reservations and SMS history | 64 / 40 |
| Printer routes, jobs, and deliveries | 7 / 100 / 140 |
| Cash shifts, bill tenders, and movements | 14 / 100 / 28 |
| Offline sync batches | 36 |

Reference masters such as dining areas, kitchen stations, print devices, payroll departments, suppliers, and channels are kept at realistic restaurant counts. The demo push tokens are placeholders and cannot deliver notifications.

## Run

After applying the current ASP.NET Core migrations, run from the repository root:

```powershell
dotnet run --project aspnet-core/tools/RestaurantBusyDemoSeed/RestaurantBusyDemoSeed.csproj
```

The runner reads `ConnectionStrings:Default` from the Web Host `appsettings.json`, targets only tenant `2`, and prints the seeded row counts. The SQL file remains available for direct SQL Server use after tenant 2's admin user has been created.

To create the six tenant employee sign-in accounts and link them to the seeded payroll staff, run:

```powershell
dotnet run --project aspnet-core/tools/FutureStarEmployeeSeed/FutureStarEmployeeSeed.csproj
```

The account seed uses the tenant's configured default employee password and requires a password change at first sign-in.

The seed uses stable IDs and `NOT EXISTS` guards, so it can be rerun without duplicating its records. It is additive and does not delete data created outside the seed.
