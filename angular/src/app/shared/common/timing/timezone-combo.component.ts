import { Component, DestroyRef, forwardRef, Input, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NameValueDto, SettingScopes, TimingServiceProxy } from '@shared/service-proxies/service-proxies';
import {
    ControlValueAccessor,
    NG_VALUE_ACCESSOR,
    UntypedFormControl,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
@Component({
    selector: 'timezone-combo',
    template: `
        <select class="form-select" [formControl]="selectedTimeZone">
            @for (timeZone of timeZones; track timeZone) {
                <option [value]="timeZone.value">{{ timeZone.name }}</option>
            }
        </select>
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => TimeZoneComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, ReactiveFormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class TimeZoneComboComponent extends AppComponentBase implements OnInit, ControlValueAccessor {
    private _timingService = inject(TimingServiceProxy);
    private destroyRef = inject(DestroyRef);
    @Input() defaultTimezoneScope: SettingScopes;
    timeZones: NameValueDto[] = [];
    selectedTimeZone = new UntypedFormControl('');

    onTouched: any = () => {};
    ngOnInit(): void {
        const self = this;
        self._timingService
            .getTimezones(self.defaultTimezoneScope)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((result) => {
                self.timeZones = result.items;
            });
    }
    writeValue(obj: any): void {
        if (this.selectedTimeZone) {
            this.selectedTimeZone.setValue(obj);
        }
    }
    registerOnChange(fn: any): void {
        this.selectedTimeZone.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(fn);
    }
    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.selectedTimeZone.disable();
        } else {
            this.selectedTimeZone.enable();
        }
    }
}
