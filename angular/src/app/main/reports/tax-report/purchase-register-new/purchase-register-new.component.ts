import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-purchase-register-new',
    templateUrl: './purchase-register-new.component.html',
    styleUrls: ['./purchase-register-new.component.css']
})
export class PurchaseRegisterNewComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

}
