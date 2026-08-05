import { Component, Injector, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityAndPropertyChangeListDto, EntityChangeServiceProxy } from '@shared/service-proxies/service-proxies';
import { ActivatedRoute } from '@angular/router';
import { NgClass, DatePipe } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './entity-changes.component.html',
    styleUrls: ['./entity-changes.component.less'],
    animations: [appModuleAnimation],
    imports: [NgClass, DatePipe, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EntityChangesComponent extends AppComponentBase implements OnInit {
    private _entityChangeService = inject(EntityChangeServiceProxy);
    private _activatedRoute = inject(ActivatedRoute);
    entityAndPropertyChanges: EntityAndPropertyChangeListDto[] = [];
    entityTypeFullName: string;
    entityTypeShortName: string;
    entityId: string;
    constructor() {
        const _injector = inject(Injector);
        super();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.loadEntityChanges();
    }
    loadEntityChanges(): void {
        this.entityId = this._activatedRoute.snapshot.params['entityId'];
        this.entityTypeFullName = this._activatedRoute.snapshot.params['entityTypeFullName'];
        this.entityTypeShortName = this.entityTypeFullName.split('.').pop();
        this._entityChangeService.getEntityChangesByEntity(this.entityTypeFullName, this.entityId).subscribe((data) => {
            this.entityAndPropertyChanges = data.items;
        });
    }
}
