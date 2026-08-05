import {ComponentFixture, TestBed} from '@angular/core/testing';

import {PurchaseRegisterNewComponent} from './purchase-register-new.component';

describe('PurchaseRegisterNewComponent', () => {
    let component: PurchaseRegisterNewComponent;
    let fixture: ComponentFixture<PurchaseRegisterNewComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [PurchaseRegisterNewComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(PurchaseRegisterNewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
