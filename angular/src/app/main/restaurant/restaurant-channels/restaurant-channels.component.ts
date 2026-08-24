import { Component, Injector, OnInit, ViewEncapsulation, inject, ChangeDetectionStrategy } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AcceptRestaurantAggregatorOrderDto,
    CreateOrEditRestaurantChannelAccountDto,
    CreateOrEditRestaurantChannelDto,
    ImportRestaurantAggregatorPayoutDto,
    ImportRestaurantAggregatorPayoutLineDto,
    RestaurantAggregatorOrderDto,
    RestaurantAggregatorPayoutDto,
    RestaurantChannelAccountDto,
    RestaurantChannelDto,
    RestaurantChannelItemDto,
    RestaurantChannelServiceProxy,
    RestaurantCustomerOrderLineDto,
    RestaurantMenuItemDto,
    RestaurantMenuServiceProxy,
    RestaurantMenuSyncLogDto,
    SaveRestaurantChannelItemDto,
    UpdateRestaurantAggregatorOrderStatusDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize, forkJoin } from 'rxjs';

type RestaurantChannelsTab = 'channels' | 'mapping' | 'orders' | 'payouts';

interface RestaurantChannelDateFilter {
    fromDate: string;
    toDate: string;
}

interface RestaurantQuickAcceptForm {
    menuItemId: string;
    qty: number;
}

interface RestaurantChannelForm {
    id: string | undefined;
    name: string;
    channelType: number;
    provider: number;
    commissionPercent: number;
    defaultPriceMarkupPercent: number;
    sortOrder: number;
    isOnline: boolean;
    isActive: boolean;
}

interface RestaurantChannelAccountForm {
    id: string | undefined;
    channelId: string;
    provider: number;
    externalStoreId: string;
    displayName: string;
    apiBaseUrl: string;
    apiCredentialsJson: string;
    webhookSecret: string;
    isOnline: boolean;
    isActive: boolean;
}

interface RestaurantChannelItemForm {
    id: string | undefined;
    channelId: string;
    menuItemId: string;
    externalItemId: string;
    externalSku: string;
    channelPrice: number;
    isOnline: boolean;
}

interface RestaurantPayoutLineForm {
    externalOrderId: string;
    expectedAmount: number;
    commissionAmount: number;
    restaurantDiscountAmount: number;
    deliveryFeeAmount: number;
    paidAmount: number;
}

interface RestaurantPayoutImportForm {
    channelId: string | undefined;
    provider: number;
    externalPayoutId: string;
    periodFrom: string;
    periodTo: string;
    paidAt: string;
    notes: string;
    lines: RestaurantPayoutLineForm[];
}

@Component({
    selector: 'restaurant-channels',
    templateUrl: './restaurant-channels.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false,
})
export class RestaurantChannelsComponent extends AppComponentBase implements OnInit {
    activeTab: RestaurantChannelsTab = 'channels';
    loading = false;
    saving = false;
    channels: RestaurantChannelDto[] = [];
    accounts: RestaurantChannelAccountDto[] = [];
    channelItems: RestaurantChannelItemDto[] = [];
    syncLogs: RestaurantMenuSyncLogDto[] = [];
    orders: RestaurantAggregatorOrderDto[] = [];
    payouts: RestaurantAggregatorPayoutDto[] = [];
    menuItems: RestaurantMenuItemDto[] = [];
    selectedChannelId = '';
    filter: RestaurantChannelDateFilter = { fromDate: '', toDate: '' };
    quickAcceptForms: Record<string, RestaurantQuickAcceptForm> = {};

    providerOptions = [
        { value: 0, label: 'Internal' },
        { value: 1, label: 'Own Online' },
        { value: 2, label: 'Foodmandu' },
        { value: 3, label: 'Pathao' },
    ];

    channelTypeOptions = [
        { value: 0, label: 'Dine In' },
        { value: 1, label: 'Take Away' },
        { value: 2, label: 'Own Online' },
        { value: 3, label: 'Aggregator' },
    ];

    channelForm: RestaurantChannelForm = this.createEmptyChannelForm();
    accountForm: RestaurantChannelAccountForm = this.createEmptyAccountForm();
    itemForm: RestaurantChannelItemForm = this.createEmptyItemForm();
    payoutForm: RestaurantPayoutImportForm = this.createEmptyPayoutForm();

    private channelService = inject(RestaurantChannelServiceProxy);
    private menuService = inject(RestaurantMenuServiceProxy);

    constructor() {
        super(inject(Injector));
    }

    ngOnInit(): void {
        this.refresh();
    }

    get activeChannelsCount(): number {
        return this.channels.filter((channel) => channel.isActive).length;
    }

    get pendingOrdersCount(): number {
        return this.orders.filter((order) => order.status === 0).length;
    }

