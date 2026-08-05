import {ComponentFixture, TestBed} from '@angular/core/testing';

import {TaxForLakhsPurchaseComponent} from './tax-for-lakhs-purchase.component';

describe('TaxForLakhsPurchaseComponent', () => {
    let component: TaxForLakhsPurchaseComponent;
    let fixture: ComponentFixture<TaxForLakhsPurchaseComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TaxForLakhsPurchaseComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(TaxForLakhsPurchaseComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
