import {ComponentFixture, TestBed} from '@angular/core/testing';

import {AddReceiptNewComponent} from './add-receipt-new.component';

describe('AddReceiptNewComponent', () => {
    let component: AddReceiptNewComponent;
    let fixture: ComponentFixture<AddReceiptNewComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [AddReceiptNewComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(AddReceiptNewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
