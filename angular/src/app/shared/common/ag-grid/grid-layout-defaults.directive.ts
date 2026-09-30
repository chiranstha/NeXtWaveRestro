import { AfterViewInit, Directive, ElementRef, HostListener, NgZone, OnDestroy, OnInit } from '@angular/core';
import { AgGridAngular } from 'ag-grid-angular';

const SERIAL_COLUMN_WIDTH = 56;
const IMAGE_COLUMN_WIDTH = 80;
const IMAGE_COLUMN_MIN_WIDTH = 72;
const ACTION_BUTTON_WIDTH = 26;
const ACTION_BUTTON_GAP = 4;
const ACTION_CELL_HORIZONTAL_PADDING = 10;
const ACTION_COLUMN_DEFAULT_BUTTON_COUNT = 3;
const ACTION_COLUMN_MAX_BUTTON_COUNT = 8;
const DEFAULT_SCROLL_ROW_BUFFER = 10;

const FORCED_SCROLL_OPTIONS: Record<string, boolean> = {
    animateRows: false,
    debounceVerticalScrollbar: false,
    ensureDomOrder: false,
    suppressAnimationFrame: false,
    suppressCellFocus: true,
    suppressColumnMoveAnimation: true,
    suppressColumnVirtualisation: false,
    enableContentVisibilityAuto: false,
    suppressRowHoverHighlight: true,
    suppressRowTransform: false,
    suppressScrollOnNewData: true,
    valueCache: true,
};

@Directive({
    selector: 'ag-grid-angular',
    standalone: false,
})
export class GridLayoutDefaultsDirective implements OnInit, AfterViewInit, OnDestroy {
    private readonly timeouts: number[] = [];
    private readonly gridEventRemovers: Array<() => void> = [];
    private gridEventsAttached = false;
    private scrollBridgeAttached = false;
    private isDestroyed = false;
    private lastColumnStateSignature = '';
    private pendingVerticalScroll = 0;
    private pendingVerticalScrollFrame: number | null = null;

    constructor(
        private readonly grid: AgGridAngular,
        private readonly elementRef: ElementRef<HTMLElement>,
        private readonly ngZone: NgZone,
    ) {}

    ngOnInit(): void {
        this.applyScrollPerformanceDefaults();
        this.applyLocalizedGridText();
        this.normalizeColumnDefs();
    }

    ngAfterViewInit(): void {
        this.scheduleNormalization(0);
        this.scheduleNormalization(100);
        this.scheduleNormalization(500);
    }

    ngOnDestroy(): void {
        this.destroy();
    }

    @HostListener('window:resize')
    onWindowResize(): void {
        if (this.isDestroyed) {
            return;
        }

        this.scheduleNormalization(100);
    }

    private scheduleNormalization(delay: number): void {
        if (this.isDestroyed) {
            return;
        }

        this.ngZone.runOutsideAngular(() => {
            const timeoutId = window.setTimeout(() => {
                if (this.isDestroyed) {
                    return;
                }

                this.attachGridEvents();
                this.attachScrollBridge();
                this.applyScrollPerformanceDefaults();
                this.normalizeColumnDefs();
                this.normalizeLiveColumns();
            }, delay);

            this.timeouts.push(timeoutId);
        });
    }

    private normalizeColumnDefs(): void {
        const columnDefs = (this.grid as any).columnDefs;
        if (!Array.isArray(columnDefs)) {
            return;
        }

        this.normalizeColumnDefCollection(columnDefs);
    }

    private applyLocalizedGridText(): void {
        const currentLocaleText = (this.grid as any).localeText ?? {};
        (this.grid as any).localeText = {
            loadingOoo: this.localize('Loading...'),
            noRowsToShow: this.localize('No Rows To Show'),
            rowGroupColumnsEmptyMessage: this.localize('Drag here to set row groups'),
            pivotColumnsEmptyMessage: this.localize('Drag here to set column labels'),
            valueColumnsEmptyMessage: this.localize('Drag here to aggregate'),
            ...currentLocaleText,
        };
    }

