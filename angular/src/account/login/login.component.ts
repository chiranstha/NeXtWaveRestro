import { AbpSessionService } from 'abp-ng2-module';
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SessionServiceProxy, UpdateUserSignInTokenOutput } from '@shared/service-proxies/service-proxies';
import { UrlHelper } from 'shared/helpers/UrlHelper';
import { ExternalLoginProvider, LoginService } from './login.service';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../shared/utils/auto-focus.directive';
import { ValidationMessagesComponent } from '../../shared/utils/validation-messages.component';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './login.component.html',
    animations: [accountModuleAnimation()],
    styleUrls: ['./login.component.less'],
    imports: [FormsModule, AutoFocusDirective, ValidationMessagesComponent, NgClass, RouterLink, LocalizePipe],
    schemas: [NO_ERRORS_SCHEMA],
})
export class LoginComponent extends AppComponentBase implements OnInit, AfterViewInit, OnDestroy {
    loginService = inject(LoginService);
    private _sessionService = inject(AbpSessionService);
    private _sessionAppService = inject(SessionServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    @ViewChild('passwordToggle') passwordToggle?: ElementRef<HTMLElement>;
    private readonly mouseMoveListener = (event: MouseEvent): void => this.trackMousePosition(event);
    private readonly shuffleCharacters = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*';
    private readonly shuffleIterations = 12;
    private readonly shuffleIntervalMs = 42;
    submitting = false;
    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    showPassword = false;
    passwordAnimation = false;
    passwordShuffleText = '';
    eyePositionX = 50;
    eyePositionY = 30;
    private animationInterval: number | null = null;
    private animationTimeout: number | null = null;
    private eyeTrackingFrame: number | null = null;
    private latestPointer: { clientX: number; clientY: number } | null = null;

    get hasPasswordValue(): boolean {
        return !!this.loginService.authenticateModel.password?.length;
    }

    trackMousePosition(event: MouseEvent): void {
        if (!this.showPassword && !this.passwordAnimation) {
            return;
        }

        this.latestPointer = {
            clientX: event.clientX,
            clientY: event.clientY,
        };

        if (this.eyeTrackingFrame !== null) {
            return;
        }

        this.eyeTrackingFrame = window.requestAnimationFrame(() => {
            this.eyeTrackingFrame = null;
            if (!this.latestPointer) {
                return;
            }

            this.updateEyePosition(this.latestPointer.clientX, this.latestPointer.clientY);
        });
    }
    constructor() {
        super();
    }
    get multiTenancySideIsTeanant(): boolean {
        return this._sessionService.tenantId > 0;
    }
    get isTenantSelfRegistrationAllowed(): boolean {
        return this.setting.getBoolean('App.TenantManagement.AllowSelfRegistration');
    }
    get isSelfRegistrationAllowed(): boolean {
        if (!this._sessionService.tenantId) {
            return false;
        }
        return this.setting.getBoolean('App.UserManagement.AllowSelfRegistration');
    }
    get isPasswordlessLoginEnabled(): boolean {
        return (
            this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled') ||
            this.setting.getBoolean('App.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled')
        );
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        super.ngOnInit();
        if (this._sessionService.userId > 0 && UrlHelper.getReturnUrl() && UrlHelper.getSingleSignIn()) {
            this._sessionAppService.updateUserSignInToken().subscribe((result: UpdateUserSignInTokenOutput) => {
                const initialReturnUrl = UrlHelper.getReturnUrl();
                const returnUrl = `${initialReturnUrl + (initialReturnUrl.indexOf('?') >= 0 ? '&' : '?')}accessToken=${
                    result.signInToken
                }&userId=${result.encodedUserId}&tenantId=${result.encodedTenantId}`;
                location.href = returnUrl;
            });
        }
        PasswordMeterComponent.bootstrap();
        this.handleExternalLoginCallbacks();
    }
    ngAfterViewInit(): void {
        this._recaptchaWrapperService.setCaptchaVisibilityOnLogin();
        if (typeof document !== 'undefined') {
            document.addEventListener('mousemove', this.mouseMoveListener);
        }
    }
    handleExternalLoginCallbacks(): void {
        const { state } = UrlHelper.getQueryParametersUsingHash();
        const queryParameters = UrlHelper.getQueryParameters();
        if (state && state.indexOf('openIdConnect') >= 0) {
            this.loginService.openIdConnectLoginCallback({});
        }
        if (queryParameters.state && queryParameters.state.indexOf('openIdConnect') >= 0) {
            this.loginService.openIdConnectLoginCallback({});
        }
        if (queryParameters.twitter && queryParameters.twitter === '1') {
            const parameters = UrlHelper.getQueryParameters();
            const token = parameters['oauth_token'];
            const verifier = parameters['oauth_verifier'];
            this.loginService.twitterLoginCallback(token, verifier);
        }
    }
    login(): void {
        const recaptchaCallback = (token: string) => {
            this.showMainSpinner();
            this.submitting = true;
            this.loginService.authenticate(
                () => {
                    this.submitting = false;
                    this.hideMainSpinner();
                },
                null,
                token,
            );
        };
        if (this._recaptchaWrapperService.useCaptchaOnLogin()) {
            this._recaptchaWrapperService.execute('login').subscribe((token) => recaptchaCallback(token));
        } else {
            recaptchaCallback(null);
        }
    }
    externalLogin(provider: ExternalLoginProvider) {
        this.loginService.externalAuthenticate(provider);
    }
    togglePasswordVisibility(): void {
        const password = this.loginService.authenticateModel.password;

        if (!password?.length) {
            return;
        }

        const shouldReveal = !this.showPassword;
        this.stopPasswordAnimation(false);
        this.showPassword = shouldReveal;

        if (shouldReveal && this.latestPointer) {
            this.updateEyePosition(this.latestPointer.clientX, this.latestPointer.clientY);
        } else {
            this.resetEyePosition();
        }

        this.startPasswordAnimation(password, shouldReveal);
        this.markViewForCheck();
    }

    startPasswordAnimation(password: string, isReveal: boolean = true): void {
        this.passwordAnimation = true;
        this.passwordShuffleText = this.buildPasswordShuffleFrame(password, isReveal, 0);
        let currentIteration = 0;

        this.animationInterval = window.setInterval(() => {
            currentIteration = Math.min(this.shuffleIterations, currentIteration + 1);
            this.passwordShuffleText = this.buildPasswordShuffleFrame(password, isReveal, currentIteration);

            if (currentIteration >= this.shuffleIterations) {
                this.passwordShuffleText = isReveal ? password : this.maskPassword(password);
                this.animationTimeout = window.setTimeout(() => this.stopPasswordAnimation(), 90);
                window.clearInterval(this.animationInterval);
                this.animationInterval = null;
            }

            this.markViewForCheck();
        }, this.shuffleIntervalMs);
    }

    stopPasswordAnimation(clearOverlay: boolean = true): void {
        if (this.animationInterval !== null) {
            window.clearInterval(this.animationInterval);
            this.animationInterval = null;
        }

        if (this.animationTimeout !== null) {
            window.clearTimeout(this.animationTimeout);
            this.animationTimeout = null;
        }

        this.passwordAnimation = false;

        if (clearOverlay) {
            this.passwordShuffleText = '';
        }

        this.markViewForCheck();
    }

    private updateEyePosition(clientX: number, clientY: number): void {
        const toggleElement = this.passwordToggle?.nativeElement;

        if (!toggleElement || (!this.showPassword && !this.passwordAnimation)) {
            return;
        }

        const rect = toggleElement.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;
        const deltaX = clientX - centerX;
        const deltaY = clientY - centerY;
        const distance = Math.sqrt(deltaX * deltaX + deltaY * deltaY) || 1;
        const intensity = Math.min(distance / 220, 1);
        const maxHorizontalMove = 12;
        const maxVerticalMove = 7;

        this.eyePositionX = 50 + (deltaX / distance) * maxHorizontalMove * intensity;
        this.eyePositionY = 30 + (deltaY / distance) * maxVerticalMove * intensity;
        this.markViewForCheck();
    }

    private resetEyePosition(): void {
        this.eyePositionX = 50;
        this.eyePositionY = 30;
        this.markViewForCheck();
    }

    private buildPasswordShuffleFrame(password: string, isReveal: boolean, iteration: number): string {
        const passwordCharacters = Array.from(password);
        const progress = Math.min(iteration / this.shuffleIterations, 1);
        const fixedCharacters = Math.floor(passwordCharacters.length * progress);

        return passwordCharacters
            .map((character, index) => {
                if (isReveal && index < fixedCharacters) {
                    return character;
                }

                if (!isReveal && index < passwordCharacters.length - fixedCharacters) {
                    return character;
                }

                if (!isReveal && progress > 0.72) {
                    return '*';
                }

                return this.getRandomShuffleCharacter();
            })
            .join('');
    }

    private getRandomShuffleCharacter(): string {
        return this.shuffleCharacters.charAt(Math.floor(Math.random() * this.shuffleCharacters.length));
    }

    private maskPassword(password: string): string {
        return Array.from(password)
            .map(() => '*')
            .join('');
    }

    ngOnDestroy(): void {
        this.stopPasswordAnimation();

        if (this.eyeTrackingFrame !== null) {
            window.cancelAnimationFrame(this.eyeTrackingFrame);
            this.eyeTrackingFrame = null;
        }

        if (typeof document !== 'undefined') {
            document.removeEventListener('mousemove', this.mouseMoveListener);
        }

        super.ngOnDestroy();
    }
}
