import { ChangeDetectionStrategy, Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { pdfDefaultOptions } from 'ngx-extended-pdf-viewer';
import { Location } from '@angular/common';
import {
    PurchaseMastersServiceProxy,
    PurchaseOrderMastersServiceProxy,
    PurchaseReturnsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { first, timer } from 'rxjs';
import { BsModalService } from 'ngx-bootstrap/modal';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-purchase-pdf',
    templateUrl: './purchase-pdf.component.html',
})
export class PurchasePdfsComponent extends AppComponentBase implements OnInit {
    id: string;
    pdfUrl: any;
    enum: number;
    title: string;

    constructor(
        private route: ActivatedRoute,
        public injector: Injector,
        private router: Router,
        private modalService: BsModalService,
        private _orderProxy: PurchaseOrderMastersServiceProxy,
        private _purchaseInvoiceProxy: PurchaseMastersServiceProxy,
        private _returnProxy: PurchaseReturnsServiceProxy,
        private _location: Location
    ) {
        super(injector);
        pdfDefaultOptions.assetsFolder = 'bleeding-edge';
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.enum = this.route.snapshot.params['enum'];
        if (this.enum == 0) {
            this.notify.info(this.l('Purchase Order Pdf'));
            this.title = 'Purchase Order Pdf';
            this._orderProxy.getPdfdownload(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        } else if (this.enum == 3) {
            this.notify.info(this.l('Purchase Invoice Pdf'));
            this.title = 'Purchase Invoice Pdf';
            this._purchaseInvoiceProxy.getPdfdownload(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        } else if (this.enum == 4) {
            this.notify.info(this.l('Purchase Return Pdf'));
            this.title = 'Purchase Return Pdf';
            this._returnProxy.getPdfDownload(this.id).subscribe((data) => {
                const linkSource = `data:application/pdf;base64,${  data}`;
                this.pdfUrl = linkSource;
            });
        } else {
            this.title = '404 Pdf not found';
        }
    }

    close() {
        if (this.enum == 0) {
            this.router.navigate(['app/main/purchase/purchaseOrderMasters']);
        } else if (this.enum == 3) {
            this.router.navigate(['app/main/purchase/purchaseMasters']);
        } else if (this.enum == 4) {
            this.router.navigate(['app/main/purchase/purchaseReturns']);
        } else {
            this._location.back(); /// needs to be changed accordingly later
        }

        timer(50)
            .pipe(first())
            .subscribe(() => {
                //this.cartService.toggleIsPdf(false)
            });
    }
}
