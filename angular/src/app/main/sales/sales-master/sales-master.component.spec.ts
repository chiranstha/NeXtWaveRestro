import {ComponentFixture, TestBed} from '@angular/core/testing';

import {SalesMasterComponent} from './sales-master.component';

describe('SalesMasterComponent', () => {
    let component: SalesMasterComponent;
    let fixture: ComponentFixture<SalesMasterComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [SalesMasterComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(SalesMasterComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