    get payoutIssueCount(): number {
        return this.payouts.reduce(
            (count, payout) =>
                count + (payout.lines || []).filter((line) => line.matchStatus === 2 || line.matchStatus === 3).length,
            0,
        );
    }

    refresh(): void {
        this.loading = true;
        forkJoin({
            channels: this.channelService.getChannels(),
            accounts: this.channelService.getChannelAccounts(undefined),
            items: this.channelService.getChannelItems(undefined),
            orders: this.channelService.getAggregatorOrders(
                this.toDateTime(this.filter.fromDate),
                this.toDateTime(this.filter.toDate),
                undefined,
                undefined,
                undefined,
            ),
            payouts: this.channelService.getPayouts(
                this.toDateTime(this.filter.fromDate),
                this.toDateTime(this.filter.toDate),
                undefined,
                undefined,
                undefined,
            ),
            menuData: this.menuService.getMenuEditorData(),
        })
            .pipe(finalize(() => (this.loading = false)))
            .subscribe((result) => {
                this.channels = result.channels || [];
                this.accounts = result.accounts || [];
                this.channelItems = result.items || [];
                this.orders = result.orders || [];
                this.payouts = result.payouts || [];
                this.menuItems = result.menuData?.menuItems || [];

                if (!this.selectedChannelId && this.channels.length) {
                    this.selectedChannelId = this.channels[0].id;
                }

                this.resetAccountForm(false);
                this.resetItemForm(false);
                this.resetQuickAcceptForms();
            });
    }

    setTab(tab: RestaurantChannelsTab): void {
        this.activeTab = tab;
    }

