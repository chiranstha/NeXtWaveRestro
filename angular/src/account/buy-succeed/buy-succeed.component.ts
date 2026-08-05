import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TenantRegistrationServiceProxy } from '@shared/service-proxies/service-proxies';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'payment-completed',
    templateUrl: './buy-succeed.component.html',
    imports: [LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class BuySucceedComponent extends AppComponentBase implements OnInit {
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _tenantRegistrationService = inject(TenantRegistrationServiceProxy);
    constructor() {
        super();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        const { paymentId } = this._activatedRoute.snapshot.queryParams;
        this._tenantRegistrationService.buyNowSucceed(paymentId).subscribe(() => {
            this._router.navigate([`${abp.appPath}app/admin/subscription-management`]);
        });
    }
}
