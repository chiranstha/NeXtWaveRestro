import { ChangeDetectorRef, Component, forwardRef, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { GetRolesInput, RoleListDto, RoleServiceProxy } from '@shared/service-proxies/service-proxies';
import {
    ControlValueAccessor,
    NG_VALUE_ACCESSOR,
    UntypedFormControl,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'role-combo',
    template: `
        <select class="form-select" [formControl]="selectedRole">
            <option value="">{{ 'FilterByRole' | localize }}</option>
            @for (role of roles; track role.id) {
                <option [value]="role.id">{{ role.displayName }}</option>
            }
        </select>
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => RoleComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, ReactiveFormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class RoleComboComponent extends AppComponentBase implements OnInit, ControlValueAccessor {
    private _roleService = inject(RoleServiceProxy);
    private _cdr = inject(ChangeDetectorRef);
    roles: RoleListDto[] = [];
    selectedRole = new UntypedFormControl('');

    onTouched: any = () => {};
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this._roleService.getRoles(new GetRolesInput({ permissions: [] })).subscribe((result) => {
            setTimeout(() => {
                this.roles = result.items || [];
                this._cdr.markForCheck();
            });
        });
    }
    writeValue(obj: any): void {
        if (this.selectedRole) {
            this.selectedRole.setValue(obj ?? '', { emitEvent: false });
        }
    }
    registerOnChange(fn: any): void {
        this.selectedRole.valueChanges.subscribe(fn);
    }
    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.selectedRole.disable();
        } else {
            this.selectedRole.enable();
        }
    }
}
