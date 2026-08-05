import { Directive, Input, forwardRef } from '@angular/core';
import { AbstractControl, NG_VALIDATORS, Validator, ValidationErrors } from '@angular/forms';
@Directive({
    selector: '[requireDigit],[requireLowercase],[requireNonAlphanumeric],[requireUppercase],[requiredLength]',
    providers: [{ provide: NG_VALIDATORS, useExisting: forwardRef(() => PasswordComplexityValidator), multi: true }],
})
export class PasswordComplexityValidator implements Validator {
    @Input('requireDigit') requireDigit: boolean;
    @Input('requireUppercase') requireUppercase: boolean;
    @Input('requireLowercase') requireLowercase: boolean;
    @Input('requireNonAlphanumeric') requireNonAlphanumeric: boolean;
    @Input('requiredLength') requiredLength: number;
    validate(control: AbstractControl): ValidationErrors | null {
        const givenPassword = control.value;
        const validationResult: ValidationErrors = {};
        const { requireDigit } = this;
        if (requireDigit && givenPassword && !/[0-9]/.test(givenPassword)) {
            validationResult.requireDigit = true;
        }
        const { requireUppercase } = this;
        if (requireUppercase && givenPassword && !/[A-Z]/.test(givenPassword)) {
            validationResult.requireUppercase = true;
        }
        const { requireLowercase } = this;
        if (requireLowercase && givenPassword && !/[a-z]/.test(givenPassword)) {
            validationResult.requireLowercase = true;
        }
        const { requiredLength } = this;
        if (requiredLength && givenPassword && givenPassword.length < requiredLength) {
            validationResult.requiredLength = true;
        }
        // use upperCaseLetters
        const { requireNonAlphanumeric } = this;
        if (requireNonAlphanumeric && givenPassword && /^[0-9a-zA-Z]+$/.test(givenPassword)) {
            validationResult.requireNonAlphanumeric = true;
        }
        return Object.keys(validationResult).length > 0 ? validationResult : null;
    }
}
