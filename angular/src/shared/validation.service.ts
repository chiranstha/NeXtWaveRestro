import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
interface CasItem {
    marks?: number | string;
}
@Injectable({
    providedIn: 'root',
})
export class ValidationService {
    // Common validation patterns
    private readonly patterns = {
        name: /^[a-zA-Z\s\-']{2,50}$/,
        description: /^[a-zA-Z0-9\s\-',.()]{0,400}$/,
        positiveNumber: /^[0-9]+$/,
        decimalNumber: /^[0-9]+(\.[0-9]{1,2})?$/,
        percentage: /^(100(\.0{1,2})?|[1-9]?\d(\.\d{1,2})?)$/,
        alphanumeric: /^[a-zA-Z0-9\s\-_]+$/,
        phone: /^9[0-9]{9}$/,
    };
    // Error messages
    private readonly errorMessages = {
        required: (fieldName: string) => `${fieldName} is required`,
        min: (fieldName: string, min: number) => `${fieldName} must be at least ${min}`,
        max: (fieldName: string, max: number) => `${fieldName} must be at most ${max}`,
        minlength: (fieldName: string, minLength: number) => `${fieldName} must be at least ${minLength} characters`,
        maxlength: (fieldName: string, maxLength: number) => `${fieldName} must be at most ${maxLength} characters`,
        pattern: (fieldName: string) => `${fieldName} contains invalid characters`,
        email: 'Please enter a valid email address',
        positiveNumber: (fieldName: string) => `${fieldName} must be a positive number`,
        decimalNumber: (fieldName: string) => `${fieldName} must be a valid number`,
        percentage: (fieldName: string) => `${fieldName} must be a valid percentage (0-100)`,
        passMarksGreaterThanFull: (fieldName: string) => `${fieldName} cannot be greater than full marks`,
        negativeValue: (fieldName: string) => `${fieldName} cannot be negative`,
        duplicateValue: (fieldName: string) => `${fieldName} already exists`,
        phone: 'Please enter a valid phone number (10 digits starting with 9)',
        invalidRange: (fieldName: string) => `${fieldName} is outside the valid range`,
        serverError: 'An error occurred while processing your request',
        networkError: 'Network connection error. Please check your internet connection',
        unauthorized: 'You are not authorized to perform this action',
        forbidden: 'Access to this resource is forbidden',
        notFound: 'The requested resource was not found',
    };
    // Custom validators
    greaterThanZero(fieldName: string = 'Value'): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            const value = Number(control.value);
            return value <= 0 ? { greaterThanZero: { message: this.errorMessages.positiveNumber(fieldName) } } : null;
        };
    }
    minValue(min: number, fieldName: string = 'Value'): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            const value = Number(control.value);
            return value < min ? { minValue: { message: this.errorMessages.min(fieldName, min) } } : null;
        };
    }
    maxValue(max: number, fieldName: string = 'Value'): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            const value = Number(control.value);
            return value > max ? { maxValue: { message: this.errorMessages.max(fieldName, max) } } : null;
        };
    }
    passMarksLessThanFullMarks(fullMarksControlName: string, fieldName: string = 'Pass marks'): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            const formGroup = control.parent;
            if (!formGroup) {
                return null;
            }
            const fullMarksControl = formGroup.get(fullMarksControlName);
            if (!fullMarksControl?.value) {
                return null;
            }
            const passMarks = Number(control.value);
            const fullMarks = Number(fullMarksControl.value);
            return passMarks > fullMarks
                ? { passMarksGreaterThanFull: { message: this.errorMessages.passMarksGreaterThanFull(fieldName) } }
                : null;
        };
    }
    patternValidator(pattern: RegExp, fieldName: string): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            return pattern.test(control.value) ? null : { pattern: { message: this.errorMessages.pattern(fieldName) } };
        };
    }
    nameValidator(): ValidatorFn {
        return this.patternValidator(this.patterns.name, 'Name');
    }
    descriptionValidator(): ValidatorFn {
        return this.patternValidator(this.patterns.description, 'Description');
    }
    phoneValidator(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            return this.patterns.phone.test(control.value) ? null : { phone: { message: this.errorMessages.phone } };
        };
    }
    positiveIntegerValidator(fieldName: string = 'Value'): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            const value = Number(control.value);
            return Number.isInteger(value) && value > 0
                ? null
                : { positiveInteger: { message: this.errorMessages.positiveNumber(fieldName) } };
        };
    }
    percentageValidator(fieldName: string = 'Percentage'): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value) {
                return null;
            }
            const value = Number(control.value);
            return value >= 0 && value <= 100 ? null : { percentage: { message: this.errorMessages.percentage } };
        };
    }
    // Cross-field validators
    marksValidation(
        theoryFullMarks: string,
        theoryPassMarks: string,
        practicalFullMarks: string,
        practicalPassMarks: string,
    ): ValidatorFn {
        return (formGroup: AbstractControl): ValidationErrors | null => {
            const theoryFull = formGroup.get(theoryFullMarks)?.value;
            const theoryPass = formGroup.get(theoryPassMarks)?.value;
            const practicalFull = formGroup.get(practicalFullMarks)?.value;
            const practicalPass = formGroup.get(practicalPassMarks)?.value;
            const errors: ValidationErrors = {};
            if (theoryFull && theoryPass && Number(theoryPass) > Number(theoryFull)) {
                errors.theoryPassGreaterThanFull = {
                    message: 'Theory pass marks cannot be greater than theory full marks',
                };
            }
            if (practicalFull && practicalPass && Number(practicalPass) > Number(practicalFull)) {
                errors.practicalPassGreaterThanFull = {
                    message: 'Practical pass marks cannot be greater than practical full marks',
                };
            }
            return Object.keys(errors).length > 0 ? errors : null;
        };
    }
    // CAS marks validation
    casMarksTotalValidation(practicalFullMarks: string): ValidatorFn {
        return (formArray: AbstractControl): ValidationErrors | null => {
            if (!(formArray instanceof AbstractControl)) {
                return null;
            }
            const formGroup = formArray.parent;
            if (!formGroup) {
                return null;
            }
            const practicalFullMarksControl = formGroup.get(practicalFullMarks);
            if (!practicalFullMarksControl?.value) {
                return null;
            }
            const practicalFullMarksValue = Number(practicalFullMarksControl.value);
            let totalCasMarks = 0;
            // Calculate total CAS marks
            if (Array.isArray(formArray.value)) {
                formArray.value.forEach((casItem: CasItem) => {
                    if (casItem && casItem.marks) {
                        totalCasMarks += Number(casItem.marks || 0);
                    }
                });
            }
            if (totalCasMarks !== practicalFullMarksValue) {
                return {
                    casMarksTotalMismatch: {
                        message: `Total CAS marks (${totalCasMarks}) must equal practical full marks (${practicalFullMarksValue})`,
                    },
                };
            }
            return null;
        };
    }
    // Error message getters
    getErrorMessage(error: ValidationErrors, fieldName?: string): string {
        if (!error) {
            return '';
        }
        // Handle custom validation errors
        if (error.greaterThanZero) {
            return error.greaterThanZero.message;
        }
        if (error.minValue) {
            return error.minValue.message;
        }
        if (error.maxValue) {
            return error.maxValue.message;
        }
        if (error.passMarksGreaterThanFull) {
            return error.passMarksGreaterThanFull.message;
        }
        if (error.pattern) {
            return error.pattern.message;
        }
        if (error.positiveInteger) {
            return error.positiveInteger.message;
        }
        if (error.percentage) {
            return error.percentage.message;
        }
        if (error.theoryPassGreaterThanFull) {
            return error.theoryPassGreaterThanFull.message;
        }
        if (error.practicalPassGreaterThanFull) {
            return error.practicalPassGreaterThanFull.message;
        }
        if (error.casMarksTotalMismatch) {
            return error.casMarksTotalMismatch.message;
        }
        // Handle standard Angular validators
        if (error.required) {
            return this.errorMessages.required(fieldName || 'This field');
        }
        if (error.min) {
            return this.errorMessages.min(fieldName || 'Value', error.min.min);
        }
        if (error.max) {
            return this.errorMessages.max(fieldName || 'Value', error.max.max);
        }
        if (error.minlength) {
            return this.errorMessages.minlength(fieldName || 'Field', error.minlength.requiredLength);
        }
        if (error.maxlength) {
            return this.errorMessages.maxlength(fieldName || 'Field', error.maxlength.requiredLength);
        }
        if (error.pattern) {
            return this.errorMessages.pattern(fieldName || 'Field');
        }
        if (error.email) {
            return this.errorMessages.email;
        }
        return 'Invalid value';
    }
    // Server error handling
    handleServerError(error: HttpErrorResponse): string {
        if (!error) {
            return this.errorMessages.serverError;
        }
        if (error.status === 401) {
            return this.errorMessages.unauthorized;
        }
        if (error.status === 403) {
            return this.errorMessages.forbidden;
        }
        if (error.status === 404) {
            return this.errorMessages.notFound;
        }
        if (error.status >= 500) {
            return this.errorMessages.serverError;
        }
        if (!navigator.onLine) {
            return this.errorMessages.networkError;
        }
        // Try to extract error message from response
        if (error.error && typeof error.error === 'string') {
            return error.error;
        }
        if (error.error?.message) {
            return error.error.message;
        }
        if (error.message) {
            return error.message;
        }
        return this.errorMessages.serverError;
    }
    // Input sanitization
    sanitizeInput(input: string): string {
        if (!input) {
            return input;
        }
        return input
            .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '') // Remove script tags
            .replace(/<[^>]*>/g, '') // Remove HTML tags
            .trim();
    }
    // Validation helpers
    isFieldInvalid(control: AbstractControl): boolean {
        return control && control.invalid && (control.dirty || control.touched);
    }
    getFieldCssClass(control: AbstractControl): string {
        if (!control) {
            return '';
        }
        if (this.isFieldInvalid(control)) {
            return 'is-invalid';
        }
        if (control.valid && (control.dirty || control.touched)) {
            return 'is-valid';
        }
        return '';
    }
    // Async validation helper
    createAsyncValidator(validatorFn: (value: unknown) => Promise<boolean>, errorMessage: string): ValidatorFn {
        return (control: AbstractControl): Promise<ValidationErrors | null> => {
            if (!control.value) {
                return Promise.resolve(null);
            }
            return validatorFn(control.value).then((isValid) => {
                return isValid ? null : { asyncValidation: { message: errorMessage } };
            });
        };
    }
}
