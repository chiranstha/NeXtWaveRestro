import {ComponentFixture, TestBed} from '@angular/core/testing';

import {TaxForLakhsSalesComponent} from './tax-for-lakhs-sales.component';

describe('TaxForLakhsSalesComponent', () => {
    let component: TaxForLakhsSalesComponent;
    let fixture: ComponentFixture<TaxForLakhsSalesComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TaxForLakhsSalesComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(TaxForLakhsSalesComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
