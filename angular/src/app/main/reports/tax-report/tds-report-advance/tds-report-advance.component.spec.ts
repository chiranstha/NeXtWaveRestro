import {ComponentFixture, TestBed} from '@angular/core/testing';

import {TDSReportAdvanceComponent} from './tds-report-advance.component';

describe('TDSReportAdvanceComponent', () => {
    let component: TDSReportAdvanceComponent;
    let fixture: ComponentFixture<TDSReportAdvanceComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TDSReportAdvanceComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(TDSReportAdvanceComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
