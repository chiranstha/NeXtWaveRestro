import { PermissionCheckerService } from 'abp-ng2-module';
import {
    AfterViewInit,
    Component,
    DestroyRef,
    ElementRef,
    Input,
    OnInit,
    Renderer2,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppMenu } from './app-menu';
import { AppNavigationService } from './app-navigation.service';
import { NavigationCancel, NavigationEnd, Router, RouterLinkActive, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { FormattedStringValueExtracter } from '@shared/helpers/FormattedStringValueExtracter';
import * as objectPath from 'object-path';
import { AppMenuItem } from './app-menu-item';
import { DrawerComponent, MenuComponent, ScrollComponent, ToggleComponent } from '@metronic/app/kt/components';
import { NgIf, NgTemplateOutlet, NgFor, NgClass } from '@angular/common';
import { MenuSearchBarComponent } from './menu-search-bar/menu-search-bar.component';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
@Component({
    templateUrl: './side-bar-menu.component.html',
    selector: 'side-bar-menu',
    encapsulation: ViewEncapsulation.None,
    imports: [
        NgIf,
        MenuSearchBarComponent,
        NgTemplateOutlet,
        NgFor,
        NgClass,
        RouterLinkActive,
        RouterLink,
        TooltipDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class SideBarMenuComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private el = inject(ElementRef);
    private router = inject(Router);
    private destroyRef = inject(DestroyRef);
    permission = inject(PermissionCheckerService);
    private _appNavigationService = inject(AppNavigationService);
    private render = inject(Renderer2);
    @Input() iconMenu = false;
    @Input() menuClass = 'menu menu-column menu-rounded menu-sub-indention px-3';
    menu: AppMenu = null;
    currentRouteUrl = '';
    insideTm: any;
    outsideTm: any;

    ngOnInit() {
        this.menu = this._appNavigationService.getMenu();
        this.currentRouteUrl = this.router.url.split(/[?#]/)[0];
        this.router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd || event instanceof NavigationCancel),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.reinitializeMenu();
            });
    }
    ngAfterViewInit(): void {
        this.scrollToCurrentMenuElement();
    }
    reinitializeMenu(): void {
        this.menu = this._appNavigationService.getMenu();
        this.currentRouteUrl = this.router.url.split(/[?#]/)[0];
        setTimeout(() => {
            MenuComponent.reinitialization();
            DrawerComponent.reinitialization();
            ToggleComponent.reinitialization();
            ScrollComponent.reinitialization();
        }, 50);
    }
    showMenuItem(menuItem): boolean {
        return this._appNavigationService.showMenuItem(menuItem);
    }
    getTriggerCssClass(item, parentItem): string {
        if (!item.items && item.items.length <= 0) {
            return '';
        }
        if (this.iconMenu && parentItem == null) {
            return 'hover';
        }
        return 'click';
    }
    isMenuItemIsActive(item): boolean {
        if (item.items.length) {
            return this.isMenuRootItemIsActive(item);
        }
        if (!item.route) {
            return false;
        }
        const urlTree = this.router.parseUrl(this.currentRouteUrl.replace(/\/$/, ''));
        const urlString = `/${urlTree.root.children.primary.segments.map((segment) => segment.path).join('/')}`;
        const exactMatch = urlString === item.route.replace(/\/$/, '');
        if (!exactMatch && item.routeTemplates) {
            for (let i = 0; i < item.routeTemplates.length; i++) {
                const result = new FormattedStringValueExtracter().Extract(urlString, item.routeTemplates[i]);
                if (result.IsMatch) {
                    return true;
                }
            }
        }
        return exactMatch;
    }
    isMenuRootItemIsActive(item): boolean {
        let result = false;
        for (const subItem of item.items) {
            result = this.isMenuItemIsActive(subItem);
            if (result) {
                return true;
            }
        }
        return false;
    }
    scrollToCurrentMenuElement(): void {
        const path = location.pathname;
        const menuItem = document.querySelector(`a[href='${path}']`);
        if (menuItem) {
            menuItem.scrollIntoView({ block: 'center' });
        }
    }
    getItemCssClasses(item: AppMenuItem, parentItem: AppMenuItem) {
        let classes = 'menu-item';
        if (item.items.length) {
            if (!this.iconMenu) {
                classes += ' menu-accordion';
            } else {
                if (parentItem == null) {
                    classes += ' menu-dropdown';
                } else {
                    classes += ' menu-accordion';
                }
            }
        }
        // custom class for menu item
        const customClass = objectPath.get(item, 'custom-class');
        if (customClass) {
            classes += ` ${customClass}`;
        }
        if (this.iconMenu && parentItem == null) {
            classes += ' pb-3';
        }
        if (!this.iconMenu && this.isMenuItemIsActive(item)) {
            classes += ' show';
        }
        return classes;
    }
    getSubMenuItemCssClass(item: AppMenuItem, parentItem: AppMenuItem): string {
        let classes = 'menu-sub';
        if (!this.iconMenu) {
            classes += ' menu-sub-accordion';
        } else {
            if (parentItem == null) {
                classes += ' menu-sub-dropdown px-1 py-4';
            } else {
                classes += ' menu-sub-accordion';
            }
        }
        return classes;
    }
}
