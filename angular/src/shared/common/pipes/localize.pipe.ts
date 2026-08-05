import { Pipe, PipeTransform, inject } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { LocalizationService } from 'abp-ng2-module';
@Pipe({
    name: 'localize',
    pure: false,
})
export class LocalizePipe implements PipeTransform {
    localizationSourceName = AppConsts.localization.defaultLocalizationSourceName;
    localization = inject(LocalizationService);
    l(key: string, ...args: any[]): string {
        args.unshift(key);
        args.unshift(this.localizationSourceName);
        return this.ls.apply(this, args);
    }
    ls(sourcename: string, key: string, ...args: any[]): string {
        let localizedText = this.localization.localize(key, sourcename);
        if (!localizedText) {
            localizedText = key;
        }
        if (!args?.length) {
            return localizedText;
        }
        args.unshift(localizedText);
        return abp.utils.formatString.apply(this, this.flattenDeep(args));
    }
    transform(key: string, ...args: any[]): string {
        return this.l(key, args);
    }
    flattenDeep(array) {
        return array.reduce(
            (acc, val) => (Array.isArray(val) ? acc.concat(this.flattenDeep(val)) : acc.concat(val)),
            [],
        );
    }
}
