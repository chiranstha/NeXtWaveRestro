import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddFinancialyearComponent } from './add-financialyear.component';
describe('AddFinancialyearComponent', () => {
    let component: AddFinancialyearComponent;
    let fixture: ComponentFixture<AddFinancialyearComponent>;
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AddFinancialyearComponent],
        }).compileComponents();
    });
    beforeEach(() => {
        fixture = TestBed.createComponent(AddFinancialyearComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('should create', () => {
        expect(component).toBeTruthy();
    });
});



