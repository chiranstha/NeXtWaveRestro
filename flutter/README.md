# NextWave Restaurant Mobile

Flutter staff application backed by the existing NextWave ERP restaurant services.

## Included workflow

- tenant login, two-factor verification, required-password reset, encrypted session storage, and permission-aware navigation;
- live POS menu, variants/modifiers, dine-in/takeaway/delivery orders, guarded context switching, open-order editing, item voids, table transfer, order split/merge, KOT/BOT history and audited reprints;
- full and partial billing by item/quantity, stock validation, manager-PIN discounts, payment/ledger selection, tips, and server-side bill posting;
- 80 mm customer receipt PDFs after billing, sent to the Android/iOS system print dialog for compatible installed printers;
- KDS ticket and item progression/cancellation with live polling;
- menu availability and recipe-cost visibility;
- low-stock replenishment, draft purchase-order generation, supplier mappings, stock counts/adjustments/wastage, consumption history, and recipe coverage;
- channel setup/menu publishing, aggregator acceptance/status handling, payout reconciliation, editable area/table/station/operational setup, device sync health, and all 17 restaurant report endpoints;
- automatic access-token refresh, secure session persistence, Android and iOS runners, and responsive phone/tablet layouts.

Final taxes, charges, inventory consumption, ledger posting, and bill totals are calculated by the backend. Receipt printing uses printers exposed by the device's system print service (such as AirPrint or Android print services). Direct Bluetooth/LAN ESC/POS pairing and KOT station routing remain planned in `../futureplan.md`. The app is online-first; offline sync is intentionally deferred there too.

## Run

```powershell
cd D:\Source\NeXtWaveRestro\flutter
C:\Users\suman\flutter\bin\flutter.bat pub get
C:\Users\suman\flutter\bin\flutter.bat run --dart-define=APP_BASE_URL=https://localhost:44305
```

The server address can also be changed on the login screen. A physical phone cannot use the PC's `localhost`; provide a reachable HTTPS hostname/IP with a certificate trusted by that device. The Android emulator normally reaches the host machine through `10.0.2.2`, but the HTTPS certificate must still be trusted.

The mobile app intentionally keeps final tax/service-charge calculation, stock posting, sales-ledger posting, payment validation, and bill numbering in the existing backend. Offline-first sync, guest QR ordering, direct wallet integrations, and printer/device drivers remain planned in `../futureplan.md`.

## Verify and build

```powershell
C:\Users\suman\flutter\bin\flutter.bat analyze
C:\Users\suman\flutter\bin\flutter.bat test
C:\Users\suman\flutter\bin\flutter.bat build apk --debug --dart-define=APP_BASE_URL=https://your-api-host
```

For production, supply the API origin through the build pipeline and use a publicly trusted TLS certificate. Do not ship a development certificate bypass.
