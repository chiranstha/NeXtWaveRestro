import {
    AfterContentInit,
    AfterViewInit,
    Component,
    ContentChildren,
    Directive,
    ElementRef,
    EventEmitter,
    HostListener,
    Injectable,
    Input,
    NgModule,
    Output,
    QueryList,
    TemplateRef,
    forwardRef,
    inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface LazyLoadEvent {
    first?: number;
    rows?: number;
    sortField?: string;
    sortOrder?: number;
    multiSortMeta?: SortMeta[];
}

export interface SortMeta {
    field: string;
    order: number;
}

export interface TreeNode {
    label?: string;
    data?: any;
    children?: TreeNode[];
    parent?: TreeNode;
    expanded?: boolean;
    expandedIcon?: string;
    collapsedIcon?: string;
    icon?: string;
    selectable?: boolean;
    selected?: boolean;
    styleClass?: string;
    value?: any;
    [key: string]: any;
}

export interface MenuItem {
    label?: string;
    disabled?: boolean;
    command?: (event?: { originalEvent?: Event; item?: MenuItem }) => void;
    [key: string]: any;
}

export interface SelectItem<T = any> {
    label?: string;
    value?: T;
    [key: string]: any;
}

@Injectable()
export class TreeDragDropService {}

@Directive({
    selector: '[appTemplate]',
    standalone: true,
})
export class AppTemplate {
    type = 'default';

    constructor(public template: TemplateRef<any>) {}

    @Input('appTemplate')
    set name(value: string) {
        this.type = value || 'default';
    }
}

@Directive({
    selector: '[appBind]',
    standalone: true,
})
export class Bind {}

@Directive({
    selector: '[appPassword]',
    standalone: true,
})
export class PasswordDirective {
    @Input() promptLabel?: string;
    @Input() weakLabel?: string;
    @Input() mediumLabel?: string;
    @Input() strongLabel?: string;
}

@Directive({
    selector: '[appSortableColumn]',
    standalone: true,
})
export class SortableColumnDirective {
    private table = inject(Table, { optional: true });

    @Input('appSortableColumn') field = '';

    @HostListener('click')
    sort(): void {
        this.table?.sort(this.field);
    }
}

@Component({
    selector: 'app-sort-icon',
    standalone: true,
    template: '<i class="fa fa-sort ms-1 text-muted"></i>',
})
export class SortIcon {
    @Input() field = '';
}

@Component({
    selector: 'app-data-table',
    standalone: true,
    imports: [CommonModule],
    template: `
        <div class="app-datatable app-component" [ngStyle]="styleObject">
            <div class="app-datatable-wrapper" [style.overflow-x]="scrollable ? 'auto' : null">
                <table class="app-datatable-table table table-row-bordered table-hover mb-0" [ngStyle]="tableStyle">
                    <thead class="app-datatable-thead">
                        <ng-container *ngIf="headerTemplate" [ngTemplateOutlet]="headerTemplate"></ng-container>
                    </thead>
                    <tbody class="app-datatable-tbody">
                        <ng-container *ngFor="let row of value || []; trackBy: trackByIndex">
                            <ng-container
                                *ngIf="bodyTemplate"
                                [ngTemplateOutlet]="bodyTemplate"
                                [ngTemplateOutletContext]="{ $implicit: row, rowData: row }"
                            ></ng-container>
                        </ng-container>
                    </tbody>
                </table>
            </div>
        </div>
    `,
})
export class Table implements AfterContentInit, AfterViewInit {
    el = inject(ElementRef);

    @Input() value: any[] = [];
    @Input() lazy: any;
    @Input() paginator: any;
    @Input() rows: any = 10;
    @Input() scrollable: any;
    @Input() ScrollWidth?: string;
    @Input() tableStyle: Record<string, any>;
    @Input() style: Record<string, any> | string;
    @Input() sortMode: 'single' | 'multiple' | string = 'single';
    @Input() resizableColumns: any;

    @Output() onLazyLoad = new EventEmitter<LazyLoadEvent>();

    @ContentChildren(AppTemplate) templates: QueryList<AppTemplate>;

    sortField = '';
    sortOrder = 1;
    multiSortMeta: SortMeta[] = [];

    get headerTemplate(): TemplateRef<any> | null {
        return this.getTemplate('header');
    }

    get bodyTemplate(): TemplateRef<any> | null {
        return this.getTemplate('body');
    }

    get styleObject(): Record<string, any> | null {
        return typeof this.style === 'string' ? null : this.style || null;
    }

    ngAfterContentInit(): void {
        this.templates?.changes.subscribe(() => undefined);
    }

    ngAfterViewInit(): void {
        if (this.lazy) {
            Promise.resolve().then(() => {
                this.onLazyLoad.emit({ first: 0, rows: this.toNumber(this.rows, 10) });
            });
        }
    }

    sort(field: string): void {
        if (!field) {
            return;
        }

        if (this.sortMode === 'multiple') {
            const existing = this.multiSortMeta.find((item) => item.field === field);
            if (existing) {
                existing.order = existing.order === 1 ? -1 : 1;
            } else {
                this.multiSortMeta = [...this.multiSortMeta, { field, order: 1 }];
            }
        } else {
            this.sortOrder = this.sortField === field && this.sortOrder === 1 ? -1 : 1;
            this.sortField = field;
        }

        this.onLazyLoad.emit({
            first: 0,
            rows: this.toNumber(this.rows, 10),
            sortField: this.sortField,
            sortOrder: this.sortOrder,
            multiSortMeta: this.multiSortMeta,
        });
    }

    trackByIndex(index: number): number {
        return index;
    }

    private getTemplate(name: string): TemplateRef<any> | null {
        return this.templates?.find((template) => template.type === name)?.template ?? null;
    }

    private toNumber(value: any, fallback: number): number {
        const numberValue = Number(value);
        return Number.isFinite(numberValue) ? numberValue : fallback;
    }
}

@Component({
    selector: 'app-paginator',
    standalone: true,
    imports: [CommonModule],
    template: `
        <div class="d-flex flex-wrap align-items-center gap-2">
            <select
                class="form-select form-select-sm w-auto"
                [value]="rows"
                (change)="changeRows($event)"
                [attr.aria-label]="'Rows per page'"
            >
                <option *ngFor="let option of rowsPerPageOptions || [rows]" [value]="option">{{ option }}</option>
            </select>
            <button class="btn btn-sm btn-light" type="button" (click)="changePage(currentPage - 1)" [disabled]="currentPage <= 0">
                <i class="fa fa-chevron-left"></i>
            </button>
            <span class="small text-muted">{{ pageReport }}</span>
            <button
                class="btn btn-sm btn-light"
                type="button"
                (click)="changePage(currentPage + 1)"
                [disabled]="first + rowsNumber >= totalRecords"
            >
                <i class="fa fa-chevron-right"></i>
            </button>
        </div>
    `,
})
export class Paginator {
    @Input() rows: any = 10;
    @Input() first: any = 0;
    @Input() totalRecords = 0;
    @Input() rowsPerPageOptions: number[] = [5, 10, 25, 50, 100, 250, 500];
    @Input() showCurrentPageReport: any;
    @Input() currentPageReportTemplate = '';

    @Output() onPageChange = new EventEmitter<LazyLoadEvent>();

    get rowsNumber(): number {
        const rows = Number(this.rows);
        return Number.isFinite(rows) && rows > 0 ? rows : 10;
    }

    get currentPage(): number {
        return Math.floor(Number(this.first || 0) / this.rowsNumber);
    }

    get pageReport(): string {
        if (this.currentPageReportTemplate) {
            return this.currentPageReportTemplate;
        }

        if (!this.totalRecords) {
            return '0';
        }

        const start = Number(this.first || 0) + 1;
        const end = Math.min(Number(this.first || 0) + this.rowsNumber, this.totalRecords);
        return `${start}-${end} / ${this.totalRecords}`;
    }

    changePage(page: number): void {
        const maxPage = Math.max(Math.ceil((this.totalRecords || 0) / this.rowsNumber) - 1, 0);
        const nextPage = Math.min(Math.max(page, 0), maxPage);
        this.first = nextPage * this.rowsNumber;
        this.onPageChange.emit({ first: this.first, rows: this.rowsNumber });
    }

    changeRows(event: Event): void {
        this.rows = Number((event.target as HTMLSelectElement).value);
        this.first = 0;
        this.onPageChange.emit({ first: 0, rows: this.rowsNumber });
    }
}

@Component({
    selector: 'app-file-upload',
    standalone: true,
    imports: [CommonModule],
    template: `
        <div class="app-file-upload">
            <div class="app-file-upload-buttonbar d-inline-flex align-items-center gap-2">
                <label class="app-file-upload-choose app-button btn btn-sm btn-light-primary mb-0">
                    <i class="fa fa-upload me-1"></i>
                    <span>{{ chooseLabel || 'Choose' }}</span>
                    <input
                        class="d-none"
                        type="file"
                        [attr.accept]="accept || null"
                        [attr.multiple]="multiple ? '' : null"
                        (change)="selectFiles($event)"
                    />
                </label>
                <button
                    *ngIf="files.length && !isAutoUpload"
                    class="btn btn-sm btn-primary"
                    type="button"
                    (click)="upload()"
                >
                    {{ uploadLabel || 'Upload' }}
                </button>
                <button *ngIf="files.length" class="btn btn-sm btn-light" type="button" (click)="clear()">
                    {{ cancelLabel || 'Clear' }}
                </button>
            </div>
            <ng-container
                *ngIf="contentTemplate"
                [ngTemplateOutlet]="contentTemplate"
                [ngTemplateOutletContext]="{ $implicit: files, files: files }"
            ></ng-container>
        </div>
    `,
})
export class FileUpload {
    @Input() accept = '';
    @Input() auto: any;
    @Input() chooseLabel = '';
    @Input() uploadLabel = '';
    @Input() cancelLabel = '';
    @Input() customUpload: any;
    @Input() maxFileSize: any;
    @Input() multiple: any;
    @Input() name = '';
    @Input() url = '';

    @Output() uploadHandler = new EventEmitter<{ files: File[] }>();
    @Output() onUpload = new EventEmitter<{ files: File[] }>();
    @Output() onError = new EventEmitter<any>();

    @ContentChildren(AppTemplate) templates: QueryList<AppTemplate>;

    files: File[] = [];
    private inputElement?: HTMLInputElement;

    get contentTemplate(): TemplateRef<any> | null {
        return this.templates?.find((template) => template.type === 'content')?.template ?? null;
    }

    get isAutoUpload(): boolean {
        return this.auto === true || this.auto === 'true' || this.auto === 'auto';
    }

    selectFiles(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.inputElement = input;
        this.files = Array.from(input.files ?? []);

        const maxFileSize = Number(this.maxFileSize);
        if (Number.isFinite(maxFileSize) && maxFileSize > 0 && this.files.some((file) => file.size > maxFileSize)) {
            this.onError.emit({ files: this.files });
            this.clear();
            return;
        }

        if (this.isAutoUpload || this.customUpload === true || this.customUpload === 'true') {
            this.upload();
        }
    }

    upload(): void {
        const event = { files: this.files };
        if (this.customUpload === true || this.customUpload === 'true' || this.uploadHandler.observed) {
            this.uploadHandler.emit(event);
        } else {
            this.onUpload.emit(event);
            this.clear();
        }
    }

    clear(): void {
        this.files = [];
        if (this.inputElement) {
            this.inputElement.value = '';
        }
    }
}

@Component({
    selector: 'app-autocomplete',
    standalone: true,
    imports: [CommonModule],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => AutoComplete),
            multi: true,
        },
    ],
    template: `
        <div class="app-autocomplete position-relative" [class]="styleClass" [ngStyle]="styleObject">
            <div *ngIf="multiple" class="form-control d-flex flex-wrap align-items-center gap-1 min-h-40px">
                <span *ngFor="let item of selectedItems; trackBy: trackByIndex" class="badge bg-primary">
                    {{ display(item) }}
                    <button class="btn-close btn-close-white btn-sm ms-1" type="button" (click)="remove(item)"></button>
                </span>
                <input
                    class="border-0 flex-grow-1"
                    [attr.placeholder]="selectedItems.length ? null : placeholder"
                    [value]="query"
                    (input)="search($event)"
                    (keydown.enter)="selectFirst($event)"
                    (blur)="touch()"
                />
            </div>
            <input
                *ngIf="!multiple"
                [class]="inputStyleClass || 'form-control'"
                [attr.placeholder]="placeholder || null"
                [attr.size]="size || null"
                [value]="display(value)"
                (input)="search($event)"
                (keydown.enter)="selectFirst($event)"
                (blur)="touch()"
            />
            <ul *ngIf="suggestions?.length && panelOpen" class="dropdown-menu show w-100 autocomplete-panel">
                <li *ngFor="let suggestion of suggestions; trackBy: trackByIndex">
                    <button class="dropdown-item" type="button" (click)="select(suggestion)">
                        {{ display(suggestion) }}
                    </button>
                </li>
            </ul>
        </div>
    `,
    styles: [
        `
            .min-h-40px {
                min-height: 40px;
            }
            .autocomplete-panel {
                max-height: 18rem;
                overflow: auto;
                z-index: 1060;
            }
            .app-autocomplete input:focus {
                outline: none;
            }
        `,
    ],
})
export class AutoComplete implements ControlValueAccessor {
    @Input() suggestions: any[] = [];
    @Input() multiple: any;
    @Input() field = '';
    @Input() minLength: any = 1;
    @Input() placeholder = '';
    @Input() size: any;
    @Input() style: Record<string, any> | string;
    @Input() styleClass = '';
    @Input() inputStyleClass = '';
    @Input() panelStyleClass = '';

