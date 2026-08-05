import { AppComponentBase } from '@shared/common/app-component-base';
import { Component, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { Subscription, timer } from 'rxjs';
@Component({ template: '', changeDetection: ChangeDetectionStrategy.Eager, standalone: false })
export class WidgetComponentBaseComponent extends AppComponentBase implements OnDestroy {
    delay = 300;
    timer: Subscription;

    /**
     * Run methods delayed. If runDelay called multiple time before its delay, only run last called.
     * @param method Method to call
     */
    runDelayed(method: () => void) {
        if (this.timer && !this.timer.closed) {
            this.timer.unsubscribe();
        }
        this.timer = timer(this.delay).subscribe(() => {
            method();
        });
    }
    ngOnDestroy(): void {
        if (this.timer && !this.timer.closed) {
            this.timer.unsubscribe();
        }
        super.ngOnDestroy();
    }
}
