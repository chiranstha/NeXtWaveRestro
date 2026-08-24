// permission-grid.component.ts
import { ChangeDetectorRef, Component, Input, ViewChild, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { FlatPermissionDto, GetUserPermissionsForEditOutput } from '@shared/service-proxies/service-proxies';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
interface PermissionRowData {
    id: string;
    name: string;
    displayName: string;
    description?: string;
    parentName?: string;
    level: number;
    isGranted: boolean;
    isParent: boolean;
    children?: PermissionRowData[];
    originalPermission: FlatPermissionDto;
}
@Component({
    selector: 'permission-grid',
    templateUrl: './permission-grid.component.html',
    imports: [AgGridAngular, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PermissionGridComponent extends AppComponentBase implements OnInit {
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('agGrid') agGrid: AgGridAngular;
    private gridApi: GridApi;
    rowData: PermissionRowData[] = [];
    columnDefs: ColDef[] = [];
    gridOptions = {
        defaultColDef: {
            resizable: true,
            sortable: true,
            filter: true,
        },
        rowSelection: {
            mode: 'multiRow',
            enableClickSelection: false,
        },
        getRowStyle: (params) => {
            if (params.data.isParent) {
                return { 'font-weight': 'bold', 'background-color': '#f8f9fa' };
            }
            return null;
        },
    };
    private _editData: GetUserPermissionsForEditOutput;
    @Input()
    set editData(value: GetUserPermissionsForEditOutput) {
        this._editData = value;
        if (value) {
            this.buildPermissionTree();
        }
        this._cdr.markForCheck();
    }
    get editData(): GetUserPermissionsForEditOutput {
        return this._editData;
    }
    constructor() {
        super();
        this.setupColumnDefs();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        // Component initialization
    }
    private setupColumnDefs(): void {
        this.columnDefs = [
            {
                headerName: '',
                field: 'isGranted',
                width: 50,
                cellRenderer: 'agCheckboxCellRenderer',
                cellRendererParams: {
                    disabled: false,
                },
                editable: true,
                onCellValueChanged: (params) => this.onPermissionChanged(params),
            },
            {
                headerName: this.l('Permission'),
                field: 'displayName',
                width: 400,
                cellRenderer: (params) => {
                    const padding = params.data.level * 20;
                    const icon = params.data.isParent
                        ? '<i class="fas fa-folder text-primary me-1"></i>'
                        : '<i class="fas fa-key text-muted me-1"></i>';
                    return `<div style="padding-left: ${padding}px;">
                        ${icon}
                        ${params.value}
                    </div>`;
                },
            },
            {
                headerName: this.l('Name'),
                field: 'name',
                width: 300,
                cellStyle: { 'font-family': 'monospace', 'font-size': '12px' },
            },
        ];
    }
    private buildPermissionTree(): void {
        if (!this.editData?.permissions) {
            return;
        }
        const permissionMap = new Map<string, PermissionRowData>();
        const rootPermissions: PermissionRowData[] = [];
        // First pass: create all permission nodes
        this.editData.permissions.forEach((permission) => {
            const permissionData: PermissionRowData = {
                id: permission.name,
                name: permission.name,
                displayName: permission.displayName || this.formatDisplayName(permission.name),
                description: permission.description,
                parentName: permission.parentName,
                level: 0,
                isGranted: this.editData.grantedPermissionNames?.includes(permission.name) || false,
                isParent: this.hasChildren(permission.name),
                children: [],
                originalPermission: permission,
            };
            permissionMap.set(permission.name, permissionData);
        });
        // Second pass: build hierarchy and calculate levels
        permissionMap.forEach((permission) => {
            if (permission.parentName) {
                const parent = permissionMap.get(permission.parentName);
                if (parent) {
                    parent.children.push(permission);
                    permission.level = parent.level + 1;
                }
            } else {
                rootPermissions.push(permission);
            }
        });
        // Flatten the tree for AG Grid
        this.rowData = this.flattenPermissionTree(rootPermissions);
        this._cdr.markForCheck();
    }
    private formatDisplayName(permissionName: string): string {
        // Convert permission name to readable format
        const parts = permissionName.split('.');
        const lastPart = parts[parts.length - 1];
        // Handle special cases
        if (lastPart === 'Create') {
            return 'Create';
        }
        if (lastPart === 'Edit') {
            return 'Edit';
        }
        if (lastPart === 'Delete') {
            return 'Delete';
        }
        if (lastPart === 'Print') {
            return 'Print';
        }
        // Convert camelCase to readable format
        return lastPart.replace(/([A-Z])/g, ' $1').trim();
    }
    private hasChildren(permissionName: string): boolean {
        return this.editData.permissions.some((p) => p.parentName === permissionName);
    }
    private flattenPermissionTree(permissions: PermissionRowData[]): PermissionRowData[] {
        const flattened: PermissionRowData[] = [];
        const flatten = (perms: PermissionRowData[]) => {
            perms.forEach((permission) => {
                flattened.push(permission);
                if (permission.children && permission.children.length > 0) {
                    // Sort children by display name
                    permission.children.sort((a, b) => a.displayName.localeCompare(b.displayName));
                    flatten(permission.children);
                }
            });
        };
        // Sort root permissions
        permissions.sort((a, b) => a.displayName.localeCompare(b.displayName));
        flatten(permissions);
        return flattened;
    }
    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.gridApi.sizeColumnsToFit();
    }
    private onPermissionChanged(params: any): void {
        const changedPermission = params.data as PermissionRowData;
        const isGranted = params.newValue;
        if (isGranted) {
            // If granting permission, also grant all parent permissions
            this.grantParentPermissions(changedPermission);
            // If this is a parent permission, grant all children
            if (changedPermission.isParent) {
                this.updateChildrenPermissions(changedPermission, true);
            }
        } else {
            // If revoking permission, also revoke all child permissions
            if (changedPermission.isParent) {
                this.updateChildrenPermissions(changedPermission, false);
            }
        }
        this.gridApi.refreshCells();
        this._cdr.markForCheck();
    }
    private grantParentPermissions(permission: PermissionRowData): void {
        if (!permission.parentName) {
            return;
        }
        const parent = this.rowData.find((p) => p.name === permission.parentName);
        if (parent && !parent.isGranted) {
            parent.isGranted = true;
            this.grantParentPermissions(parent);
        }
    }
    private updateChildrenPermissions(parent: PermissionRowData, granted: boolean): void {
        this.rowData.forEach((permission) => {
            if (this.isChildOf(permission, parent.name)) {
                permission.isGranted = granted;
            }
        });
    }
    private updateParentPermissions(permission: PermissionRowData, granted: boolean): void {
        if (!permission.parentName) {
            return;
        }
        const parent = this.rowData.find((p) => p.name === permission.parentName);
        if (parent && parent.isGranted !== granted) {
            parent.isGranted = granted;
            this.updateParentPermissions(parent, granted);
        }
    }
    private checkAndUpdateParentPermissions(permission: PermissionRowData): void {
        if (!permission.parentName) {
            return;
        }
        const parent = this.rowData.find((p) => p.name === permission.parentName);
        if (parent) {
            const siblings = this.rowData.filter((p) => p.parentName === permission.parentName);
            const allSiblingsGranted = siblings.every((s) => s.isGranted);
            if (allSiblingsGranted && !parent.isGranted) {
                parent.isGranted = true;
                this.checkAndUpdateParentPermissions(parent);
            }
        }
    }
    // Debug method to compare with your JSON data
    getDebugInfo(): any {
        const granted = this.getGrantedPermissionNames();
        return {
            total: this.rowData.length,
            granted: granted.length,
            grantedNames: granted,
        };
    }
    // Method to export data in the exact same format as your JSON
    exportPermissionData(userId: number): any {
        return {
            id: userId,
            grantedPermissionNames: this.getGrantedPermissionNames(),
        };
    }
    // Method to validate against sample data
    validateAgainstSample(): void {
        this.getGrantedPermissionNames();
    }
    private isChildOf(permission: PermissionRowData, parentName: string): boolean {
        if (!permission.parentName) {
            return false;
        }
        if (permission.parentName === parentName) {
            return true;
        }
        const parent = this.rowData.find((p) => p.name === permission.parentName);
        return parent ? this.isChildOf(parent, parentName) : false;
    }
    getGrantedPermissionNames(): string[] {
        // Return all granted permissions (both parent and child)
        // This matches the exact format from your JSON data
        return this.rowData.filter((permission) => permission.isGranted).map((permission) => permission.name);
    }
    // Utility methods for external access
    selectAll(): void {
        this.rowData.forEach((permission) => {
            permission.isGranted = true;
        });
        this.gridApi.refreshCells();
        this._cdr.markForCheck();
    }
    deselectAll(): void {
        this.rowData.forEach((permission) => {
            permission.isGranted = false;
        });
        this.gridApi.refreshCells();
        this._cdr.markForCheck();
    }
    expandAll(): void {
        this.gridApi.expandAll();
    }
    collapseAll(): void {
        this.gridApi.collapseAll();
    }
    filterPermissions(searchText: string): void {
        this.gridApi.setGridOption('quickFilterText', searchText);
    }
}
