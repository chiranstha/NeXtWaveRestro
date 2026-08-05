import {ComponentFixture, TestBed} from '@angular/core/testing';

import {AddSalesMasterComponent} from './add-sales-master.component';

describe('AddSalesMasterComponent', () => {
    let component: AddSalesMasterComponent;
    let fixture: ComponentFixture<AddSalesMasterComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [AddSalesMasterComponent]
        })
            .compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(AddSalesMasterComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
