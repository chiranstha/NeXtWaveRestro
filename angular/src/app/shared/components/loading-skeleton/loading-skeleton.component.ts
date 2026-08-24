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
                    <div class="skeleton-header rounded-1"></div>
                    <div class="skeleton-content d-flex flex-column gap-2">
                        @for (line of linesArray(); track line) {
                            <div class="skeleton-line rounded-1" [style.width.%]="lineWidth(line)"></div>
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
            }
            @keyframes loading {
                0% {
                    background-position: 200% 0;
                }
                100% {
                    background-position: -200% 0;
                }
            }
            .skeleton.text {
                height: 1rem;
            }

            .skeleton.circle {
                aspect-ratio: 1;
            }

            .skeleton.card {
                min-height: 120px;
            }

            .skeleton-card {
                display: contents;
            }

            .skeleton-header {
                height: 1.5rem;
                width: 60%;
                background: inherit;
                animation: inherit;
            }

            .skeleton-line {
                height: 0.875rem;
                background: inherit;
                animation: inherit;
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
    skeletonClasses = computed(() => {
        const type = this._type();
        const bootstrapClasses = {
            text: 'rounded-1 my-1',
            circle: 'rounded-circle',
            rectangle: 'rounded-3',
            card: 'rounded-3 p-3 d-flex flex-column gap-3',
        };
        return `skeleton ${type} ${bootstrapClasses[type]}`;
    });
    lineWidth(index: number): number {
        return [100, 85, 70, 90][index % 4];
    }
    linesArray = computed(() => Array.from({ length: this._lines() }, (_, i) => i));
}