    @Output() completeMethod = new EventEmitter<{ query: string }>();

    value: any;
    query = '';
    panelOpen = false;

    private onChange: (value: any) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    get selectedItems(): any[] {
        return Array.isArray(this.value) ? this.value : [];
    }

    get styleObject(): Record<string, any> | null {
        return typeof this.style === 'string' ? null : this.style || null;
    }

    writeValue(value: any): void {
        this.value = value;
    }

    registerOnChange(fn: (value: any) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(_isDisabled: boolean): void {}

    search(event: Event): void {
        const query = (event.target as HTMLInputElement).value;
        this.query = query;
        this.panelOpen = query.length >= Number(this.minLength || 0);

        if (this.panelOpen) {
            this.completeMethod.emit({ query });
        }

        if (!this.multiple) {
            this.value = query;
            this.onChange(this.value);
        }
    }

    select(item: any): void {
        if (this.multiple) {
            this.value = [...this.selectedItems, item];
            this.query = '';
        } else {
            this.value = item;
        }

        this.panelOpen = false;
        this.onChange(this.value);
        this.onTouched();
    }

    selectFirst(event: Event): void {
        event.preventDefault();
        if (this.suggestions?.length) {
            this.select(this.suggestions[0]);
        } else if (this.multiple && this.query) {
            this.select(this.query);
        }
    }

    remove(item: any): void {
        this.value = this.selectedItems.filter((selected) => selected !== item);
        this.onChange(this.value);
        this.onTouched();
    }

    touch(): void {
        this.onTouched();
    }

    display(item: any): string {
        if (item === null || item === undefined) {
            return '';
        }

        if (this.field && typeof item === 'object') {
            return item[this.field] ?? '';
        }

        return String(item);
    }

    trackByIndex(index: number): number {
        return index;
    }
}

@Component({
    selector: 'app-input-mask',
    standalone: true,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => InputMask),
            multi: true,
        },
    ],
    template: `
        <input
            [class]="styleClass || 'form-control'"
            [attr.placeholder]="placeholder || slotChar || null"
            [value]="value || ''"
            (input)="input($event)"
            (blur)="onTouched()"
        />
    `,
})
export class InputMask implements ControlValueAccessor {
    @Input() mask = '';
    @Input() placeholder = '';
    @Input() slotChar = '';
    @Input() styleClass = '';

