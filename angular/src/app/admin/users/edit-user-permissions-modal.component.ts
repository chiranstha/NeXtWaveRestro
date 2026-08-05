import { ChangeDetectorRef, Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDtoOfInt64,
    UpdateUserPermissionsInput,
    UserServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { PermissionTreeComponent } from '@app/admin/shared/permission-tree.component';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { PermissionTreeComponent as PermissionTreeComponent_1 } from '../shared/permission-tree.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'editUserPermissionsModal',
    templateUrl: './edit-user-permissions-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, PermissionTreeComponent_1, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EditUserPermissionsModalComponent extends AppComponentBase {
    private _userService = inject(UserServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('editModal', { static: true }) modal: ModalDirective;
    @ViewChild('permissionTree') permissionTree: PermissionTreeComponent;
    @ViewChild('userForm') userForm: any;
    saving = false;
    resettingPermissions = false;
    isFormValid = false;
    userId: number;
    userName: string;

    show(userId: number, userName?: string): void {
        this.userId = userId;
        this.userName = userName;
        this._userService.getUserPermissionsForEdit(userId).subscribe((result) => {
            this.permissionTree.editData = result;
            this.modal.show();
            if (this.userForm) {
                this.userForm.form.valueChanges.subscribe(() => {
                    this.isFormValid = this.userForm.form.valid;
                    this._cdr.markForCheck();
                });
            }
            this._cdr.markForCheck();
        });
    }
    save(): void {
        if (!this.permissionTree) {
            this.notify.error('Permission tree not initialized');
            return;
        }
        const input = new UpdateUserPermissionsInput();
        input.id = this.userId;
        input.grantedPermissionNames = this.permissionTree.getGrantedPermissionNames();
        this.saving = true;
        this._userService
            .updateUserPermissions(input)
            .pipe(
                finalize(() => {
                    this.saving = false;
                    this._cdr.markForCheck();
                }),
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
            });
    }
    resetPermissions(): void {
        const input = new EntityDtoOfInt64();
        input.id = this.userId;
        this.resettingPermissions = true;
        this._userService.resetUserSpecificPermissions(input).subscribe({
            next: () => {
                this.notify.info(this.l('ResetSuccessfully'));
                this._userService.getUserPermissionsForEdit(this.userId).subscribe((result) => {
                    this.permissionTree.editData = result;
                    this._cdr.markForCheck();
                });
            },
            complete: () => {
                this.resettingPermissions = false;
                this._cdr.markForCheck();
            },
        });
    }
    close(): void {
        this.modal.hide();
        this._cdr.markForCheck();
    }
}
