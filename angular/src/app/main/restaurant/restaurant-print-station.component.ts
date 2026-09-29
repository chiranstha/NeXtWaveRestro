import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, ICellRendererParams } from 'ag-grid-community';
import { Subscription, firstValueFrom, interval } from 'rxjs';
import { RestaurantGuestApiService } from './restaurant-guest-api.service';

@Component({
    selector: 'restaurant-print-station',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular],
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
    routes: any[] = [];
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
                container.append(document.createTextNode(params.data.type === 0 ? 'Kitchen ticket' : 'Receipt'));
                const id = document.createElement('small');
                id.className = 'd-block text-muted';
                id.textContent = params.data.externalJobId || '';
                container.append(id);
                return container;
            },
        },
        { headerName: 'Route', field: 'routeName', minWidth: 130 },
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
                if (params.data.status !== 3) return '';
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
            this.error = '';
        } catch (error: any) { this.error = error?.message || 'Could not read local printer routes.'; }
    }

    start(): void {
        if (!this.token) { this.error = 'Pair this browser with the local Print Agent before starting the station.'; return; }
        this.running = true;
        this.message = 'This browser is claiming print jobs while this page stays open.';
        this.workerSubscription = new Subscription();
        this.workerSubscription.add(interval(2500).subscribe(() => void this.claimOne()));
        void this.claimOne();
    }

    stop(): void { this.running = false; this.workerSubscription.unsubscribe(); this.message = 'Print station stopped.'; }

    loadJobs(): void {
        this.api.printJobs().subscribe({ next: (jobs) => this.jobs = jobs || [], error: (error) => this.error = this.errorText(error) });
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
                agentId: this.agentId,
                agentJobId,
                status: printed ? 2 : 3,
                error: printed ? undefined : (final.lastError || `Print Agent job ended in ${final.state}.`),
            }));
            this.message = printed ? `${job.type === 0 ? 'Kitchen ticket' : 'Receipt'} printed on route ${job.routeName}.` : `Print failed: ${final.lastError || final.state}`;
        } catch (error: any) {
            try {
                await firstValueFrom(this.api.reportPrintJob({ id: job.id, agentId: this.agentId, agentJobId, status: 3, error: error?.message || 'Print Agent connection failed.' }));
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
    private savePairing(): void { localStorage.setItem(this.storageKey, JSON.stringify({ url: this.agentUrl, token: this.token, agentId: this.agentId })); }
    private errorText(error: any): string { return error?.error?.error?.message || error?.error?.message || error?.message || 'Could not load print queue.'; }

    ngOnDestroy(): void { this.stop(); }
}
