import { Component, Input, signal, computed } from '@angular/core';

export type SpinnerSize = 'sm' | 'md' | 'lg' | 'xl';
export type SpinnerType = 'border' | 'grow';
@Component({
    selector: 'app-loading-spinner',
    standalone: true,
    imports: [],
    template: `
        <div [class]="containerClasses()" role="status" [attr.aria-label]="ariaLabel()">
            <div [class]="spinnerClasses()" [attr.aria-hidden]="true"></div>
            @if (showText) {
                <span class="small">{{ text }}</span>
            }
            <span class="visually-hidden">{{ ariaLabel() }}</span>
        </div>
    `,
    styles: [
        `
            .spinner-size-lg {
                width: 3rem;
                height: 3rem;
            }

            .spinner-size-xl {
                width: 4rem;
                height: 4rem;
            }

            .spinner-border.spinner-size-lg {
                border-width: 0.3em;
            }

            .spinner-border.spinner-size-xl {
                border-width: 0.4em;
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
        const displayClasses = {
            inline: 'd-inline-flex',
            block: 'd-flex justify-content-center w-100',
            overlay: 'position-absolute top-50 start-50 translate-middle',
        };
        const classes = ['align-items-center', 'gap-2', displayClasses[this._display()], `text-${this._color()}`];
        return classes.join(' ');
    });
    spinnerClasses = computed(() => {
        const typeClass = `spinner-${this._type()}`;
        const sizeClasses = {
            sm: `${typeClass}-sm`,
            md: '',
            lg: 'spinner-size-lg',
            xl: 'spinner-size-xl',
        };
        const classes = [typeClass, sizeClasses[this._size()]];
        return classes.join(' ');
    });
    ariaLabel = computed(() => {
        return this.showText ? this.text : 'Loading content';
    });
}
