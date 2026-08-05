import { AbpSessionService } from 'abp-ng2-module';
import {
    AfterViewInit,
    Component,
    OnInit,
    OnDestroy,
    ViewChild,
    inject,
    ChangeDetectorRef,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SessionServiceProxy, UpdateUserSignInTokenOutput } from '@shared/service-proxies/service-proxies';
import { UrlHelper } from 'shared/helpers/UrlHelper';
import { ExternalLoginProvider, LoginService } from './login.service';
import { ReCaptchaV3WrapperService } from '@account/shared/recaptchav3-wrapper.service';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { NgForm, FormsModule } from '@angular/forms';
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
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class LoginComponent extends AppComponentBase implements OnInit, AfterViewInit, OnDestroy {
    loginService = inject(LoginService);
    private _sessionService = inject(AbpSessionService);
    private _sessionAppService = inject(SessionServiceProxy);
    private _recaptchaWrapperService = inject(ReCaptchaV3WrapperService);
    private cdr = inject(ChangeDetectorRef);
    @ViewChild('loginForm') loginForm: NgForm;
    submitting = false;
    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    showPassword = false;
    passwordAnimation = false;
    originalPassword = '';
    shuffleIterations = 10; // Reduced from 10 to 5 for faster animation
    currentIteration = 0;
    revealTimeout: number | null = null;
    isLoginButtonDisabled = true;
    // Eye tracking properties
    eyePositionX = 50; // Default position (center)
    eyePositionY = 30; // Default position (center)
    isTrackingMouse = false;
    animationInterval: number;
    /**
     * Tracks the mouse position and updates the eye position
     */
    trackMousePosition(event: MouseEvent): void {
        if (this.showPassword) {
            // Calculate eye position based on mouse coordinates
            // Increased movement range for more noticeable and dynamic tracking
            const maxHorizontalMove = 15;
            const maxVerticalMove = 8;
            // Get window dimensions
            const windowWidth = window.innerWidth;
            const windowHeight = window.innerHeight;
            // Calculate normalized position (0-1)
            const normalizedX = Math.min(Math.max(event.clientX / windowWidth, 0), 1);
            const normalizedY = Math.min(Math.max(event.clientY / windowHeight, 0), 1);
            // Map to eye movement range with smoother tracking
            // Use an easing function for more natural movement
            const easeInOutX = this.easeInOutQuad(normalizedX);
            const easeInOutY = this.easeInOutQuad(normalizedY);
            // Apply the eased values to eye position
            this.eyePositionX = 50 + (easeInOutX - 0.5) * maxHorizontalMove * 2;
            this.eyePositionY = 30 + (easeInOutY - 0.5) * maxVerticalMove * 2;
        }
    }
    /**
     * Easing function to make eye movement more natural
     */
    easeInOutQuad(t: number): number {
        return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
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
        // Setup mouse tracking for eye movement
        if (typeof document !== 'undefined') {
            document.addEventListener('mousemove', this.trackMousePosition.bind(this));
        }
        // Track form validity changes
        if (this.loginForm) {
            this.loginForm.form.valueChanges.subscribe(() => {
                this.isLoginButtonDisabled = !this.loginForm.form.valid;
            });
            // Set initial state
            this.isLoginButtonDisabled = !this.loginForm.form.valid;
            this.cdr.detectChanges();
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
        // Don't do anything if password field is empty
        if (
            !this.loginService.authenticateModel.password ||
            this.loginService.authenticateModel.password.length === 0
        ) {
            return;
        }
        // Store original password
        this.originalPassword = this.loginService.authenticateModel.password;
        if (this.showPassword) {
            // Start the mask animation (hide password)
            this.showPassword = false;
            this.startPasswordAnimation(false);
            // Reset eye position when hiding password
            this.eyePositionX = 50;
            this.eyePositionY = 30;
        } else {
            // Start the reveal animation (show password)
            this.showPassword = true;
            this.startPasswordAnimation(true);
            // Initial position for eye based on current mouse position if available
            if (typeof document !== 'undefined' && document.querySelector('.password-toggle')) {
                const passwordToggle = document.querySelector('.password-toggle');
                if (passwordToggle) {
                    const rect = passwordToggle.getBoundingClientRect();
                    const x = rect.left + rect.width / 2;
                    const y = rect.top + rect.height / 2;
                    // Simulate a mouse event at the eye's location
                    this.trackMousePosition({
                        clientX: x,
                        clientY: y,
                    } as MouseEvent);
                }
            }
        }
    }
    startPasswordAnimation(isReveal: boolean = true): void {
        this.passwordAnimation = true;
        this.currentIteration = 0;
        // Store original password
        const { originalPassword } = this;
        const passwordLength = originalPassword.length;
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()';
        // Clear any existing animation
        window.clearInterval(this.animationInterval);
        // Start rapid animation
        this.animationInterval = window.setInterval(() => {
            this.currentIteration++;
            if (this.currentIteration > this.shuffleIterations) {
                // Animation complete, show the final state
                this.loginService.authenticateModel.password = originalPassword;
                this.stopPasswordAnimation();
                return;
            }
            // Generate a shuffled password with progressively more correct or masked letters
            let shuffledPassword = '';
            const progress = this.currentIteration / this.shuffleIterations;
            if (isReveal) {
                // Revealing animation (mask to text) - left to right
                const revealIndex = Math.floor(progress * passwordLength);
                for (let i = 0; i < passwordLength; i++) {
                    if (i < revealIndex) {
                        // Show correct character at this position
                        shuffledPassword += originalPassword.charAt(i);
                    } else {
                        // Show random character at this position - gradually revealing from left to right
                        const randomness = Math.max(0, 1 - (i - revealIndex) / (passwordLength / 3));
                        if (Math.random() < randomness * 0.2) {
                            // Occasionally show the correct character as a teaser
                            shuffledPassword += originalPassword.charAt(i);
                        } else {
                            shuffledPassword += chars.charAt(Math.floor(Math.random() * chars.length));
                        }
                    }
                }
            } else {
                // Masking animation (text to mask) - right to left
                const maskIndex = Math.floor(progress * passwordLength);
                for (let i = 0; i < passwordLength; i++) {
                    if (i < passwordLength - maskIndex) {
                        // Still show the correct character
                        shuffledPassword += originalPassword.charAt(i);
                    } else {
                        // Show random character for masking effect - with increasing randomness
                        const randomChance = Math.min(
                            1,
                            (i - (passwordLength - maskIndex)) / (passwordLength / 3) + 0.2,
                        );
                        if (Math.random() < randomChance * 0.7) {
                            // Use special chars more often during masking for dramatic effect
                            const specialChars = '!@#$%^&*()_+-={}[]|:;<>,.?/';
                            shuffledPassword += specialChars.charAt(Math.floor(Math.random() * specialChars.length));
                        } else {
                            shuffledPassword += chars.charAt(Math.floor(Math.random() * chars.length));
                        }
                    }
                }
            }
            // Update the displayed password
            this.loginService.authenticateModel.password = shuffledPassword;
        }, 40); // Even faster animation for a smoother effect
    }
    stopPasswordAnimation(): void {
        if (this.animationInterval) {
            window.clearInterval(this.animationInterval);
            this.animationInterval = null;
        }
        // Use a short timeout to allow the final animation state to be visible before removing the animation class
        setTimeout(() => {
            this.passwordAnimation = false;
        }, 100);
        // Ensure the password is restored to its original value
        if (this.originalPassword) {
            this.loginService.authenticateModel.password = this.originalPassword;
        }
    }
    ngOnDestroy(): void {
        this.stopPasswordAnimation();
        // If the component is destroyed while animating, restore the original password
        if (this.passwordAnimation && this.originalPassword) {
            this.loginService.authenticateModel.password = this.originalPassword;
        }
        // Remove event listener for mouse tracking
        if (typeof document !== 'undefined') {
            document.removeEventListener('mousemove', this.trackMousePosition.bind(this));
        }
    }
}
