import { Pipe, PipeTransform, inject } from '@angular/core';
import { DateTime } from 'luxon';
import { NepaliDatepickerService } from '@app/shared/common/nepalidatepicker/services/nepali-datepicker-angular.service';

@Pipe({ name: 'restaurantNepaliDate' })
export class RestaurantNepaliDatePipe implements PipeTransform {
    private readonly nepaliDates = inject(NepaliDatepickerService);

    transform(value: unknown, includeTime = false): string {
        if (value === null || value === undefined || value === '') {
            return '-';
        }

        const dateTime = DateTime.isDateTime(value)
            ? (value as DateTime)
            : value instanceof Date
              ? DateTime.fromJSDate(value)
              : DateTime.fromISO(String(value), { setZone: true });
        if (!dateTime.isValid) {
            return String(value);
        }

        const kathmanduTime = dateTime.setZone('Asia/Kathmandu');
        const date = this.nepaliDates.ADToBS(kathmanduTime.toISODate()!, 'yyyy-mm-dd');
        return includeTime ? `${date} ${kathmanduTime.toFormat('HH:mm')} BS` : `${date} BS`;
    }
}
