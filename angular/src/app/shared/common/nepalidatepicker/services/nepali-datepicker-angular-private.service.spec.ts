import { TestBed } from '@angular/core/testing';
import { NepaliDatepickerPrivateService } from './nepali-datepicker-angular-private.service';
describe('NepaliDatepickerPrivateService', () => {
    let service: NepaliDatepickerPrivateService;
    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(NepaliDatepickerPrivateService);
    });
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});
