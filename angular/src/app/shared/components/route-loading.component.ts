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
            <div class="route-loading-overlay position-fixed top-0 start-0 w-100 h-100 d-flex justify-content-center align-items-center">
                <div class="text-center route-loading-content">
                    <div class="spinner-border text-primary mb-4 route-loading-spinner" role="status">
                        <span class="visually-hidden">Loading module...</span>
                    </div>
                    <p class="m-0 fs-6 text-muted">Loading module...</p>
                </div>
            </div>
        }
    `,
    changeDetection: ChangeDetectionStrategy.Eager,
    styles: [
        `
            .route-loading-overlay {
                background: rgba(255, 255, 255, 0.95);
                z-index: 9999;
                opacity: 1;
                transition: opacity 0.3s ease-out;
            }

            .route-loading-content {
                transform: translateY(-50px);
            }

            .route-loading-spinner {
                width: 50px;
                height: 50px;
                border-width: 4px;
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