    value = '';
    private onChange: (value: string) => void = () => undefined;
    onTouched: () => void = () => undefined;

    writeValue(value: string): void {
        this.value = value || '';
    }

    registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    input(event: Event): void {
        this.value = (event.target as HTMLInputElement).value;
        this.onChange(this.value);
    }
}

@Component({
    selector: 'app-editor',
    standalone: true,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => Editor),
            multi: true,
        },
    ],
    template: `
        <textarea
            class="form-control"
            [ngStyle]="styleObject"
            [value]="value || ''"
            (input)="input($event)"
            (blur)="onTouched()"
        ></textarea>
    `,
    imports: [CommonModule],
})
export class Editor implements ControlValueAccessor {
    @Input() style: Record<string, any> | string;

    value = '';
    private onChange: (value: string) => void = () => undefined;
    onTouched: () => void = () => undefined;

    get styleObject(): Record<string, any> | null {
        return typeof this.style === 'string' ? null : this.style || null;
    }

    writeValue(value: string): void {
        this.value = value || '';
    }

    registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    input(event: Event): void {
        this.value = (event.target as HTMLTextAreaElement).value;
        this.onChange(this.value);
    }
}

@Component({
    selector: 'app-select',
    standalone: true,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => Select),
            multi: true,
        },
    ],
    template: `
        <select class="form-select form-control" [ngStyle]="styleObject" [value]="value || ''" (change)="select($event)" (blur)="onTouched()">
            <option *ngFor="let option of options || []; trackBy: trackByIndex" [value]="option.value">
                {{ option.label }}
            </option>
        </select>
    `,
    imports: [CommonModule],
})
export class Select implements ControlValueAccessor {
    @Input() options: SelectItem[] = [];
    @Input() appendTo: any;
    @Input() filter: any;
    @Input() virtualScroll: any;
    @Input() virtualScrollItemSize: any;
    @Input() style: Record<string, any> | string;

