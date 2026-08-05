import { Injectable } from '@angular/core';
@Injectable()
export class StyleLoaderService {
    private styles: { [key: string]: boolean } = {};
    load(...styles: string[]) {
        this.styles = styles.reduce((acc, style) => ({ ...acc, [style]: true }), {});
        const promises: Promise<void>[] = [];
        styles.forEach((style) => promises.push(this.loadStyle(style)));
        return Promise.all(promises);
    }
    loadArray(styles: string[]) {
        this.styles = styles.reduce((acc, style) => ({ ...acc, [style]: true }), {});
        const promises: Promise<void>[] = [];
        styles.forEach((style) => promises.push(this.loadStyle(style)));
        return Promise.all(promises);
    }
    loadStyle(name: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            const style = document.createElement('link') as any; // Cast to any for IE-specific properties
            style.type = 'text/css';
            style.rel = 'stylesheet';
            style.href = name;
            if (style.readyState) {
                //IE
                style.onreadystatechange = () => {
                    if (style.readyState === 'loaded' || style.readyState === 'complete') {
                        style.onreadystatechange = null;
                        resolve();
                    }
                };
            } else {
                //Others
                style.onload = () => {
                    resolve();
                };
            }
            style.onerror = (error: Event) => resolve();
            document.getElementsByTagName('head')[0].appendChild(style);
        });
    }
}
