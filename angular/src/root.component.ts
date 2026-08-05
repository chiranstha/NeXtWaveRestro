import { AfterViewInit, Component, ElementRef, OnDestroy, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, Router, RouterOutlet } from '@angular/router';
import { NgxSpinnerTextService } from '@app/shared/ngx-spinner-text.service';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { NgxSpinnerComponent } from 'ngx-spinner';
@Component({
    selector: 'app-root',
    template: `
        <router-outlet></router-outlet>
        <ngx-spinner type="ball-clip-rotate" size="medium" color="#5ba7ea">
            @if (ngxSpinnerText) {
                <p>{{ getSpinnerText() }}</p>
            }
        </ngx-spinner>
    `,
    imports: [RouterOutlet, NgxSpinnerComponent],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class RootComponent implements AfterViewInit, OnDestroy {
    private readonly _uiCustomizationService = inject(AppUiCustomizationService);
    private readonly _elementRef = inject(ElementRef);
    ngxSpinnerText: NgxSpinnerTextService;
    private readonly _routerEventsSubscription: Subscription;
    private _ariaHiddenObserver?: MutationObserver;
    private _dialogFocusGuardRetry?: number;
    private readonly _hiddenRootAttributes = ['inert', 'aria-hidden'];
    constructor() {
        const _ngxSpinnerText = inject(NgxSpinnerTextService);
        const router = inject(Router);
        this.ngxSpinnerText = _ngxSpinnerText;
        this._routerEventsSubscription = router.events
            .pipe(
                filter(
                    (event): event is NavigationEnd | NavigationCancel | NavigationError =>
                        event instanceof NavigationEnd ||
                        event instanceof NavigationCancel ||
                        event instanceof NavigationError,
                ),
            )
            .subscribe((event) => {
                setTimeout(() => {
                    if (event instanceof NavigationEnd) {
                        this.toggleBodyCssClass(event.urlAfterRedirects);
                    }
                    this.cleanupOrphanedGlobalOverlays();
                }, 0);
                setTimeout(() => this.cleanupOrphanedGlobalOverlays(), 250);
            });
    }
    ngOnDestroy(): void {
        this._routerEventsSubscription.unsubscribe();
        this._ariaHiddenObserver?.disconnect();
        if (this._dialogFocusGuardRetry) {
            window.clearTimeout(this._dialogFocusGuardRetry);
        }
    }
    ngAfterViewInit(): void {
        this.installDialogFocusGuard();

        if (typeof MutationObserver === 'undefined') {
            return;
        }
        const hostElement = this._elementRef.nativeElement as HTMLElement;
        this._ariaHiddenObserver = new MutationObserver(() => {
            this.moveFocusFromHiddenHost(hostElement);
        });
        this._ariaHiddenObserver.observe(hostElement, {
            attributes: true,
            attributeFilter: this._hiddenRootAttributes,
        });
    }
    private installDialogFocusGuard(attempt = 0): void {
        const patchedAnyDialog = this.patchAbpMessageDialogOpeners() || this.patchSweetAlertOpeners();
        if (patchedAnyDialog || attempt >= 10) {
            return;
        }

        this._dialogFocusGuardRetry = window.setTimeout(() => this.installDialogFocusGuard(attempt + 1), 250);
    }
    private patchAbpMessageDialogOpeners(): boolean {
        const abpMessage = (window as any).abp?.message;
        if (!abpMessage) {
            return false;
        }

        return ['confirm', 'info', 'success', 'warn', 'error'].some((methodName) =>
            this.patchDialogMethod(abpMessage, methodName),
        );
    }
    private patchSweetAlertOpeners(): boolean {
        const win = window as any;
        const candidates = [win.Swal, win.swal, win.sweetAlert, win.SweetAlert, win.Sweetalert2].filter(Boolean);
        return candidates.some((candidate) => this.patchDialogMethod(candidate, 'fire'));
    }
    private patchDialogMethod(owner: Record<string, any>, methodName: string): boolean {
        const originalMethod = owner?.[methodName];
        if (typeof originalMethod !== 'function' || originalMethod.__appFocusGuardPatched) {
            return false;
        }

        const rootComponent = this;
        const guardedMethod = function (...args: unknown[]) {
            rootComponent.blurActiveElementBeforeExternalDialog();
            return originalMethod.apply(this, args);
        };
        guardedMethod.__appFocusGuardPatched = true;
        owner[methodName] = guardedMethod;
        return true;
    }
    private blurActiveElementBeforeExternalDialog(): void {
        const hostElement = this._elementRef.nativeElement as HTMLElement;
        const activeElement = document.activeElement;
        if (!(activeElement instanceof HTMLElement) || activeElement === document.body) {
            return;
        }

        if (hostElement.contains(activeElement)) {
            activeElement.blur();
        }
    }
    private toggleBodyCssClass(url: string): void {
        if (!url) {
            return;
        }
        if (url === '/') {
            if (abp.session.userId > 0) {
                this.setAppModuleBodyClassInternal();
            } else {
                this.setAccountModuleBodyClassInternal();
            }
            return;
        }
        if (url.indexOf('/account/') >= 0) {
            this.setAccountModuleBodyClassInternal();
        } else {
            this.setAppModuleBodyClassInternal();
        }
    }
    private setAppModuleBodyClassInternal(): void {
        const currentBodyClass = document.body.className;
        let classesToRemember = '';
        if (currentBodyClass.indexOf('brand-minimize') >= 0) {
            classesToRemember += ' brand-minimize ';
        }
        if (currentBodyClass.indexOf('aside-left-minimize') >= 0) {
            classesToRemember += ' aside-left-minimize';
        }
        if (currentBodyClass.indexOf('brand-hide') >= 0) {
            classesToRemember += ' brand-hide';
        }
        if (currentBodyClass.indexOf('aside-left-hide') >= 0) {
            classesToRemember += ' aside-left-hide';
        }
        if (currentBodyClass.indexOf('swal2-toast-shown') >= 0) {
            classesToRemember += ' swal2-toast-shown';
        }
        document.body.className = `${this._uiCustomizationService.getAppModuleBodyClass()} ${classesToRemember}`;
    }
    private setAccountModuleBodyClassInternal(): void {
        const currentBodyClass = document.body.className;
        let classesToRemember = '';
        if (currentBodyClass.indexOf('swal2-toast-shown') >= 0) {
            classesToRemember += ' swal2-toast-shown';
        }
        document.body.className = `${this._uiCustomizationService.getAccountModuleBodyClass()} ${classesToRemember}`;
    }
    getSpinnerText(): string {
        return this.ngxSpinnerText.getText();
    }
    private moveFocusFromHiddenHost(hostElement: HTMLElement): void {
        const rootIsHidden = hostElement.hasAttribute('inert') || hostElement.getAttribute('aria-hidden') === 'true';
        if (!rootIsHidden) {
            return;
        }

        const activeElement = document.activeElement as HTMLElement | null;
        if (!activeElement || !hostElement.contains(activeElement)) {
            return;
        }

        const visibleDialog = Array.from(document.querySelectorAll<HTMLElement>('.modal.show, .swal2-container')).find(
            (element) => !hostElement.contains(element),
        );
        const focusTarget = visibleDialog ? this.getFirstFocusable(visibleDialog) : null;
        if (focusTarget) {
            focusTarget.focus();
        } else if (visibleDialog) {
            visibleDialog.setAttribute('tabindex', '-1');
            visibleDialog.focus();
        } else {
            activeElement.blur();
        }
    }
    private getFirstFocusable(container: HTMLElement): HTMLElement | null {
        const focusableSelectors = [
            'a[href]',
            'button:not([disabled])',
            'textarea:not([disabled])',
            'input:not([disabled])',
            'select:not([disabled])',
            '[tabindex]:not([tabindex="-1"])',
            '[contenteditable="true"]',
        ];
        return container.querySelector(focusableSelectors.join(', '));
    }

    private cleanupOrphanedGlobalOverlays(): void {
        this.cleanupOrphanedModalBackdrops();
        this.cleanupOrphanedDrawerOverlays();
        this.cleanupOrphanedOffcanvasBackdrops();
    }

    private cleanupOrphanedModalBackdrops(): void {
        if (this.hasVisibleElement('.modal.show, .modal.in, .modal[style*="display: block"]')) {
            return;
        }

        document.querySelectorAll<HTMLElement>('.modal-backdrop').forEach((element) => element.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');
    }

    private cleanupOrphanedDrawerOverlays(): void {
        if (this.hasVisibleElement('.drawer-on[data-kt-drawer="true"]')) {
            return;
        }

        document.querySelectorAll<HTMLElement>('.drawer-overlay').forEach((element) => element.remove());
        document.body.removeAttribute('data-kt-drawer');
        Array.from(document.body.attributes)
            .filter((attribute) => attribute.name.startsWith('data-kt-drawer-'))
            .forEach((attribute) => document.body.removeAttribute(attribute.name));
    }

    private cleanupOrphanedOffcanvasBackdrops(): void {
        if (this.hasVisibleElement('.offcanvas.show')) {
            return;
        }

        document.querySelectorAll<HTMLElement>('.offcanvas-backdrop').forEach((element) => element.remove());
    }

    private hasVisibleElement(selector: string): boolean {
        return Array.from(document.querySelectorAll<HTMLElement>(selector)).some((element) => {
            const style = window.getComputedStyle(element);
            return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
        });
    }
}
