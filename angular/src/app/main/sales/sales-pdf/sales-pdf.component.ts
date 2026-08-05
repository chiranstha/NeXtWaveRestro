import { ChangeDetectionStrategy, AfterViewInit, Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
    SalesMastersServiceProxy,
    SalesReturnMastersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { pdfDefaultOptions } from 'ngx-extended-pdf-viewer';
import { Location } from '@angular/common';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { first, timer } from 'rxjs';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-sales-pdf',
    templateUrl: './sales-pdf.component.html',
})
export class SalesPdfComponent extends AppComponentBase implements OnInit, AfterViewInit {
    shortcuts: ShortcutInput[] = [];
    title: string = '';
    id: string = '';
    enum: number = 0;
    pdfUrl: string = '';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        injector: Injector,
        private _salesProxy: SalesMastersServiceProxy,
        private _salesReturnProxy: SalesReturnMastersServiceProxy,
        private _location: Location
    ) {
        super(injector);
        pdfDefaultOptions.assetsFolder = 'bleeding-edge';
    }

    ngAfterViewInit(): void {
        // this.shortcuts.push({
        //     key: 'esc',
        //     preventDefault: true,
        //     label: 'Cancel',
        //     description: 'Take to previous page',
        //     command: () => document.getElementById('cancel').click(),
        // });
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.enum = this.route.snapshot.params['enum'];
        if (this.enum == 3 && this.id) {
            this.notify.info(this.l('Sales Masters Pdf'));
            this.title = 'Sales Masters Pdf';
            this._salesProxy.getPdfDownload(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        } else if (this.enum == 4) {
            this.notify.info(this.l('Sales Returns Pdf'));
            this.title = 'Sales Returns Pdf';
            this._salesReturnProxy.getPdfDownload(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        }else if (this.enum == 6) {
            this.notify.info(this.l('sales Pos Pdf'));
            this.title = 'Sales Pos Pdf';
            this._salesProxy.getPdfDownload(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        } else if (this.enum == 7) {
            this.notify.info(this.l('sales Pos Pdf'));
            this.title = 'Sales Pos Pdf';
            this._salesProxy.getPosBillGetAll(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        }
    }

    cancel() {
        if (this.enum == 3) {
            this.router.navigate(['app/main/sales/salesInvoiceMasters']);
        } else if (this.enum == 1) {
            this.router.navigate(['app/main/sales/salesOrderMaster']);
        } else if (this.enum == 6) {
            this.router.navigate(['app/main/sales/sales-pos']);
        } else if (this.enum == 0) {
            this.router.navigate(['app/main/sales/salesQuotationMasters']);
        } else {
            this.ngOnInit();
                        this._location.back(); /// needs to be changed accordingly later
        }

        timer(50)
            .pipe(first())
            .subscribe(() => {
            });
    }
}
