import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-sales-register',
    templateUrl: './sales-register.component.html',
    styleUrls: ['./sales-register.component.css']
})
export class SalesRegisterComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

}
