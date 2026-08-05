import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-sales-register-new',
    templateUrl: './sales-register-new.component.html',
    styleUrls: ['./sales-register-new.component.css']
})
export class SalesRegisterNewComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

}
