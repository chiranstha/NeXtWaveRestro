// Updated create-or-edit-role-modal.component.ts
import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Output,
    ViewChild,
    OnInit,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrUpdateRoleInput,
    RoleEditDto,
    RoleServiceProxy,
    GetRoleForEditOutput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { PermissionGridComponent } from '../users/permission-grid.component';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'createOrEditRoleModal',
    templateUrl: './create-or-edit-role-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ReactiveFormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        PermissionGridComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class CreateOrEditRoleModalComponent extends AppComponentBase implements OnInit {
    private _roleService = inject(RoleServiceProxy);
    private _fb = inject(FormBuilder);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @ViewChild('permissionGrid') permissionGrid: PermissionGridComponent; // Updated reference
    @Output() modalSave: EventEmitter<void> = new EventEmitter<void>();
    active = false;
    saving = false;
    role: RoleEditDto = new RoleEditDto();
    roleForm: FormGroup;
    private _editData: GetRoleForEditOutput;
    // Store the edit data for later use

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.createForm();
    }
    createForm(): void {
        this.roleForm = this._fb.group({
            displayName: ['', [Validators.required, Validators.maxLength(64)]],
            isDefault: [false],
        });
    }
    show(roleId?: number): void {
        const self = this;
        self.active = true;
        self._roleService.getRoleForEdit(roleId).subscribe((result) => {
            self.role = result.role;
            self._editData = result; // Store the edit data
            self.roleForm.patchValue({
                displayName: self.role.displayName,
                isDefault: self.role.isDefault,
            });
            self.modal.show();
            self._cdr.markForCheck();
        });
    }
    onShown(): void {
        // Focus on role name input when modal is shown
        const roleNameInput = document.getElementById('RoleDisplayName');
        if (roleNameInput) {
            roleNameInput.focus();
        }
        // Set the permission grid data after the modal is shown
        // This ensures the ViewChild is available
        setTimeout(() => {
            if (this.permissionGrid && this._editData) {
                this.permissionGrid.editData = this._editData;
            }
            this._cdr.markForCheck();
        }, 100);
    }
    save(): void {
        const self = this;
        // Validate permission grid is initialized
        if (!self.permissionGrid) {
            self.notify.error('Permission grid not initialized');
            return;
        }
        // Update role with form values
        self.role.displayName = self.roleForm.value.displayName;
        self.role.isDefault = self.roleForm.value.isDefault;
        const input = new CreateOrUpdateRoleInput();
        input.role = self.role;
        input.grantedPermissionNames = self.permissionGrid.getGrantedPermissionNames();
        this.saving = true;
        this._roleService
            .createOrUpdateRole(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
        this._cdr.markForCheck();
    }
    // Helper method to validate role form
    isFormValid(): boolean {
        return this.roleForm.valid;
    }
    // Debug method to check permission data
    debugPermissions(): void {
        this.permissionGrid?.getGrantedPermissionNames();
    }
}
