import { Component, OnInit, OnDestroy, inject, ChangeDetectionStrategy } from '@angular/core';

import { trigger, transition, style, animate } from '@angular/animations';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ServiceWorkerService } from '../services/service-worker.service';

/**
 * Component to notify users of Service Worker updates
 * Shows a dismissible notification prompting user to reload for latest version
 */
@Component({
    selector: 'app-update-notification',
    standalone: true,
    imports: [],
    template: `
        @if (showNotification) {
            <div class="update-notification position-fixed text-white rounded-3 shadow-lg" [@slideDown]>
                <div class="notification-content p-3 d-flex flex-column flex-sm-row justify-content-between align-items-center gap-3">
                    <div class="notification-message flex-grow-1">
                        <strong class="d-block mb-1">New version available!</strong>
                        <p class="m-0 small">
                            A new version of the application is ready. Reload to get the latest features and
                            improvements.
                        </p>
                    </div>
                    <div class="notification-actions d-flex gap-2 w-100 w-sm-auto">
                        <button type="button" class="btn btn-reload border-0 rounded-1 px-3 py-2 small fw-medium flex-grow-1 flex-sm-grow-0" (click)="onReload()">Reload Now</button>
                        <button type="button" class="btn btn-dismiss border-0 rounded-1 px-3 py-2 small fw-medium flex-grow-1 flex-sm-grow-0" (click)="onDismiss()">Later</button>
                    </div>
                </div>
            </div>
        }
    `,
    styles: [
        `
            .update-notification {
                bottom: 20px;
                right: 20px;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                max-width: 400px;
                z-index: 10000;

                @media (max-width: 640px) {
                    bottom: 10px;
                    right: 10px;
                    left: 10px;
                    max-width: none;
                }
            }

            .notification-message p {
                opacity: 0.95;
                line-height: 1.4;
            }
            .btn-reload {
                background: rgba(255, 255, 255, 0.25);
                color: white;
                backdrop-filter: blur(10px);
                transition: all 0.3s ease;

                &:hover {
                    background: rgba(255, 255, 255, 0.35);
                    transform: translateY(-2px);
                }

                &:active {
                    transform: translateY(0);
                }
            }

            .btn-dismiss {
                background: rgba(255, 255, 255, 0.1);
                color: white;
                opacity: 0.8;
                transition: all 0.3s ease;

                &:hover {
                    opacity: 1;
                    background: rgba(255, 255, 255, 0.15);
                }
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    animations: [
        trigger('slideDown', [
            transition(':enter', [
                style({ transform: 'translateY(100%)', opacity: 0 }),
                animate('300ms ease-out', style({ transform: 'translateY(0)', opacity: 1 })),
            ]),
            transition(':leave', [animate('300ms ease-in', style({ transform: 'translateY(100%)', opacity: 0 }))]),
        ]),
    ],
})
export class UpdateNotificationComponent implements OnInit, OnDestroy {
    showNotification = false;
    private destroy$ = new Subject<void>();
    private swService = inject(ServiceWorkerService);

    ngOnInit() {
        this.swService.updateAvailable$.pipe(takeUntil(this.destroy$)).subscribe(() => {
            this.showNotification = true;
            console.log('Update notification shown');
        });
    }

    onReload() {
        this.swService.activateUpdate();
    }

    onDismiss() {
        this.showNotification = false;
    }

    ngOnDestroy() {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