    private applyScrollPerformanceDefaults(): void {
        const gridRef = this.grid as any;
        const gridOptions = {
            ...(gridRef.gridOptions ?? {}),
        };

        Object.entries(FORCED_SCROLL_OPTIONS).forEach(([optionName, optionValue]) => {
            gridOptions[optionName] = optionValue;
            gridRef[optionName] = optionValue;
        });

        if (this.shouldNormalizeRowBuffer(gridOptions.rowBuffer)) {
            gridOptions.rowBuffer = DEFAULT_SCROLL_ROW_BUFFER;
        }

        if (this.shouldNormalizeRowBuffer(gridRef.rowBuffer)) {
            gridRef.rowBuffer = DEFAULT_SCROLL_ROW_BUFFER;
        }

        gridRef.gridOptions = gridOptions;

        const api = this.getLiveGridApi();
        if (!api?.updateGridOptions) {
            return;
        }

        api.updateGridOptions({
            animateRows: false,
            suppressCellFocus: true,
            suppressColumnMoveAnimation: true,
            suppressRowHoverHighlight: true,
            suppressScrollOnNewData: true,
        });
    }

    private shouldNormalizeRowBuffer(value: unknown): boolean {
        const rowBuffer = this.getNumericWidth(value);
        return rowBuffer === null || rowBuffer !== DEFAULT_SCROLL_ROW_BUFFER;
    }

    private normalizeColumnDefCollection(columnDefs: any[]): void {
        columnDefs.forEach((columnDef) => {
            if (Array.isArray(columnDef?.children)) {
                this.normalizeColumnDefCollection(columnDef.children);
            }

            if (this.isSerialColumn(columnDef)) {
                columnDef.width = SERIAL_COLUMN_WIDTH;
                columnDef.minWidth = SERIAL_COLUMN_WIDTH;
                columnDef.maxWidth = SERIAL_COLUMN_WIDTH;
                columnDef.suppressSizeToFit = true;
                delete columnDef.flex;
                columnDef.cellClass = this.appendClass(columnDef.cellClass, 'ag-serial-cell');
                columnDef.headerClass = this.appendClass(columnDef.headerClass, 'ag-serial-header');
                return;
            }

            if (this.isImageColumn(columnDef)) {
                this.applyImageColumnDefaults(columnDef);
                return;
            }

            if (this.isActionColumn(columnDef)) {
                this.applyActionColumnDefaults(columnDef, this.getActionColumnWidthForColumnDef(columnDef));
            }
        });
    }

    private normalizeLiveColumns(): void {
        const api = this.getLiveGridApi();
        if (!api) {
            return;
        }

        this.normalizeRenderedActionButtons();

        const columns = api.getColumns?.() ?? api.getAllGridColumns?.() ?? [];
        const state = [];

        columns.forEach((column: any) => {
            const columnDef = column.getColDef?.();
            const colId = column.getColId?.();
            if (!columnDef || !colId) {
                return;
            }

            if (this.isSerialColumn(columnDef)) {
                columnDef.width = SERIAL_COLUMN_WIDTH;
                columnDef.minWidth = SERIAL_COLUMN_WIDTH;
                columnDef.maxWidth = SERIAL_COLUMN_WIDTH;
                columnDef.suppressSizeToFit = true;
                delete columnDef.flex;
                columnDef.cellClass = this.appendClass(columnDef.cellClass, 'ag-serial-cell');
                columnDef.headerClass = this.appendClass(columnDef.headerClass, 'ag-serial-header');
                state.push({ colId, width: SERIAL_COLUMN_WIDTH });
                return;
            }

            if (this.isImageColumn(columnDef)) {
                const width = this.applyImageColumnDefaults(columnDef);
                state.push({ colId, width, pinned: 'left' });
                return;
            }

            if (this.isActionColumn(columnDef)) {
                const width = this.getActionColumnWidthForColumnDef(columnDef, colId);
                this.applyActionColumnDefaults(columnDef, width);
                state.push({ colId, width });
            }
        });

        if (!state.length) {
            return;
        }

        const columnStateSignature = state
            .map((columnState) => `${columnState.colId}:${columnState.width}:${columnState.pinned ?? ''}`)
            .join('|');
        if (columnStateSignature === this.lastColumnStateSignature) {
            this.elementRef.nativeElement.classList.add('ag-grid-layout-defaults');
            return;
        }

        this.lastColumnStateSignature = columnStateSignature;
        api.applyColumnState?.({ state, applyOrder: false });
        api.refreshHeader?.();
        api.refreshCells?.({ force: true, columns: state.map((columnState) => columnState.colId) });
        this.elementRef.nativeElement.classList.add('ag-grid-layout-defaults');
    }

