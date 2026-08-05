import { FormGroup } from '@angular/forms';

type ConfirmCallback = (isConfirmed: boolean) => void;

export interface ConfirmDialogService {
    confirm(title: string, message: string, callback: ConfirmCallback): void;
}

export interface NotifyService {
    error(message: string): void;
}

export function isFormValidOrNotify(
    form: FormGroup,
    notify?: NotifyService,
    message = 'Form is invalid!!',
): boolean {
    if (form.valid) {
        return true;
    }

    form.markAllAsTouched();
    notify?.error(message);
    return false;
}

export function trimStringControls(form: FormGroup, keys: string[]): void {
    keys.forEach((key) => {
        const control = form.get(key);
        const value = control?.value;
        if (typeof value === 'string') {
            control.setValue(value.trim(), { emitEvent: false });
        }
    });
}

export function confirmSave(
    dialog: ConfirmDialogService,
    localize: (text: string) => string,
    isEdit: boolean,
    onConfirmed: () => void,
): void {
    const message = `Do you want to ${isEdit ? 'Update' : 'Save'} ?`;
    dialog.confirm('', localize(message), (isConfirmed) => {
        if (isConfirmed) {
            onConfirmed();
        }
    });
}

export function confirmCancel(
    dialog: ConfirmDialogService,
    localize: (text: string) => string,
    onConfirmed: () => void,
): void {
    dialog.confirm('', localize('Do you want to Cancel ?'), (isConfirmed) => {
        if (isConfirmed) {
            onConfirmed();
        }
    });
}

export function confirmDelete(
    dialog: ConfirmDialogService,
    localize: (text: string) => string,
    onConfirmed: () => void,
    prompt = 'Are you sure you want to Delete ?',
): void {
    dialog.confirm('', localize(prompt), (isConfirmed) => {
        if (isConfirmed) {
            onConfirmed();
        }
    });
}
