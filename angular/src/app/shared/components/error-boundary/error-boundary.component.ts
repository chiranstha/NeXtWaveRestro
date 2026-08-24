import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';

import { environment } from '../../../../environments/environment';
export interface ErrorInfo {
    message: string;
    stack?: string;
    timestamp: Date;
    componentName?: string;
    userAgent?: string;
}
@Component({
    selector: 'app-error-boundary',
    standalone: true,
    imports: [],
    template: `
        @if (hasError()) {
            <div class="alert alert-danger p-3 my-3" role="alert" aria-live="assertive">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <h3 class="h5 fw-semibold m-0">Something went wrong</h3>
                    <button
                        type="button"
                        class="btn btn-sm btn-outline-secondary"
                        (click)="retry()"
                        aria-label="Try again"
                    >
                        <i class="fa fa-refresh" aria-hidden="true"></i>
                        Retry
                    </button>
                </div>
                @if (showDetails()) {
                    <div class="mt-2">
                        <details>
                            <summary>Error Details (Click to expand)</summary>
                            <pre class="bg-light p-2 rounded-1 small overflow-x-auto text-wrap">{{ error()?.stack }}</pre>
                        </details>
                    </div>
                }
                <div class="mt-2 d-flex gap-2">
                    <button
                        type="button"
                        class="btn btn-sm btn-link"
                        (click)="toggleDetails()"
                        aria-expanded="showDetails()"
                    >
                        {{ showDetails() ? 'Hide' : 'Show' }} Details
                    </button>
                    @if (errorReported() === false) {
                        <button type="button" class="btn btn-sm btn-link" (click)="reportError()">Report Issue</button>
                    }
                </div>
            </div>
        } @else {
            <ng-content></ng-content>
        }
    `,
})
export class ErrorBoundaryComponent {
    private _error = signal<ErrorInfo | null>(null);
    private _showDetails = signal(false);
    private _errorReported = signal<boolean | null>(null);
    @Input() componentName?: string;
    @Output() errorOccurred = new EventEmitter<ErrorInfo>();
    @Output() retryRequested = new EventEmitter<void>();
    error = this._error.asReadonly();
    showDetails = this._showDetails.asReadonly();
    errorReported = this._errorReported.asReadonly();
    hasError = computed(() => this._error() !== null);
    handleError(error: Error): void {
        const errorInfo: ErrorInfo = {
            message: error.message,
            stack: error.stack,
            timestamp: new Date(),
            componentName: this.componentName,
            userAgent: navigator.userAgent,
        };
        this._error.set(errorInfo);
        this._errorReported.set(null);
        this.errorOccurred.emit(errorInfo);
        // Log to console in development
        if (!environment.production) {
            console.error('Error Boundary caught an error:', errorInfo);
        }
    }
    retry(): void {
        this._error.set(null);
        this._showDetails.set(false);
        this._errorReported.set(null);
        this.retryRequested.emit();
    }
    toggleDetails(): void {
        this._showDetails.update((show) => !show);
    }
    reportError(): void {
        // Here you could integrate with error reporting services like Sentry, LogRocket, etc.
        this._errorReported.set(true);
        if (this._error()) {
            // Example: Send to error reporting service
            console.log('Reporting error:', this._error());
            // In a real app, you would send this to your error reporting service
            // this.errorReportingService.report(this._error());
        }
    }
}
