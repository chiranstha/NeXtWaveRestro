import { Injectable } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { Observable, of, timer } from 'rxjs';
import { catchError, mergeMap } from 'rxjs/operators';
@Injectable({
    providedIn: 'root',
})
export class NetworkAwarePreloadingStrategy implements PreloadingStrategy {
    preload(route: Route, load: () => Observable<any>): Observable<any> {
        // Check if the route has preload data
        if (route.data?.['preload']) {
            // Check network connection
            if ('connection' in navigator) {
                const { connection } = navigator as any;
                // Preload only on fast connections
                if (connection.effectiveType === '4g' || connection.effectiveType === '3g') {
                    return this.preloadAfterDelay(load, 1000);
                }
            }
            // Fallback: preload after a delay
            return this.preloadAfterDelay(load, 2000);
        }
        return of(null);
    }

    private preloadAfterDelay(load: () => Observable<any>, delayMs: number): Observable<any> {
        return timer(delayMs).pipe(
            mergeMap(() => load()),
            catchError((error) => {
                console.warn('Skipping route preload after lazy module load failed.', error);
                return of(null);
            }),
        );
    }
}
