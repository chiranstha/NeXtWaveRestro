import {ComponentFixture, TestBed} from '@angular/core/testing';

import {TDSReportComponent} from './tds-report.component';

describe('TDSReportComponent', () => {
    let component: TDSReportComponent;
    let fixture: ComponentFixture<TDSReportComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TDSReportComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(TDSReportComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
