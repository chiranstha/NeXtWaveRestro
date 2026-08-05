import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Output,
    ViewChild,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrUpdateUserInput,
    OrganizationUnitDto,
    PasswordComplexitySetting,
    ProfileServiceProxy,
    UserEditDto,
    UserRoleDto,
    UserServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import {
    IOrganizationUnitsTreeComponentData,
    OrganizationUnitsTreeComponent,
} from '../shared/organization-unit-tree.component';
import { map as _map, filter as _filter } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { PasswordMeterComponent } from '@metronic/app/kt/components';
import { AbpSessionService } from 'abp-ng2-module';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule, NgForm } from '@angular/forms';
import { TabsetComponent, TabDirective, TabHeadingDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { EqualValidator } from '../../../shared/utils/validation/equal-validator.directive';
import { PasswordComplexityValidator } from '../../../shared/utils/validation/password-complexity-validator.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { ChangeProfilePictureModalComponent } from '../../shared/layout/profile/change-profile-picture-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'createOrEditUserModal',
    templateUrl: './create-or-edit-user-modal.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['create-or-edit-user-modal.component.less'],
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        TooltipDirective,
        EqualValidator,
        PasswordComplexityValidator,
        TabHeadingDirective,
        OrganizationUnitsTreeComponent,
        ButtonBusyDirective,
        ChangeProfilePictureModalComponent,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateOrEditUserModalComponent extends AppComponentBase {
    private _userService = inject(UserServiceProxy);
    private _profileService = inject(ProfileServiceProxy);
    private _abpSessionService = inject(AbpSessionService);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @ViewChild('organizationUnitTree') organizationUnitTree: OrganizationUnitsTreeComponent;
    @ViewChild('userForm') userForm: NgForm;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    canChangeUserName = true;
    canChangeProfilePicture = false;
    isTwoFactorEnabled: boolean = this.setting.getBoolean('Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled');
    isLockoutEnabled: boolean = this.setting.getBoolean('Abp.Zero.UserManagement.UserLockOut.IsEnabled');
    passwordComplexitySetting: PasswordComplexitySetting = new PasswordComplexitySetting();
    user: UserEditDto = new UserEditDto();
    roles: UserRoleDto[] = [];
    sendActivationEmail = true;
    setRandomPassword = true;
    passwordComplexityInfo = '';
    profilePicture: string;
    allowedUserNameCharacters = '';
    isSMTPSettingsProvided = false;
    passwordMeterInitialized = false;
    allOrganizationUnits: OrganizationUnitDto[] = [];
    memberedOrganizationUnits: string[] = [];
    organizationUnitTreeData: IOrganizationUnitsTreeComponentData | null = null;
    assignedRoleCount = 0;
    assignedMemberedOrganizationUnitCount = 0;
    userPasswordRepeat = '';
    isHost: boolean;
    constructor() {
        super();
        this.isHost = !this._abpSessionService.tenantId;
    }
    show(userId?: number): void {
        this.active = false;
        this.organizationUnitTreeData = null;
        this.roles = [];
        this.memberedOrganizationUnits = [];
        this.assignedRoleCount = 0;
        this.assignedMemberedOrganizationUnitCount = 0;
        this.userPasswordRepeat = '';
        this._cdr.markForCheck();

        if (!userId) {
            this.setRandomPassword = true;
            this.sendActivationEmail = true;
            this.canChangeProfilePicture = false;
        } else {
            this.canChangeProfilePicture = this.permission.isGranted('Pages.Administration.Users.ChangeProfilePicture');
        }
        this._userService.getUserForEdit(userId).subscribe((userResult) => {
            this.user = userResult.user;
            this.roles = userResult.roles || [];
            this.canChangeUserName = this.user.userName !== AppConsts.userManagement.defaultAdminUserName;
            this.allowedUserNameCharacters = userResult.allowedUserNameCharacters;
            this.isSMTPSettingsProvided = userResult.isSMTPSettingsProvided;
            this.sendActivationEmail = userResult.isSMTPSettingsProvided;
            this.allOrganizationUnits = userResult.allOrganizationUnits || [];
            this.memberedOrganizationUnits = userResult.memberedOrganizationUnits || [];
            this.organizationUnitTreeData = {
                allOrganizationUnits: this.allOrganizationUnits,
                selectedOrganizationUnits: this.memberedOrganizationUnits,
            };
            this.updateAssignedRoleCount();
            this.updateAssignedMemberedOrganizationUnitCount();
            this.getProfilePicture(userId);
            if (userId) {
                this.setRandomPassword = false;
                this.sendActivationEmail = false;
            }
            this._profileService.getPasswordComplexitySetting().subscribe((passwordComplexityResult) => {
                this.passwordComplexitySetting = passwordComplexityResult.setting;
                this.setPasswordComplexityInfo();
                this.active = true;
                this.modal.show();
                this._cdr.markForCheck();
            });
            this._cdr.markForCheck();
        });
    }
    setPasswordComplexityInfo(): void {
        this.passwordComplexityInfo = '<ul>';
        if (this.passwordComplexitySetting.requireDigit) {
            this.passwordComplexityInfo += `<li>${this.l('PasswordComplexity_RequireDigit_Hint')}</li>`;
        }
        if (this.passwordComplexitySetting.requireLowercase) {
            this.passwordComplexityInfo += `<li>${this.l('PasswordComplexity_RequireLowercase_Hint')}</li>`;
        }
        if (this.passwordComplexitySetting.requireUppercase) {
            this.passwordComplexityInfo += `<li>${this.l('PasswordComplexity_RequireUppercase_Hint')}</li>`;
        }
        if (this.passwordComplexitySetting.requireNonAlphanumeric) {
            this.passwordComplexityInfo += `<li>${this.l('PasswordComplexity_RequireNonAlphanumeric_Hint')}</li>`;
        }
        if (this.passwordComplexitySetting.requiredLength) {
            this.passwordComplexityInfo += `<li>${this.l(
                'PasswordComplexity_RequiredLength_Hint',
                this.passwordComplexitySetting.requiredLength,
            )}</li>`;
        }
        this.passwordComplexityInfo += '</ul>';
    }
    getProfilePicture(userId: number): void {
        if (!userId) {
            this.profilePicture = `${this.appRootUrl()}assets/common/images/default-profile-picture.png`;
            this._cdr.markForCheck();
            return;
        }
        this._profileService.getProfilePictureByUser(userId).subscribe((result) => {
            if (result && result.profilePicture) {
                this.profilePicture = `data:image/jpeg;base64,${result.profilePicture}`;
            } else {
                this.profilePicture = `${this.appRootUrl()}assets/common/images/default-profile-picture.png`;
            }
            this._cdr.markForCheck();
        });
    }
    onShown(): void {
        document.getElementById('Name')?.focus();
    }
    save(): void {
        const input = new CreateOrUpdateUserInput();
        input.user = this.user;
        input.setRandomPassword = this.setRandomPassword;
        input.sendActivationEmail = this.sendActivationEmail;
        input.assignedRoleNames = _map(
            _filter(this.roles, { isAssigned: true, inheritedFromOrganizationUnit: false }),
            (role) => role.roleName,
        );
        input.organizationUnits = this.organizationUnitTree.getSelectedOrganizationIds();
        this.saving = true;
        this._userService
            .createOrUpdateUser(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }
    close(): void {
        this.active = false;
        this.userPasswordRepeat = '';
        this.modal.hide();
        this._cdr.markForCheck();
    }
    onRoleAssignmentChanged(role: UserRoleDto, isAssigned: boolean): void {
        role.isAssigned = isAssigned;
        this.updateAssignedRoleCount();
        this._cdr.markForCheck();
    }

    private updateAssignedRoleCount(): void {
        this.assignedRoleCount = _filter(this.roles, { isAssigned: true }).length;
    }

    private updateAssignedMemberedOrganizationUnitCount(): void {
        this.assignedMemberedOrganizationUnitCount = this.memberedOrganizationUnits.length;
    }

    onOrganizationUnitTreeSelectionChanged(): void {
        const organizationUnits = this.organizationUnitTree.getSelectedOrganizations();
        this.memberedOrganizationUnits = _map(organizationUnits, (ou) => ou.code);
        this.updateAssignedMemberedOrganizationUnitCount();
        this._cdr.markForCheck();
    }
    setRandomPasswordChange(_event): void {
        if (this.passwordMeterInitialized && this.setRandomPassword) {
            this.passwordMeterInitialized = false;
            return;
        }
        setTimeout(() => {
            PasswordMeterComponent.bootstrap();
            this.passwordMeterInitialized = true;
            this._cdr.markForCheck();
        }, 0);
    }
}
