import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MultipleLedgerComponent } from './multiple-ledger.component';
describe('MultipleLedgerComponent', () => {
    let component: MultipleLedgerComponent;
    let fixture: ComponentFixture<MultipleLedgerComponent>;
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [MultipleLedgerComponent],
        }).compileComponents();
    });
    beforeEach(() => {
        fixture = TestBed.createComponent(MultipleLedgerComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('should create', () => {
        expect(component).toBeTruthy();
    });
});



