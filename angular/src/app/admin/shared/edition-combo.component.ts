import {
    Component,
    ElementRef,
    EventEmitter,
    Input,
    OnInit,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ComboboxItemDto, EditionServiceProxy } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
@Component({
    selector: 'edition-combo',
    template: `
        <select
            #EditionCombobox
            class="form-select"
            [(ngModel)]="selectedEdition"
            (ngModelChange)="selectedEditionChange.emit($event)"
        >
            @for (edition of editions; track edition) {
                <option [value]="edition.value">{{ edition.displayText }}</option>
            }
        </select>
    `,
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EditionComboComponent extends AppComponentBase implements OnInit {
    private _editionService = inject(EditionServiceProxy);
    @ViewChild('EditionCombobox', { static: true }) editionComboboxElement: ElementRef;
    @Input() selectedEdition: string = undefined;
    @Output() selectedEditionChange: EventEmitter<string> = new EventEmitter<string>();
    editions: ComboboxItemDto[] = [];

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this._editionService.getEditionComboboxItems(0, true, false).subscribe((editions) => {
            this.editions = editions;
        });
    }
}
