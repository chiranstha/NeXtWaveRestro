import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddAccountLedgersComponent } from './add-account-ledgers.component';
describe('AddAccountLedgersComponent', () => {
    let component: AddAccountLedgersComponent;
    let fixture: ComponentFixture<AddAccountLedgersComponent>;
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AddAccountLedgersComponent],
        }).compileComponents();
    });
    beforeEach(() => {
        fixture = TestBed.createComponent(AddAccountLedgersComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('should create', () => {
        expect(component).toBeTruthy();
    });
});



