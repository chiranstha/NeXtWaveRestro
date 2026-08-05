import { Component, EventEmitter, Input, Output, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationUnitDto } from '@shared/service-proxies/service-proxies';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { TreeNode } from '@shared/ui-compat';
import { FormsModule } from '@angular/forms';
import { Tree } from '@shared/ui-compat';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
export interface IOrganizationUnitsTreeComponentData {
    allOrganizationUnits: OrganizationUnitDto[];
    selectedOrganizationUnits: string[];
}
@Component({
    selector: 'organization-unit-tree',
    template: `
        <div class="form-group">
            <input
                id="OrganizationUnitsTreeFilter"
                type="text"
                (input)="filterOrganizationUnits($event)"
                [(ngModel)]="filter"
                class="form-control"
                placeholder="{{ 'SearchWithThreeDot' | localize }}"
            />
        </div>
        <app-tree
            [value]="treeData"
            selectionMode="checkbox"
            [(selection)]="selectedOus"
            (onNodeSelect)="nodeSelect($event)"
            (onNodeUnselect)="onNodeUnselect($event)"
            [propagateSelectionUp]="false"
            [propagateSelectionDown]="cascadeSelectEnabled"
        ></app-tree>
    `,
    imports: [FormsModule, Tree, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class OrganizationUnitsTreeComponent extends AppComponentBase {
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);
    @Input() cascadeSelectEnabled = true;
    @Output() onChange: EventEmitter<any> = new EventEmitter<any>();
    treeData: any;
    selectedOus: TreeNode[] = [];
    filter = '';
    private _allOrganizationUnits: OrganizationUnitDto[];
    private _selectedOrganizationUnits: string[];

    @Input()
    set data(data: IOrganizationUnitsTreeComponentData | null) {
        if (!data) {
            this.treeData = [];
            this.selectedOus = [];
            this._allOrganizationUnits = [];
            this._selectedOrganizationUnits = [];
            return;
        }

        this.setTreeData(data.allOrganizationUnits);
        this.setSelectedNodes(data.selectedOrganizationUnits);
        this._allOrganizationUnits = data.allOrganizationUnits;
        this._selectedOrganizationUnits = data.selectedOrganizationUnits;
    }
    setTreeData(organizationUnits: OrganizationUnitDto[]) {
        this.treeData = this._arrayToTreeConverterService.createTree(
            organizationUnits,
            'parentId',
            'id',
            null,
            'children',
            [
                {
                    target: 'label',
                    source: 'displayName',
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
                    target: 'expanded',
                    value: true,
                },
            ],
        );
    }
    setSelectedNodes(selectedOrganizationUnits: string[]) {
        this.selectedOus = [];
        selectedOrganizationUnits.forEach((ou) => {
            const item = this._treeDataHelperService.findNode(this.treeData, { data: { code: ou } });
            if (item) {
                this.selectedOus.push(item);
            }
        });
    }
    getSelectedOrganizationIds(): number[] {
        if (!this.selectedOus) {
            return [];
        }
        const organizationIds = [];
        this.selectedOus.forEach((ou) => {
            organizationIds.push(ou.data.id);
        });
        return organizationIds;
    }
    getSelectedOrganizations(): any[] {
        if (!this.selectedOus) {
            return [];
        }
        const organizations = [];
        this.selectedOus.forEach((ou) => {
            organizations.push({ displayName: ou.data.displayName, id: ou.data.id, code: ou.data.code });
        });
        return organizations;
    }
    filterOrganizationUnit(nodes, filterText): any {
        nodes.forEach((node) => {
            if (node.data.displayName.toLowerCase().indexOf(filterText.toLowerCase()) >= 0) {
                node.styleClass = this.showParentNodes(node);
            } else {
                node.styleClass = 'hidden-tree-node';
            }
            if (node.children) {
                this.filterOrganizationUnit(node.children, filterText);
            }
        });
    }
    showParentNodes(node): void {
        if (!node.parent) {
            return;
        }
        node.parent.styleClass = '';
        this.showParentNodes(node.parent);
    }
    filterOrganizationUnits(_event): void {
        this.filterOrganizationUnit(this.treeData, this.filter);
    }
    nodeSelect(event) {
        this.onChange.emit();
        if (!this.cascadeSelectEnabled) {
            return;
        }
        let parentNode = this._treeDataHelperService.findParent(this.treeData, { data: { id: event.node.data.id } });
        while (parentNode != null) {
            this.selectedOus.push(parentNode);
            parentNode = this._treeDataHelperService.findParent(this.treeData, { data: { id: parentNode.data.id } });
        }
    }
    onNodeUnselect(event) {
        this.selectedOus = this.selectedOus.filter((x) => x.data.id !== event.node.data.id);
        this.onChange.emit();
    }
}
