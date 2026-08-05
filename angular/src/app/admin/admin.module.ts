import { NgModule } from '@angular/core';
import { AdminRoutingModule } from './admin-routing.module';
import { TreeDragDropService } from '@shared/ui-compat';
import {
    BsDatepickerConfig,
    BsDatepickerModule,
    BsDaterangepickerConfig,
    BsLocaleService,
} from 'ngx-bootstrap/datepicker';
import { NgxBootstrapDatePickerConfigService } from '../../assets/ngx-bootstrap/ngx-bootstrap-datepicker-config.service';
import { ModalModule } from 'ngx-bootstrap/modal';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';

@NgModule({
    imports: [
        AdminRoutingModule,
        ModalModule,
        TabsModule,
        TooltipModule,
        PopoverModule,
        BsDropdownModule,
        BsDatepickerModule,
    ],
    declarations: [],
    exports: [],
    providers: [
        TreeDragDropService,
        { provide: BsDatepickerConfig, useFactory: NgxBootstrapDatePickerConfigService.getDatepickerConfig },
        { provide: BsDaterangepickerConfig, useFactory: NgxBootstrapDatePickerConfigService.getDaterangepickerConfig },
        { provide: BsLocaleService, useFactory: NgxBootstrapDatePickerConfigService.getDatepickerLocale },
    ],
})
export class AdminModule {}
