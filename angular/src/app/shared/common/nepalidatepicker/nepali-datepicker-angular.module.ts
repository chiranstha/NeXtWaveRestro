import { ModuleWithProviders, NgModule } from '@angular/core';
import { ToNpPipe } from './pipes/to-np.pipe';
import { CommonModule, DatePipe } from '@angular/common';
import { ConfigType } from './interface/interface';
import { FormsModule } from '@angular/forms';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { NepaliDatepickerComponent } from './nepali-datepicker-angular.component';

@NgModule({
    imports: [CommonModule, FormsModule, BsDatepickerModule, ToNpPipe, NepaliDatepickerComponent],
    providers: [DatePipe],
    exports: [NepaliDatepickerComponent],
})
export class NepaliDatepickerModule {
    static forRoot(config: ConfigType): ModuleWithProviders<NepaliDatepickerModule> {
        return {
            ngModule: NepaliDatepickerModule,
            providers: [
                {
                    provide: 'config',
                    useValue: config,
                },
            ],
        };
    }
}

// Standalone version for Angular 21 compatibility
export const provideNepaliDatepicker = () => [
    DatePipe,
    {
        provide: 'config',
        useValue: {} as ConfigType, // Default empty config
    },
];
