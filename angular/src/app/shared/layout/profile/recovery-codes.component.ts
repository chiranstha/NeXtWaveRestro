import { Component, ViewChild, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UpdateGoogleAuthenticatorKeyOutput } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'recoveryCodesComponent',
    templateUrl: './recovery-codes.component.html',
    imports: [LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class RecoveryCodesComponent extends AppComponentBase implements OnInit {
    @ViewChild('recoveryCodesComponent', { static: true }) recoveryCodesComponent: ModalDirective;
    public model: UpdateGoogleAuthenticatorKeyOutput;

    ngOnInit(): void {
        this.model = new UpdateGoogleAuthenticatorKeyOutput();
    }
}