    private attachGridEvents(): void {
        const api = this.getLiveGridApi();
        if (this.gridEventsAttached || !api?.addEventListener) {
            return;
        }

        const preDestroyListener = () => this.destroy();
        api.addEventListener('gridPreDestroyed', preDestroyListener);
        this.gridEventRemovers.push(() => this.removeGridEventListener(api, 'gridPreDestroyed', preDestroyListener));

        ['firstDataRendered', 'modelUpdated', 'rowDataUpdated'].forEach((eventName) => {
            const listener = () => {
                this.scheduleNormalization(0);
                // AG Grid emits these events before every framework renderer has
                // necessarily committed its action buttons to the DOM.
                this.scheduleNormalization(50);
            };
            api.addEventListener(eventName, listener);
            this.gridEventRemovers.push(() => this.removeGridEventListener(api, eventName, listener));
        });

        this.gridEventsAttached = true;
    }

    private attachScrollBridge(): void {
        if (this.scrollBridgeAttached) {
            return;
        }

        const host = this.elementRef.nativeElement;
        const wheelListener = (event: WheelEvent) => this.onGridWheel(event);
        const keydownListener = (event: KeyboardEvent) => this.onGridKeyDown(event);

        host.addEventListener('wheel', wheelListener, { passive: false });
        host.addEventListener('keydown', keydownListener, true);

        this.gridEventRemovers.push(() => {
            host.removeEventListener('wheel', wheelListener);
            host.removeEventListener('keydown', keydownListener, true);
        });

        this.scrollBridgeAttached = true;
    }

    private onGridWheel(event: WheelEvent): void {
        if (
            this.isDestroyed ||
            event.defaultPrevented ||
            event.ctrlKey ||
            this.isInteractiveScrollTarget(event.target)
        ) {
            return;
        }

        const horizontalDelta = this.normalizeWheelDelta(event.deltaX, event.deltaMode);
        const verticalDelta = this.normalizeWheelDelta(event.deltaY, event.deltaMode);
        if (
            verticalDelta !== 0 &&
            !event.shiftKey &&
            Math.abs(horizontalDelta) <= Math.abs(verticalDelta) &&
            this.queueVerticalScroll(verticalDelta)
        ) {
            event.preventDefault();
            event.stopPropagation();
        }
    }

    private onGridKeyDown(event: KeyboardEvent): void {
        if (
            this.isDestroyed ||
            event.defaultPrevented ||
            event.altKey ||
            event.ctrlKey ||
            event.metaKey ||
            this.isInteractiveScrollTarget(event.target)
        ) {
            return;
        }

        const verticalViewport = this.getVerticalScrollViewport();
        const horizontalViewport = this.getHorizontalScrollViewport();
        const rowHeight = this.getNumericWidth((this.grid as any).rowHeight) ?? 44;
        let handled = false;

        switch (event.key) {
            case 'PageDown':
                handled = this.scrollElementBy(
                    verticalViewport,
                    this.getPageScrollAmount(verticalViewport, rowHeight),
                    'top',
                );
                break;
            case 'PageUp':
                handled = this.scrollElementBy(
                    verticalViewport,
                    -this.getPageScrollAmount(verticalViewport, rowHeight),
                    'top',
                );
                break;
            case 'Home':
                handled = this.scrollElementTo(verticalViewport, 0, 'top');
                break;
            case 'End':
                handled = this.scrollElementTo(verticalViewport, this.getMaxScroll(verticalViewport, 'top'), 'top');
                break;
            case 'ArrowDown':
                handled = this.scrollElementBy(verticalViewport, rowHeight, 'top');
                break;
            case 'ArrowUp':
                handled = this.scrollElementBy(verticalViewport, -rowHeight, 'top');
                break;
            case 'ArrowRight':
                handled = this.scrollElementBy(horizontalViewport, 48, 'left');
                break;
            case 'ArrowLeft':
                handled = this.scrollElementBy(horizontalViewport, -48, 'left');
                break;
        }

        if (handled) {
            event.preventDefault();
            event.stopPropagation();
        }
    }

