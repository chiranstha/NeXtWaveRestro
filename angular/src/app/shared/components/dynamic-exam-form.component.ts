import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { FormValidatorsService } from '@shared/common/form-validators.service';
export interface DynamicFormField {
    name: string;
    label: string;
    type: 'text' | 'email' | 'password' | 'number' | 'select' | 'textarea';
    required?: boolean;
    minLength?: number;
    maxLength?: number;
    pattern?: string;
    options?: { value: any; label: string }[];
    placeholder?: string;
    validation?: {
        custom?: string[];
        async?: string[];
    };
}
export interface DynamicFormConfig {
    fields: DynamicFormField[];
    submitButtonText?: string;
    cancelButtonText?: string;
}
@Component({
    selector: 'app-dynamic-exam-form',
    standalone: true,
    imports: [ReactiveFormsModule, NgSelectModule],
    template: `
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="dynamic-form w-100">
            @for (field of config.fields; track field) {
                <div class="mb-3">
                    <label [for]="field.name" class="form-label d-block mb-2 fw-medium">
                        {{ field.label }}
                        @if (field.required) {
                            <span class="text-danger">*</span>
                        }
                    </label>
                    <!-- Text Input -->
                    @if (field.type === 'text' || field.type === 'email' || field.type === 'password') {
                        <input
                            [id]="field.name"
                            [type]="field.type"
                            [formControlName]="field.name"
                            [placeholder]="field.placeholder || ''"
                            class="form-control"
                            [class.is-invalid]="isFieldInvalid(field.name)"
                            [attr.aria-describedby]="field.name + '-error'"
                            [attr.aria-required]="field.required"
                        />
                    }
                    <!-- Number Input -->
                    @if (field.type === 'number') {
                        <input
                            [id]="field.name"
                            type="number"
                            [formControlName]="field.name"
                            [placeholder]="field.placeholder || ''"
                            class="form-control"
                            [class.is-invalid]="isFieldInvalid(field.name)"
                            [attr.aria-describedby]="field.name + '-error'"
                            [attr.aria-required]="field.required"
                        />
                    }
                    <!-- Textarea -->
                    @if (field.type === 'textarea') {
                        <textarea
                            [id]="field.name"
                            [formControlName]="field.name"
                            [placeholder]="field.placeholder || ''"
                            class="form-control"
                            [class.is-invalid]="isFieldInvalid(field.name)"
                            [attr.aria-describedby]="field.name + '-error'"
                            [attr.aria-required]="field.required"
                            rows="3"
                        ></textarea>
                    }
                    <!-- Select Dropdown -->
                    @if (field.type === 'select') {
                        <ng-select
                            [id]="field.name"
                            [formControlName]="field.name"
                            [placeholder]="field.placeholder || ''"
                            [items]="field.options"
                            bindLabel="label"
                            bindValue="value"
                            [class.is-invalid]="isFieldInvalid(field.name)"
                            [attr.aria-describedby]="field.name + '-error'"
                            [attr.aria-required]="field.required"
                        ></ng-select>
                    }
                    <!-- Error Messages -->
                    @if (isFieldInvalid(field.name)) {
                        <div [id]="field.name + '-error'" class="invalid-feedback d-block" role="alert" aria-live="polite">
                            @for (error of getFieldErrors(field.name); track error) {
                                <div>
                                    {{ error }}
                                </div>
                            }
                        </div>
                    }
                </div>
            }
            <!-- Form Actions -->
            <div class="mt-5 pt-3 border-top">
                <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting">
                    {{ config.submitButtonText || 'Submit' }}
                </button>
                <button type="button" class="btn btn-secondary ms-2" (click)="onCancel()" [disabled]="isSubmitting">
                    {{ config.cancelButtonText || 'Cancel' }}
                </button>
            </div>
        </form>
    `,
    styles: [
        `
            .dynamic-form {
                max-width: 600px;
            }
        `,
    ],
})
export class DynamicExamFormComponent implements OnInit {
    @Input() config!: DynamicFormConfig;
    @Input() initialData?: any;
    @Input() isSubmitting = false;
    @Output() formSubmit = new EventEmitter<any>();
    @Output() formCancel = new EventEmitter<void>();
    form!: FormGroup;

    private fb = inject(FormBuilder);
    private validatorsService = inject(FormValidatorsService);
    ngOnInit() {
        this.buildForm();
    }
    private buildForm() {
        const formControls: any = {};
        this.config.fields.forEach((field) => {
            const validators = this.buildValidators(field);
            formControls[field.name] = [this.initialData?.[field.name] || '', validators];
        });
        this.form = this.fb.group(formControls);
    }
    private buildValidators(field: DynamicFormField) {
        const validators = [];
        if (field.required) {
            validators.push(this.validatorsService.required());
        }
        if (field.minLength) {
            validators.push(this.validatorsService.minLength(field.minLength));
        }
        if (field.maxLength) {
            validators.push(Validators.maxLength(field.maxLength));
        }
        if (field.pattern) {
            validators.push(Validators.pattern(field.pattern));
        }
        if (field.type === 'email') {
            validators.push(this.validatorsService.email());
        }
        if (field.type === 'password' && field.validation?.custom?.includes('passwordStrength')) {
            validators.push(this.validatorsService.passwordStrength());
        }
        // Add async validators
        if (field.validation?.async?.includes('examNameAvailable')) {
            // For demo purposes, we'll add a simple async validator
            // In real usage, you'd pass additional context like excludeId
            validators.push(this.validatorsService.examNameAvailable());
        }
        return validators;
    }
    isFieldInvalid(fieldName: string): boolean {
        const control = this.form.get(fieldName);
        return !!(control && control.invalid && (control.dirty || control.touched));
    }
    getFieldErrors(fieldName: string): string[] {
        const control = this.form.get(fieldName);
        if (!control?.errors) {
            return [];
        }
        const errors = [];
        const fieldConfig = this.config.fields.find((f) => f.name === fieldName);
        if (control.errors['required']) {
            errors.push(`${fieldConfig?.label || fieldName} is required`);
        }
        if (control.errors['minlength']) {
            errors.push(
                `${fieldConfig?.label || fieldName} must be at least ${control.errors['minlength'].requiredLength} characters`,
            );
        }
        if (control.errors['maxlength']) {
            errors.push(
                `${fieldConfig?.label || fieldName} cannot exceed ${control.errors['maxlength'].requiredLength} characters`,
            );
        }
        if (control.errors['email']) {
            errors.push('Please enter a valid email address');
        }
        if (control.errors['pattern']) {
            errors.push(`${fieldConfig?.label || fieldName} format is invalid`);
        }
        if (control.errors['passwordStrength']) {
            errors.push(
                'Password must contain at least 8 characters with uppercase, lowercase, number, and special character',
            );
        }
        if (control.errors['examNameAvailable']) {
            errors.push('This exam name is already taken');
        }
        return errors;
    }
    onSubmit() {
        if (this.form.valid) {
            this.formSubmit.emit(this.form.value);
        } else {
            // Mark all fields as touched to show validation errors
            Object.keys(this.form.controls).forEach((key) => {
                this.form.get(key)?.markAsTouched();
            });
        }
    }
    onCancel() {
        this.formCancel.emit();
    }
}
