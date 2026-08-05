import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-purchase-register',
    templateUrl: './purchase-register.component.html',
    styleUrls: ['./purchase-register.component.css']
})
export class PurchaseRegisterComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

}
