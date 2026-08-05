import { Component, Input } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { NgClass } from '@angular/common';
@Component({
    selector: 'app-loading-spinner',
    templateUrl: './loading-spinner.component.html',
    styleUrls: ['./loading-spinner.component.css'],
    imports: [NgClass],
    schemas: [NO_ERRORS_SCHEMA],
})
export class LoadingSpinnerComponent {
    @Input() isLoading: boolean = false;
    @Input() message: string = 'Loading...';
    @Input() size: 'small' | 'medium' | 'large' = 'medium';
    @Input() showMessage: boolean = true;
}
