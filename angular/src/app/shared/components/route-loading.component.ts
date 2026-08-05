import {
    Component,
    OnInit,
    OnDestroy,
    ChangeDetectorRef,
    NgZone,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { Router, NavigationStart, NavigationEnd, NavigationError } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/**
 * Component to display loading indicator while lazy-loaded modules are being loaded
 * This component helps users understand that content is loading
 */
@Component({
    selector: 'app-route-loading',
    standalone: true,
    imports: [],
    template: `
        @if (isLoading) {
            <div class="route-loading-overlay">
                <div class="loading-container">
                    <div class="spinner"></div>
                    <p class="loading-text">Loading module...</p>
                </div>
            </div>
        }
    `,
    changeDetection: ChangeDetectionStrategy.Eager,
    styles: [
        `
            .route-loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(255, 255, 255, 0.95);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 9999;
                opacity: 1;
                transition: opacity 0.3s ease-out;
            }

            .loading-container {
                text-align: center;
                transform: translateY(-50px);
            }

            .spinner {
                width: 50px;
                height: 50px;
                border: 4px solid #e0e0e0;
                border-top: 4px solid #2196f3;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin: 0 auto 20px;
            }

            .loading-text {
                margin: 0;
                font-size: 16px;
                color: #666;
                font-family:
                    -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            }

            @keyframes spin {
                to {
                    transform: rotate(360deg);
                }
            }

            /* Smooth fade out */
            .route-loading-overlay.fade-out {
                opacity: 0;
                pointer-events: none;
            }
        `,
    ],
})
export class RouteLoadingComponent implements OnInit, OnDestroy {
    isLoading = false;
    private destroy$ = new Subject<void>();
    private router = inject(Router);
    private cdr = inject(ChangeDetectorRef);
    private ngZone = inject(NgZone);

    ngOnInit() {
        this.router.events.pipe(takeUntil(this.destroy$)).subscribe((event) => {
            if (event instanceof NavigationStart) {
                // Show loader on navigation start
                this.ngZone.run(() => {
                    this.isLoading = true;
                    this.cdr.markForCheck();
                });
            }
            if (event instanceof NavigationEnd || event instanceof NavigationError) {
                // Hide loader when navigation completes or errors
                this.ngZone.runOutsideAngular(() => {
                    setTimeout(() => {
                        this.ngZone.run(() => {
                            this.isLoading = false;
                            this.cdr.markForCheck();
                        });
                    }, 100);
                });
            }
        });
    }

    ngOnDestroy() {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
