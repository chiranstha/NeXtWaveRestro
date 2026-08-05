import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'password-input-with-show-button',
    templateUrl: './password-input-with-show-button.component.html',
    imports: [FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PasswordInputWithShowButtonComponent {
    @Input() data: string;
    @Output() dataChange = new EventEmitter();
    isVisible = false;
    toggleVisibility(): void {
        this.isVisible = !this.isVisible;
    }
    onChange() {
        this.dataChange.emit(this.data);
    }
}
