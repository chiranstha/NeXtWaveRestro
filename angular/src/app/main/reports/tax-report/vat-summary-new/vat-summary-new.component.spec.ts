/* eslint-disable @typescript-eslint/no-unused-vars */
import {async, ComponentFixture, TestBed} from '@angular/core/testing';

import {VatSummaryNewComponent} from './vat-summary-new.component';

describe('VatSummaryNewComponent', () => {
    let component: VatSummaryNewComponent;
    let fixture: ComponentFixture<VatSummaryNewComponent>;

    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [VatSummaryNewComponent]
        })
            .compileComponents();
    }));

    beforeEach(() => {
        fixture = TestBed.createComponent(VatSummaryNewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
