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
            <div class="error-boundary" role="alert" aria-live="assertive">
                <div class="error-header">
                    <h3 class="error-title">Something went wrong</h3>
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
                    <div class="error-details">
                        <details>
                            <summary>Error Details (Click to expand)</summary>
                            <pre class="error-stack">{{ error()?.stack }}</pre>
                        </details>
                    </div>
                }
                <div class="error-actions">
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
    styles: [
        `
            .error-boundary {
                padding: 1rem;
                border: 1px solid #dc3545;
                border-radius: 0.375rem;
                background-color: #f8d7da;
                color: #721c24;
                margin: 1rem 0;
            }
            .error-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 0.5rem;
            }
            .error-title {
                margin: 0;
                font-size: 1.1rem;
                font-weight: 600;
            }
            .error-details {
                margin-top: 0.5rem;
            }
            .error-stack {
                background: #f1f3f4;
                padding: 0.5rem;
                border-radius: 0.25rem;
                font-size: 0.875rem;
                overflow-x: auto;
                white-space: pre-wrap;
                word-break: break-word;
            }
            .error-actions {
                margin-top: 0.5rem;
                display: flex;
                gap: 0.5rem;
            }
            .btn {
                padding: 0.25rem 0.5rem;
                border: 1px solid transparent;
                border-radius: 0.25rem;
                cursor: pointer;
                font-size: 0.875rem;
                text-decoration: none;
                display: inline-flex;
                align-items: center;
                gap: 0.25rem;
            }
            .btn:hover {
                text-decoration: none;
            }
            .btn-outline-secondary {
                color: #6c757d;
                border-color: #6c757d;
            }
            .btn-outline-secondary:hover {
                background-color: #6c757d;
                color: white;
            }
            .btn-link {
                color: #007bff;
                background: none;
                border: none;
                padding: 0;
            }
            .btn-link:hover {
                color: #0056b3;
                text-decoration: underline;
            }
            .btn-sm {
                padding: 0.2rem 0.4rem;
                font-size: 0.8rem;
            }
        `,
    ],
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
