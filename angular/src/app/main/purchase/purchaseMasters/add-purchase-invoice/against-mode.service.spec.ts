import {TestBed} from '@angular/core/testing';

import {AgainstModeService} from './against-mode.service';

describe('AgainstModeService', () => {
    let service: AgainstModeService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(AgainstModeService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});
