import {
    Component,
    EventEmitter,
    Input,
    OnInit,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TreeNode } from '@shared/ui-compat';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { PermissionTreeComponent } from './permission-tree.component';
import { FlatPermissionDto, PermissionServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'permission-tree-modal',
    templateUrl: './permission-tree-modal.component.html',
    imports: [AppBsModalDirective, PermissionTreeComponent, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PermissionTreeModalComponent extends AppComponentBase implements OnInit {
    private _permissionService = inject(PermissionServiceProxy);
    @Input() dontAddOpenerButton: boolean;
    @Input() singleSelect: boolean;
    @Input() disableCascade: boolean;
    @Output() onModalclose = new EventEmitter<string[]>();
    @ViewChild('permissionTreeModal', { static: true }) permissionTreeModal: ModalDirective;
    @ViewChild('permissionTree') permissionTree: PermissionTreeComponent;
    selectedPermissions: TreeNode[] = [];
    NumberOfFilteredPermission = 0;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.loadAllPermissionsToFilterTree();
    }
    openPermissionTreeModal(): void {
        this.permissionTreeModal.show();
    }
    closePermissionTreeModal(): void {
        const selections = this.getSelectedPermissions();
        this.NumberOfFilteredPermission = selections.length;
        this.onModalclose.emit(selections);
        this.permissionTreeModal.hide();
        abp.notify.success(this.l('XCountPermissionFiltered', this.NumberOfFilteredPermission));
    }
    getSelectedPermissions(): string[] {
        if (!this.permissionTree) {
            return [];
        }
        const permissions = this.permissionTree
            .getGrantedPermissionNames()
            .filter((test, index, array) => index === array.findIndex((findTest) => findTest === test));
        return permissions;
    }
    private loadAllPermissionsToFilterTree() {
        const treeModel: FlatPermissionDto[] = [];
        this._permissionService.getAllPermissions().subscribe((result) => {
            if (result.items) {
                result.items.forEach((item) => {
                    treeModel.push(
                        new FlatPermissionDto({
                            name: item.name,
                            description: item.description,
                            displayName: item.displayName,
                            isGrantedByDefault: item.isGrantedByDefault,
                            parentName: item.parentName,
                        }),
                    );
                });
            }
            this.permissionTree.editData = { permissions: treeModel, grantedPermissionNames: [] };
        });
    }
}
