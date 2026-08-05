import { Location } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ContraMastersServiceProxy,
    JournalMastersServiceProxy,
    PaymentMastersServiceProxy,
    PDCClearancesServiceProxy,
    PDCPayablesServiceProxy,
    PDCReceivablesServiceProxy,
    ReceiptMastersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Observable, finalize } from 'rxjs';

interface DetailColumn {
    label: string;
    field: string;
    amount?: boolean;
}

interface VoucherPdfConfig {
    title: string;
    backRoute: string;
    detailsKey?: string;
    columns: DetailColumn[];
    load: () => Observable<any>;
}

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    selector: 'app-transaction-pdf',
    standalone: false,
    styleUrls: ['./transaction-pdf.component.css'],
    templateUrl: './transaction-pdf.component.html',
})
export class TransactionPdfComponent extends AppComponentBase implements OnInit {
    id = '';
    enum = 0;
    title = 'Transaction Voucher';
    backRoute = '/app/main/transaction/paymentMasters';
    loading = false;
    voucher: any;
    detailRows: any[] = [];
    detailColumns: DetailColumn[] = [];

    constructor(
        injector: Injector,
        private route: ActivatedRoute,
        private router: Router,
        private location: Location,
        private cdr: ChangeDetectorRef,
        private contraProxy: ContraMastersServiceProxy,
        private paymentProxy: PaymentMastersServiceProxy,
        private receiptProxy: ReceiptMastersServiceProxy,
        private journalProxy: JournalMastersServiceProxy,
        private pdcClearanceProxy: PDCClearancesServiceProxy,
        private pdcPayableProxy: PDCPayablesServiceProxy,
        private pdcReceivableProxy: PDCReceivablesServiceProxy
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.paramMap.get('id') || '';
        this.enum = Number(this.route.snapshot.paramMap.get('enum') || 0);
        this.loadVoucher();
    }

    loadVoucher(): void {
        const config = this.getConfig(this.enum);
        this.title = config.title;
        this.backRoute = config.backRoute;
        this.detailColumns = config.columns;

        if (!this.id) {
            this.notify.warn(this.l('Voucher id is missing'));
            this.loading = false;
            this.cdr.markForCheck();
            return;
        }

        this.loading = true;
        config
            .load()
            .pipe(
                finalize(() => {
                    this.loading = false;
                    this.cdr.markForCheck();
                })
            )
            .subscribe({
                next: (data) => {
                    this.voucher = data;
                    this.detailRows = config.detailsKey ? data?.[config.detailsKey] || [] : [];
                },
                error: () => {
                    this.notify.error(this.l('Unable to load transaction voucher'));
                },
            });
    }

    close(): void {
        if (this.backRoute) {
            this.router.navigate([this.backRoute]);
            return;
        }

        this.location.back();
    }

    print(): void {
        window.print();
    }

    companyName(): string {
        return this.voucher?.companyName || this.voucher?.branchName || '';
    }

    companyAddress(): string {
        return this.voucher?.companyAddress || this.voucher?.branchAddress || '';
    }

    companyContact(): string {
        return this.voucher?.companyContact || this.voucher?.branchContact || '';
    }

    logoSrc(): string {
        const logo = this.voucher?.logo1;
        if (!logo) {
            return '';
        }

        return logo.startsWith('data:') ? logo : `data:image/png;base64,${logo}`;
    }

    formatDate(value: any): string {
        if (!value) {
            return '';
        }

        if (typeof value === 'string') {
            return value.includes('T') ? value.split('T')[0] : value;
        }

        if (typeof value.toFormat === 'function') {
            return value.toFormat('yyyy-MM-dd');
        }

        if (typeof value.toISODate === 'function') {
            return value.toISODate();
        }

        return value.toString();
    }

    formatAmount(value: any): string {
        if (value === undefined || value === null || value === '') {
            return '';
        }

        return Number(value).toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    getValue(row: any, column: DetailColumn): string {
        const value = row?.[column.field];
        return column.amount ? this.formatAmount(value) : value || '';
    }

    private getConfig(value: number): VoucherPdfConfig {
        const amountColumns: DetailColumn[] = [
            { label: 'S.N', field: 'slNo' },
            { label: 'Ledger', field: 'ledgerName' },
            { label: 'Cheque No', field: 'chequeNo' },
            { label: 'Cheque Date', field: 'chequeDate' },
            { label: 'Amount', field: 'amount', amount: true },
        ];

        switch (value) {
            case 0:
                return {
                    title: 'Contra Voucher',
                    backRoute: '/app/main/transaction/contraMasters',
                    detailsKey: 'contraDetails',
                    columns: amountColumns,
                    load: () => this.contraProxy.getContraMasterForPdf(this.id),
                };
            case 1:
                return {
                    title: 'Payment Voucher',
                    backRoute: '/app/main/transaction/paymentMasters',
                    detailsKey: 'paymentDetails',
                    columns: amountColumns,
                    load: () => this.paymentProxy.getPaymentMasterForPdf(this.id),
                };
            case 2:
                return {
                    title: 'Receipt Voucher',
                    backRoute: '/app/main/transaction/receiptMaster',
                    detailsKey: 'receiptDetails',
                    columns: [
                        { label: 'S.N', field: 'slNo' },
                        { label: 'Ledger', field: 'ledgerName' },
                        { label: 'Cheque No', field: 'chequeNo' },
                        { label: 'Cheque Miti', field: 'chequeMiti' },
                        { label: 'Amount', field: 'amount', amount: true },
                    ],
                    load: () => this.receiptProxy.getReceiptMasterForPdf(this.id),
                };
            case 3:
                return {
                    title: 'Journal Voucher',
                    backRoute: '/app/main/transaction/journalMasters',
                    detailsKey: 'journalDetails',
                    columns: [
                        { label: 'S.N', field: 'slNo' },
                        { label: 'Ledger', field: 'ledgerName' },
                        { label: 'Cheque No', field: 'chequeNo' },
                        { label: 'Debit', field: 'debit', amount: true },
                        { label: 'Credit', field: 'credit', amount: true },
                    ],
                    load: () => this.journalProxy.getJournalMasterForPdf(this.id),
                };
            case 4:
            case 5:
                return {
                    title: 'PDC Clearance Voucher',
                    backRoute: '/app/main/transaction/PdcClearance',
                    columns: [],
                    load: () => this.pdcClearanceProxy.getPDCClearancesForPdf(this.id),
                };
            case 6:
                return {
                    title: 'PDC Payable Voucher',
                    backRoute: '/app/main/transaction/PdcPayable',
                    columns: [],
                    load: () => this.pdcPayableProxy.getPDCPayablesForPdf(this.id),
                };
            case 7:
                return {
                    title: 'PDC Receivable Voucher',
                    backRoute: '/app/main/transaction/PdcReceivable',
                    columns: [],
                    load: () => this.pdcReceivableProxy.getPDCReceivablesForPdf(this.id),
                };
            default:
                return {
                    title: 'Transaction Voucher',
                    backRoute: '/app/main/transaction/paymentMasters',
                    columns: [],
                    load: () => this.paymentProxy.getPaymentMasterForPdf(this.id),
                };
        }
    }
}
