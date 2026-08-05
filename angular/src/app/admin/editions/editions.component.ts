import { Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EditionListDto, EditionServiceProxy } from '@shared/service-proxies/service-proxies';
import { CreateEditionModalComponent } from './create-edition-modal.component';
import { EditEditionModalComponent } from './edit-edition-modal.component';
import { MoveTenantsToAnotherEditionModalComponent } from './move-tenants-to-another-edition-modal.component';
import { finalize } from 'rxjs/operators';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './editions.component.html',
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        AgGridAngular,
        CreateEditionModalComponent,
        EditEditionModalComponent,
        MoveTenantsToAnotherEditionModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EditionsComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);
    @ViewChild('createEditionModal', { static: true }) createEditionModal: CreateEditionModalComponent;
    @ViewChild('editEditionModal', { static: true }) editEditionModal: EditEditionModalComponent;
    @ViewChild('moveTenantsToAnotherEditionModal', { static: true })
    moveTenantsToAnotherEditionModal: MoveTenantsToAnotherEditionModalComponent;
    private gridApi!: GridApi;
    public rowData: EditionListDto[] = [];
    public columnDefs: ColDef[] = [
        {
            headerName: this.l('Actions'),
            field: 'actions',
            width: 130,
            cellRenderer: this.actionsCellRenderer.bind(this),
            sortable: false,
            filter: false,
            resizable: false,
            hide: !(
                this.permission.isGranted('Pages.Editions.Edit') || this.permission.isGranted('Pages.Editions.Delete')
            ),
        },
        {
            headerName: this.l('EditionName'),
            field: 'displayName',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('Price'),
            field: 'price',
            sortable: false,
            filter: false,
            cellRenderer: this.priceCellRenderer.bind(this),
        },
        {
            headerName: this.l('IsTrialActive'),
            field: 'trialDayCount',
            sortable: true,
            filter: true,
            cellRenderer: (params) => {
                const trialDayCount = params.value;
                if (trialDayCount) {
                    return `<span>${this.l('Yes')}, ${trialDayCount} ${this.l('Days')}</span>`;
                } else {
                    return `<span>${this.l('No')}</span>`;
                }
            },
        },
        {
            headerName: this.l('WaitingDayAfterExpire'),
            field: 'waitingDayAfterExpire',
            sortable: true,
            filter: true,
        },
        {
            headerName: this.l('ExpiringEdition'),
            field: 'expiringEditionDisplayName',
            sortable: true,
            filter: true,
        },
    ];

    onGridReady(params: GridReadyEvent): void {
        this.gridApi = params.api;
        this.getEditions();
    }
    getEditions(): void {
        this.showMainSpinner();
        this._editionService
            .getEditions()
            .pipe(finalize(() => this.hideMainSpinner()))
            .subscribe((result) => {
                this.rowData = result.items;
                this.hideMainSpinner();
            });
    }
    createEdition(): void {
        this.createEditionModal.show();
    }
    deleteEdition(edition: EditionListDto): void {
        this.message.confirm(
            this.l('EditionDeleteWarningMessage', edition.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._editionService.deleteEdition(edition.id).subscribe(() => {
                        this.getEditions();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            },
        );
    }
    private actionsCellRenderer(params: any): HTMLElement {
        const record = params.data;
        const container = document.createElement('div');
        container.className = 'btn-group';
        const dropdownBtn = document.createElement('button');
        dropdownBtn.className = 'btn btn-sm btn-primary dropdown-toggle';
        dropdownBtn.innerHTML = `<i class="fa fa-cog"></i> <span class="caret"></span> ${this.l('Actions')}`;
        dropdownBtn.setAttribute('data-bs-toggle', 'dropdown');
        dropdownBtn.setAttribute('aria-expanded', 'false');
        const dropdownMenu = document.createElement('ul');
        dropdownMenu.className = 'dropdown-menu';
        if (this.permission.isGranted('Pages.Editions.Edit')) {
            const editItem = document.createElement('li');
            const editLink = document.createElement('a');
            editLink.className = 'dropdown-item';
            editLink.href = 'javascript:;';
            editLink.innerText = this.l('Edit');
            editLink.onclick = () => this.editEditionModal.show(record.id);
            editItem.appendChild(editLink);
            dropdownMenu.appendChild(editItem);
        }
        if (this.permission.isGranted('Pages.Editions.Delete')) {
            const deleteItem = document.createElement('li');
            const deleteLink = document.createElement('a');
            deleteLink.className = 'dropdown-item';
            deleteLink.href = 'javascript:;';
            deleteLink.innerText = this.l('Delete');
            deleteLink.onclick = () => this.deleteEdition(record);
            deleteItem.appendChild(deleteLink);
            dropdownMenu.appendChild(deleteItem);
        }
        if (this.permission.isGranted('Pages.Editions.MoveTenantsToAnotherEdition')) {
            const moveItem = document.createElement('li');
            const moveLink = document.createElement('a');
            moveLink.className = 'dropdown-item';
            moveLink.href = 'javascript:;';
            moveLink.innerText = this.l('MoveTenantsToAnotherEdition');
            moveLink.onclick = () => this.moveTenantsToAnotherEditionModal.show(record.id);
            moveItem.appendChild(moveLink);
            dropdownMenu.appendChild(moveItem);
        }
        const dropdown = document.createElement('div');
        dropdown.className = 'dropdown';
        dropdown.appendChild(dropdownBtn);
        dropdown.appendChild(dropdownMenu);
        container.appendChild(dropdown);
        return container;
    }
    private priceCellRenderer(params: any): string {
        const record = params.data;
        if (record.monthlyPrice || record.annualPrice) {
            return `${this.appSession.application.currencySign}${record.monthlyPrice} ${this.l('Monthly')} / ${this.appSession.application.currencySign}${record.annualPrice} ${this.l('Annual')}`;
        } else {
            return this.l('Free');
        }
    }
}