    @Output() onChange = new EventEmitter<{ value: any }>();

    value: any;
    private propagateChange: (value: any) => void = () => undefined;
    onTouched: () => void = () => undefined;

    get styleObject(): Record<string, any> | null {
        return typeof this.style === 'string' ? null : this.style || null;
    }

    writeValue(value: any): void {
        this.value = value;
    }

    registerOnChange(fn: (value: any) => void): void {
        this.propagateChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    select(event: Event): void {
        this.value = (event.target as HTMLSelectElement).value;
        this.propagateChange(this.value);
        this.onChange.emit({ value: this.value });
    }

    trackByIndex(index: number): number {
        return index;
    }
}

@Component({
    selector: 'app-context-menu',
    standalone: true,
    imports: [CommonModule],
    template: `
        <ul
            *ngIf="visible"
            class="dropdown-menu show"
            [style.left.px]="left"
            [style.top.px]="top"
            [style.position]="'fixed'"
            [style.z-index]="baseZIndex || 1055"
        >
            <li *ngFor="let item of model || []; trackBy: trackByIndex">
                <button class="dropdown-item" type="button" [disabled]="item.disabled" (click)="run(item, $event)">
                    {{ item.label }}
                </button>
            </li>
        </ul>
    `,
})
export class ContextMenu {
    @Input() model: MenuItem[] = [];
    @Input() appendTo: any;
    @Input() baseZIndex: any;

