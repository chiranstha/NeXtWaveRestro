import { AfterViewInit, Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountServiceProxy, SendEmailActivationLinkInput } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './email-activation.component.html',
    animations: [accountModuleAnimation()],
    imports: [
        FormsModule,
        ReactiveFormsModule,
        ValidationMessagesComponent,
        RouterLink,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EmailActivationComponent extends AppComponentBase implements AfterViewInit, OnInit {
    private _accountService = inject(AccountServiceProxy);
    private _router = inject(Router);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    private _fb = inject(FormBuilder);
    emailActivationForm: FormGroup;
    model: SendEmailActivationLinkInput = new SendEmailActivationLinkInput();
    saving = false;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.createForm();
    }
    createForm(): void {
        this.emailActivationForm = this._fb.group({
            emailAddress: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
        });
    }
    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnEmailActivation();
    }
    save(): void {
        const recaptchaCallback = (token: string) => {
            this.saving = true;
            this.model.emailAddress = this.emailActivationForm.value.emailAddress;
            this.model.captchaResponse = token;
            this._accountService
                .sendEmailActivationLink(this.model)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe(() => {
                    this.message
                        .success(this.l('ActivationMailSentIfEmailAssociatedMessage'), this.l('MailSent'))
                        .then(() => {
                            this._router.navigate(['account/login']);
                        });
                });
        };
        if (this._recaptchaWrapperService.useCaptchaOnEmailActivation()) {
            this._recaptchaWrapperService.execute('emailActivation').subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
}
