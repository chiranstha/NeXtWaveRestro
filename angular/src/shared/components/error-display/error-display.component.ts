import { Component, Input, inject } from '@angular/core';
import { ValidationErrors } from '@angular/forms';
import { ValidationService } from '../../validation.service';
@Component({
    selector: 'app-error-display',
    templateUrl: './error-display.component.html',
    styleUrls: ['./error-display.component.css'],
})
export class ErrorDisplayComponent {
    private validationService = inject(ValidationService);
    @Input() errors: ValidationErrors;
    @Input() fieldName: string = '';
    @Input() customMessage: string = '';
    @Input() showIcon: boolean = true;
    constructor() {}
    get errorMessage(): string {
        if (this.customMessage) {
            return this.customMessage;
        }
        return this.validationService.getErrorMessage(this.errors, this.fieldName);
    }
    get hasErrors(): boolean {
        return this.errors && Object.keys(this.errors).length > 0;
    }
}