    private getGridElement(selector: string): HTMLElement | null {
        return this.elementRef.nativeElement.querySelector<HTMLElement>(selector);
    }

    private getVerticalScrollViewport(): HTMLElement | null {
        return this.getScrollableGridElement(
            ['.ag-grid-viewport', '.ag-body-viewport', '.ag-center-cols-viewport', '.ag-body-vertical-scroll-viewport'],
            'top',
        );
    }

    private getHorizontalScrollViewport(): HTMLElement | null {
        return this.getScrollableGridElement(
            ['.ag-grid-viewport', '.ag-body-horizontal-scroll-viewport', '.ag-center-cols-viewport'],
            'left',
        );
    }

    private getScrollableGridElement(selectors: string[], axis: 'top' | 'left'): HTMLElement | null {
        const elements = selectors
            .map((selector) => this.getGridElement(selector))
            .filter((element): element is HTMLElement => Boolean(element));

        return elements.find((element) => this.getMaxScroll(element, axis) > 0.5) ?? elements[0] ?? null;
    }

    private queueVerticalScroll(delta: number): boolean {
        const verticalViewport = this.getVerticalScrollViewport();
        if (!this.canScrollElement(verticalViewport, delta, 'top')) {
            return false;
        }

        this.pendingVerticalScroll += delta;
        if (this.pendingVerticalScrollFrame === null) {
            this.pendingVerticalScrollFrame = window.requestAnimationFrame(() => {
                this.pendingVerticalScrollFrame = null;
                const pendingScroll = this.pendingVerticalScroll;
                this.pendingVerticalScroll = 0;
                this.scrollElementBy(verticalViewport, pendingScroll, 'top');
            });
        }

        return true;
    }

    private isInteractiveScrollTarget(target: EventTarget | null): boolean {
        const element = target instanceof HTMLElement ? target : null;
        if (!element) {
            return false;
        }

        if (element.closest('.ag-menu, .ag-popup, .ag-filter, .ag-rich-select, .ag-select-list')) {
            return true;
        }

        return Boolean(element.closest('input, textarea, select, [contenteditable="true"]'));
    }

    private scrollElementBy(element: HTMLElement | null, delta: number, axis: 'top' | 'left'): boolean {
        if (!element || delta === 0) {
            return false;
        }

        const current = axis === 'top' ? element.scrollTop : element.scrollLeft;
        return this.scrollElementTo(element, current + delta, axis);
    }

    private scrollElementTo(element: HTMLElement | null, value: number, axis: 'top' | 'left'): boolean {
        if (!element) {
            return false;
        }

        const current = axis === 'top' ? element.scrollTop : element.scrollLeft;
        const maxScroll = this.getMaxScroll(element, axis);
        const next = Math.max(0, Math.min(value, maxScroll));
        if (Math.abs(current - next) < 0.5) {
            return false;
        }

        element.scrollTo({
            top: axis === 'top' ? next : element.scrollTop,
            left: axis === 'left' ? next : element.scrollLeft,
            behavior: 'auto',
        });

        return true;
    }

    private canScrollElement(element: HTMLElement | null, delta: number, axis: 'top' | 'left'): boolean {
        if (!element || delta === 0) {
            return false;
        }

        const current = axis === 'top' ? element.scrollTop : element.scrollLeft;
        const maxScroll = this.getMaxScroll(element, axis);
        return delta > 0 ? current < maxScroll - 0.5 : current > 0.5;
    }

    private getMaxScroll(element: HTMLElement | null, axis: 'top' | 'left'): number {
        if (!element) {
            return 0;
        }

        return axis === 'top'
            ? Math.max(element.scrollHeight - element.clientHeight, 0)
            : Math.max(element.scrollWidth - element.clientWidth, 0);
    }

    private getPageScrollAmount(element: HTMLElement | null, rowHeight: number): number {
        return Math.max((element?.clientHeight ?? 0) - rowHeight, rowHeight);
    }

