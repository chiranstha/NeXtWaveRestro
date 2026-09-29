import { ElementRef, Component, ViewChild, OnInit, AfterViewInit, DestroyRef, inject } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { ThemesLayoutBaseComponent } from '@app/shared/layout/themes/themes-layout-base.component';
import { AppConsts } from '@shared/AppConsts';
import { AppNavigationService } from '@app/shared/layout/nav/app-navigation.service';
import { AppMenuItem } from '@app/shared/layout/nav/app-menu-item';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    
    standalone: false,
    templateUrl: './theme2-layout.component.html',
    styleUrls: ['./theme2-layout.component.less'],
    selector: 'theme2-layout',
    animations: [appModuleAnimation],
})
export class Theme2LayoutComponent extends ThemesLayoutBaseComponent implements OnInit, AfterViewInit {
    @ViewChild('ktHeader', { static: true }) ktHeader: ElementRef;

    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    isMobileMenuOpen = false;
    theme2ModuleItems: AppMenuItem[] = [];
    selectedTheme2Module: AppMenuItem | null = null;
    private readonly destroyRef = inject(DestroyRef);
    constructor(
        private appNavigationService: AppNavigationService,
        private router: Router,
    ) {
        super();
    }

    ngOnInit() {
        this.installationMode = UrlHelper.isInstallUrl(location.href);
        const visibleRootItems = this.appNavigationService
            .getMenu()
            .items.filter((item) => this.appNavigationService.showMenuItem(item));

        this.theme2ModuleItems = visibleRootItems;

        this.selectModuleForRoute(this.router.url);
        this.router.events
            .pipe(
                filter((event): event is NavigationEnd => event instanceof NavigationEnd),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe((event) => this.selectModuleForRoute(event.urlAfterRedirects || event.url));
    }

    ngAfterViewInit(): void {

    }

    toggleMobileMenu(): void {
        this.isMobileMenuOpen = !this.isMobileMenuOpen;
    }

    selectTheme2Module(item: AppMenuItem): void {
        this.selectedTheme2Module = item;

        if (!item.items.length && item.route) {
            this.router.navigate([item.route], { queryParams: item.parameters });
        }
    }

    private selectModuleForRoute(route: string): void {
        const normalizedRoute = (route || '').split(/[?#]/)[0];
        const matchingItem = this.theme2ModuleItems.find((item) => this.menuContainsRoute(item, normalizedRoute));

        if (matchingItem) {
            this.selectedTheme2Module = matchingItem;
        } else if (!this.selectedTheme2Module && this.theme2ModuleItems.length) {
            this.selectedTheme2Module = this.theme2ModuleItems[0];
        }
    }

    private menuContainsRoute(item: AppMenuItem, route: string): boolean {
        if (item.route && item.route.replace(/\/$/, '') === route.replace(/\/$/, '')) {
            return true;
        }

        if (item.routeTemplates?.some((template) => route.startsWith(template.split('{')[0]))) {
            return true;
        }

        return item.items.some((child) => this.menuContainsRoute(child, route));
    }
}
