import { ErrorHandler, Injectable, Injector, inject } from '@angular/core';
import { ValidationService } from './validation.service';
@Injectable({
    providedIn: 'root',
})
export class GlobalErrorHandler implements ErrorHandler {
    private injector = inject(Injector);
    private readonly chunkReloadStorageKey = 'suktas.lazyChunkReloadedAt';
    private readonly chunkReloadCooldownMs = 10000;
    constructor() {}
    handleError(error: any): void {
        const validationService = this.injector.get(ValidationService);

        if (this.handleLazyChunkLoadError(error)) {
            return;
        }

        // Log the error
        console.error('Global Error Handler:', error);
        // Handle different types of errors
        if (error instanceof Error) {
            this.logClientError(error);
        } else if (error.rejection) {
            // Handle promise rejections
            this.logPromiseError(error);
        } else {
            // Handle other types of errors
            this.logUnknownError(error);
        }
        // Don't rethrow the error, as this would cause an infinite loop
        // Instead, show a user-friendly message
        this.showUserFriendlyError(error, validationService);
    }
    private logClientError(error: Error): void {
        const errorMessage = {
            message: error.message,
            stack: error.stack,
            timestamp: new Date().toISOString(),
            userAgent: navigator.userAgent,
            url: window.location.href,
        };
        // In a real application, you would send this to a logging service
        console.error('Client Error:', errorMessage);
    }
    private logPromiseError(error: any): void {
        const errorMessage = {
            message: error.rejection?.message || 'Promise rejection',
            stack: error.rejection?.stack,
            timestamp: new Date().toISOString(),
            userAgent: navigator.userAgent,
            url: window.location.href,
        };
        console.error('Promise Error:', errorMessage);
    }
    private logUnknownError(error: any): void {
        const errorMessage = {
            error,
            timestamp: new Date().toISOString(),
            userAgent: navigator.userAgent,
            url: window.location.href,
        };
        console.error('Unknown Error:', errorMessage);
    }
    private showUserFriendlyError(error: any, validationService: ValidationService): void {
        // Try to get a user-friendly message
        let userMessage = 'An unexpected error occurred. Please try again.';
        if (error.message) {
            userMessage = validationService.handleServerError(error);
        }
        // In a real application, you might want to show this in a toast or modal
        console.warn('User-friendly error message:', userMessage);
        // For now, we'll just log it. In a real app, you'd show a toast notification
        // this.notificationService.error(userMessage);
    }

    private handleLazyChunkLoadError(error: any): boolean {
        const errorMessage = this.getErrorMessage(error);

        if (!this.isLazyChunkLoadError(errorMessage)) {
            return false;
        }

        const lastReloadAt = Number(sessionStorage.getItem(this.chunkReloadStorageKey) || 0);

        if (Date.now() - lastReloadAt < this.chunkReloadCooldownMs) {
            console.error('Lazy chunk load failed after reload:', error);
            return true;
        }

        sessionStorage.setItem(this.chunkReloadStorageKey, Date.now().toString());
        console.warn('A stale application chunk was requested. Reloading once to fetch the current build.', {
            message: errorMessage,
            url: window.location.href,
        });
        window.location.reload();
        return true;
    }

    private getErrorMessage(error: any): string {
        const candidate =
            error?.message ||
            error?.rejection?.message ||
            error?.reason?.message ||
            error?.error?.message ||
            error?.toString?.();

        return candidate ? String(candidate) : '';
    }

    private isLazyChunkLoadError(message: string): boolean {
        return (
            message.includes('Failed to fetch dynamically imported module') ||
            message.includes('Importing a module script failed') ||
            message.includes('Loading chunk') ||
            message.includes('ChunkLoadError')
        );
    }
}
// Error boundary component for catching component errors
@Injectable({
    providedIn: 'root',
})
export class ErrorBoundaryService {
    private validationService = inject(ValidationService);
    constructor() {}
    handleComponentError(error: any, componentName: string): string {
        console.error(`Error in component ${componentName}:`, error);
        const errorMessage = {
            component: componentName,
            error: error.message || 'Unknown component error',
            stack: error.stack,
            timestamp: new Date().toISOString(),
        };
        // Log to external service in production
        console.error('Component Error:', errorMessage);
        // Return user-friendly message
        return this.validationService.handleServerError(error);
    }
    handleFormError(error: any, formName: string): string {
        console.error(`Form validation error in ${formName}:`, error);
        if (error && typeof error === 'object') {
            // Handle validation errors
            return this.validationService.getErrorMessage(error, formName);
        }
        return this.validationService.handleServerError(error);
    }
}
