import { Component, Input, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppLocalizationService } from '@app/shared/common/localization/app-localization.service';
class ErrorDef {
    error: string;
    localizationKey: string;
    errorProperty: string;
}
@Component({
    selector: '<validation-messages>',
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `
        @if (formCtrl.invalid && (formCtrl.dirty || formCtrl.touched)) {
            <div class="has-danger">
                @for (errorDef of errorDefsInternal; track errorDef) {
                    <div>
                        @if (getErrorDefinitionIsInValid(errorDef)) {
                            <div class="form-control-feedback">
                                {{ getErrorDefinitionMessage(errorDef) }}
                            </div>
                        }
                    </div>
                }
            </div>
        }
    `,
})
export class ValidationMessagesComponent {
    private appLocalizationService = inject(AppLocalizationService);
    @Input() formCtrl;
    _errorDefs: ErrorDef[] = [];
    readonly standartErrorDefs: ErrorDef[] = [
        { error: 'required', localizationKey: 'ThisFieldIsRequired' } as ErrorDef,
        {
            error: 'minlength',
            localizationKey: 'PleaseEnterAtLeastNCharacter',
            errorProperty: 'requiredLength',
        },
        {
            error: 'maxlength',
            localizationKey: 'PleaseEnterNoMoreThanNCharacter',
            errorProperty: 'requiredLength',
        },
        { error: 'email', localizationKey: 'InvalidEmailAddress' } as ErrorDef,
        { error: 'min', localizationKey: 'ValueMustBeBiggerThanOrEqualToX', errorProperty: 'min' },
        { error: 'max', localizationKey: 'ValueMustBeSmallerThanOrEqualToX', errorProperty: 'max' },
        { error: 'pattern', localizationKey: 'InvalidPattern', errorProperty: 'requiredPattern' },
    ];
    constructor() {}
    get errorDefsInternal(): ErrorDef[] {
        const standarts = this.standartErrorDefs.filter((ed) => !this._errorDefs.find((edC) => edC.error === ed.error));
        const all = [...standarts, ...this._errorDefs];
        return all;
    }
    @Input() set errorDefs(value: ErrorDef[]) {
        this._errorDefs = value;
    }
    getErrorDefinitionIsInValid(errorDef: ErrorDef): boolean {
        return !!this.formCtrl.errors[errorDef.error];
    }
    getErrorDefinitionMessage(errorDef: ErrorDef): string {
        const errorRequirement = this.formCtrl.errors[errorDef.error][errorDef.errorProperty];
        return errorRequirement
            ? this.appLocalizationService.l(errorDef.localizationKey, errorRequirement)
            : this.appLocalizationService.l(errorDef.localizationKey);
    }
}
