import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-tds-report',
    templateUrl: './tds-report.component.html',
    styleUrls: ['./tds-report.component.css']
})
export class TDSReportComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

}
