import {ComponentFixture, TestBed} from '@angular/core/testing';

import {AddPurchaseInvoiceReturnComponent} from './add-purchase-invoice-return.component';

describe('AddPurchaseInvoiceReturnComponent', () => {
    let component: AddPurchaseInvoiceReturnComponent;
    let fixture: ComponentFixture<AddPurchaseInvoiceReturnComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [AddPurchaseInvoiceReturnComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(AddPurchaseInvoiceReturnComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
