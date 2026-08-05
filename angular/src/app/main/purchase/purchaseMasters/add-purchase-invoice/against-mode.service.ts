import { Injectable } from '@angular/core';

export class AgainstMode {
    id: number;
    name: string;
}

export class ModeType {
    id: number | null;
    againstid: number;
    name: string;
}

@Injectable({
    providedIn: 'root',
})
export class AgainstModeService {
    getAgainstMode() {
        return [
            { id: 0, name: 'N/A' },
            { id: 1, name: 'Purchase Order' },
        ] as AgainstMode[];
    }

    getModeType() {
        return [
            { id: null, againstid: 1, name: 'N/A' },
            { id: 0, againstid: 1, name: 'Purchase Order' },
        ] as ModeType[];
    }
}
