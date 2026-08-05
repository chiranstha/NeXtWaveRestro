import { Component, EventEmitter, Output, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NameValueDto,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
} from '@shared/service-proxies/service-proxies';
import {
    IOrganizationUnitsTreeComponentData,
    OrganizationUnitsTreeComponent,
} from '../shared/organization-unit-tree.component';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'organization-unit-lookup-table-modal',
    templateUrl: './organization-unit-lookup-table-modal.component.html',
    imports: [AppBsModalDirective, OrganizationUnitsTreeComponent, ButtonBusyDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class OrganizationUnitLookupTableModalComponent extends AppComponentBase {
    private _organizationUnitServiceProxy = inject(OrganizationUnitServiceProxy);
    @ViewChild('organizationUnitLookupTableModal', { static: true }) modal: ModalDirective;
    @ViewChild('organizationUnitTree') organizationUnitTree: OrganizationUnitsTreeComponent;
    @Output() modalSave: EventEmitter<NameValueDto[]> = new EventEmitter<NameValueDto[]>();
    filterText = '';
    active = false;
    saving = false;
    allOrganizationUnits: OrganizationUnitDto[] = [];
    organizationUnitTreeData: IOrganizationUnitsTreeComponentData | null = null;

    show(): void {
        this.active = false;
        this.organizationUnitTreeData = null;
        this.getOrganizationUnits();
    }
    getOrganizationUnits(): void {
        this._organizationUnitServiceProxy.getAll().subscribe((result) => {
            this.allOrganizationUnits = result || [];
            this.organizationUnitTreeData = {
                allOrganizationUnits: this.allOrganizationUnits,
                selectedOrganizationUnits: [],
            };
            this.active = true;
            this.modal.show();
        });
    }
    save() {
        this.active = false;
        this.modal.hide();
        this.modalSave.emit(this.organizationUnitTree.getSelectedOrganizations());
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
