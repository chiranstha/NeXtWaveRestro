import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    CreateTenantInput,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    SubscribableEditionComboboxItemDto,
    TenantServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { EqualValidator } from '../../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../../shared/utils/validation/password-complexity-validator.directive';
import { BsDatepickerInputDirective, BsDatepickerDirective } from 'ngx-bootstrap/datepicker';
import { DatePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-picker-luxon-modifier.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'createTenantModal',
    templateUrl: './create-tenant-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        EqualValidator,
        PasswordComplexityValidator,
        BsDatepickerInputDirective,
        BsDatepickerDirective,
        DatePickerLuxonModifierDirective,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateTenantModalComponent extends AppComponentBase {
    private _tenantService = inject(TenantServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    private _profileService = inject(ProfileServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    @ViewChild('createModal', { static: true }) modal: ModalDirective;
    @ViewChild('tenantCreateForm') tenantCreateForm: any;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    isFormValid = false;
    setRandomPassword = true;
    useHostDb = true;
    editions: SubscribableEditionComboboxItemDto[] = [];
    tenant: CreateTenantInput;
    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    isUnlimited = false;
    isSubscriptionFieldsVisible = false;
    isSelectedEditionFree = false;
    tenantAdminPasswordRepeat = '';
    // Password visibility properties
    showPassword = false;
    showPasswordRepeat = false;
    passwordAnimation = false;
    passwordRepeatAnimation = false;
    originalPassword = '';
    originalPasswordRepeat = '';
    animationInterval: any;
    animationIntervalRepeat: any;
    shuffleIterations = 5; // Number of shuffles before revealing
    currentIteration = 0;
    currentIterationRepeat = 0;
    // Add validation state tracking
    isValidating = true;

    show() {
        this.active = true;
        this.init();
        this._profileService.getPasswordComplexitySetting().subscribe((result) => {
            this.passwordComplexitySetting = result.setting;
            this.modal.show();
        });
    }
    onShown(): void {
        document.getElementById('TenancyName').focus();
        if (this.tenantCreateForm) {
            this.tenantCreateForm.form.valueChanges.subscribe(() => {
                this.isFormValid = this.tenantCreateForm.form.valid;
            });
        }
    }
    init(): void {
        this.tenant = new CreateTenantInput();
        this.tenant.isActive = true;
        this.tenant.shouldChangePasswordOnNextLogin = true;
        this.tenant.sendActivationEmail = true;
        this.tenant.editionId = 0;
        this.tenant.isInTrialPeriod = false;
        this._commonLookupService.getEditionsForCombobox(false).subscribe((result) => {
            this.editions = result.items;
            const notAssignedItem = new SubscribableEditionComboboxItemDto();
            notAssignedItem.value = '';
            notAssignedItem.displayText = this.l('NotAssigned');
            this.editions.unshift(notAssignedItem);
            this._commonLookupService.getDefaultEditionName().subscribe((getDefaultEditionResult) => {
                const defaultEdition = this.editions.filter(
                    (edition) => edition.displayText === getDefaultEditionResult.name,
                );
                if (defaultEdition && defaultEdition[0]) {
                    this.tenant.editionId = parseInt(defaultEdition[0].value);
                    this.toggleSubscriptionFields();
                }
            });
        });
    }
    getEditionValue(item): number {
        return parseInt(item.value);
    }
    selectedEditionIsFree(): boolean {
        const selectedEditions = this.editions
            .filter((edition) => edition.value === this.tenant.editionId.toString())
            .map((u) => Object.assign(new SubscribableEditionComboboxItemDto(), u));
        if (selectedEditions.length !== 1) {
            this.isSelectedEditionFree = true;
        }
        const selectedEdition = selectedEditions[0];
        this.isSelectedEditionFree = selectedEdition.isFree;
        return this.isSelectedEditionFree;
    }
    subscriptionEndDateIsValid(): boolean {
        if (this.tenant.editionId <= 0) {
            return true;
        }
        if (this.isUnlimited) {
            return true;
        }
        if (!this.tenant.subscriptionEndDateUtc) {
            return false;
        }
        return this.tenant.subscriptionEndDateUtc !== undefined;
    }
    save(): void {
        this.saving = true;
        if (this.setRandomPassword) {
            this.tenant.adminPassword = null;
        }
        if (this.tenant.editionId === 0) {
            this.tenant.editionId = null;
        }
        if (this.isUnlimited) {
            this.tenant.isInTrialPeriod = false;
        }
        if (this.isUnlimited || this.tenant.editionId <= 0) {
            this.tenant.subscriptionEndDateUtc = null;
        } else {
            this.tenant.subscriptionEndDateUtc = this._dateTimeService.toUtcDate(this.tenant.subscriptionEndDateUtc);
        }
        this._tenantService
            .createTenant(this.tenant)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }
    close(): void {
        this.stopPasswordAnimation();
        this.stopPasswordRepeatAnimation();
        this.active = false;
        this.tenantAdminPasswordRepeat = '';
        this.modal.hide();
    }
    onEditionChange(): void {
        this.tenant.isInTrialPeriod = this.tenant.editionId > 0 && !this.selectedEditionIsFree();
        this.toggleSubscriptionFields();
    }
    toggleSubscriptionFields() {
        this.isSelectedEditionFree = this.selectedEditionIsFree();
        if (this.tenant.editionId <= 0 || this.isSelectedEditionFree) {
            this.isSubscriptionFieldsVisible = false;
            if (this.isSelectedEditionFree) {
                this.isUnlimited = true;
            } else {
                this.isUnlimited = false;
            }
        } else {
            this.isSubscriptionFieldsVisible = true;
        }
    }
    onIsUnlimitedChange() {
        if (this.isUnlimited) {
            this.tenant.isInTrialPeriod = false;
        }
    }
    togglePasswordVisibility(): void {
        if (this.showPassword) {
            // Hide password immediately
            this.showPassword = false;
            this.stopPasswordAnimation();
        } else {
            // Start the reveal animation
            this.showPassword = true;
            this.originalPassword = this.tenant.adminPassword;
            // Temporarily disable validation
            this.isValidating = false;
            this.startPasswordAnimation();
        }
    }
    togglePasswordRepeatVisibility(): void {
        if (this.showPasswordRepeat) {
            // Hide password immediately
            this.showPasswordRepeat = false;
            this.stopPasswordRepeatAnimation();
        } else {
            // Start the reveal animation
            this.showPasswordRepeat = true;
            this.originalPasswordRepeat = this.tenantAdminPasswordRepeat;
            // Temporarily disable validation
            this.isValidating = false;
            this.startPasswordRepeatAnimation();
        }
    }
    startPasswordAnimation(): void {
        this.passwordAnimation = true;
        this.currentIteration = 0;
        // Store original password
        const originalPassword = this.tenant.adminPassword;
        const passwordLength = originalPassword ? originalPassword.length : 0;
        if (passwordLength === 0) {
            this.passwordAnimation = false;
            return;
        }
        // Start rapid animation
        this.animationInterval = setInterval(() => {
            this.currentIteration++;
            if (this.currentIteration > this.shuffleIterations) {
                // Animation complete, show the real password
                this.tenant.adminPassword = originalPassword;
                this.stopPasswordAnimation();
                return;
            }
            // Generate a shuffled password with progressively more correct letters
            let shuffledPassword = '';
            const revealIndex = Math.floor((this.currentIteration / this.shuffleIterations) * passwordLength);
            for (let i = 0; i < passwordLength; i++) {
                if (i < revealIndex) {
                    // Show correct character at this position
                    shuffledPassword += originalPassword.charAt(i);
                } else {
                    // Show random character at this position
                    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()';
                    shuffledPassword += chars.charAt(Math.floor(Math.random() * chars.length));
                }
            }
            // Update the displayed password
            this.tenant.adminPassword = shuffledPassword;
        }, 50); // Fast animation speed - 50ms
    }
    startPasswordRepeatAnimation(): void {
        this.passwordRepeatAnimation = true;
        this.currentIterationRepeat = 0;
        // Store original password
        const originalPassword = this.tenantAdminPasswordRepeat;
        const passwordLength = originalPassword ? originalPassword.length : 0;
        if (passwordLength === 0) {
            this.passwordRepeatAnimation = false;
            return;
        }
        // Start rapid animation
        this.animationIntervalRepeat = setInterval(() => {
            this.currentIterationRepeat++;
            if (this.currentIterationRepeat > this.shuffleIterations) {
                // Animation complete, show the real password
                this.tenantAdminPasswordRepeat = originalPassword;
                this.stopPasswordRepeatAnimation();
                return;
            }
            // Generate a shuffled password with progressively more correct letters
            let shuffledPassword = '';
            const revealIndex = Math.floor((this.currentIterationRepeat / this.shuffleIterations) * passwordLength);
            for (let i = 0; i < passwordLength; i++) {
                if (i < revealIndex) {
                    // Show correct character at this position
                    shuffledPassword += originalPassword.charAt(i);
                } else {
                    // Show random character at this position
                    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()';
                    shuffledPassword += chars.charAt(Math.floor(Math.random() * chars.length));
                }
            }
            // Update the displayed password
            this.tenantAdminPasswordRepeat = shuffledPassword;
        }, 50); // Fast animation speed - 50ms
    }
    stopPasswordAnimation(): void {
        if (this.animationInterval) {
            clearInterval(this.animationInterval);
            this.animationInterval = null;
        }
        this.passwordAnimation = false;
        // Re-enable validation after animation completes
        this.isValidating = true;
    }
    stopPasswordRepeatAnimation(): void {
        if (this.animationIntervalRepeat) {
            clearInterval(this.animationIntervalRepeat);
            this.animationIntervalRepeat = null;
        }
        this.passwordRepeatAnimation = false;
        // Re-enable validation after animation completes
        this.isValidating = true;
    }
    // Clean up any animations when component is destroyed
    ngOnDestroy(): void {
        this.stopPasswordAnimation();
        this.stopPasswordRepeatAnimation();
        // Restore original passwords if component is destroyed during animation
        if (this.passwordAnimation && this.originalPassword) {
            this.tenant.adminPassword = this.originalPassword;
        }
        if (this.passwordRepeatAnimation && this.originalPasswordRepeat) {
            this.tenantAdminPasswordRepeat = this.originalPasswordRepeat;
        }
    }
}
