import { Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityDto, TenantServiceProxy, UpdateTenantFeaturesInput } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { FeatureTreeComponent } from '../shared/feature-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'tenantFeaturesModal',
    templateUrl: './tenant-features-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        FeatureTreeComponent,
        ButtonBusyDirective,
        TooltipDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class TenantFeaturesModalComponent extends AppComponentBase {
    private _tenantService = inject(TenantServiceProxy);
    @ViewChild('tenantFeaturesModal', { static: true }) modal: ModalDirective;
    @ViewChild('featureTree') featureTree: FeatureTreeComponent;
    active = false;
    saving = false;
    resettingFeatures = false;
    tenantId: number;
    tenantName: string;
    featureEditData: any = null;

    show(tenantId: number, tenantName: string): void {
        this.tenantId = tenantId;
        this.tenantName = tenantName;
        this.active = true;
        this.loadFeatures();
        this.modal.show();
    }
    loadFeatures(): void {
        const self = this;
        self._tenantService.getTenantFeaturesForEdit(this.tenantId).subscribe((result) => {
            self.featureTree.editData = result;
        });
    }
    save(): void {
        if (!this.featureTree.areAllValuesValid()) {
            this.message.warn(this.l('InvalidFeaturesWarning'));
            return;
        }
        const input = new UpdateTenantFeaturesInput();
        input.id = this.tenantId;
        input.featureValues = this.featureTree.getGrantedFeatures();
        this.saving = true;
        this._tenantService
            .updateTenantFeatures(input)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
            });
    }
    resetFeatures(): void {
        const input = new EntityDto();
        input.id = this.tenantId;
        this.resettingFeatures = true;
        this._tenantService
            .resetTenantSpecificFeatures(input)
            .pipe(finalize(() => (this.resettingFeatures = false)))
            .subscribe(() => {
                this.notify.info(this.l('ResetSuccessfully'));
                this.loadFeatures();
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
