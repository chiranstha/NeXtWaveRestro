import { CommonModule, DecimalPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CashFlowReportServiceProxy, CashFlowStatementDto } from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { finalize } from 'rxjs';
import { NepaliDatepickerComponent } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';

interface FlattenedCashFlowRow {
    name: string;
    groupType: string;
    currentAmount: number;
    previousAmount: number;
}

@Component({
    selector: 'app-cash-flow-report',
    templateUrl: './cash-flow-report.component.html',
    styleUrls: ['./cash-flow-report.component.css'],
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        NepaliDatepickerComponent,
        DecimalPipe,
        NgxExtendedPdfViewerModule,
    ],
})
export class CashFlowReportComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly fb = inject(FormBuilder);
    private readonly proxy = inject(CashFlowReportServiceProxy);
    private readonly fileDownloadService = inject(FileDownloadService);
    private readonly changeDetector = inject(ChangeDetectorRef);

    form: FormGroup;
    rows: FlattenedCashFlowRow[] = [];
    loading = false;
    showPdfOk = false;
    pdfUrl: string;
    title = 'Cash Flow Statement';
    private destroyed = false;
    private reportRequestId = 0;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.form = this.fb.group({
            fromMiti: [this.fromMiti || this.today],
            toMiti: [this.toMiti || this.today],
            comparisonPeriod: ['previous-year'],
        });

        this.loadReport();
    }

    ngOnDestroy(): void {
        this.destroyed = true;
    }

    loadReport(): void {
        const requestId = ++this.reportRequestId;
        const value = this.form.getRawValue();
        this.loading = true;
        this.proxy
            .getCashFlowReport(value.fromMiti, value.toMiti, value.comparisonPeriod)
            .pipe(
                finalize(() => {
                    this.commitViewState(() => {
                        if (requestId === this.reportRequestId) {
                            this.loading = false;
                        }
                    });
                }),
            )
            .subscribe({
                next: (result) => {
                    this.commitViewState(() => {
                        if (requestId !== this.reportRequestId) {
                            return;
                        }

                        this.rows = this.flatten(result ?? []);
                    });
                },
                error: () => {
                    this.notify.error(this.l('FailedToLoadData'));
                },
            });
    }

    exportToExcel(): void {
        const value = this.form.getRawValue();
        this.loading = true;
        // this.proxy
        //     .cashFlowExportToExcel(value.fromMiti, value.toMiti, value.comparisonPeriod)
        //     .pipe(finalize(() => this.commitViewState(() => (this.loading = false))))
        //     .subscribe({
        //         next: (result) => {
        //             this.fileDownloadService.downloadTempFile(result);
        //             this.notify.success(this.l('ExportSuccessful'));
        //         },
        //         error: () => {
        //             this.notify.error(this.l('ExportFailed'));
        //         },
        //     });
    }

    showPdf(): void {
        const value = this.form.getRawValue();
        this.showPdfOk = true;
        this.loading = true;
        this.proxy
            .getPdfDownload(value.fromMiti, value.toMiti, value.comparisonPeriod)
            .pipe(finalize(() => this.commitViewState(() => (this.loading = false))))
            .subscribe({
                next: (data) => {
                    this.commitViewState(() => {
                        this.pdfUrl = `data:application/pdf;base64,${data}`;
                    });
                },
                error: () => {
                    this.commitViewState(() => {
                        this.showPdfOk = false;
                    });
                    this.notify.error(this.l('Failed to generate PDF'));
                },
            });
    }

    back(): void {
        this.showPdfOk = false;
    }

    private flatten(items: CashFlowStatementDto[], level = 0): FlattenedCashFlowRow[] {
        const rows: FlattenedCashFlowRow[] = [];
        (items ?? []).forEach((item) => {
            if (!item?.data) {
                return;
            }

            rows.push({
                name: `${' '.repeat(Math.max(level, 0) * 2)}${item.data.name ?? ''}`,
                groupType: item.data.groupType?.toString() ?? '',
                currentAmount: item.data.currentAmount ?? 0,
                previousAmount: item.data.previousAmount ?? 0,
            });

            if (item.children?.length > 0) {
                rows.push(...this.flatten(item.children, level + 1));
            }
        });

        return rows;
    }

    private commitViewState(update: () => void): void {
        setTimeout(() => {
            if (this.destroyed) {
                return;
            }

            update();
            this.changeDetector.markForCheck();
        });
    }
}
