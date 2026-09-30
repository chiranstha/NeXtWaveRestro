import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, ICellRendererParams } from 'ag-grid-community';
import { Subscription, firstValueFrom, interval } from 'rxjs';
import { RestaurantGuestApiService } from './restaurant-guest-api.service';
import { RestaurantNepaliDatePipe } from './restaurant-nepali-date.pipe';

interface LocalPrinterRoute {
    name: string;
    kind: 'WindowsQueue' | 'Tcp9100';
    printerName: string;
    host: string;
    port: number;
}

@Component({
    selector: 'restaurant-print-station',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular, RestaurantNepaliDatePipe],
    templateUrl: './restaurant-print-station.component.html',
    styleUrl: './restaurant-staff.component.scss',
})
export class RestaurantPrintStationComponent implements OnInit, OnDestroy {
    private readonly api = inject(RestaurantGuestApiService);
    private workerSubscription = new Subscription();
    readonly storageKey = 'nextwave.restaurant.print-agent';
    agentUrl = 'https://localhost:631';
    pairingCode = '';
    token = '';
    agentId = '';
    routes: LocalPrinterRoute[] = [];
    erpRoutes: any[] = [];
    printerNames: string[] = [];
    devices: any[] = [];
    jobs: any[] = [];
    running = false;
    busy = false;
    message = '';
    error = '';

    readonly columnDefs: ColDef[] = [
        {
            headerName: 'Type',
            minWidth: 210,
            flex: 1,
            cellRenderer: (params: ICellRendererParams) => {
                const container = document.createElement('div');
                const title = params.data.type === 0
                    ? (params.data.routeName?.toLowerCase() === 'bar' ? 'Bar ticket' : 'Kitchen ticket')
                    : 'Receipt';
                container.append(document.createTextNode(title));
                const id = document.createElement('small');
                id.className = 'd-block text-muted';
                id.textContent = params.data.externalJobId || '';
                container.append(id);
                return container;
            },
        },
        { headerName: 'Route', field: 'routeName', minWidth: 130 },
        {
            headerName: 'Device copies', minWidth: 230, flex: 1,
            cellRenderer: (params: ICellRendererParams) => {
                const container = document.createElement('div');
                for (const delivery of params.data.deliveries || []) {
                    const row = document.createElement('div');
                    row.append(document.createTextNode(`${delivery.deviceName} · ${this.statusLabel(delivery.status)}`));
                    if (delivery.status === 3) {
                        const button = document.createElement('button');
                        button.type = 'button';
                        button.className = 'btn btn-xs btn-light-primary ms-2';
                        button.textContent = 'Retry';
                        button.addEventListener('click', () => this.retryDelivery(params.data, delivery));
                        row.append(button);
                    }
                    container.append(row);
                }
                if (!(params.data.deliveries || []).length) container.textContent = 'No device copy queued';
                return container;
            },
        },
        {
            headerName: 'Status', field: 'status', width: 140,
            valueFormatter: (params) => this.statusLabel(params.value),
            cellClass: (params) => params.value === 3 ? 'status-bad' : undefined,
        },
        { headerName: 'Attempts', field: 'attempts', width: 110, valueFormatter: (params) => params.value || '—' },
        {
            headerName: 'Last error', field: 'lastError', minWidth: 220, flex: 1,
            cellRenderer: (params: ICellRendererParams) => {
                const container = document.createElement('div');
                container.append(document.createTextNode(params.value || '—'));
                if (params.data.reprintReason) {
                    const reason = document.createElement('small');
                    reason.className = 'd-block text-muted';
                    reason.textContent = `Reprint: ${params.data.reprintReason}`;
                    container.append(reason);
                }
                return container;
            },
        },
        {
            headerName: '', minWidth: 95, maxWidth: 110, sortable: false, filter: false,
            cellRenderer: (params: ICellRendererParams) => {
                if (params.data.status !== 3 || params.data.deliveries?.length) return '';
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'btn btn-xs btn-light-primary';
                button.textContent = 'Retry';
                button.addEventListener('click', () => this.retry(params.data));
                return button;
            },
        },
    ];

