import { AppConsts } from '@shared/AppConsts';
export class LocaleMappingService {
    map(mappingSource: string, locale: string): string {
        if (!AppConsts.localeMappings && !AppConsts.localeMappings[mappingSource]) {
            return locale;
        }
        const localeMappings = AppConsts.localeMappings[mappingSource].filter((mapping) => mapping.from === locale);
        if (localeMappings?.length) {
            return localeMappings[0]['to'];
        }
        return locale;
    }
}
