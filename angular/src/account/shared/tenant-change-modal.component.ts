import { Component, ElementRef, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountServiceProxy,
    IsTenantAvailableInput,
    IsTenantAvailableOutput,
    TenantAvailabilityState,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'tenantChangeModal',
    templateUrl: './tenant-change-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, NgClass, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class TenantChangeModalComponent extends AppComponentBase {
    private _accountService = inject(AccountServiceProxy);
    @ViewChild('tenantChangeModal', { static: true }) modal: ModalDirective;
    @ViewChild('tenancyNameInput', { static: true }) tenancyNameInput: ElementRef;
    tenancyName = '';
    tenancyNameBuffer = '';
    submitButtonText = '';
    isSwitchToTenant = false;
    active = false;
    saving = false;

    focusTenancyNameInput(): void {
        setTimeout(() => {
            document.getElementById('tenancyNameInput').focus();
        }, 100);
    }
    updateSubmitButtonText(): void {
        if (this.isSwitchToTenant) {
            this.submitButtonText = this.l('SwitchToTheTenant');
        } else {
            this.submitButtonText = this.l('SwitchToTheHost');
        }
    }
    show(tenancyName: string): void {
        this.tenancyName = tenancyName;
        this.isSwitchToTenant = tenancyName ? true : false;
        this.updateSubmitButtonText();
        this.active = true;
        this.modal.show();
    }
    onShown(): void {
        this.focusTenancyNameInput();
    }
    switchToTenant(e): void {
        if (e.target.checked) {
            this.tenancyName = this.tenancyNameBuffer;
            this.focusTenancyNameInput();
        } else {
            this.tenancyNameBuffer = this.tenancyName;
            this.tenancyName = '';
        }
        this.updateSubmitButtonText();
    }
    save(): void {
        if (!this.tenancyName) {
            abp.multiTenancy.setTenantIdCookie(undefined);
            this.close();
            location.reload();
            return;
        }
        const input = new IsTenantAvailableInput();
        input.tenancyName = this.tenancyName;
        this.saving = true;
        this._accountService
            .isTenantAvailable(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe((result: IsTenantAvailableOutput) => {
                switch (result.state) {
                    case TenantAvailabilityState.Available:
                        abp.multiTenancy.setTenantIdCookie(result.tenantId);
                        this.close();
                        location.reload();
                        return;
                    case TenantAvailabilityState.InActive:
                        this.message.warn(this.l('TenantIsNotActive', this.tenancyName));
                        this.focusTenancyNameInput();
                        break;
                    case TenantAvailabilityState.NotFound: //NotFound
                        this.message.warn(this.l('ThereIsNoTenantDefinedWithName{0}', this.tenancyName));
                        this.focusTenancyNameInput();
                        break;
                }
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
