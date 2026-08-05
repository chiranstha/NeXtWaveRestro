import {ComponentFixture, TestBed} from '@angular/core/testing';

import {PurchaseTaxReportComponent} from './purchase-tax-report.component';

describe('PurchaseTaxReportComponent', () => {
    let component: PurchaseTaxReportComponent;
    let fixture: ComponentFixture<PurchaseTaxReportComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [PurchaseTaxReportComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(PurchaseTaxReportComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
