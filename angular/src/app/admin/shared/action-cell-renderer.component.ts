import { Component, ChangeDetectionStrategy } from '@angular/core';
@Component({
    selector: 'app-action-cell-renderer',
    templateUrl: './action-cell-renderer.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './action-cell-renderer.component.css',
})
export class ActionCellRendererComponent {
    params: any;
    agInit(params: any): void {
        this.params = params;
    }
    edit(): void {
        this.params.context.componentParent.onEdit(this.params.data);
    }
    delete(): void {
        this.params.context.componentParent.onDelete(this.params.data);
    }
    print(): void {
        this.params.context.componentParent.onPrint(this.params.data);
    }
}
