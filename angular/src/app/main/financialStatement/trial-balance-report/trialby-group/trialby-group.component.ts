import { Component, OnDestroy, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TrailBalanceReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { TreeNode } from '@shared/ui-compat';
import { Subject } from 'rxjs';
@Component({
    standalone: false,
    selector: 'app-trialby-group',
    templateUrl: './trialby-group.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./trialby-group.component.css'],
})
export class TrialbyGroupComponent extends AppComponentBase implements OnInit, OnDestroy {
    private route = inject(ActivatedRoute);
    private _proxy = inject(TrailBalanceReportServiceProxy);
    loading: boolean;
    branchId = signal<number>(0);
    group = signal<number>(0);
    treeData: TreeNode[];
    cols: any[];
    private destroy$: Subject<void> = new Subject<void>();

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        const bid = this.route.snapshot.params['bid'];
        const gid = this.route.snapshot.params['gid'];
        this.branchId.set(bid);
        this.group.set(gid);
        this.loadReport();
    }
    loadReport() {}
    ngOnDestroy(): void {
        this.destroy$.next();

        this.destroy$.complete();
    }
}
