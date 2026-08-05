import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { MergeLedgerComponent } from './merge-ledger.component';
describe('MergeLedgerComponent', () => {
    let component: MergeLedgerComponent;
    let fixture: ComponentFixture<MergeLedgerComponent>;
    beforeEach(async(() => {
        TestBed.configureTestingModule({
            imports: [MergeLedgerComponent],
        }).compileComponents();
    }));
    beforeEach(() => {
        fixture = TestBed.createComponent(MergeLedgerComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('should create', () => {
        expect(component).toBeTruthy();
    });
});