    readonly defaultColDef: ColDef = { resizable: true, sortable: true, minWidth: 85 };

    ngOnInit(): void {
        try {
            const saved = JSON.parse(localStorage.getItem(this.storageKey) || '{}');
            this.agentUrl = saved.url || this.agentUrl;
            this.token = saved.token || '';
            this.agentId = saved.agentId || crypto.randomUUID();
        } catch { this.agentId = crypto.randomUUID(); }
        this.savePairing();
        this.loadJobs();
        this.loadDevices();
        this.api.printRoutes().subscribe({ next: (routes) => this.erpRoutes = routes || [] });
        if (this.token) this.refreshRoutes();
    }

    openAgentHealth(): void { window.open(`${this.normalizedAgentUrl()}/api/v1/health`, '_blank', 'noopener'); }

    async pair(): Promise<void> {
        this.error = '';
        this.message = '';
        try {
            const response = await fetch(`${this.normalizedAgentUrl()}/api/v1/pair`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code: this.pairingCode.trim(), origin: location.origin }),
            });
            if (!response.ok) throw new Error(response.status === 401 ? 'Pairing code is invalid or expired.' : `Print Agent returned ${response.status}.`);
            const paired = await response.json();
            this.token = paired.token;
            this.pairingCode = '';
            this.savePairing();
            this.message = 'This browser is paired with the local Print Agent.';
            await this.refreshRoutes();
        } catch (error: any) {
            this.error = `${error?.message || 'Could not connect to the Print Agent.'} If this is the first connection, open the Print Agent health page and trust its local certificate.`;
        }
    }

    saveSettings(): void {
        this.savePairing();
        this.message = 'Print station settings saved in this browser.';
    }

    async refreshRoutes(): Promise<void> {
        if (!this.token) return;
        try {
            const response = await this.agentFetch('/api/v1/routes');
            if (!response.ok) throw new Error(`Print Agent returned ${response.status}.`);
            this.routes = await response.json();
            const printers = await this.agentFetch('/api/v1/printers');
            if (printers.ok) this.printerNames = await printers.json();
            await this.registerWindowsDevice();
            this.error = '';
        } catch (error: any) { this.error = error?.message || 'Could not read local printer routes.'; }
    }

    async start(): Promise<void> {
        if (!this.token) { this.error = 'Pair this browser with the local Print Agent before starting the station.'; return; }
        this.error = '';
        try {
            if (!this.routes.length) await this.refreshRoutes();
            if (!this.routes.length) throw new Error('Configure at least one local route before starting the station.');
            await this.registerWindowsDevice();
            const device = this.windowsDevice();
            if (!device?.id) throw new Error('The Windows Print Agent could not be registered with ERP.');
            await firstValueFrom(this.api.setPrintDeviceEnabled({ id: device.id, isEnabled: true }));
            this.running = true;
            this.message = 'This browser is claiming print jobs while this page stays open.';
            this.workerSubscription = new Subscription();
            this.workerSubscription.add(interval(2500).subscribe(() => void this.claimOne()));
            void this.claimOne();
            this.loadDevices();
        } catch (error: any) { this.error = this.errorText(error); }
    }

    async stop(): Promise<void> {
        this.running = false;
        this.workerSubscription.unsubscribe();
        const device = this.windowsDevice();
        if (device?.id && device.isEnabled) {
            try {
                await firstValueFrom(this.api.setPrintDeviceEnabled({ id: device.id, isEnabled: false }));
                this.loadDevices();
                this.loadJobs();
            } catch (error: any) { this.error = this.errorText(error); }
        }
        this.message = 'Print station stopped.';
    }

    loadJobs(): void {
        this.api.printJobs().subscribe({ next: (jobs) => this.jobs = jobs || [], error: (error) => this.error = this.errorText(error) });
    }

    loadDevices(): void {
        this.api.printDevices().subscribe({ next: (devices) => this.devices = devices || [], error: (error) => this.error = this.errorText(error) });
    }

    addRoute(): void {
        const existing = new Set(this.routes.map((route) => route.name.toLowerCase()));
        const name = ['kitchen', 'bar', 'receipt'].find((routeName) => !existing.has(routeName)) || `route${this.routes.length + 1}`;
        this.routes = [...this.routes, { name, kind: 'WindowsQueue', printerName: this.printerNames[0] || '', host: '', port: 9100 }];
    }

    removeRoute(index: number): void { this.routes = this.routes.filter((_, routeIndex) => routeIndex !== index); }

    async saveRoutes(): Promise<void> {
        this.error = '';
        this.message = '';
        const names = this.routes.map((route) => route.name.trim());
        if (!names.length || names.some((name) => !name) || new Set(names.map((name) => name.toLowerCase())).size !== names.length) {
            this.error = 'Add at least one route and use a unique name for each route.';
            return;
        }
        try {
            const response = await this.agentFetch('/api/v1/routes', {
                method: 'PUT', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(this.routes.map((route) => ({ ...route, name: route.name.trim() }))),
            });
            if (!response.ok) throw new Error(`Could not save local routes (Print Agent ${response.status}).`);
            this.routes = this.routes.map((route) => ({ ...route, name: route.name.trim() }));
            const merged = new Map<string, any>();
            for (const route of this.erpRoutes) merged.set(route.name.toLowerCase(), route);
            for (const route of this.routes) merged.set(route.name.toLowerCase(), { name: route.name, displayName: route.name, isActive: true });
            await firstValueFrom(this.api.savePrintRoutes({ routes: Array.from(merged.values()) }));
            if (this.routes.length) await this.registerWindowsDevice();
            this.message = 'Printer routes saved and assigned to this Windows station.';
            await this.refreshRoutes();
        } catch (error: any) { this.error = error?.error?.error?.message || error?.message || 'Could not save printer routes.'; }
    }

    async testRoute(route: LocalPrinterRoute): Promise<void> {
        try {
            const response = await this.agentFetch('/api/v1/test-print', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ route: route.name, paperWidth: 80 }),
            });
            if (!response.ok) throw new Error(`Test print failed (Print Agent ${response.status}).`);
            this.message = `Test receipt queued for ${route.name}.`;
            this.error = '';
        } catch (error: any) { this.error = error?.message || 'Could not send a test print.'; }
    }

    toggleDevice(device: any): void {
        this.api.setPrintDeviceEnabled({ id: device.id, isEnabled: !device.isEnabled }).subscribe({
            next: () => { this.message = `${device.name} ${device.isEnabled ? 'disabled' : 'enabled'}.`; this.loadDevices(); this.loadJobs(); },
            error: (error) => this.error = this.errorText(error),
        });
    }

    retryDelivery(job: any, delivery: any): void {
        this.api.retryPrintJob({ jobId: job.id, deliveryId: delivery.id }).subscribe({
            next: () => { this.message = `Delivery returned to ${delivery.deviceName}'s queue.`; this.loadJobs(); },
            error: (error) => this.error = this.errorText(error),
        });
    }

    retry(job: any): void {
        this.api.retryPrintJob({ jobId: job.id }).subscribe({ next: () => { this.message = 'Print job returned to the queue.'; this.loadJobs(); }, error: (e) => this.error = this.errorText(e) });
    }

    statusLabel(status: number): string { return ['Waiting', 'Being handled', 'Printed', 'Failed', 'Cancelled'][status] || 'Waiting'; }

    private async claimOne(): Promise<void> {
        if (!this.running || this.busy) return;
        this.busy = true;
        try {
            const job = await firstValueFrom(this.api.claimPrintJob({ agentId: this.agentId }));
            if (job) await this.sendToAgent(job);
        } catch (error: any) { this.error = this.errorText(error); }
        finally { this.busy = false; this.loadJobs(); }
    }

    private async sendToAgent(job: any): Promise<void> {
        let agentJobId = '';
        try {
            const response = await this.agentFetch('/api/v1/jobs', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ externalJobId: job.externalJobId, route: job.routeName, payloadBase64: job.payloadBase64 }),
            });
            if (!response.ok) throw new Error((await response.text()) || `Print Agent returned ${response.status}.`);
            const accepted = await response.json();
            agentJobId = accepted.id;
            const final = await this.waitForAgentJob(agentJobId);
            const printed = final.state === 'Printed';
            await firstValueFrom(this.api.reportPrintJob({
                id: job.id,
                deliveryId: job.deliveryId,
                leaseToken: job.leaseToken,
                agentId: this.agentId,
                agentJobId,
                status: printed ? 2 : 3,
                error: printed ? undefined : (final.lastError || `Print Agent job ended in ${final.state}.`),
            }));
            const jobLabel = job.type === 0 ? (job.routeName?.toLowerCase() === 'bar' ? 'Bar ticket' : 'Kitchen ticket') : 'Receipt';
            this.message = printed ? `${jobLabel} printed on route ${job.routeName}.` : `Print failed: ${final.lastError || final.state}`;
        } catch (error: any) {
            try {
                await firstValueFrom(this.api.reportPrintJob({ id: job.id, deliveryId: job.deliveryId, leaseToken: job.leaseToken, agentId: this.agentId, agentJobId, status: 3, error: error?.message || 'Print Agent connection failed.' }));
            } catch { /* The job lease will expire and the stable external ID makes the next attempt safe. */ }
            this.error = error?.message || 'The print job could not be sent.';
        }
    }

    private async waitForAgentJob(id: string): Promise<any> {
        const deadline = Date.now() + 150000;
        while (Date.now() < deadline) {
            await new Promise((resolve) => setTimeout(resolve, 1200));
            const response = await this.agentFetch(`/api/v1/jobs/${encodeURIComponent(id)}`);
            if (!response.ok) throw new Error(`Could not read Print Agent job ${id}.`);
            const job = await response.json();
            if (job.state === 'Printed' || job.state === 'Failed' || job.state === 'Expired' || job.state === 'Cancelled') return job;
        }
        throw new Error('The Print Agent is still processing. The job will be checked again after its lease expires.');
    }

    private agentFetch(path: string, init: RequestInit = {}): Promise<Response> {
        return fetch(`${this.normalizedAgentUrl()}${path}`, {
            ...init,
            headers: { ...(init.headers || {}), Authorization: `Bearer ${this.token}` },
        });
    }

    private normalizedAgentUrl(): string { return this.agentUrl.trim().replace(/\/$/, ''); }
    windowsDevice(): any { return this.devices.find((device) => device.clientDeviceId === this.agentId); }
    private async registerWindowsDevice(): Promise<void> {
        if (!this.routes.length) return;
        this.erpRoutes = await firstValueFrom(this.api.printRoutes());
        await firstValueFrom(this.api.savePrintRoutes({
            routes: [...this.erpRoutes, ...this.routes.map((route) => ({ name: route.name, displayName: route.name, isActive: true }))]
                .filter((route, index, all) => all.findIndex((candidate) => candidate.name?.toLowerCase() === route.name?.toLowerCase()) === index),
        }));
        await firstValueFrom(this.api.registerPrintDevice({
            clientDeviceId: this.agentId,
            name: 'Windows Print Station',
            platform: 'Windows',
            routeNames: this.routes.map((route) => route.name),
        }));
        this.devices = await firstValueFrom(this.api.printDevices());
    }
    private savePairing(): void { localStorage.setItem(this.storageKey, JSON.stringify({ url: this.agentUrl, token: this.token, agentId: this.agentId })); }
    private errorText(error: any): string { return error?.error?.error?.message || error?.error?.message || error?.message || 'Could not load print queue.'; }

    ngOnDestroy(): void { void this.stop(); }
}
