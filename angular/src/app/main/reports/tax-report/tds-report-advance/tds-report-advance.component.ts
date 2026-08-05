import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-tds-report-advance',
    templateUrl: './tds-report-advance.component.html',
    styleUrls: ['./tds-report-advance.component.css']
})
export class TDSReportAdvanceComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

}
