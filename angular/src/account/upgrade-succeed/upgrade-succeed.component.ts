import { Component, Injector, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TenantRegistrationServiceProxy } from '@shared/service-proxies/service-proxies';
@Component({
    selector: 'upgrade-succeed',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './upgrade-succeed.component.html',
})
export class UpgradeSucceedComponent extends AppComponentBase implements OnInit {
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _tenantRegistrationService = inject(TenantRegistrationServiceProxy);
    constructor() {
        const _injector = inject(Injector);
        super();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        super.ngOnInit();
        const paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this._tenantRegistrationService.upgradeSucceed(paymentId).subscribe(() => {
            this._router.navigate([`${abp.appPath}app/admin/subscription-management`]);
        });
    }
}
