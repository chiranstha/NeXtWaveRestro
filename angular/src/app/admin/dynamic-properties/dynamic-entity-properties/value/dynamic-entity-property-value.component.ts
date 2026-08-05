import { Component, Injector, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute, Params } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ManagerComponent } from '@app/admin/dynamic-properties/dynamic-entity-properties/value/manager.component';
import { SubHeaderComponent } from '../../../../shared/common/sub-header/sub-header.component';
import { ManagerComponent as ManagerComponent_1 } from './manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './dynamic-entity-property-value.component.html',
    animations: [appModuleAnimation],
    imports: [SubHeaderComponent, ManagerComponent_1, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DynamicEntityPropertyValueComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);
    @ViewChild('dynamicEntityPropertyValueManager', { static: false })
    dynamicEntityPropertyValueManager: ManagerComponent;
    entityFullName: string;
    entityId: string;
    initialized = false;
    constructor() {
        const _injector = inject(Injector);
        super();
    }
    ngOnInit() {
        this._activatedRoute.params.subscribe((params: Params) => {
            this.entityFullName = params['entityFullName'];
            this.entityId = params['rowId'];
            this.initialized = true;
        });
    }
    saveAll(): void {
        this.dynamicEntityPropertyValueManager.saveAll();
    }
}