    visible = false;
    left = 0;
    top = 0;

    show(event: MouseEvent, node?: TreeNode): void {
        event.preventDefault();
        this.left = event.clientX;
        this.top = event.clientY;
        this.visible = true;
        (event as any).node = node;
    }

    hide(): void {
        this.visible = false;
    }

    run(item: MenuItem, originalEvent: Event): void {
        if (item.disabled) {
            return;
        }

        item.command?.({ originalEvent, item });
        this.hide();
    }

    @HostListener('document:click')
    onDocumentClick(): void {
        this.hide();
    }

    trackByIndex(index: number): number {
        return index;
    }
}

@Component({
    selector: 'app-tree',
    standalone: true,
    imports: [CommonModule],
    template: `
        <div class="app-tree app-component">
            <ng-template #renderNodes let-nodes>
                <ul class="app-tree-container list-unstyled mb-0">
                    <li *ngFor="let node of nodes || []; trackBy: trackByNode" [ngClass]="node.styleClass">
                        <div
                            class="app-tree-node-content d-flex align-items-center gap-2 py-1"
                            [class.active]="isSelected(node)"
                            (click)="selectNode(node, $event)"
                            (contextmenu)="openContextMenu($event, node)"
                        >
                            <input
                                *ngIf="selectionMode === 'checkbox'"
                                class="form-check-input m-0"
                                type="checkbox"
                                [checked]="isSelected(node)"
                                (click)="$event.stopPropagation()"
                                (change)="toggleCheckbox(node, $event)"
                                [disabled]="node.selectable === false"
                            />
                            <button
                                *ngIf="node.children?.length"
                                class="btn btn-sm btn-link p-0 text-muted"
                                type="button"
                                (click)="toggleExpanded(node, $event)"
                            >
                                <i [class]="node.expanded === false ? 'fa fa-chevron-right' : 'fa fa-chevron-down'"></i>
                            </button>
                            <span *ngIf="!node.children?.length" class="d-inline-block" style="width: 14px"></span>
                            <i *ngIf="node.icon || node.expandedIcon || node.collapsedIcon" [class]="nodeIcon(node)"></i>
                            <ng-container
                                *ngIf="nodeTemplate; else defaultNodeTemplate"
                                [ngTemplateOutlet]="nodeTemplate"
                                [ngTemplateOutletContext]="{ $implicit: node, node: node }"
                            ></ng-container>
                            <ng-template #defaultNodeTemplate>
                                <span>{{ node.label }}</span>
                            </ng-template>
                        </div>
                        <div class="ms-5" *ngIf="node.children?.length && node.expanded !== false">
                            <ng-container
                                [ngTemplateOutlet]="renderNodes"
                                [ngTemplateOutletContext]="{ $implicit: node.children }"
                            ></ng-container>
                        </div>
                    </li>
                </ul>
            </ng-template>
            <ng-container [ngTemplateOutlet]="renderNodes" [ngTemplateOutletContext]="{ $implicit: value || [] }"></ng-container>
        </div>
    `,
    styles: [
        `
            .app-tree-node-content.active {
                background: rgba(13, 110, 253, 0.08);
                border-radius: 0.25rem;
            }
            .hidden-tree-node {
                display: none;
            }
        `,
    ],
})
export class Tree {
    @Input() value: TreeNode[] = [];
    @Input() selection: TreeNode | TreeNode[] | null;
    @Input() selectionMode: 'single' | 'checkbox' | string = 'single';
    @Input() propagateSelectionUp: any;
    @Input() propagateSelectionDown: any;
    @Input() contextMenu?: ContextMenu;
    @Input() draggableNodes: any;
    @Input() droppableNodes: any;