    private normalizeWheelDelta(delta: number, deltaMode: number): number {
        if (!delta) {
            return 0;
        }

        if (deltaMode === WheelEvent.DOM_DELTA_LINE) {
            return delta * (this.getNumericWidth((this.grid as any).rowHeight) ?? 44);
        }

        if (deltaMode === WheelEvent.DOM_DELTA_PAGE) {
            return (
                delta *
                this.getPageScrollAmount(
                    this.getVerticalScrollViewport(),
                    this.getNumericWidth((this.grid as any).rowHeight) ?? 44,
                )
            );
        }

        return delta;
    }

    private destroy(): void {
        if (this.isDestroyed) {
            return;
        }

        this.isDestroyed = true;
        this.timeouts.splice(0).forEach((timeoutId) => window.clearTimeout(timeoutId));
        if (this.pendingVerticalScrollFrame !== null) {
            window.cancelAnimationFrame(this.pendingVerticalScrollFrame);
            this.pendingVerticalScrollFrame = null;
        }
        this.pendingVerticalScroll = 0;
        this.gridEventRemovers.splice(0).forEach((removeListener) => removeListener());
    }

    private getLiveGridApi(): any {
        const api = (this.grid as any).api;
        return this.isGridApiAlive(api) ? api : null;
    }

    private isGridApiAlive(api: any): boolean {
        return Boolean(api && (!api.isDestroyed || !api.isDestroyed()));
    }

    private removeGridEventListener(api: any, eventName: string, listener: () => void): void {
        if (this.isGridApiAlive(api)) {
            api.removeEventListener?.(eventName, listener);
        }
    }

    private applyActionColumnDefaults(columnDef: any, width: number): void {
        columnDef.width = width;
        columnDef.minWidth = width;
        columnDef.maxWidth = Math.max(width, this.getActionColumnWidth(ACTION_COLUMN_MAX_BUTTON_COUNT));
        columnDef.suppressSizeToFit = true;
        columnDef.lockPosition = 'right';
        columnDef.suppressMovable = true;
        delete columnDef.flex;
        columnDef.cellClass = this.appendClass(columnDef.cellClass, 'ag-actions-cell actions-cell');
    }

    private applyImageColumnDefaults(columnDef: any): number {
        const width = this.getImageColumnWidth(columnDef);
        columnDef.width = width;
        columnDef.minWidth = Math.min(this.getNumericWidth(columnDef.minWidth) ?? IMAGE_COLUMN_MIN_WIDTH, width);
        if (columnDef.maxWidth && Number(columnDef.maxWidth) < width) {
            columnDef.maxWidth = width;
        }
        columnDef.pinned = 'left';
        columnDef.lockPinned = true;
        columnDef.lockPosition = 'left';
        columnDef.suppressMovable = true;
        columnDef.suppressSizeToFit = true;
        columnDef.sortable = false;
        columnDef.filter = false;
        columnDef.rowGroup = false;
        columnDef.enableRowGroup = false;
        delete columnDef.flex;
        columnDef.cellClass = this.appendClass(columnDef.cellClass, 'ag-image-cell');
        columnDef.headerClass = this.appendClass(columnDef.headerClass, 'ag-image-header');

        return width;
    }

    private getActionColumnWidthForColumnDef(columnDef: any, colId?: string): number {
        const configuredButtonCount =
            this.getNumericWidth(columnDef?.cellRendererParams?.actionCount) ??
            this.getNumericWidth(columnDef?.cellRendererParams?.minButtonCount) ??
            this.getNumericWidth(columnDef?.actionCount);

        const renderedButtonCount = colId ? this.getRenderedActionButtonCount(colId) : 0;
        const minimumButtonCount = configuredButtonCount ?? ACTION_COLUMN_DEFAULT_BUTTON_COUNT;
        return this.getActionColumnWidth(Math.max(renderedButtonCount, minimumButtonCount));
    }

    private getRenderedActionButtonCount(colId: string): number {
        let buttonCount = 0;

        this.elementRef.nativeElement.querySelectorAll<HTMLElement>('.ag-cell[col-id]').forEach((cell) => {
            if (cell.getAttribute('col-id') === colId) {
                buttonCount = Math.max(buttonCount, cell.querySelectorAll('.btnaction').length);
            }
        });

        return buttonCount;
    }

