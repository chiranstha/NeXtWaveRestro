import { Component, forwardRef, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AbpLoginResultType, NameValueDto, SettingScopes } from '@shared/service-proxies/service-proxies';
import {
    ControlValueAccessor,
    NG_VALUE_ACCESSOR,
    UntypedFormControl,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';
@Component({
    selector: 'login-result-type-combo',
    template: `
        <select class="form-select" [formControl]="selectedLoginResultType">
            @for (loginResultType of loginResultTypes; track loginResultType) {
                <option [value]="loginResultType.value">
                    {{ loginResultType.name }}
                </option>
            }
        </select>
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => LoginResultTypeComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, ReactiveFormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class LoginResultTypeComboComponent extends AppComponentBase implements OnInit, ControlValueAccessor {
    @Input() defaultTimezoneScope: SettingScopes;
    loginResultTypes: NameValueDto[] = [];
    selectedLoginResultType = new UntypedFormControl('');
    loginResultType: AbpLoginResultType;

    onTouched: any = () => {};
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.loginResultTypes.push(new NameValueDto({ name: this.l('All'), value: '' }));
        for (const value in AbpLoginResultType) {
            if (typeof AbpLoginResultType[value] === 'string') {
                continue;
            }
            this.loginResultTypes.push(new NameValueDto({ name: this.l(`AbpLoginResultType_${value}`), value }));
        }
    }
    writeValue(obj: any): void {
        if (this.selectedLoginResultType) {
            this.selectedLoginResultType.setValue(obj);
        }
    }
    registerOnChange(fn: any): void {
        this.selectedLoginResultType.valueChanges.subscribe(fn);
    }
    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.selectedLoginResultType.disable();
        } else {
            this.selectedLoginResultType.enable();
        }
    }
}