    saveChannel(): void {
        if (!this.channelForm.name) {
            this.notify.warn('Channel name is required');
            return;
        }

        this.saving = true;
        this.channelService
            .createOrEditChannel(new CreateOrEditRestaurantChannelDto(this.channelForm))
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((id) => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.selectedChannelId = id || this.selectedChannelId;
                this.channelForm = this.createEmptyChannelForm();
                this.refresh();
            });
    }

    editChannel(channel: RestaurantChannelDto): void {
        this.channelForm = { ...channel };
        this.selectedChannelId = channel.id;
    }

    resetChannelForm(): void {
        this.channelForm = this.createEmptyChannelForm();
    }

    saveAccount(): void {
        if (!this.accountForm.channelId || !this.accountForm.displayName) {
            this.notify.warn('Channel and account name are required');
            return;
        }

        this.saving = true;
        this.channelService
            .createOrEditChannelAccount(new CreateOrEditRestaurantChannelAccountDto(this.accountForm))
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetAccountForm();
                this.refresh();
            });
    }

    editAccount(account: RestaurantChannelAccountDto): void {
        this.accountForm = {
            id: account.id,
            channelId: account.channelId,
            provider: account.provider,
            externalStoreId: account.externalStoreId || '',
            displayName: account.displayName || '',
            apiBaseUrl: account.apiBaseUrl || '',
            apiCredentialsJson: '',
            webhookSecret: '',
            isOnline: account.isOnline,
            isActive: account.isActive,
        };
        this.selectedChannelId = account.channelId;
    }

    resetAccountForm(clearId = true): void {
        this.accountForm = this.createEmptyAccountForm();
        this.accountForm.channelId = this.selectedChannelId || this.channels[0]?.id || '';
        if (clearId) {
            this.accountForm.id = undefined;
        }
    }

    saveItem(): void {
        if (!this.itemForm.channelId || !this.itemForm.menuItemId) {
            this.notify.warn('Channel and menu item are required');
            return;
        }

        this.saving = true;
        this.channelService
            .saveChannelItem(new SaveRestaurantChannelItemDto(this.itemForm))
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.resetItemForm();
                this.refresh();
            });
    }

    editItem(item: RestaurantChannelItemDto): void {
        this.itemForm = { ...item };
        this.selectedChannelId = item.channelId;
    }

    resetItemForm(clearId = true): void {
        this.itemForm = this.createEmptyItemForm();
        this.itemForm.channelId = this.selectedChannelId || this.channels[0]?.id || '';
        this.itemForm.menuItemId = this.menuItems[0]?.id || '';
        if (clearId) {
            this.itemForm.id = undefined;
        }
    }

    queuePublish(channel: RestaurantChannelDto): void {
        this.saving = true;
        this.channelService
            .queueMenuPublish(channel.id)
            .pipe(finalize(() => (this.saving = false)))
            .subscribe((logs) => {
                this.syncLogs = logs || [];
                this.notify.success('Menu publish queued');
                this.refresh();
            });
    }

    acceptOrder(order: RestaurantAggregatorOrderDto): void {
        const form = this.quickAcceptForms[order.id];
        if (!form?.menuItemId || Number(form.qty || 0) <= 0) {
            this.notify.warn('Select an item and quantity before accepting');
            return;
        }

        this.saving = true;
        this.channelService
            .acceptAggregatorOrder(
                new AcceptRestaurantAggregatorOrderDto({
                    aggregatorOrderId: order.id,
                    sendToKitchen: true,
                    lines: [
                        new RestaurantCustomerOrderLineDto({
                            menuItemId: form.menuItemId,
                            variantId: undefined,
                            qty: Number(form.qty || 1),
                            notes: undefined,
                            modifiers: [],
                        }),
                    ],
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success('Order accepted');
                this.refresh();
            });
    }

    updateOrder(order: RestaurantAggregatorOrderDto, status: number): void {
        this.saving = true;
        this.channelService
            .updateAggregatorOrderStatus(
                new UpdateRestaurantAggregatorOrderStatusDto({
                    aggregatorOrderId: order.id,
                    status,
                    message: status === 2 ? 'Rejected by restaurant' : 'Cancelled by restaurant',
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.refresh();
            });
    }

    importPayout(): void {
        if (!this.payoutForm.externalPayoutId || !this.payoutForm.lines.length) {
            this.notify.warn('Payout id and at least one line are required');
            return;
        }

        this.saving = true;
        this.channelService
            .importPayout(
                new ImportRestaurantAggregatorPayoutDto({
                    ...this.payoutForm,
                    periodFrom: this.toDateTime(this.payoutForm.periodFrom) || DateTime.local(),
                    periodTo: this.toDateTime(this.payoutForm.periodTo) || DateTime.local(),
                    paidAt: this.toDateTime(this.payoutForm.paidAt),
                    lines: this.payoutForm.lines.map((line) => new ImportRestaurantAggregatorPayoutLineDto(line)),
                }),
            )
            .pipe(finalize(() => (this.saving = false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.payoutForm = this.createEmptyPayoutForm();
                this.refresh();
            });
    }

    addPayoutLine(): void {
        this.payoutForm.lines.push(this.createEmptyPayoutLine());
    }

    removePayoutLine(index: number): void {
        this.payoutForm.lines.splice(index, 1);
    }

    providerText(value: number): string {
        return this.providerOptions.find((option) => option.value === value)?.label || 'Provider';
    }

    channelTypeText(value: number): string {
        return this.channelTypeOptions.find((option) => option.value === value)?.label || 'Channel';
    }

    syncStatusText(value: number): string {
        return ['Pending', 'Synced', 'Failed', 'Disabled'][value] || 'Pending';
    }

    orderStatusText(value: number): string {
        return ['Received', 'Accepted', 'Rejected', 'Cancelled', 'Completed'][value] || 'Received';
    }

    payoutStatusText(value: number): string {
        return ['Pending', 'Matched', 'Unmatched', 'Discrepancy'][value] || 'Pending';
    }

    statusClass(value: number): string {
        if (value === 1) {
            return 'bg-light-success text-success';
        }
        if (value === 2 || value === 3) {
            return 'bg-light-danger text-danger';
        }

        return 'bg-light-primary text-primary';
    }

    private resetQuickAcceptForms(): void {
        for (const order of this.orders) {
            this.quickAcceptForms[order.id] = this.quickAcceptForms[order.id] || {
                menuItemId: this.menuItems[0]?.id || '',
                qty: 1,
            };
        }
    }

    private createEmptyChannelForm(): RestaurantChannelForm {
        return {
            id: undefined,
            name: '',
            channelType: 3,
            provider: 2,
            commissionPercent: 0,
            defaultPriceMarkupPercent: 0,
            sortOrder: 0,
            isOnline: true,
            isActive: true,
        };
    }

    private createEmptyAccountForm(): RestaurantChannelAccountForm {
        return {
            id: undefined,
            channelId: '',
            provider: 2,
            externalStoreId: '',
            displayName: '',
            apiBaseUrl: '',
            apiCredentialsJson: '',
            webhookSecret: '',
            isOnline: true,
            isActive: true,
        };
    }

    private createEmptyItemForm(): RestaurantChannelItemForm {
        return {
            id: undefined,
            channelId: '',
            menuItemId: '',
            externalItemId: '',
            externalSku: '',
            channelPrice: 0,
            isOnline: true,
        };
    }

    private createEmptyPayoutForm(): RestaurantPayoutImportForm {
        const today = new Date().toISOString().substring(0, 10);
        return {
            channelId: undefined,
            provider: 2,
            externalPayoutId: '',
            periodFrom: today,
            periodTo: today,
            paidAt: today,
            notes: '',
            lines: [this.createEmptyPayoutLine()],
        };
    }

    private createEmptyPayoutLine(): RestaurantPayoutLineForm {
        return {
            externalOrderId: '',
            expectedAmount: 0,
            commissionAmount: 0,
            restaurantDiscountAmount: 0,
            deliveryFeeAmount: 0,
            paidAmount: 0,
        };
    }

    private toDateTime(value: string | undefined): DateTime | undefined {
        return value ? DateTime.fromISO(value) : undefined;
    }
}
