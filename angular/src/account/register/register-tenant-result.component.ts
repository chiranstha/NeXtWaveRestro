import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Router } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppUrlService } from '@shared/common/nav/app-url.service';
import { RegisterTenantOutput } from '@shared/service-proxies/service-proxies';
import { TenantRegistrationHelperService } from './tenant-registration-helper.service';
import { SubdomainTenancyNameFinder } from '@shared/helpers/SubdomainTenancyNameFinder';
import { AppConsts } from '@shared/AppConsts';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './register-tenant-result.component.html',
    animations: [accountModuleAnimation()],
    imports: [LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class RegisterTenantResultComponent extends AppComponentBase implements OnInit {
    private _router = inject(Router);
    private _appUrlService = inject(AppUrlService);
    private _tenantRegistrationHelper = inject(TenantRegistrationHelperService);
    model: RegisterTenantOutput = new RegisterTenantOutput();
    tenantUrl: string;
    saving = false;

    ngOnInit() {
        if (!this._tenantRegistrationHelper.registrationResult) {
            this._router.navigate(['account/login']);
            return;
        }
        this.model = this._tenantRegistrationHelper.registrationResult;
        if (!new SubdomainTenancyNameFinder().urlHasTenancyNamePlaceholder(AppConsts.remoteServiceBaseUrlFormat)) {
            abp.multiTenancy.setTenantIdCookie(this.model.tenantId);
        }
        this.tenantUrl = this._appUrlService.getAppRootUrlOfTenant(this.model.tenancyName);
    }
}
