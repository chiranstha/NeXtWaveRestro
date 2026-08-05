import { Directive, ElementRef, inject } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AccessibilityService } from '@app/shared/services/accessibility.service';

@Directive({
    selector: '[appBsModal]',
    exportAs: 'bs-modal',
})
export class AppBsModalDirective extends ModalDirective {
    private _accessibilityService = inject(AccessibilityService);
    private _elementRef = inject(ElementRef);
    private _focusTrapId: string | null = null;

    show(): void {
        this.blurActiveElementOutsideModal();
        super.show();
    }

    showElement(): void {
        super.showElement();
        this.setZIndexes();
        // Create focus trap when modal is shown
        this._focusTrapId = this._accessibilityService.createFocusTrap(this._elementRef.nativeElement, {
            restoreFocus: true,
            autoFocus: true,
        });
    }

    hide(event?: Event): void {
        this.blurActiveElementInsideModal();
        // Remove focus trap when modal is hidden
        if (this._focusTrapId) {
            this._accessibilityService.removeFocusTrap(this._focusTrapId);
            this._focusTrapId = null;
        }
        super.hide(event);
        setTimeout(() => this.cleanupOrphanedBackdrops(), 250);
    }

    setZIndexes(): void {
        const newZIndex = this.setAndGetModalZIndex();
        this.setBackDropZIndex(newZIndex - 1);
    }

    setAndGetModalZIndex(): number {
        const modalBaseZIndex = 1050;
        const modalsLength = document.querySelectorAll('.modal.fade.show').length;
        const newZIndex = modalBaseZIndex + modalsLength * 2;
        (this as any)._element.nativeElement.style.zIndex = newZIndex.toString();
        return newZIndex;
    }

    setBackDropZIndex(zindex: number): void {
        const modalBackdrops = document.querySelectorAll('.modal-backdrop.fade.show');
        const backdrop = modalBackdrops[modalBackdrops.length - 1] as HTMLElement | undefined;
        if (backdrop) {
            backdrop.style.zIndex = zindex.toString();
        }
    }

    private blurActiveElementInsideModal(): void {
        const modalElement = this._elementRef.nativeElement as HTMLElement;
        const activeElement = document.activeElement;

        if (activeElement instanceof HTMLElement && modalElement.contains(activeElement)) {
            activeElement.blur();
        }
    }

    private blurActiveElementOutsideModal(): void {
        const modalElement = this._elementRef.nativeElement as HTMLElement;
        const activeElement = document.activeElement;

        if (activeElement instanceof HTMLElement && !modalElement.contains(activeElement)) {
            activeElement.blur();
        }
    }

    private cleanupOrphanedBackdrops(): void {
        const hasVisibleModal = Array.from(
            document.querySelectorAll<HTMLElement>('.modal.show, .modal.in, .modal[style*="display: block"]'),
        ).some((element) => {
            const style = window.getComputedStyle(element);
            return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
        });

        if (hasVisibleModal) {
            return;
        }

        document.querySelectorAll<HTMLElement>('.modal-backdrop').forEach((element) => element.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');
    }
}
