import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HttpClientJsonpModule } from '@angular/common/http';

// Shared modules
import { UtilsModule } from '@shared/utils/utils.module';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { ServiceProxyModule } from '@shared/service-proxies/service-proxy.module';

// Third-party modules
import { IMaskModule } from 'angular-imask';
import { FileUploadModule } from 'ng2-file-upload';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ModalModule } from 'ngx-bootstrap/modal';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { NgxSpinnerModule } from 'ngx-spinner';
import { NgSelectComponent } from '@ng-select/ng-select';

// Shared components
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

// Pipes and directives
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { AutoFocusDirective } from '@shared/utils/auto-focus.directive';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';

export const SHARED_IMPORTS = [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    HttpClientJsonpModule,
    UtilsModule,
    AppCommonModule,
    ServiceProxyModule,
    IMaskModule,
    FileUploadModule,
    BsDatepickerModule,
    BsDropdownModule,
    ModalModule,
    PopoverModule,
    TabsModule,
    NgScrollbarModule,
    NgxSpinnerModule,
    SubHeaderComponent,
    // NepaliDatepickerModule,
    LocalizePipe,
    PermissionPipe,
    ButtonBusyDirective,
    AutoFocusDirective,
    BusyIfDirective,
    NgSelectComponent,
];