    private normalizeRenderedActionButtons(): void {
        this.elementRef.nativeElement
            .querySelectorAll<HTMLElement>(
                '.ag-actions-cell .btnaction, .actions-cell .btnaction, .ag-actions-cell button.btn-icon, .actions-cell button.btn-icon',
            )
            .forEach((button) => {
                this.applyActionSemanticClass(button);
                button.classList.add('btnaction', 'btn', 'btn-sm', 'btn-icon', this.getBootstrapActionVariant(button));
                button.style.removeProperty('background');
                button.style.removeProperty('border');

                const icons = Array.from(button.querySelectorAll<HTMLElement>('i'));
                if (this.shouldUseDuotoneActionIcon(button)) {
                    icons.push(button);
                }

                icons.forEach((icon) => {
                    if (this.shouldUseDuotoneActionIcon(icon)) {
                        icon.classList.add('fa-duotone');
                    }
                    icon.style.setProperty('color', 'inherit', 'important');
                    icon.style.setProperty('pointer-events', 'none', 'important');
                });
            });
    }

    private shouldUseDuotoneActionIcon(icon: HTMLElement): boolean {
        const hasFontAwesomeIconClass = Array.from(icon.classList).some(
            (className) => className.startsWith('fa-') && className !== 'fa-duotone',
        );

        return (
            !icon.classList.contains('fa-duotone') &&
            !icon.classList.contains('fa-brands') &&
            !icon.classList.contains('fab') &&
            (icon.classList.contains('fa-solid') ||
                icon.classList.contains('fas') ||
                (icon.classList.contains('fa') && hasFontAwesomeIconClass))
        );
    }

    private applyActionSemanticClass(button: HTMLElement): void {
        const actionName = this.getActionName(button);
        const actionClassByName: Record<string, string> = {
            view: 'btn-view',
            edit: 'btn-edit',
            delete: 'btn-delete',
            print: 'btn-print',
            download: 'btn-download',
            pdf: 'btn-pdf',
            lock: 'btn-lock',
            enroll: 'btn-view',
            'mark-read': 'btn-view',
            'provision-mobile': 'btn-provision-mobile',
            'reset-password': 'btn-reset-password',
            'set-password': 'btn-set-password',
        };

        const actionClass = actionClassByName[actionName];
        if (actionClass) {
            button.classList.add(actionClass);
            button.setAttribute('data-action', actionName);
        }
    }

    private getActionName(button: HTMLElement): string {
        const explicitAction = button.getAttribute('data-action') || button.getAttribute('data-grid-action');
        if (explicitAction) {
            return explicitAction.trim().toLowerCase();
        }

        const knownClassActions: Array<[string, string]> = [
            ['btn-view', 'view'],
            ['btn-all-changes', 'view'],
            ['btn-view-change', 'view'],
            ['btn-edit', 'edit'],
            ['btn-change', 'edit'],
            ['btn-delete', 'delete'],
            ['btn-print', 'print'],
            ['btn-download', 'download'],
            ['btn-pdf', 'pdf'],
            ['btn-lock', 'lock'],
            ['btn-enroll', 'enroll'],
            ['btn-markread', 'mark-read'],
            ['btn-provision-mobile', 'provision-mobile'],
            ['btn-reset-password', 'reset-password'],
            ['btn-set-password', 'set-password'],
        ];
        const classAction = knownClassActions.find(([className]) => button.classList.contains(className));
        if (classAction) {
            return classAction[1];
        }

        const label = [button.getAttribute('aria-label'), button.getAttribute('title'), button.textContent]
            .filter(Boolean)
            .join(' ')
            .trim()
            .toLowerCase();

        if (/\b(view|detail|preview|show|open)\b/.test(label)) {
            return 'view';
        }
        if (/\b(edit|update|modify|change)\b/.test(label)) {
            return 'edit';
        }
        if (/\b(delete|remove)\b/.test(label)) {
            return 'delete';
        }
        if (/\b(print)\b/.test(label)) {
            return 'print';
        }
        if (/\b(download|export|excel)\b/.test(label)) {
            return 'download';
        }
        if (/\bpdf\b/.test(label)) {
            return 'pdf';
        }
        if (/\b(lock|unlock)\b/.test(label)) {
            return 'lock';
        }
        if (/\benroll\b/.test(label)) {
            return 'enroll';
        }
        if (/\bmark\s+read\b/.test(label)) {
            return 'mark-read';
        }

        return '';
    }

