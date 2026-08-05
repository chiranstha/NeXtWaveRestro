import { Pipe, PipeTransform } from '@angular/core';
import { formatNepali } from './number-formatter';
@Pipe({
    name: 'nepaliNumber',
    standalone: true,
})
export class NepaliNumberPipe implements PipeTransform {
    transform(value: number | string, decimals = 2, useNepaliDigits = true, useParenForNegative = true): string {
        return formatNepali(value, decimals, useNepaliDigits, useParenForNegative);
    }
}
