import { Component, Input, signal, computed } from '@angular/core';

export type SkeletonType = 'text' | 'circle' | 'rectangle' | 'card';
@Component({
    selector: 'app-loading-skeleton',
    standalone: true,
    imports: [],
    template: `
        <div
            class="skeleton"
            [class]="skeletonClasses()"
            [style.width]="width"
            [style.height]="height"
            [attr.aria-label]="'Loading ' + type"
        >
            @if (type === 'card') {
                <div class="skeleton-card">
                    <div class="skeleton-header"></div>
                    <div class="skeleton-content">
                        @for (line of linesArray(); track line) {
                            <div class="skeleton-line"></div>
                        }
                    </div>
                </div>
            }
        </div>
    `,
    styles: [
        `
            .skeleton {
                background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                background-size: 200% 100%;
                animation: loading 1.5s infinite;
                border-radius: 4px;
            }
            @keyframes loading {
                0% {
                    background-position: 200% 0;
                }
                100% {
                    background-position: -200% 0;
                }
            }
            /* Text skeleton */
            .skeleton.text {
                height: 1rem;
                margin: 0.25rem 0;
            }
            /* Circle skeleton */
            .skeleton.circle {
                border-radius: 50%;
                aspect-ratio: 1;
            }
            /* Rectangle skeleton */
            .skeleton.rectangle {
                border-radius: 8px;
            }
            /* Card skeleton */
            .skeleton.card {
                padding: 1rem;
                border-radius: 8px;
                min-height: 120px;
            }
            .skeleton-card {
                display: flex;
                flex-direction: column;
                gap: 0.75rem;
            }
            .skeleton-header {
                height: 1.5rem;
                width: 60%;
                border-radius: 4px;
                background: inherit;
                animation: inherit;
            }
            .skeleton-content {
                display: flex;
                flex-direction: column;
                gap: 0.5rem;
            }
            .skeleton-line {
                height: 0.875rem;
                border-radius: 4px;
                background: inherit;
                animation: inherit;
            }
            .skeleton-line:nth-child(1) {
                width: 100%;
            }
            .skeleton-line:nth-child(2) {
                width: 85%;
            }
            .skeleton-line:nth-child(3) {
                width: 70%;
            }
            .skeleton-line:nth-child(4) {
                width: 90%;
            }
        `,
    ],
})
export class LoadingSkeletonComponent {
    @Input() type: SkeletonType = 'text';
    @Input() width = '';
    @Input() height = '';
    @Input() lines = 3;
    private _type = signal<SkeletonType>('text');
    private _lines = signal(3);
    constructor() {
        // Update signals when inputs change
        this._type.set(this.type);
        this._lines.set(this.lines);
    }
    ngOnChanges() {
        this._type.set(this.type);
        this._lines.set(this.lines);
    }
    skeletonClasses = computed(() => `skeleton ${this._type()}`);
    linesArray = computed(() => Array.from({ length: this._lines() }, (_, i) => i));
}