    private getBootstrapActionVariant(button: HTMLElement): string {
        const existingVariant = Array.from(button.classList).find((className) => className.startsWith('btn-light-'));
        if (existingVariant) {
            return existingVariant;
        }

        if (button.classList.contains('btn-delete') || button.classList.contains('btn-pdf')) {
            return 'btn-light-danger';
        }

        if (
            button.classList.contains('btn-edit') ||
            button.classList.contains('btn-change') ||
            button.classList.contains('btn-lock')
        ) {
            return 'btn-light-warning';
        }

        if (
            button.classList.contains('btn-print') ||
            button.classList.contains('btn-download') ||
            button.classList.contains('btn-whatsapp') ||
            button.classList.contains('btn-reset-password')
        ) {
            return 'btn-light-success';
        }

        return 'btn-light-primary';
    }

    private getActionColumnWidth(buttonCount = ACTION_COLUMN_DEFAULT_BUTTON_COUNT): number {
        const width =
            ACTION_CELL_HORIZONTAL_PADDING +
            buttonCount * ACTION_BUTTON_WIDTH +
            Math.max(buttonCount - 1, 0) * ACTION_BUTTON_GAP;

        return width;
    }

    private getImageColumnWidth(columnDef: any): number {
        return this.getNumericWidth(columnDef?.width) ?? IMAGE_COLUMN_WIDTH;
    }

    private getNumericWidth(value: unknown): number | null {
        const numberValue = Number(value);
        return Number.isFinite(numberValue) && numberValue > 0 ? numberValue : null;
    }

    private isSerialColumn(columnDef: any): boolean {
        const headerName = this.normalizeLabel(columnDef?.headerName);
        const field = this.normalizeLabel(columnDef?.field);
        return (
            headerName === 'sn' ||
            headerName === 's.n' ||
            headerName === 's.n.' ||
            field === 'sn' ||
            field === 'serialno'
        );
    }

    private isImageColumn(columnDef: any): boolean {
        const headerName = this.normalizeCompactLabel(columnDef?.headerName);
        const field = this.normalizeCompactLabel(columnDef?.field);
        return [headerName, field].some(
            (label) =>
                label === 'image' ||
                label === 'photo' ||
                label === 'picture' ||
                label === 'avatar' ||
                label === 'profilepicture' ||
                label === 'imageurl' ||
                label === 'photourl' ||
                label === 'pictureurl' ||
                label === 'avatarurl' ||
                label === 'profilepictureurl' ||
                label.endsWith('image') ||
                label.endsWith('photo') ||
                label.endsWith('picture') ||
                label.endsWith('avatar') ||
                label.endsWith('imageurl') ||
                label.endsWith('photourl') ||
                label.endsWith('pictureurl') ||
                label.endsWith('avatarurl'),
        );
    }

    private isActionColumn(columnDef: any): boolean {
        const actionLabels = ['action', 'actions', 'operation', 'operations', 'command', 'commands'];
        return (
            actionLabels.includes(this.normalizeLabel(columnDef?.field)) ||
            actionLabels.includes(this.normalizeLabel(columnDef?.headerName))
        );
    }

    private normalizeLabel(value: unknown): string {
        return typeof value === 'string' ? value.trim().toLowerCase() : '';
    }

    private normalizeCompactLabel(value: unknown): string {
        return this.normalizeLabel(value).replace(/[\s._-]+/g, '');
    }

    private localize(key: string): string {
        const abpRef = typeof window !== 'undefined' ? (window as any).abp : undefined;
        return abpRef?.localization?.localize?.(key, 'Erp') || key;
    }

    private appendClass(current: any, className: string): any {
        if (!current) {
            return className;
        }

        if (typeof current === 'string') {
            const classes = new Set([...current.split(/\s+/), ...className.split(/\s+/)].filter(Boolean));
            return Array.from(classes).join(' ');
        }

        if (Array.isArray(current)) {
            const classes = new Set([...current, ...className.split(/\s+/)].filter(Boolean));
            return Array.from(classes);
        }

        return current;
    }
}
