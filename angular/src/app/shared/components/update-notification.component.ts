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
            <div class="update-notification" [@slideDown]>
                <div class="notification-content">
                    <div class="notification-message">
                        <strong>New version available!</strong>
                        <p>
                            A new version of the application is ready. Reload to get the latest features and
                            improvements.
                        </p>
                    </div>
                    <div class="notification-actions">
                        <button type="button" class="btn-reload" (click)="onReload()">Reload Now</button>
                        <button type="button" class="btn-dismiss" (click)="onDismiss()">Later</button>
                    </div>
                </div>
            </div>
        }
    `,
    styles: [
        `
            .update-notification {
                position: fixed;
                bottom: 20px;
                right: 20px;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
                border-radius: 8px;
                box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
                max-width: 400px;
                z-index: 10000;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;

                @media (max-width: 640px) {
                    bottom: 10px;
                    right: 10px;
                    left: 10px;
                    max-width: none;
                }
            }

            .notification-content {
                padding: 16px;
                display: flex;
                justify-content: space-between;
                align-items: center;
                gap: 16px;

                @media (max-width: 640px) {
                    flex-direction: column;
                    gap: 12px;
                }
            }

            .notification-message {
                flex: 1;

                strong {
                    display: block;
                    font-size: 16px;
                    margin-bottom: 4px;
                }

                p {
                    margin: 0;
                    font-size: 14px;
                    opacity: 0.95;
                    line-height: 1.4;
                }
            }

            .notification-actions {
                display: flex;
                gap: 8px;
                white-space: nowrap;

                @media (max-width: 640px) {
                    width: 100%;
                    gap: 8px;

                    button {
                        flex: 1;
                        white-space: normal;
                    }
                }
            }

            button {
                padding: 8px 16px;
                border: none;
                border-radius: 4px;
                font-size: 14px;
                font-weight: 500;
                cursor: pointer;
                transition: all 0.3s ease;
                font-family: inherit;
            }

            .btn-reload {
                background: rgba(255, 255, 255, 0.25);
                color: white;
                backdrop-filter: blur(10px);

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
