import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy, NameValueOfString } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AutoComplete } from '@shared/ui-compat';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'demo-ui-selection',
    templateUrl: './demo-ui-selection.component.html',
    animations: [appModuleAnimation],
    imports: [FormsModule, AutoComplete, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DemoUiSelectionComponent extends AppComponentBase {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);
    filteredCountries: NameValueOfString[];
    country: any;
    countries: NameValueOfString[] = new Array<NameValueOfString>();

    // get countries
    filterCountries(event): void {
        this.demoUiComponentsService.getCountries(event.query).subscribe((countries) => {
            this.filteredCountries = countries;
        });
    }
    // single select - post
    submitSelectedCountry(): void {
        const selectedCountries = new Array<NameValueOfString>();
        selectedCountries.push(this.country);
        this.demoUiComponentsService
            .sendAndGetSelectedCountries(selectedCountries)
            .subscribe((countries: NameValueOfString[]) => {
                let message = '';
                countries.forEach((item) => {
                    message += `<div><strong>id</strong>: ${item.value} - <strong>name</strong>: ${item.name}</div>`;
                });
                this.message.info(message, this.l('PostedValue'), { isHtml: true });
            });
    }
    // multi select - post
    submitSelectedCountries(): void {
        this.demoUiComponentsService
            .sendAndGetSelectedCountries(this.countries)
            .subscribe((countries: NameValueOfString[]) => {
                let message = '';
                countries.forEach((item) => {
                    message += `<div><strong>id</strong>: ${item.value} - <strong>name</strong>: ${item.name}</div>`;
                });
                this.message.info(message, this.l('PostedValue'), { isHtml: true });
            });
    }
}
