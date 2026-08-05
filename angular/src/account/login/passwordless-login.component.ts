import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    PasswordlessLoginProviderType,
    SendPasswordlessLoginCodeInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './passwordless-login.component.html',
    animations: [accountModuleAnimation()],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PasswordlessLoginComponent extends AppComponentBase implements OnInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    submitting = false;
    PasswordlessLoginProviderType = PasswordlessLoginProviderType;
    selectedPasswordlessLoginProvider: PasswordlessLoginProviderType;
    public selectedProviderInputValue: string;
    passwordlessLoginProviders: { value: PasswordlessLoginProviderType; text: string }[];

    get isEmailPasswordlessLoginEnabled(): boolean {
        return this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled');
    }
    get isSmsPasswordlessLoginEnabled(): boolean {
        return this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled');
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.passwordlessLoginProviders = Object.keys(PasswordlessLoginProviderType)
            .filter((key) => isNaN(Number(key)))
            .map((value) => ({
                value: PasswordlessLoginProviderType[value],
                text: this.getPasswordlessProviderText(PasswordlessLoginProviderType[value]),
            }));
        if (this.isEmailPasswordlessLoginEnabled) {
            this.selectedPasswordlessLoginProvider = PasswordlessLoginProviderType.Email;
        } else {
            this.selectedPasswordlessLoginProvider = PasswordlessLoginProviderType.Sms;
        }
    }
    submit(): void {
        const model = new SendPasswordlessLoginCodeInput();
        model.providerValue = this.selectedProviderInputValue;
        model.providerType = this.selectedPasswordlessLoginProvider; // Cast the value to 'PasswordlessProviderType'
        this._accountService
            .sendPasswordlessLoginCode(model)
            .pipe(finalize(() => (this.submitting = false)))
            .subscribe(() => {
                this._router.navigate(['account/verify-passwordless-login'], {
                    queryParams: {
                        selectedProviderInputValue: this.selectedProviderInputValue,
                        selectedProvider: this.passwordlessLoginProviders.find(
                            (p) => p.value === this.selectedPasswordlessLoginProvider,
                        ).text,
                    },
                });
            });
    }
    getPasswordlessProviderText(providerType: PasswordlessLoginProviderType): string {
        const key = Object.keys(PasswordlessLoginProviderType).find(
            (k) => PasswordlessLoginProviderType[k] === providerType,
        );
        return key ? key : '';
    }
}
