import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TrialbyGroupComponent } from './trialby-group.component';
describe('TrialbyGroupComponent', () => {
    let component: TrialbyGroupComponent;
    let fixture: ComponentFixture<TrialbyGroupComponent>;
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TrialbyGroupComponent],
        }).compileComponents();
    });
    beforeEach(() => {
        fixture = TestBed.createComponent(TrialbyGroupComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('should create', () => {
        expect(component).toBeTruthy();
    });
});



