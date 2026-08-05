import {ComponentFixture, TestBed} from '@angular/core/testing';

import {SalesRegisterNewComponent} from './sales-register-new.component';

describe('SalesRegisterNewComponent', () => {
    let component: SalesRegisterNewComponent;
    let fixture: ComponentFixture<SalesRegisterNewComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [SalesRegisterNewComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(SalesRegisterNewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
