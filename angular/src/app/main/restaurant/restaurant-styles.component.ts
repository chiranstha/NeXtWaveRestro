import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';

@Component({
    selector: 'restaurant-styles',
    template: '',
    styleUrl: './restaurant-shared.css',
    host: { class: 'd-none' },
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: true,
})
export class RestaurantStylesComponent {}
