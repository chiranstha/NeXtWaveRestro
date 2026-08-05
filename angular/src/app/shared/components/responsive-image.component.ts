import { Component, Input, HostBinding } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';

/**
 * Responsive image component with WebP support and lazy loading
 * Automatically serves optimized images based on device capabilities and viewport
 *
 * Usage:
 * <app-responsive-image
 *   [src]="'assets/images/photo'"
 *   [alt]="'Photo description'"
 *   [width]="800"
 *   [height]="600"
 *   sizes="(max-width: 768px) 100vw, (max-width: 1024px) 80vw, 60vw">
 * </app-responsive-image>
 */
@Component({
    selector: 'app-responsive-image',
    standalone: true,
    imports: [NgOptimizedImage],
    template: `
        <picture>
            <!-- WebP format for modern browsers -->
            @if (supportedFormats.webp) {
                <source [srcset]="getWebPSrcSet()" [sizes]="sizes" type="image/webp" />
            }

            <!-- AVIF format for cutting-edge browsers -->
            @if (supportedFormats.avif) {
                <source [srcset]="getAvifSrcSet()" [sizes]="sizes" type="image/avif" />
            }

            <!-- Fallback JPEG -->
            <img
                [ngSrc]="getJpegSrc()"
                [alt]="alt"
                [width]="width"
                [height]="height"
                loading="lazy"
                decoding="async"
                class="responsive-img"
            />
        </picture>
    `,
    styles: [
        `
            :host {
                display: block;
                overflow: hidden;
            }

            picture {
                display: block;
                width: 100%;
            }

            .responsive-img {
                width: 100%;
                height: auto;
                display: block;
            }
        `,
    ],
})
export class ResponsiveImageComponent {
    @Input() src!: string;
    @Input() alt: string = '';
    @Input() width: number = 800;
    @Input() height: number = 600;
    @Input() sizes: string = '100vw';
    @Input() quality: number = 80;

    @HostBinding('class.image-container') imageContainer = true;

    supportedFormats = {
        webp: this.supportsFormat('webp'),
        avif: this.supportsFormat('avif'),
    };

    /**
     * Check if browser supports a specific image format
     */
    private supportsFormat(format: 'webp' | 'avif'): boolean {
        if (typeof document === 'undefined') {
            return false;
        }

        const canvas = document.createElement('canvas');
        canvas.width = canvas.height = 1;

        switch (format) {
            case 'webp':
                return canvas.toDataURL('image/webp').includes('webp');
            case 'avif':
                return canvas.toDataURL('image/avif').includes('avif');
            default:
                return false;
        }
    }

    /**
     * Generate responsive WebP srcset
     * Generates multiple resolutions for different viewport sizes
     */
    getWebPSrcSet(): string {
        const sizes = [320, 640, 960, 1280, 1920];
        return sizes.map((size) => `${this.src}-${size}w.webp ${size}w`).join(', ');
    }

    /**
     * Generate responsive AVIF srcset
     * For cutting-edge browsers with better compression
     */
    getAvifSrcSet(): string {
        const sizes = [320, 640, 960, 1280, 1920];
        return sizes.map((size) => `${this.src}-${size}w.avif ${size}w`).join(', ');
    }

    /**
     * Get fallback JPEG source
     * Used as final fallback for compatibility
     */
    getJpegSrc(): string {
        return `${this.src}-800w.jpg`;
    }
}
