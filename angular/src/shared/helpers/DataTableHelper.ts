import { LazyLoadEvent } from '@shared/ui-compat';
import { Paginator } from '@shared/ui-compat';
import { Table } from '@shared/ui-compat';
import * as rtlDetect from 'rtl-detect';
export class DataTableHelper {
    predefinedRecordsCountPerPage = [5, 10, 25, 50, 100, 250, 500];
    defaultRecordsCountPerPage = 10;
    resizableColumns: false;
    totalRecordsCount = 0;
    records: any[];
    isLoading = false;
    showLoadingIndicator(): void {
        setTimeout(() => {
            this.isLoading = true;
        }, 0);
    }
    hideLoadingIndicator(): void {
        setTimeout(() => {
            this.isLoading = false;
        }, 0);
    }
    private readSignalValue<T>(value: T | (() => T)): T {
        return typeof value === 'function' ? (value as () => T)() : value;
    }
    getSorting(table: Table): string {
        let sorting = '';
        if (this.readSignalValue(table.sortMode) === 'multiple') {
            if (table.multiSortMeta) {
                for (let i = 0; i < table.multiSortMeta.length; i++) {
                    const element = table.multiSortMeta[i];
                    if (i > 0) {
                        sorting += ',';
                    }
                    sorting += element.field;
                    if (element.order === 1) {
                        sorting += ' ASC';
                    } else if (element.order === -1) {
                        sorting += ' DESC';
                    }
                }
            }
        } else {
            if (table.sortField) {
                sorting = table.sortField;
                if (table.sortOrder === 1) {
                    sorting += ' ASC';
                } else if (table.sortOrder === -1) {
                    sorting += ' DESC';
                }
            }
        }
        return sorting;
    }
    getMaxResultCount(paginator: Paginator, event: LazyLoadEvent): number {
        const rows = this.readSignalValue(paginator.rows);
        if (rows) {
            return rows;
        }
        if (!event) {
            return 0;
        }
        return event.rows ?? 0;
    }
    getSkipCount(paginator: Paginator, event: LazyLoadEvent): number {
        const first = this.readSignalValue(paginator.first);
        if (first) {
            return first;
        }
        if (!event) {
            return 0;
        }
        return event.first ?? 0;
    }
    shouldResetPaging(event: LazyLoadEvent): boolean {
        if (!event /*|| event.sortField*/) {
            // if you want to reset after sorting, comment out parameter
            return true;
        }
        return false;
    }
    adjustScroll(table: Table) {
        const rtl = rtlDetect.isRtlLang(abp.localization.currentLanguage.name);
        if (!rtl) {
            return;
        }
        const body: HTMLElement = table.el.nativeElement.querySelector('.app-datatable-scrollable-body');
        const header: HTMLElement = table.el.nativeElement.querySelector('.app-datatable-scrollable-header');
        body.addEventListener('scroll', () => {
            header.scrollLeft = body.scrollLeft;
        });
    }
}
