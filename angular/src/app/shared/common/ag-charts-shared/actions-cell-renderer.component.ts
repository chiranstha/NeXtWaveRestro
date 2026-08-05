import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';
import { ICellRendererParams } from 'ag-grid-enterprise';

@Component({
    selector: 'app-actions-cell-renderer',
    template: `
        @if (!params.data.isDeleted) {
            <button type="button"
                class="btnaction btn-view fa-duotone fa-eye"
                (click)="onView.emit(params.data.id)"
                aria-label="View receipt"
            ></button>
        }
        @if (!params.data.isDeleted) {
            <button type="button"
                class="btnaction btn-download fa-duotone fa-download"
                (click)="onDownload.emit(params.data.id)"
                aria-label="Download receipt"
            ></button>
        }
        @if (!params.data.isDeleted) {
            <button type="button"
                class="btnaction btn-print fa-duotone fa-print"
                (click)="onPrint.emit(params.data.id)"
                aria-label="Print receipt"
            ></button>
        }
        @if (!params.data.isDeleted) {
            <button type="button"
                class="btnaction btn-delete fa-duotone fa-trash cursor-pointer"
                (click)="onDelete.emit(params.data.id)"
                aria-label="Delete receipt"
            ></button>
        }
    `,
    styles: [
        `
            .btnaction {
                background: none;
                border: none;
                cursor: pointer;
                margin: 0 2px;
                padding: 5px;
                font-size: 16px;
            }
            .btnaction:hover {
                opacity: 0.7;
            }
        `,
    ],
    imports: [],
    schemas: [NO_ERRORS_SCHEMA],
})
export class ActionsCellRendererComponent implements ICellRendererAngularComp {
    @Input() params: ICellRendererParams;
    @Output() onView = new EventEmitter<string>();
    @Output() onDownload = new EventEmitter<string>();
    @Output() onPrint = new EventEmitter<string>();
    @Output() onDelete = new EventEmitter<string>();

    public isGrouped: boolean = false;
    public hideDeleteOnGroup: boolean = true;

    agInit(params: ICellRendererParams): void {
        this.params = params;
        this.isGrouped = params.api.getRowGroupColumns().length > 0;
        this.hideDeleteOnGroup = params.colDef.cellRendererParams?.hideDeleteOnGroup ?? true;
    }

    refresh(params: ICellRendererParams): boolean {
        this.agInit(params);
        return true;
    }
}
