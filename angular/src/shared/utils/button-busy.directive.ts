import { Directive, ElementRef, Input, OnInit, AfterViewInit, inject } from '@angular/core';
import { AppLocalizationService } from '@app/shared/common/localization/app-localization.service';
@Directive({ selector: '[buttonBusy]' })
export class ButtonBusyDirective implements OnInit, AfterViewInit {
    private _element = inject(ElementRef);
    private _appLocalizationService = inject(AppLocalizationService);
    @Input() busyText: string;
    private _originalButtonInnerHtml: any;
    private _button: any;
    private _busyStateClass = 'btn-loading';
    constructor() {}
    @Input() set buttonBusy(isBusy: boolean) {
        this.refreshState(isBusy);
    }
    ngOnInit(): void {
        this._button = this._element.nativeElement;
        // Add the necessary CSS if it doesn't exist
        this.ensureAnimationStylesExist();
    }
    ngAfterViewInit(): void {
        this._originalButtonInnerHtml = this._button.innerHTML;
    }
    refreshState(isBusy: boolean): void {
        if (!this._button) {
            return;
        }
        if (isBusy) {
            // disable button
            this._button.setAttribute('disabled', 'disabled');
            // Add loading class for animation
            this._button.classList.add(this._busyStateClass);
            this._button.innerHTML =
                '<span class="spinner-container">' +
                '<i class="fa-duotone fa-spinner-third fa-spin-pulse"></i>' +
                '</span>' +
                `<span class="button-text">${
                    this.busyText ? this.busyText : this._appLocalizationService.l('Processing...')
                }</span>`;
            this._button.setAttribute('_disabledBefore', true);
        } else {
            if (!this._button.getAttribute('_disabledBefore')) {
                return;
            }
            // enable button
            this._button.removeAttribute('disabled');
            // Remove loading class
            this._button.classList.remove(this._busyStateClass);
            this._button.innerHTML = this._originalButtonInnerHtml;
        }
    }
    // Dynamically add the necessary styles for button animation
    private ensureAnimationStylesExist(): void {
        const styleId = 'button-busy-animation-styles';
        // Check if styles already exist
        if (!document.getElementById(styleId)) {
            const styleElement = document.createElement('style');
            styleElement.id = styleId;
            styleElement.textContent = `
                .btn-loading {
                    position: relative;
                    pointer-events: none;
                    transition: all 0.2s ease;
                }
                .btn-loading .spinner-container {
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    margin-right: 8px;
                }
                .btn-loading .fa-spin-pulse {
                    animation: spin 1s linear infinite;
                }
                .btn-loading .button-text {
                    opacity: 0.8;
                }
                @keyframes spin {
                    from {
                        transform: rotate(0deg);
                    }
                    to {
                        transform: rotate(360deg);
                    }
                }
                .btn-loading.btn-sm .spinner-container {
                    margin-right: 5px;
                }
                /* Animate button when loading */
                .btn-loading {
                    animation: button-pulse 2s infinite;
                }
                @keyframes button-pulse {
                    0% {
                        box-shadow: 0 0 0 0 rgba(var(--bs-primary-rgb, 13, 110, 253), 0.2);
                    }
                    70% {
                        box-shadow: 0 0 0 5px rgba(var(--bs-primary-rgb, 13, 110, 253), 0);
                    }
                    100% {
                        box-shadow: 0 0 0 0 rgba(var(--bs-primary-rgb, 13, 110, 253), 0);
                    }
                }
            `;
            document.head.appendChild(styleElement);
        }
    }
}
