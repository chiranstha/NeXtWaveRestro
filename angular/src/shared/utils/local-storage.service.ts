import { Injectable } from '@angular/core';
import * as localForage from 'localforage';
const lf: any = (localForage as any).default ?? localForage;
@Injectable()
export class LocalStorageService {
    getItem(key: string, callback: any): void {
        if (!lf) {
            return;
        }
        lf.getItem(key, callback);
    }
    setItem(key, value, callback?: any): void {
        if (!lf) {
            return;
        }
        if (value === null) {
            value = undefined;
        }
        lf.setItem(key, value, callback);
    }
    removeItem(key, callback?: any): void {
        if (!lf) {
            return;
        }
        lf.removeItem(key, callback);
    }
}
