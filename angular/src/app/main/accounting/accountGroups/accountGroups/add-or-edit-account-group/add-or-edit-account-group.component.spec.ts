import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddOrEditAccountGroupComponent } from './add-or-edit-account-group.component';
describe('AddOrEditAccountGroupComponent', () => {
    let component: AddOrEditAccountGroupComponent;
    let fixture: ComponentFixture<AddOrEditAccountGroupComponent>;
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AddOrEditAccountGroupComponent],
        }).compileComponents();
    });
    beforeEach(() => {
        fixture = TestBed.createComponent(AddOrEditAccountGroupComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('should create', () => {
        expect(component).toBeTruthy();
    });
});