    @Output() selectionChange = new EventEmitter<TreeNode | TreeNode[] | null>();
    @Output() onNodeSelect = new EventEmitter<{ node: TreeNode; originalEvent?: Event }>();
    @Output() onNodeUnselect = new EventEmitter<{ node: TreeNode; originalEvent?: Event }>();
    @Output() onNodeDrop = new EventEmitter<any>();

    @ContentChildren(AppTemplate) templates: QueryList<AppTemplate>;

    get nodeTemplate(): TemplateRef<any> | null {
        return this.templates?.find((template) => template.type === 'default')?.template ?? null;
    }

    isSelected(node: TreeNode): boolean {
        if (this.selectionMode === 'checkbox') {
            return Array.isArray(this.selection) && this.selection.includes(node);
        }

        return this.selection === node;
    }

    selectNode(node: TreeNode, event: Event): void {
        if (node.selectable === false) {
            return;
        }

        if (this.selectionMode === 'checkbox') {
            this.setCheckboxNode(node, !this.isSelected(node), event);
            return;
        }

        this.selection = node;
        this.selectionChange.emit(this.selection);
        this.onNodeSelect.emit({ node, originalEvent: event });
    }

    toggleCheckbox(node: TreeNode, event: Event): void {
        this.setCheckboxNode(node, (event.target as HTMLInputElement).checked, event);
    }

