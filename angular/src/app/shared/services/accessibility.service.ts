import { Injectable, signal, computed } from '@angular/core';
export interface FocusTrapConfig {
    restoreFocus?: boolean;
    autoFocus?: boolean;
    focusSelector?: string;
}
export interface AriaLiveConfig {
    priority?: 'polite' | 'assertive';
    atomic?: boolean;
    relevant?: 'additions' | 'removals' | 'text' | 'all';
}
@Injectable({
    providedIn: 'root',
})
export class AccessibilityService {
    private _highContrast = signal(false);
    private _reducedMotion = signal(false);
    private _screenReader = signal(false);
    private _focusTraps = signal<Map<string, FocusTrapConfig>>(new Map());
    highContrast = this._highContrast.asReadonly();
    reducedMotion = this._reducedMotion.asReadonly();
    screenReader = this._screenReader.asReadonly();
    prefersReducedMotion = computed(() => {
        if (typeof window !== 'undefined') {
            return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        }
        return false;
    });
    constructor() {
        this.detectPreferences();
        this.listenForPreferenceChanges();
    }
    private detectPreferences(): void {
        if (typeof window === 'undefined') {
            return;
        }
        // Detect high contrast mode
        const testElement = document.createElement('div');
        testElement.style.color = 'rgb(31, 41, 55)';
        testElement.style.backgroundColor = 'rgb(255, 255, 255)';
        document.body.appendChild(testElement);
        const computedColor = window.getComputedStyle(testElement).color;
        const computedBg = window.getComputedStyle(testElement).backgroundColor;
        this._highContrast.set(computedColor === computedBg);
        document.body.removeChild(testElement);
        // Detect screen reader
        this._screenReader.set(
            navigator.userAgent.includes('NVDA') ||
                navigator.userAgent.includes('JAWS') ||
                navigator.userAgent.includes('VoiceOver') ||
                document.querySelector('[aria-live]') !== null,
        );
    }
    private listenForPreferenceChanges(): void {
        if (typeof window === 'undefined') {
            return;
        }
        const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
        const contrastQuery = window.matchMedia('(prefers-contrast: high)');
        motionQuery.addEventListener('change', (e) => {
            this._reducedMotion.set(e.matches);
        });
        contrastQuery.addEventListener('change', (e) => {
            this._highContrast.set(e.matches);
        });
    }
    // Focus management
    focusElement(element: HTMLElement | string): void {
        const el = typeof element === 'string' ? document.querySelector(element) : element;
        if (el) {
            (el as HTMLElement).focus();
        }
    }
    // Focus trap management
    createFocusTrap(container: HTMLElement, config: FocusTrapConfig = {}): string {
        const id = `focus-trap-${Date.now()}`;
        const trapConfig = {
            restoreFocus: true,
            autoFocus: true,
            ...config,
        };
        this._focusTraps.update((traps) => {
            traps.set(id, trapConfig);
            return new Map(traps);
        });
        this.setupFocusTrap(container, trapConfig);
        return id;
    }
    removeFocusTrap(id: string): void {
        this._focusTraps.update((traps) => {
            traps.delete(id);
            return new Map(traps);
        });
    }
    private setupFocusTrap(container: HTMLElement, config: FocusTrapConfig): void {
        const focusableElements = this.getFocusableElements(container);
        if (config.autoFocus && focusableElements.length > 0) {
            const firstElement = config.focusSelector
                ? container.querySelector(config.focusSelector)
                : focusableElements[0];
            if (firstElement) {
                (firstElement as HTMLElement).focus();
            }
        }
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key !== 'Tab') {
                return;
            }
            const firstElement = focusableElements[0];
            const lastElement = focusableElements[focusableElements.length - 1];
            if (event.shiftKey) {
                if (document.activeElement === firstElement) {
                    lastElement.focus();
                    event.preventDefault();
                }
            } else {
                if (document.activeElement === lastElement) {
                    firstElement.focus();
                    event.preventDefault();
                }
            }
        };
        container.addEventListener('keydown', handleKeyDown);
        // Store cleanup function
        (container as any)._focusTrapCleanup = () => {
            container.removeEventListener('keydown', handleKeyDown);
        };
    }
    private getFocusableElements(container: HTMLElement): HTMLElement[] {
        const focusableSelectors = [
            'a[href]',
            'button:not([disabled])',
            'textarea:not([disabled])',
            'input:not([disabled])',
            'select:not([disabled])',
            '[tabindex]:not([tabindex="-1"])',
            '[contenteditable="true"]',
        ];
        return Array.from(container.querySelectorAll(focusableSelectors.join(', ')));
    }
    // ARIA live region management
    announce(message: string, config: AriaLiveConfig = {}): void {
        const { priority = 'polite', atomic = false, relevant = 'additions' } = config;
        let liveRegion = document.querySelector<HTMLElement>('.accessibility-live-region');
        if (!liveRegion) {
            liveRegion = document.createElement('div');
            liveRegion.className = 'accessibility-live-region';
            liveRegion.setAttribute('aria-live', priority);
            liveRegion.setAttribute('aria-atomic', atomic.toString());
            liveRegion.setAttribute('aria-relevant', relevant);
            liveRegion.style.position = 'absolute';
            liveRegion.style.left = '-10000px';
            liveRegion.style.width = '1px';
            liveRegion.style.height = '1px';
            liveRegion.style.overflow = 'hidden';
            document.body.appendChild(liveRegion);
        }
        liveRegion.textContent = message;
        // Clear after announcement
        setTimeout(() => {
            liveRegion.textContent = '';
        }, 1000);
    }
    // Skip link management
    addSkipLink(targetId: string, text: string = 'Skip to main content'): void {
        const existingSkipLink = document.querySelector('.skip-link');
        if (existingSkipLink) {
            return;
        }
        const skipLink = document.createElement('a');
        skipLink.href = `#${targetId}`;
        skipLink.className = 'skip-link';
        skipLink.textContent = text;
        skipLink.style.cssText = `
      position: absolute;
      top: -40px;
      left: 6px;
      background: #000;
      color: #fff;
      padding: 8px;
      text-decoration: none;
      z-index: 1000;
      border-radius: 4px;
    `;
        skipLink.addEventListener('focus', () => {
            skipLink.style.top = '6px';
        });
        skipLink.addEventListener('blur', () => {
            skipLink.style.top = '-40px';
        });
        document.body.insertBefore(skipLink, document.body.firstChild);
    }
    // Color contrast utilities
    getContrastRatio(_color1: string, _color2: string): number {
        // Simple contrast ratio calculation
        // In a real implementation, you'd use a proper color parsing library
        return 4.5; // Placeholder - would need proper implementation
    }
    // Keyboard navigation helpers
    isNavigationKey(event: KeyboardEvent): boolean {
        return ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'].includes(
            event.key,
        );
    }
    handleRovingTabIndex(container: HTMLElement, event: KeyboardEvent): void {
        const focusableElements = this.getFocusableElements(container);
        if (!this.isNavigationKey(event) || focusableElements.length === 0) {
            return;
        }
        event.preventDefault();
        const currentIndex = focusableElements.indexOf(document.activeElement as HTMLElement);
        let nextIndex = currentIndex;
        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown':
                nextIndex = (currentIndex + 1) % focusableElements.length;
                break;
            case 'ArrowLeft':
            case 'ArrowUp':
                nextIndex = currentIndex <= 0 ? focusableElements.length - 1 : currentIndex - 1;
                break;
            case 'Home':
                nextIndex = 0;
                break;
            case 'End':
                nextIndex = focusableElements.length - 1;
                break;
        }
        focusableElements[nextIndex].focus();
    }
}
