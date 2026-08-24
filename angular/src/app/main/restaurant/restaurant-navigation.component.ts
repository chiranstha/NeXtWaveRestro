import { ChangeDetectionStrategy, Component, Injector } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    selector: 'restaurant-navigation',
    templateUrl: './restaurant-navigation.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
})
export class RestaurantNavigationComponent extends AppComponentBase {
    constructor(injector: Injector) {
        super(injector);
    }
}
