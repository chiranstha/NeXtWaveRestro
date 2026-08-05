import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
@Component({
    selector: 'subscription-notification-bar',
    templateUrl: './subscription-notification-bar.component.html',
    imports: [NgClass, RouterLink],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class SubscriptionNotificationBarComponent extends ThemesLayoutBaseComponent {
    @Input() customStyle =
        'btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px position-relative';
}
