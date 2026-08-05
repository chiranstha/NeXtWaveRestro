import { Component, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';
import { ICellRendererParams } from 'ag-grid-enterprise';
import { NgClass } from '@angular/common';
@Component({
    selector: 'app-balance-sheet-cell-renderer',
    template: `
        <div class="d-flex align-items-center" [ngClass]="{ 'font-weight-bold': isGroupRow, 'text-primary': isTotal }">
            @if (isGroupRow) {
                <span class="me-2">
                    @if (params.node.expanded) {
                        <i class="fas fa-chevron-down text-primary"></i>
                    }
                    @if (!params.node.expanded) {
                        <i class="fas fa-chevron-right text-primary"></i>
                    }
                </span>
            }
            <span [innerHTML]="formattedValue" [ngClass]="{ 'text-primary': isGroupRow }"></span>
        </div>
    `,
    styles: [
        `
            .font-weight-bold {
                font-weight: 600;
            }
            .text-primary {
                color: #3699ff;
            }
        `,
    ],
    imports: [NgClass],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class BalanceSheetCellRendererComponent implements ICellRendererAngularComp {
    public params: ICellRendererParams;
    public formattedValue: string;
    public isGroupRow: boolean;
    public isTotal: boolean;
    agInit(params: ICellRendererParams): void {
        this.params = params;
        this.isGroupRow = params.node.group;

        this.isTotal =
            params.data?.name &&
            (params.data.name.toLowerCase().includes('total') ||
                params.data.name.toLowerCase().includes('net profit') ||
                params.data.name.toLowerCase().includes('net loss'));
        this.formatValue();
    }
    formatValue(): void {
        if (this.params.colDef.field === 'name') {
            const level = this.params.data.level || 0;
            const padding = level * 20;
            this.formattedValue = `<span style="margin-left: ${padding}px">${this.params.value}</span>`;
        } else if (this.params.colDef.field === 'debit' || this.params.colDef.field === 'credit') {
            if (this.params.value !== undefined && this.params.value !== null) {
                const value = Math.abs(parseFloat(this.params.value));
                if (value === 0) {
                    this.formattedValue = '-';
                } else {
                    this.formattedValue = new Intl.NumberFormat('en-US', {
                        minimumFractionDigits: 2,
                    }).format(value);
                }
            } else {
                this.formattedValue = '-';
            }
        } else {
            this.formattedValue =
                this.params.value !== undefined && this.params.value !== null ? this.params.value.toString() : '';
        }
    }
    refresh(params: ICellRendererParams): boolean {
        this.params = params;
        this.formatValue();
        return true;
    }
}