    toggleExpanded(node: TreeNode, event: Event): void {
        event.stopPropagation();
        node.expanded = node.expanded === false;
    }

    openContextMenu(event: MouseEvent, node: TreeNode): void {
        this.selection = node;
        this.selectionChange.emit(this.selection);
        this.contextMenu?.show(event, node);
    }

    nodeIcon(node: TreeNode): string {
        if (node.icon) {
            return node.icon;
        }

        return node.expanded === false ? node.collapsedIcon || '' : node.expandedIcon || '';
    }

    trackByNode(index: number, node: TreeNode): any {
        return node?.data?.id ?? node?.data?.name ?? node?.label ?? index;
    }

    private setCheckboxNode(node: TreeNode, selected: boolean, event: Event): void {
        const selection = Array.isArray(this.selection) ? [...this.selection] : [];

        this.applySelection(selection, node, selected);
        if (this.propagateSelectionDown !== false) {
            this.walkChildren(node, (child) => this.applySelection(selection, child, selected));
        }

        this.selection = selection;
        this.selectionChange.emit(this.selection);

        if (selected) {
            this.onNodeSelect.emit({ node, originalEvent: event });
        } else {
            this.onNodeUnselect.emit({ node, originalEvent: event });
        }
    }

    private applySelection(selection: TreeNode[], node: TreeNode, selected: boolean): void {
        const index = selection.indexOf(node);
        if (selected && index === -1) {
            selection.push(node);
        } else if (!selected && index >= 0) {
            selection.splice(index, 1);
        }
    }

    private walkChildren(node: TreeNode, callback: (node: TreeNode) => void): void {
        node.children?.forEach((child) => {
            callback(child);
            this.walkChildren(child, callback);
        });
    }
}

@NgModule({
    imports: [Table, AppTemplate, SortableColumnDirective, SortIcon],
    exports: [Table, AppTemplate, SortableColumnDirective, SortIcon],
})
export class TableModule {}

@NgModule({
    imports: [Paginator],
    exports: [Paginator],
})
export class PaginatorModule {}

@NgModule({
    imports: [FileUpload, AppTemplate],
    exports: [FileUpload, AppTemplate],
})
export class FileUploadModule {}

@NgModule({
    imports: [AutoComplete],
    exports: [AutoComplete],
})
export class AutoCompleteModule {}

@NgModule({
    imports: [InputMask],
    exports: [InputMask],
})
export class InputMaskModule {}

@NgModule({
    imports: [Editor],
    exports: [Editor],
})
export class EditorModule {}

@NgModule({
    imports: [Select, AppTemplate],
    exports: [Select, AppTemplate],
})
export class SelectModule {}

@NgModule({
    imports: [Tree, AppTemplate],
    exports: [Tree, AppTemplate],
})
export class TreeModule {}

@NgModule({
    imports: [ContextMenu],
    exports: [ContextMenu],
})
export class ContextMenuModule {}

@NgModule({
    imports: [PasswordDirective],
    exports: [PasswordDirective],
})
export class PasswordModule {}

@NgModule()
export class DragDropModule {}

@NgModule()
export class TreeTableModule {}

@NgModule()
export class ProgressBarModule {}
