import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy } from '@shared/service-proxies/service-proxies';
import { Editor } from '@shared/ui-compat';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'demo-ui-editor',
    templateUrl: './demo-ui-editor.component.html',
    animations: [appModuleAnimation],
    imports: [Editor, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class DemoUiEditorComponent extends AppComponentBase {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);
    htmlEditorInput: string;

    // input mask - post
    submitValue(): void {
        this.demoUiComponentsService.sendAndGetValue(this.htmlEditorInput).subscribe((data) => {
            this.message.info(data.output, this.l('PostedValue'), { isHtml: true });
        });
    }
}
