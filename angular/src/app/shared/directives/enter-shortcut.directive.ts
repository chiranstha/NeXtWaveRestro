import { Directive, ElementRef, HostListener, Input, inject } from '@angular/core';
/**
 * Usage: Add attribute `appEnterShortcut="'targetId:open'"` or `appEnterShortcut="'targetId'"` on any input.
 *
 * When Enter is pressed on the current element:
 * - if targetId is an element id, it will `focus()` that element
 * - if :open suffix is provided and the target is a `ng-select`, it will attempt to open the dropdown
 */
@Directive({
    selector: '[appEnterShortcut]',
})
export class EnterShortcutDirective {
    @Input('appEnterShortcut') target: string;

    private el = inject(ElementRef<HTMLElement>);
    @HostListener('keyup.enter', ['$event'])
    onEnter(event: KeyboardEvent) {
        event.preventDefault();
        if (!this.target) {
            // default: try to focus next focusable element
            const next = this.findNextFocusable();
            if (next) {
                next.focus();
            }
            return;
        }
        // allow format 'targetId:open' to open an ng-select
        const parts = this.target.split(':');
        const id = parts[0];
        const action = parts[1] || 'focus';
        const targetEl = document.getElementById(id);
        if (!targetEl) {
            console.warn('EnterShortcut: target element not found with id=', id);
            const fallback = this.findNextFocusable();
            if (fallback) {
                fallback.focus();
            }
            return;
        }
        if (action === 'open') {
            // Try to open custom/ng-select like dropdown by clicking its container
            // first try a direct focus
            targetEl.focus();
            // then try to find a clickable container
            const container = targetEl.querySelector('.ng-select-container, .app-dropdown, .ui-dropdown');
            if (container) {
                (container as HTMLElement).click();
            }
            return;
        }
        // default: focus the element
        targetEl.focus();
    }
    /** Find next focusable element in the document */
    private findNextFocusable(): HTMLElement | null {
        const focusableSelectors = [
            'a[href]',
            'button',
            'input',
            'select',
            'textarea',
            '[tabindex]:not([tabindex="-1"])',
        ].join(',');
        const focusables = Array.from(document.querySelectorAll(focusableSelectors));
        const currentIndex = focusables.indexOf(this.el.nativeElement);
        if (currentIndex < 0) {
            return null;
        }
        // Find next that is not disabled and is visible
        for (let i = currentIndex + 1; i < focusables.length; i++) {
            const el = focusables[i];
            if (!el.hasAttribute('disabled') && el.offsetParent !== null) {
                return el;
            }
        }
        return null;
    }
}
