import { Component, Input, signal, computed } from '@angular/core';

export type SpinnerSize = 'sm' | 'md' | 'lg' | 'xl';
export type SpinnerType = 'border' | 'grow';
@Component({
    selector: 'app-loading-spinner',
    standalone: true,
    imports: [],
    template: `
        <div class="spinner-container" [class]="containerClasses()" role="status" [attr.aria-label]="ariaLabel()">
            <div class="spinner" [class]="spinnerClasses()" [attr.aria-hidden]="true"></div>
            @if (showText) {
                <span class="spinner-text">{{ text }}</span>
            }
            <span class="visually-hidden">{{ ariaLabel() }}</span>
        </div>
    `,
    styles: [
        `
            .spinner-container {
                display: inline-flex;
                align-items: center;
                gap: 0.5rem;
            }
            .spinner-container.inline {
                display: inline-flex;
            }
            .spinner-container.block {
                display: flex;
                justify-content: center;
                width: 100%;
            }
            .spinner-container.overlay {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                z-index: 10;
            }
            /* Border spinner */
            .spinner.border {
                display: inline-block;
                border: 0.25em solid currentColor;
                border-right-color: transparent;
                border-radius: 50%;
                animation: spinner-border 0.75s linear infinite;
            }
            /* Grow spinner */
            .spinner.grow {
                display: inline-block;
                background-color: currentColor;
                border-radius: 50%;
                animation: spinner-grow 0.75s linear infinite;
            }
            /* Size variants */
            .spinner.sm {
                width: 1rem;
                height: 1rem;
            }
            .spinner.md {
                width: 2rem;
                height: 2rem;
            }
            .spinner.lg {
                width: 3rem;
                height: 3rem;
            }
            .spinner.xl {
                width: 4rem;
                height: 4rem;
            }
            /* Border spinner specific sizes */
            .spinner.border.sm {
                border-width: 0.2em;
            }
            .spinner.border.lg {
                border-width: 0.3em;
            }
            .spinner.border.xl {
                border-width: 0.4em;
            }
            @keyframes spinner-border {
                to {
                    transform: rotate(360deg);
                }
            }
            @keyframes spinner-grow {
                0% {
                    transform: scale(0);
                    opacity: 1;
                }
                50% {
                    opacity: 0.8;
                }
                100% {
                    transform: scale(1);
                    opacity: 0;
                }
            }
            .spinner-text {
                font-size: 0.875rem;
                color: inherit;
            }
            .visually-hidden {
                position: absolute;
                width: 1px;
                height: 1px;
                padding: 0;
                margin: -1px;
                overflow: hidden;
                clip: rect(0, 0, 0, 0);
                white-space: nowrap;
                border: 0;
            }
            /* Color variants */
            .spinner-container.primary {
                color: #007bff;
            }
            .spinner-container.secondary {
                color: #6c757d;
            }
            .spinner-container.success {
                color: #28a745;
            }
            .spinner-container.danger {
                color: #dc3545;
            }
            .spinner-container.warning {
                color: #ffc107;
            }
            .spinner-container.info {
                color: #17a2b8;
            }
            .spinner-container.light {
                color: #f8f9fa;
            }
            .spinner-container.dark {
                color: #343a40;
            }
        `,
    ],
})
export class LoadingSpinnerComponent {
    @Input() size: SpinnerSize = 'md';
    @Input() type: SpinnerType = 'border';
    @Input() color: string = 'primary';
    @Input() text: string = 'Loading...';
    @Input() showText: boolean = false;
    @Input() display: 'inline' | 'block' | 'overlay' = 'inline';
    private _size = signal<SpinnerSize>('md');
    private _type = signal<SpinnerType>('border');
    private _color = signal('primary');
    private _display = signal<'inline' | 'block' | 'overlay'>('inline');
    constructor() {
        this._size.set(this.size);
        this._type.set(this.type);
        this._color.set(this.color);
        this._display.set(this.display);
    }
    ngOnChanges() {
        this._size.set(this.size);
        this._type.set(this.type);
        this._color.set(this.color);
        this._display.set(this.display);
    }
    containerClasses = computed(() => {
        const classes = [`spinner-container-${this._display()}`, `spinner-container-${this._color()}`];
        return classes.join(' ');
    });
    spinnerClasses = computed(() => {
        const classes = [`spinner-${this._type()}`, `spinner-${this._size()}`];
        return classes.join(' ');
    });
    ariaLabel = computed(() => {
        return this.showText ? this.text : 'Loading content';
    });
}
