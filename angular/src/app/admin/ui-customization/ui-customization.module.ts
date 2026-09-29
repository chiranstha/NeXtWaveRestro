import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { UICustomizationRoutingModule } from './ui-customization-routing.module';
import { UiCustomizationComponent } from './ui-customization.component';
import { DefaultThemeUiSettingsComponent } from './default-theme-ui-settings.component';
import { Theme8ThemeUiSettingsComponent } from './theme8-theme-ui-settings.component';
import { Theme11ThemeUiSettingsComponent } from './theme11-theme-ui-settings.component';
@NgModule({
    imports: [
        AppSharedModule,
        AdminSharedModule,
        UICustomizationRoutingModule,
        UiCustomizationComponent,
        DefaultThemeUiSettingsComponent,
        Theme8ThemeUiSettingsComponent,
        Theme11ThemeUiSettingsComponent,
    ],
})
export class UICustomizationModule {}
