import { PermissionCheckerService } from 'abp-ng2-module';
import { AppSessionService } from '@shared/common/session/app-session.service';

import { Injectable, inject } from '@angular/core';
import { AppMenu } from './app-menu';
import { AppMenuItem } from './app-menu-item';

@Injectable()
export class AppNavigationService {
    private _permissionCheckerService = inject(PermissionCheckerService);
    private _appSessionService = inject(AppSessionService);

    getMenu(): AppMenu {
        return new AppMenu('MainMenu', 'MainMenu', [
            new AppMenuItem(
                'Dashboard',
                'Pages.Administration.Host.Dashboard',
                'fa-light  fa-house',
                '/app/admin/hostDashboard'
            ),
            new AppMenuItem('Dashboard', 'Pages.Restaurant.Reports', 'fa-light  fa-house', '/app/main/dashboard'),
            new AppMenuItem('Saas', '', 'flaticon-users', '', [],
                [
                    new AppMenuItem('Tenants', 'Pages.Tenants', 'flaticon-list-3', '/app/admin/tenants'),
                    new AppMenuItem('Editions', 'Pages.Editions', 'flaticon-app', '/app/admin/editions')]),
            new AppMenuItem(
                'Restaurant',
                'Pages.Restaurant',
                'fa-light fa-utensils',
                '',
                [],
                [
                    new AppMenuItem(
                        'POS Billing',
                        'Pages.Restaurant.Pos',
                        'fa-light fa-cash-register',
                        '/app/main/restaurant/pos'
                    ),
                    new AppMenuItem(
                        'KDS',
                        'Pages.Restaurant.Kds',
                        'fa-light fa-kitchen-set',
                        '/app/main/restaurant/kds'
                    ),
                    new AppMenuItem(
                        'Restaurant Inventory',
                        'Pages.Restaurant.Inventory',
                        'fa-light fa-boxes-stacked',
                        '/app/main/restaurant/inventory'
                    ),
                    new AppMenuItem(
                        'Menu & Recipes',
                        'Pages.Restaurant.Menu',
                        'fa-light fa-clipboard-list',
                        '/app/main/restaurant/menu'
                    ),
                    new AppMenuItem(
                        'Channels',
                        'Pages.Restaurant.Channels',
                        'fa-light fa-store',
                        '/app/main/restaurant/channels'
                    ),
                    new AppMenuItem(
                        'Restaurant Setup',
                        'Pages.Restaurant.Setup',
                        'fa-light fa-table-picnic',
                        '/app/main/restaurant/setup'
                    ),
                    new AppMenuItem(
                        'Restaurant Reports',
                        'Pages.Restaurant.Reports',
                        'fa-light fa-chart-column',
                        '/app/main/restaurant/reports'
                    ),

                    new AppMenuItem(
                        'Printer Setup',
                        '',
                        'fa-light fa-print',
                        '/app/main/restaurant/print-station',
                    ),



                    new AppMenuItem(
                        'Restaurant Payroll',
                        'Pages.Restaurant.Payroll',
                        'fa-light fa-money-check-dollar',
                        '/app/main/restaurant/payroll'
                    ),
                ]
            ),
            new AppMenuItem('Accounting', 'Pages.Accounting', 'fa-light fa-books', '', [],
                [
                    new AppMenuItem('Account Groups', 'Pages.AccountGroups', 'fa-light fa-user-group', '/app/main/accounting/accountGroups',
                        [
                            '/app/main/accounting/accountGroups/add',
                            '/app/main/accounting/accountGroups/edit'
                        ]
                    ),
                    new AppMenuItem(
                        'Account Ledgers',
                        'Pages.AccountLedgers',
                        'fa-light fa-memo',
                        '/app/main/accounting/accountLedgers',
                        [
                            '/app/main/accounting/accountLedgers/add',
                            '/app/main/accounting/accountLedgers/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Financial Years',
                        'Pages.FinancialYears',
                        'fa-light fa-calendar',
                        '/app/main/accounting/financialYears',
                        [
                            '/app/main/accounting/financialYears/add',
                            '/app/main/accounting/financialYears/edit',
                        ]
                    ),
                    new AppMenuItem('Taxes', 'Pages.Taxes', 'fa-light fa-money-check-dollar', '/app/main/accounting/taxes',
                        [
                            '/app/main/accounting/taxes/add',
                            '/app/main/accounting/taxes/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Voucher Types',
                        'Pages.VoucherTypes',
                        'fa-light fa-receipt',
                        '/app/main/accounting/voucherTypes',
                        [
                            '/app/main/accounting/voucherTypes/add',
                            '/app/main/accounting/voucherTypes/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Merge Ledger',
                        'Pages.MergeLedger',
                        'fa-light fa-layer-plus',
                        '/app/main/accounting/mergeLedger',
                    )
                ]),

            new AppMenuItem(
                'Inventory',
                'Pages.Inventory',
                'fa-light fa-pancakes',
                '',
                [],
                [
                    new AppMenuItem('Units', 'Pages.Units', 'fa-light fa-weight-scale', '/app/main/inventory/units',
                        [
                            '/app/main/inventory/units/add',
                            '/app/main/inventory/units/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'ProductGroups',
                        'Pages.ProductGroups',
                        'fa-light fa-ball-pile',
                        '/app/main/inventory/productGroups',
                        [
                            '/app/main/inventory/productGroups/add',
                            '/app/main/inventory/productGroups/edit',
                        ]
                    ),
                    new AppMenuItem('Products', 'Pages.Products', 'fa-light fa-boxes-stacked', '/app/main/inventory/products',
                        [
                            '/app/main/inventory/products/add',
                            '/app/main/inventory/products/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Opening Stocks',
                        'Pages.Products',
                        'fa-light fa-chart-line-up',
                        '/app/main/inventory/openingStock'
                    ),
                    new AppMenuItem(
                        'Product Merge',
                        'Pages.ProductMerge',
                        'fa-light fa-barcode-scan',
                        '/app/main/inventory/productMerge'
                    ),
                ]
            ),
            new AppMenuItem(
                'Purchase',
                'Pages.Purchase',
                'fa-light fa-cart-shopping-fast',
                '',
                [],
                [
                    new AppMenuItem(
                        'PurchaseOrder',
                        'Pages.PurchaseOrderMasters',
                        'fa-light fa-cart-plus',
                        '/app/main/purchase/purchaseOrderMasters',
                        [
                            '/app/main/purchase/purchaseOrderMasters/add',
                            '/app/main/purchase/purchaseOrderMasters/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'PurchaseInvoice',
                        'Pages.PurchaseMasters',
                        'fa-light fa-file-invoice-dollar',
                        '/app/main/purchase/purchaseMasters',
                        [
                            '/app/main/purchase/purchaseMasters/add',
                            '/app/main/purchase/purchaseMasters/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'PurchaseReturns',
                        'Pages.PurchaseReturns',
                        'fa-light fa-rotate-left',
                        '/app/main/purchase/purchaseReturns',
                        [
                            '/app/main/purchase/purchaseReturns/add',
                            '/app/main/purchase/purchaseReturns/edit',
                        ]
                    ),
                ]
            ),

            new AppMenuItem(
                'Sales',
                'Pages.Sales',
                'fa-light fa-cart-circle-check',
                '',
                [],
                [
                    new AppMenuItem(
                        'SalesInvoice',
                        'Pages.SalesMasters',
                        'fa-light fa-file-invoice-dollar',
                        '/app/main/sales/salesInvoiceMasters',
                        [
                            '/app/main/sales/salesInvoiceMasters/add',
                            '/app/main/sales/salesInvoiceMasters/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'SalesReturn',
                        'Pages.SalesReturnMasters',
                        'fa-light fa-cart-circle-xmark',
                        '/app/main/sales/salesReturnMasters',
                        [
                            '/app/main/sales/salesReturnMasters/add',
                            '/app/main/sales/salesReturnMasters/edit',
                        ]
                    ),
                ]
            ),

            new AppMenuItem(
                'Transaction',
                'Pages.Transaction',
                'fa-light fa-gift-card',
                '',
                [],
                [
                    new AppMenuItem(
                        'Payment Masters',
                        'Pages.PaymentMasters',
                        'fa-light fa-sack-dollar',
                        '/app/main/transaction/paymentMasters',
                        [
                            '/app/main/transaction/paymentMasters/add',
                            '/app/main/transaction/paymentMasters/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Restaurant Stock Used',
                        'Pages.Restaurant.Inventory.StockAdjustment',
                        'fa-light fa-box-open',
                        '/app/main/transaction/restaurantStockUsed',
                        [
                            '/app/main/transaction/restaurantStockUsed/add',
                        ]
                    ),
                    new AppMenuItem(
                        'Receipt Masters',
                        'Pages.ReceiptMasters',
                        'fa-light fa-receipt',
                        '/app/main/transaction/receiptMaster',
                        [
                            '/app/main/transaction/receiptMaster/add',
                            '/app/main/transaction/receiptMaster/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Journal Masters',
                        'Pages.JournalMasters',
                        'fa-light fa-book',
                        '/app/main/transaction/journalMasters',
                        [
                            '/app/main/transaction/journalMasters/add',
                            '/app/main/transaction/journalMasters/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'Contra Masters',
                        'Pages.ContraMasters',
                        'fa-light fa-bank',
                        '/app/main/transaction/contraMasters',
                        [
                            '/app/main/transaction/contraMasters/add',
                            '/app/main/transaction/contraMasters/edit',
                        ]
                    ),
                     new AppMenuItem(
                        'PDC Payable',
                        'Pages.PDCPayables',
                        'fa-light fa-hand-holding-dollar',
                        '/app/main/transaction/PdcPayable',
                        [
                            '/app/main/transaction/PdcPayable/add',
                            '/app/main/transaction/PdcPayable/edit',
                        ]
                    ),
                    new AppMenuItem(
                        'PDC Receivable',
                        'Pages.PDCReceivables',
                        'fa-light fa-hands-holding-dollar',
                        '/app/main/transaction/PdcReceivable',
                        [
                            '/app/main/transaction/PdcReceivable/add',
                            '/app/main/transaction/PdcReceivable/edit',
                        ]

                    ),
                    new AppMenuItem(
                        'PDC Clearance',
                        'Pages.PDCClearances',
                        'fa-light fa-file-check',
                        '/app/main/transaction/PdcClearance',
                        [
                            '/app/main/transaction/PdcClearance/add',
                            '/app/main/transaction/PdcClearance/edit',
                        ]
                    ),
                ]
            ),


            new AppMenuItem(
                'Reports',
                'Pages.Reporting',
                'fa-light fa-chart-simple',
                '',
                [],
                [
                    new AppMenuItem(
                        'Accounting',
                        'Pages.AccountingReport',
                        'fa-light fa-books',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'AccountGroup',
                                'Pages.AccountGroupReport',
                                'fa-light fa-layer-group',
                                '/app/main/reports/account-group'
                            ),
                            new AppMenuItem(
                                'AccountLedger',
                                'Pages.AccountLedgerReport',
                                'fa-light fa-layer-group',
                                '/app/main/reports/account-ledger'
                            ),
                            new AppMenuItem(
                                'Daybook',
                                'Pages.BookReport',
                                'fa-light fa-book-open',
                                '/app/main/reports/daybook'
                            ),
                            new AppMenuItem(
                                'Book Report',
                                'Pages.BookReport',
                                'fa-light fa-book-open',
                                '/app/main/reports/book-report'
                            )
                        ]
                    ),
                    new AppMenuItem(
                        'Inventory',
                        'Pages.InventoryReport',
                        'fa-light fa-pancakes',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'StockReport',
                                'Pages.StockReport',
                                'fa-light fa-layer-group',
                                '/app/main/reports/stock-report'
                            ),
                            new AppMenuItem(
                                'ProductProfit',
                                'Pages.ProductProfitReport',
                                'fa-light fa-chart-mixed',
                                '/app/main/reports/product-profit'
                            ),
                        ]
                    ),

                    new AppMenuItem(
                        'Purchase',
                        'Pages.PurchaseReport',
                        'fa-light fa-receipt',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'PurchaseReport',
                                'Pages.PurchaseMasterReport',
                                'fa-light fa-layer-group',
                                '/app/main/reports/purchase-report'
                            ),
                            new AppMenuItem(
                                'PurchaseReturnReport',
                                'Pages.PurchaseReturnReport',
                                'fa-light fa-rotate-left',
                                '/app/main/reports/purchase-return'
                            ),
                            new AppMenuItem(
                                'Purchase Party Wise Tax',
                                'Pages.TaxableCustomerReport',
                                'fa-light fa-percent',
                                '/app/main/reports/purchase-party-tax'
                            ),
                            new AppMenuItem(
                                'Product Wise Monthly',
                                'Pages.ProductWiseMonthlyReport',
                                'fa-light fa-calendar',
                                '/app/main/reports/productWiseMonthlyPurchase'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'Sales',
                        'Pages.SalesReport',
                        'fa-light fa-chart-waterfall',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'SalesReport',
                                'Pages.SalesMasterReport',
                                'fa-light fa-layer-group',
                                '/app/main/reports/sales-report'
                            ),
                            new AppMenuItem(
                                'Sales Return',
                                'Pages.SalesReturnReport',
                                'fa-light fa-arrow-rotate-left',
                                '/app/main/reports/sales-return-report'
                            ),
                             new AppMenuItem(
                                'Material Sales',
                                'Pages.MaterialSalesReport',
                                'fa-light fa-chart-waterfall',
                                '/app/main/reports/material-sales-report'
                            ),
                            new AppMenuItem(
                                'LedgerWiseSales',
                                'Pages.LedgerWiseSalesReport',
                                'fa-light fa-chart-gantt',
                                '/app/main/reports/ledger-wise-sales'
                            ),
                            new AppMenuItem(
                                'ProductWiseSales',
                                'Pages.ProductWiseSalesReport',
                                'fa-light fa-chart-line-up',
                                '/app/main/reports/product-wise-sales'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'TaxReport',
                        'Pages.TaxReporting',
                        'fa-light fa-memo-pad',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'VatSummaryReport',
                                'Pages.VatSummaryReport',
                                'fa-light fa-percent',
                                '/app/main/reports/tax-report/vatSummaryReport'
                            ),
                            new AppMenuItem(
                                'Purchase Tax Report',
                                'Pages.TaxPurchaseRegisterReport',
                                'fa-light fa-chart-mixed',
                                '/app/main/reports/tax-report/purchaseTaxReport'
                            ),
                            new AppMenuItem(
                                'Sales Tax Report',
                                'Pages.TaxSalesRegisterReport',
                                'fa-light fa-chart-line-up',
                                '/app/main/reports/tax-report/salesTaxReport'
                            ),
                            new AppMenuItem(
                                'Sales Above Lakhs',
                                'Pages.SalesReturnReport',
                                'fa-light fa-chart-line',
                                '/app/main/reports/tax-report/salesAboveLakhsReport'
                            ),
                            new AppMenuItem(
                                'Purchase Above Lakhs',
                                'Pages.SalesReturnReport',
                                'fa-light fa-cart-shopping',
                                '/app/main/reports/tax-report/purchaseAboveLakhsReport'
                            ),
                            new AppMenuItem(
                                'Party wise tax',
                                'Pages.SalesReturnReport',
                                'fa-light fa-badge-percent',
                                '/app/main/reports/partyWiseTax'
                            ),
                        ]
                    ),

                     new AppMenuItem(
                        'FinancialStatement',
                        'Pages.FinancialStatement',
                        'fa-light fa-chart-mixed',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'TrialBalance',
                                'Pages.TrialBalanceReport',
                                'fa-light fa-receipt',
                                '/app/main/financialStatement/trailbalance',
                            ),
                            new AppMenuItem(
                                'ProfitandLoss',
                                'Pages.ProfitAndLossReport',
                                'fa-light fa-arrow-down-arrow-up',
                                '/app/main/financialStatement/profit-loss',
                            ),
                            new AppMenuItem(
                                'BalanceSheet',
                                'Pages.PagesBalanceSheetReport',
                                'fa-light fa-memo-pad',
                                '/app/main/financialStatement/balance-sheet',
                            ),
                            new AppMenuItem(
                                'CashFlow',
                                'Pages.PagesCashFlowReport',
                                'fa-light fa-water',
                                '/app/main/financialStatement/cash-flow',
                            ),
                        ],
                    ),
                ]
            ),

            new AppMenuItem(
                'Administration',
                'Pages.Administration',
                'fa-light fa-gear',
                '',
                [],
                [
                    new AppMenuItem(
                        'OrganizationUnits',
                        'Pages.Administration.OrganizationUnits',
                        'fa-light fa-buildings',
                        '/app/admin/organization-units'
                    ),
                    new AppMenuItem('Roles', 'Pages.Administration.Roles', 'fa-light fa-person', '/app/admin/roles'),
                    new AppMenuItem('Users', 'Pages.Administration.Users', 'fa-light fa-users', '/app/admin/users'),
                    new AppMenuItem(
                        'Languages',
                        'Pages.Administration.Languages',
                        'fa-light fa-language',
                        '/app/admin/languages',
                        ['/app/admin/languages/{name}/texts']
                    ),
                    new AppMenuItem(
                        'AuditLogs',
                        'Pages.Administration.AuditLogs',
                        'fa-light fa-list-check',
                        '/app/admin/auditLogs'
                    ),
                    new AppMenuItem(
                        'Maintenance',
                        'Pages.Administration.Host.Maintenance',
                        'fa-light fa-sliders',
                        '/app/admin/maintenance'
                    ),
                    new AppMenuItem(
                        'Subscription',
                        'Pages.Administration.Tenant.SubscriptionManagement',
                        'fa-light fa-hand-pointer',
                        '/app/admin/subscription-management'
                    ),
                    new AppMenuItem(
                        'VisualSettings',
                        'Pages.Administration.UiCustomization',
                        'fa-light fa-eye',
                        '/app/admin/ui-customization'
                    ),
                    new AppMenuItem(
                        'WebhookSubscriptions',
                        'Pages.Administration.WebhookSubscription',
                        'fa-light fa-bell',
                        '/app/admin/webhook-subscriptions'
                    ),
                    new AppMenuItem(
                        'DynamicProperties',
                        'Pages.Administration.DynamicProperties',
                        'fa-light fa-star',
                        '/app/admin/dynamic-property'
                    ),
                    new AppMenuItem(
                        'Notifications',
                        '',
                        'fa-light fa-alarm-clock',
                        '',
                        [],
                        [
                            new AppMenuItem('Inbox', '', 'fa-light fa-envelope', '/app/notifications'),
                            new AppMenuItem(
                                'MassNotifications',
                                'Pages.Administration.MassNotification',
                                'fa-light fa-paper-plane',
                                '/app/admin/mass-notifications'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'Settings',
                        'Pages.Administration.Host.Settings',
                        'fa-light fa-gear',
                        '/app/admin/hostSettings'
                    ),
                    new AppMenuItem(
                        'Settings',
                        'Pages.Administration.Tenant.Settings',
                        'fa-light fa-gear',
                        '/app/admin/tenantSettings'
                    ),
                ]
            ),

        ]);
    }

    checkChildMenuItemPermission(menuItem): boolean {
        for (let i = 0; i < menuItem.items.length; i++) {
            const subMenuItem = menuItem.items[i];

            if (subMenuItem.permissionName === '' || subMenuItem.permissionName === null) {
                if (subMenuItem.route) {
                    return true;
                }
            } else if (this._permissionCheckerService.isGranted(subMenuItem.permissionName)) {
                if (!subMenuItem.hasFeatureDependency()) {
                    return true;
                }

                if (subMenuItem.featureDependencySatisfied()) {
                    return true;
                }
            }

            if (subMenuItem.items?.length) {
                const isAnyChildItemActive = this.checkChildMenuItemPermission(subMenuItem);
                if (isAnyChildItemActive) {
                    return true;
                }
            }
        }

        return false;
    }

    showMenuItem(menuItem: AppMenuItem): boolean {
        if (
            menuItem.permissionName === 'Pages.Administration.Tenant.SubscriptionManagement' &&
            this._appSessionService.tenant &&
            !this._appSessionService.tenant.edition
        ) {
            return false;
        }

        let hideMenuItem = false;

        if (menuItem.requiresAuthentication && !this._appSessionService.user) {
            hideMenuItem = true;
        }

        if (menuItem.permissionName && !this._permissionCheckerService.isGranted(menuItem.permissionName)) {
            hideMenuItem = true;
        }

        if (this._appSessionService.tenant || !abp.multiTenancy.ignoreFeatureCheckForHostUsers) {
            if (menuItem.hasFeatureDependency() && !menuItem.featureDependencySatisfied()) {
                hideMenuItem = true;
            }
        }

        if (!hideMenuItem && menuItem.items && menuItem.items.length) {
            return this.checkChildMenuItemPermission(menuItem);
        }

        return !hideMenuItem;
    }

    /**
     * Returns all menu items recursively
     */
    getAllMenuItems(): AppMenuItem[] {
        const menu = this.getMenu();
        let allMenuItems: AppMenuItem[] = [];
        menu.items.forEach((menuItem) => {
            allMenuItems = allMenuItems.concat(this.getAllMenuItemsRecursive(menuItem));
        });

        return allMenuItems;
    }

    private getAllMenuItemsRecursive(menuItem: AppMenuItem): AppMenuItem[] {
        if (!menuItem.items) {
            return [menuItem];
        }

        let menuItems = [menuItem];
        menuItem.items.forEach((subMenu) => {
            menuItems = menuItems.concat(this.getAllMenuItemsRecursive(subMenu));
        });

        return menuItems;
    }
}
