import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    OnInit,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Router } from '@angular/router';
import {
    ListResultDtoOfOrganizationUnitDto,
    MoveOrganizationUnitInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { CreateOrEditUnitModalComponent } from './create-or-edit-unit-modal.component';
import { IUserWithOrganizationUnit } from './user-with-organization-unit';
import { IUsersWithOrganizationUnit } from './users-with-organization-unit';
import { IRoleWithOrganizationUnit } from './role-with-organization-unit';
import { IRolesWithOrganizationUnit } from './roles-with-organization-unit';
import { MenuItem, TreeNode, AppTemplate } from '@shared/ui-compat';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { Tree } from '@shared/ui-compat';
import { ContextMenu } from '@shared/ui-compat';
import { EntityTypeHistoryModalComponent } from '../../shared/common/entityHistory/entity-type-history-modal.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
export interface IOrganizationUnitOnTree extends IBasicOrganizationUnitInfo {
    id: number;
    parent: string | number;
    code: string;
    displayName: string;
    memberCount: number;
    roleCount: number;
    text: string;
    state: any;
}
@Component({
    selector: 'organization-tree',
    templateUrl: './organization-tree.component.html',
    imports: [
        Tree,
        AppTemplate,
        ContextMenu,
        CreateOrEditUnitModalComponent,
        EntityTypeHistoryModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class OrganizationTreeComponent extends AppComponentBase implements OnInit {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);
    private _router = inject(Router);
    private _cdr = inject(ChangeDetectorRef);
    @Output() ouSelected = new EventEmitter<IBasicOrganizationUnitInfo>();
    @ViewChild('createOrEditOrganizationUnitModal', { static: true })
    createOrEditOrganizationUnitModal: CreateOrEditUnitModalComponent;
    treeData: any[] = [];
    selectedOu: TreeNode;
    ouContextMenuItems: MenuItem[] = [];
    canManageOrganizationUnits = false;
    totalUnitCount = 0;
    _entityTypeFullName = 'Abp.Organizations.OrganizationUnit';

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.canManageOrganizationUnits = this.isGranted(
            'Pages.Administration.OrganizationUnits.ManageOrganizationTree',
        );
        this.ouContextMenuItems = this.getContextMenuItems();
        this.getTreeDataFromServer();
    }
    nodeSelect(event) {
        this.ouSelected.emit({
            id: event.node.data.id,
            displayName: event.node.data.displayName,
        });
    }
    isDroppingBetweenTwoNodes(event: any): boolean {
        return event.originalEvent.target.nodeName === 'LI';
    }
    nodeDrop(event) {
        const input = new MoveOrganizationUnitInput();
        input.id = event.dragNode.data.id;
        let dropNodeDisplayName = '';
        if (this.isDroppingBetweenTwoNodes(event)) {
            //between two item
            input.newParentId = event.dropNode.parent ? event.dropNode.parent.data.id : null;
            dropNodeDisplayName = event.dropNode.parent ? event.dropNode.parent.data.displayName : this.l('Root');
        } else {
            input.newParentId = event.dropNode.data.id;
            dropNodeDisplayName = event.dropNode.data.displayName;
        }
        this.message.confirm(
            this.l('OrganizationUnitMoveConfirmMessage', event.dragNode.data.displayName, dropNodeDisplayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._organizationUnitService
                        .moveOrganizationUnit(input)
                        .pipe(
                            catchError((error) => {
                                this.revertDragDrop();
                                return throwError(error);
                            }),
                        )
                        .subscribe(() => {
                            this.notify.success(this.l('SuccessfullyMoved'));
                            this.reload();
                            this._cdr.markForCheck();
                        });
                } else {
                    this.revertDragDrop();
                }
            },
        );
    }
    revertDragDrop() {
        this.reload();
    }
    reload(): void {
        this.getTreeDataFromServer();
    }
    addUnit(parentId?: number): void {
        this.createOrEditOrganizationUnitModal.show({
            parentId,
        });
    }
    unitCreated(ou: OrganizationUnitDto): void {
        if (ou.parentId) {
            const unit = this._treeDataHelperService.findNode(this.treeData, { data: { id: ou.parentId } });
            if (!unit) {
                return;
            }
            unit.children.push({
                label: ou.displayName,
                expandedIcon: 'fa fa-folder-open text-warning',
                collapsedIcon: 'fa fa-folder text-warning',
                selected: true,
                children: [],
                data: ou,
                memberCount: ou.memberCount,
                roleCount: ou.roleCount,
            });
        } else {
            this.treeData.push({
                label: ou.displayName,
                expandedIcon: 'fa fa-folder-open text-warning',
                collapsedIcon: 'fa fa-folder text-warning',
                selected: true,
                children: [],
                data: ou,
                memberCount: ou.memberCount,
                roleCount: ou.roleCount,
            });
        }
        this.totalUnitCount += 1;
        this._cdr.markForCheck();
    }
    deleteUnit(id) {
        const node = this._treeDataHelperService.findNode(this.treeData, { data: { id } });
        if (!node) {
            return;
        }
        if (!node.data.parentId) {
            this.treeData = this.treeData.filter((item) => item.data.id !== id);
            this._cdr.markForCheck();
        }
        const parentNode = this._treeDataHelperService.findNode(this.treeData, { data: { id: node.data.parentId } });
        if (!parentNode) {
            return;
        }
        parentNode.children = parentNode.children.filter((item) => item.data.id !== id);
        this._cdr.markForCheck();
    }
    unitUpdated(ou: OrganizationUnitDto): void {
        const item = this._treeDataHelperService.findNode(this.treeData, { data: { id: ou.id } });
        if (!item) {
            return;
        }
        item.data.displayName = ou.displayName;
        item.label = ou.displayName;
        item.memberCount = ou.memberCount;
        item.roleCount = ou.roleCount;
        this._cdr.markForCheck();
    }
    membersAdded(data: IUsersWithOrganizationUnit): void {
        this.incrementMemberCount(data.ouId, data.userIds.length);
    }
    memberRemoved(data: IUserWithOrganizationUnit): void {
        this.incrementMemberCount(data.ouId, -1);
    }
    incrementMemberCount(ouId: number, incrementAmount: number): void {
        const item = this._treeDataHelperService.findNode(this.treeData, { data: { id: ouId } });
        item.data.memberCount += incrementAmount;
        item.memberCount = item.data.memberCount;
        this._cdr.markForCheck();
    }
    rolesAdded(data: IRolesWithOrganizationUnit): void {
        this.incrementRoleCount(data.ouId, data.roleIds.length);
    }
    roleRemoved(data: IRoleWithOrganizationUnit): void {
        this.incrementRoleCount(data.ouId, -1);
    }
    incrementRoleCount(ouId: number, incrementAmount: number): void {
        const item = this._treeDataHelperService.findNode(this.treeData, { data: { id: ouId } });
        item.data.roleCount += incrementAmount;
        item.roleCount = item.data.roleCount;
        this._cdr.markForCheck();
    }
    private getTreeDataFromServer(): void {
        this._organizationUnitService.getOrganizationUnits().subscribe((result: ListResultDtoOfOrganizationUnitDto) => {
            this.totalUnitCount = result.items.length;
            this.treeData = this._arrayToTreeConverterService.createTree(
                result.items,
                'parentId',
                'id',
                null,
                'children',
                [
                    {
                        target: 'label',
                        targetFunction(item) {
                            return item.displayName;
                        },
                    },
                    {
                        target: 'expandedIcon',
                        value: 'fa fa-folder-open text-warning',
                    },
                    {
                        target: 'collapsedIcon',
                        value: 'fa fa-folder text-warning',
                    },
                    {
                        target: 'selectable',
                        value: true,
                    },
                    {
                        target: 'memberCount',
                        targetFunction(item) {
                            return item.memberCount;
                        },
                    },
                    {
                        target: 'roleCount',
                        targetFunction(item) {
                            return item.roleCount;
                        },
                    },
                ],
            );
            this._cdr.markForCheck();
        });
    }
    private isEntityHistoryEnabled(): boolean {
        const customSettings = (abp as any).custom;
        return (
            customSettings.EntityHistory?.isEnabled &&
            customSettings.EntityHistory.enabledEntities.filter((entityType) => entityType === this._entityTypeFullName)
                .length === 1
        );
    }
    private getContextMenuItems(): any[] {
        const canManageOrganizationTree = this.isGranted(
            'Pages.Administration.OrganizationUnits.ManageOrganizationTree',
        );
        const items = [
            {
                label: this.l('Edit'),
                disabled: !canManageOrganizationTree,
                command: (_event) => {
                    this.createOrEditOrganizationUnitModal.show({
                        id: this.selectedOu.data.id,
                        displayName: this.selectedOu.data.displayName,
                    });
                },
            },
            {
                label: this.l('AddSubUnit'),
                disabled: !canManageOrganizationTree,
                command: () => {
                    this.addUnit(this.selectedOu.data.id);
                },
            },
            {
                label: this.l('Delete'),
                disabled: !canManageOrganizationTree,
                command: () => {
                    this.message.confirm(
                        this.l('OrganizationUnitDeleteWarningMessage', this.selectedOu.data.displayName),
                        this.l('AreYouSure'),
                        (isConfirmed) => {
                            if (isConfirmed) {
                                this._organizationUnitService
                                    .deleteOrganizationUnit(this.selectedOu.data.id)
                                    .subscribe(() => {
                                        this.deleteUnit(this.selectedOu.data.id);
                                        this.notify.success(this.l('SuccessfullyDeleted'));
                                        this.selectedOu = null;
                                        this.reload();
                                        this.ouSelected.emit(null);
                                        this._cdr.markForCheck();
                                    });
                            }
                        },
                    );
                },
            },
        ];
        if (this.isEntityHistoryEnabled()) {
            items.push({
                label: this.l('History'),
                disabled: false,
                command: (_event) => {
                    this._router.navigate([
                        `${
                            abp.appPath
                        }/app/admin/entity-changes/${this.selectedOu.data.id}/${this._entityTypeFullName}`,
                    ]);
                },
            });
        }
        return items;
    }
}
