import { enableProdMode, Injector, ErrorHandler, APP_INITIALIZER, LOCALE_ID, DEFAULT_CURRENCY_CODE, importProvidersFrom } from '@angular/core';
import { BrowserModule, bootstrapApplication } from '@angular/platform-browser';
import { environment } from './environments/environment';
import { hmrBootstrap } from './hmr';
import { getRemoteServiceBaseUrl, appInitializerFactory, getCurrencyCode, getCurrentLanguage } from './root.module';
import { provideAnimations } from '@angular/platform-browser/animations';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { PlatformLocation } from '@angular/common';
import { NgxSpinnerModule } from 'ngx-spinner';
import { GlobalErrorHandler } from '@shared/error-boundary.service';
import { SuktasCommonModule } from '@shared/common/common.module';
import { ServiceProxyModule } from '@shared/service-proxies/service-proxy.module';
import { HttpClientModule } from '@angular/common/http';
import { RootRoutingModule } from './root-routing.module';
import { AbpModule } from 'abp-ng2-module';
import { RootComponent } from './root.component';
import { AppModule } from './app/app.module'; // Keep for now to import all providers
import { provideNepaliDatepicker } from '@app/shared/common/nepalidatepicker/nepali-datepicker-angular.module';
import { provideGlobalGridOptions } from 'ag-grid-community';
import { setupAgGridEnterprise } from '@app/shared/common/ag-grid/ag-grid-enterprise-setup';

if (environment.production) {
    enableProdMode();
}

const bootstrap = () => {
    setupAgGridEnterprise();
    provideGlobalGridOptions(
        {
            theme: 'legacy',
            rowGroupPanelShow: 'never',
            defaultColDef: {
                suppressHeaderFilterButton: true,
                suppressHeaderMenuButton: true,
            },
        },
        'deep',
    );
    return bootstrapApplication(RootComponent, {
        providers: [
            importProvidersFrom(
                BrowserModule,
                AppModule,
                SuktasCommonModule.forRoot(),
                ServiceProxyModule,
                HttpClientModule,
                RootRoutingModule,
                NgxSpinnerModule,
                AbpModule,
            ),
            provideAnimations(),
            provideNepaliDatepicker(),
            { provide: API_BASE_URL, useFactory: getRemoteServiceBaseUrl },
            { provide: ErrorHandler, useClass: GlobalErrorHandler },
            {
                provide: APP_INITIALIZER,
                useFactory: appInitializerFactory,
                deps: [Injector, PlatformLocation],
                multi: true,
            },
            {
                provide: LOCALE_ID,
                useFactory: getCurrentLanguage,
            },
            {
                provide: DEFAULT_CURRENCY_CODE,
                useFactory: getCurrencyCode,
                deps: [Injector],
            },
        ],
    });
};

if (environment.hmr) {
    hmrBootstrap(module, bootstrap);
} else {
    bootstrap().catch((err) => console.log(err));
}
