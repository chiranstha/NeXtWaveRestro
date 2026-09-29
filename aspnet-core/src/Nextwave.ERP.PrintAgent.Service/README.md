# Nextwave ERP Print Agent

The service accepts authenticated IPP 2.0 ESC/POS jobs on `https://localhost:631` and routes them to a Windows RAW queue or TCP 9100 printer.

## Installed setup

Build the self-contained Windows installer from an elevated development prompt:

```powershell
./installer/Build-PrintAgent.ps1
```

Install the generated MSI on every cashier terminal. The installer installs the
`NextwaveERPPrintAgent` service, trusts the local HTTPS certificate, starts the
tray application with Windows, and creates the default `nextwavepos` route from
the default Windows printer when one is available.

After installation:

1. Choose **Manage printer routes** and select a Windows queue or TCP 9100
   printer.
2. Choose **Show pairing code**, then use **Pair printer** on the Sales POS page.
3. Use **Test print** on the POS page before processing live sales.

The pairing code authorizes the current ERP origin for that browser and terminal.
This works when the ERP is deployed to a production domain; no server-origin
entry is required on every cashier computer. The **Configure ERP origins** menu
remains available for existing installations, but it is not required for pairing.

## Developer setup

1. Run `installer/Nextwave.ERP.PrintAgent.Setup/Install-Certificate.ps1` from an elevated PowerShell session.
2. Configure the ERP origins in `appsettings.json`, or use the tray's
   **Configure ERP origins** command while the service is running.
3. Start the service project and the tray project.
4. Add a route through `PUT /api/v1/routes`, using either `WindowsQueue` with `printerName` or `Tcp9100` with `host` and `port`.
5. Select **Show pairing code** from the tray and pair the Sales POS browser.

The Development configuration uses `https://localhost:61620`, a separate data
directory, and a separate management pipe so it can run alongside the installed
service on `https://localhost:631`, whether started from Visual Studio or with
`dotnet run`. Use the installed service URL for normal POS printing; use the
development URL only when testing the service project itself.

For local development without the installed HTTPS certificate, run the service with
`PrintAgent__Urls=http://localhost:631` and set the ERP tenant's Print Bridge URL to
`http://localhost:631`. The installed service should keep the default HTTPS URL.

Production builds publish the service and tray as self-contained `win-x64` applications before building the MSI.
