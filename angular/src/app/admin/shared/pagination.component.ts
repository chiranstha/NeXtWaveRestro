import { Component, EventEmitter, Input, NO_ERRORS_SCHEMA, Output, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-pagination',
    templateUrl: './pagination.component.html',
    imports: [FormsModule],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class PaginationComponent {
    @Input() pageSizeOptions: number[] = [10, 20, 50, 100];
    @Input() totalRecords: number = 0;
    @Input() pageSize: number = 10;
    @Input() currentPage: number = 0;
    @Output() pageChange = new EventEmitter<number>();
    @Output() pageSizeChange = new EventEmitter<number>();
    get totalPages(): number {
        return Math.ceil(this.totalRecords / this.pageSize);
    }
    get currentPageIndex(): number {
        return this.pageSize > 0 ? Math.floor(this.currentPage / this.pageSize) : 0;
    }
    get visiblePageNumbers(): number[] {
        const displayedPages: number[] = [];
        const { totalPages } = this;
        const current = this.currentPageIndex + 1; // current is 1-based

        if (totalPages <= 10) {
            for (let i = 1; i <= totalPages; i++) {
                displayedPages.push(i);
            }
        } else {
            // Always show first page
            displayedPages.push(1);

            // Calculate range around current page
            const start = Math.max(2, current - 2);
            const end = Math.min(totalPages - 1, current + 2);

            // If there's a gap before start, add ellipsis (we'll represent as -1 for now, but handle in template)
            if (start > 2) {
                displayedPages.push(-1); // Ellipsis
            }

            // Add the range
            for (let i = start; i <= end; i++) {
                displayedPages.push(i);
            }

            // If there's a gap after end, add ellipsis
            if (end < totalPages - 1) {
                displayedPages.push(-1); // Ellipsis
            }

            // Always show last page if not already included
            if (totalPages > 1 && !displayedPages.includes(totalPages)) {
                displayedPages.push(totalPages);
            }
        }

        return displayedPages;
    }
    onPageSizeChange(event: any): void {
        const newSize = +event.target.value;
        this.pageSizeChange.emit(newSize);
    }
    goToPage(page: number): void {
        const targetPageIndex = page - 1;
        const currentPageIndex = this.pageSize > 0 ? Math.floor(this.currentPage / this.pageSize) : 0;
        if (targetPageIndex !== currentPageIndex && page >= 1 && page <= this.totalPages) {
            this.pageChange.emit(targetPageIndex * this.pageSize);
        }
    }
    firstPage(): void {
        const currentPageIndex = this.pageSize > 0 ? Math.floor(this.currentPage / this.pageSize) : 0;
        if (currentPageIndex !== 0) {
            this.pageChange.emit(0);
        }
    }
    previousPage(): void {
        const currentPageIndex = this.pageSize > 0 ? Math.floor(this.currentPage / this.pageSize) : 0;
        if (currentPageIndex > 0) {
            this.pageChange.emit((currentPageIndex - 1) * this.pageSize);
        }
    }
    nextPage(): void {
        const currentPageIndex = this.pageSize > 0 ? Math.floor(this.currentPage / this.pageSize) : 0;
        if (currentPageIndex < this.totalPages - 1) {
            this.pageChange.emit((currentPageIndex + 1) * this.pageSize);
        }
    }
    lastPage(): void {
        const currentPageIndex = this.pageSize > 0 ? Math.floor(this.currentPage / this.pageSize) : 0;
        if (currentPageIndex !== this.totalPages - 1) {
            this.pageChange.emit((this.totalPages - 1) * this.pageSize);
        }
    }
}
