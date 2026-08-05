import { Component, Injector, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppNavigationService } from '../app-navigation.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'menu-search-bar',
    templateUrl: './menu-search-bar.component.html',
    styleUrls: ['./menu-search-bar.component.css'],
    imports: [FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class MenuSearchBarComponent extends AppComponentBase {
    private _appNavigationService = inject(AppNavigationService);
    private router = inject(Router);
    allMenuItems: any[];
    searchMenuResults: any[];
    isMenuSearchActive = false;
    searchQuery = '';
    constructor() {
        const injector = inject(Injector);
        super();
        this.initializeMenuSearch();
    }
    showMenuItem(menuItem): boolean {
        return this._appNavigationService.showMenuItem(menuItem);
    }
    selectMenuItem(event) {
        if (event?.route) {
            if (event.external) {
                window.open(event.route, '_blank');
            } else {
                this.router.navigate([event.route], { queryParams: event.parameters }).then((navigated) => {
                    this.searchQuery = '';
                    this.searchMenuResults = [];
                });
            }
        }
    }
    onSearchInput(event) {
        const query = event.target.value;
        if (query.length >= 1) {
            this.searchMenuResults = this.allMenuItems
                .filter(
                    (item) =>
                        item.name.toLowerCase().includes(query.toLowerCase()) ||
                        item.route.toLowerCase().includes(query.toLowerCase()),
                )
                .map((menuItem) => {
                    return {
                        name: menuItem.name,
                        route: menuItem.route,
                        external: menuItem.external,
                        parameters: menuItem.parameters,
                    };
                });
        } else {
            this.searchMenuResults = [];
        }
    }
    onEnter() {
        if (this.searchMenuResults && this.searchMenuResults.length > 0) {
            this.selectMenuItem(this.searchMenuResults[0]);
        }
    }
    private getAllMenuItems() {
        return this._appNavigationService
            .getAllMenuItems()
            .filter((item) => this.showMenuItem(item) && item.route)
            .map((menuItem) => {
                return {
                    name: this.l(menuItem.name),
                    route: menuItem.route,
                    external: menuItem.external,
                    parameters: menuItem.parameters,
                };
            });
    }
    private initializeMenuSearch() {
        this.isMenuSearchActive = false;
        const themeSettings = this.currentTheme.baseSettings;
        if (themeSettings && themeSettings.menu && themeSettings.menu.searchActive) {
            this.allMenuItems = this.getAllMenuItems();
            this.isMenuSearchActive = true;
        }
    }
}
