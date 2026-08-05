import { Directive, ElementRef, Input, OnChanges, OnDestroy, Renderer2, SimpleChanges, inject } from '@angular/core';

/**
 * Global loading overlay directive
 *
 * Usage:
 * - Simple boolean: <div [busyIf]="isLoading()">Content</div>
 * - With message: <div [busyIf]="data$ | async" [busyMessage]="'Loading data...'">Content</div>
 *
 * Creates a scoped, semi-transparent overlay with centered spinner when true.
 * No external dependencies (ngx-spinner removed).
 */
@Directive({
  selector: '[busyIf]',
  standalone: true,
  host: { style: 'position: relative; display: block;' }
})
export class BusyIfDirective implements OnChanges, OnDestroy {
  @Input() busyIf: boolean = false;
  @Input() busyMessage: string = 'Loading...';

  private elementRef = inject(ElementRef<HTMLElement>);
  private renderer = inject(Renderer2);
  private overlayElement: HTMLElement | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.busyIf) {
      if (changes.busyIf.currentValue) {
        this.show();
      } else {
        this.hide();
      }
    }
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  private show(): void {
    if (!this.overlayElement) {
      this.createOverlay();
    }
    if (this.overlayElement) {
      this.renderer.setStyle(this.overlayElement, 'display', 'flex');
      this.renderer.setStyle(this.overlayElement, 'opacity', '1');
      this.renderer.setStyle(this.overlayElement, 'pointer-events', 'auto');
    }
  }

  private hide(): void {
    if (this.overlayElement) {
      this.renderer.setStyle(this.overlayElement, 'opacity', '0');
      this.renderer.setStyle(this.overlayElement, 'pointer-events', 'none');
      // Keep display: flex but hidden via opacity for smooth transitions
    }
  }

  private createOverlay(): void {
    // Main overlay container
    this.overlayElement = this.renderer.createElement('div');
    this.renderer.setStyle(this.overlayElement, 'position', 'absolute');
    this.renderer.setStyle(this.overlayElement, 'top', '0');
    this.renderer.setStyle(this.overlayElement, 'left', '0');
    this.renderer.setStyle(this.overlayElement, 'right', '0');
    this.renderer.setStyle(this.overlayElement, 'bottom', '0');
    this.renderer.setStyle(this.overlayElement, 'display', 'flex');
    this.renderer.setStyle(this.overlayElement, 'justify-content', 'center');
    this.renderer.setStyle(this.overlayElement, 'align-items', 'center');
    this.renderer.setStyle(this.overlayElement, 'flex-direction', 'column');
    this.renderer.setStyle(this.overlayElement, 'z-index', '999');
    this.renderer.setStyle(this.overlayElement, 'background-color', 'rgba(255, 255, 255, 0.85)');
    this.renderer.setStyle(this.overlayElement, 'opacity', '0');
    this.renderer.setStyle(this.overlayElement, 'pointer-events', 'none');
    this.renderer.setStyle(this.overlayElement, 'transition', 'opacity 0.3s ease-in-out');

    // Spinner container - 8 spokes/edges (Sudarshan Chakra style)
    const spinner = this.renderer.createElement('div');
    this.renderer.setAttribute(spinner, 'class', 'busy-spinner-chakra');
    this.renderer.setStyle(spinner, 'width', '2.5rem');
    this.renderer.setStyle(spinner, 'height', '2.5rem');
    this.renderer.setStyle(spinner, 'position', 'relative');
    this.renderer.setStyle(spinner, 'animation', 'spin-chakra 1.2s linear infinite');

    // Create 8 spokes radiating from center
    for (let i = 0; i < 8; i++) {
      const spoke = this.renderer.createElement('div');
      this.renderer.setStyle(spoke, 'position', 'absolute');
      this.renderer.setStyle(spoke, 'width', '0.3rem');
      this.renderer.setStyle(spoke, 'height', '1rem');
      this.renderer.setStyle(spoke, 'background-color', '#808080');
      this.renderer.setStyle(spoke, 'top', '50%');
      this.renderer.setStyle(spoke, 'left', '50%');
      this.renderer.setStyle(spoke, 'border-radius', '2px');

      const angle = (i * 45);
      this.renderer.setStyle(spoke, 'transform', `translate(-50%, -50%) rotate(${angle}deg) translateY(-0.65rem)`);

      this.renderer.appendChild(spinner, spoke);
    }

    // Inject styles into head if not already there
    this.injectStyles();

    // Message element
    const message = this.renderer.createElement('div');
    this.renderer.setStyle(message, 'margin-top', '1rem');
    this.renderer.setStyle(message, 'font-size', '0.875rem');
    this.renderer.setStyle(message, 'color', '#6c757d');
    this.renderer.setStyle(message, 'font-weight', '500');
    this.renderer.setStyle(message, 'letter-spacing', '0.5px');
    const messageText = this.renderer.createText(this.busyMessage);
    this.renderer.appendChild(message, messageText);

    // Assemble overlay
    this.renderer.appendChild(this.overlayElement, spinner);
    this.renderer.appendChild(this.overlayElement, message);

    // Append to host element
    this.renderer.appendChild(this.elementRef.nativeElement, this.overlayElement);
  }

  private injectStyles(): void {
    // Check if animation style already exists
    if (document.getElementById('busy-if-spinner-styles')) {
      return;
    }

    const style = this.renderer.createElement('style');
    this.renderer.setAttribute(style, 'id', 'busy-if-spinner-styles');
    const cssText = `
      @keyframes spin-chakra {
        0% {
          transform: rotate(0deg);
        }
        100% {
          transform: rotate(360deg);
        }
      }
      .busy-spinner-chakra {
        animation: spin-chakra 1.2s linear infinite;
      }
    `;
    const textNode = this.renderer.createText(cssText);
    this.renderer.appendChild(style, textNode);
    this.renderer.appendChild(document.head, style);
  }

  private cleanup(): void {
    if (this.overlayElement) {
      this.renderer.removeChild(this.elementRef.nativeElement, this.overlayElement);
      this.overlayElement = null;
    }
  }
}
