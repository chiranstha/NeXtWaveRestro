import { Injectable, inject } from '@angular/core';
import { SwUpdate, VersionEvent, VersionReadyEvent } from '@angular/service-worker';
import { Subject, filter } from 'rxjs';

// Declare browser globals for service worker
declare const caches: CacheStorage;

/**
 * Service for managing Service Worker operations and updates
 * Provides offline support, caching strategies, and update notifications
 */
@Injectable({
    providedIn: 'root',
})
export class ServiceWorkerService {
    updateAvailable$ = new Subject<void>();
    online$ = new Subject<boolean>();

    private swUpdate = inject(SwUpdate);
    private isOnline = navigator.onLine;

    constructor() {
        this.setupUpdateChecking();
        this.setupNetworkMonitoring();
    }

    /**
     * Check for Service Worker updates periodically
     */
    private setupUpdateChecking() {
        if (!this.swUpdate.isEnabled) {
            console.log('Service Worker not enabled in this environment');
            return;
        }

        // Check for updates when component loads
        this.swUpdate.checkForUpdate().catch((err) => {
            console.error('Service Worker update check failed:', err);
        });

        // Check for updates every 60 minutes
        setInterval(() => {
            this.swUpdate.checkForUpdate().catch((err) => {
                console.error('Service Worker update check failed:', err);
            });
        }, 3600000);

        // Listen for available updates
        this.swUpdate.versionUpdates
            .pipe(filter((event: VersionEvent): event is VersionReadyEvent => event.type === 'VERSION_READY'))
            .subscribe((event) => {
                console.log('Service Worker update available', event);
                this.updateAvailable$.next();
            });
    }

    /**
     * Monitor network connectivity
     */
    private setupNetworkMonitoring() {
        window.addEventListener('online', () => {
            this.isOnline = true;
            this.online$.next(true);
            console.log('Network connection restored');
        });

        window.addEventListener('offline', () => {
            this.isOnline = false;
            this.online$.next(false);
            console.log('Network connection lost - offline mode active');
        });
    }

    /**
     * Activate a pending Service Worker update
     */
    activateUpdate() {
        if (this.swUpdate.isEnabled) {
            this.swUpdate
                .activateUpdate()
                .then(() => {
                    console.log('Service Worker update activated, reloading...');
                    window.location.reload();
                })
                .catch((err) => {
                    console.error('Failed to activate update:', err);
                });
        }
    }

    /**
     * Check if user is online
     */
    getOnlineStatus(): boolean {
        return this.isOnline;
    }

    /**
     * Get online status as observable
     */
    getOnlineStatus$() {
        return this.online$;
    }

    /**
     * Clear service worker cache
     */
    clearCache() {
        if ('caches' in window) {
            caches.keys().then((cacheNames) => {
                cacheNames.forEach((cacheName) => {
                    caches.delete(cacheName).then(() => {
                        console.log(`Cleared cache: ${cacheName}`);
                    });
                });
            });
        }
    }
}
