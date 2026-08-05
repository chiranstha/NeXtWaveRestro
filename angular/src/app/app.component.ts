import { Component, OnInit, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { SubscriptionStartType } from '@shared/service-proxies/service-proxies';
import { ChatSignalrService } from 'app/shared/layout/chat/chat-signalr.service';
import { AppComponentBase } from 'shared/common/app-component-base';
import { SignalRHelper } from 'shared/helpers/SignalRHelper';
import { LinkedAccountsModalComponent } from '@app/shared/layout/linked-accounts-modal.component';
import { UserDelegationsModalComponent } from '@app/shared/layout/user-delegations-modal.component';
import { ChangePasswordModalComponent } from '@app/shared/layout/profile/change-password-modal.component';
import { ChangeProfilePictureModalComponent } from '@app/shared/layout/profile/change-profile-picture-modal.component';
import { MySettingsModalComponent } from '@app/shared/layout/profile/my-settings-modal.component';
import { NotificationSettingsModalComponent } from '@app/shared/layout/notifications/notification-settings-modal.component';
import { UserNotificationHelper } from '@app/shared/layout/notifications/UserNotificationHelper';
import { DateTimeService } from './shared/common/timing/date-time.service';
import {
    ToggleComponent,
    ScrollTopComponent,
    DrawerComponent,
    StickyComponent,
    MenuComponent,
    ScrollComponent,
} from '@metronic/app/kt/components';
import { NgClass } from '@angular/common';
import { DefaultLayoutComponent } from './shared/layout/themes/default/default-layout.component';
import { Theme2LayoutComponent } from './shared/layout/themes/theme2/theme2-layout.component';
import { Theme3LayoutComponent } from './shared/layout/themes/theme3/theme3-layout.component';
import { Theme4LayoutComponent } from './shared/layout/themes/theme4/theme4-layout.component';
import { Theme5LayoutComponent } from './shared/layout/themes/theme5/theme5-layout.component';
import { Theme6LayoutComponent } from './shared/layout/themes/theme6/theme6-layout.component';
import { Theme7LayoutComponent } from './shared/layout/themes/theme7/theme7-layout.component';
import { Theme8LayoutComponent } from './shared/layout/themes/theme8/theme8-layout.component';
import { Theme9LayoutComponent } from './shared/layout/themes/theme9/theme9-layout.component';
import { Theme10LayoutComponent } from './shared/layout/themes/theme10/theme10-layout.component';
import { Theme11LayoutComponent } from './shared/layout/themes/theme11/theme11-layout.component';
import { Theme12LayoutComponent } from './shared/layout/themes/theme12/theme12-layout.component';
import { Theme13LayoutComponent } from './shared/layout/themes/theme13/theme13-layout.component';
import { ScrollTopComponent as ScrollTopComponent_1 } from './shared/layout/scroll-top.component';
import { LinkedAccountsModalComponent as LinkedAccountsModalComponent_1 } from './shared/layout/linked-accounts-modal.component';
import { UserDelegationsModalComponent as UserDelegationsModalComponent_1 } from './shared/layout/user-delegations-modal.component';
import { ChangePasswordModalComponent as ChangePasswordModalComponent_1 } from './shared/layout/profile/change-password-modal.component';
import { ChangeProfilePictureModalComponent as ChangeProfilePictureModalComponent_1 } from './shared/layout/profile/change-profile-picture-modal.component';
import { MySettingsModalComponent as MySettingsModalComponent_1 } from './shared/layout/profile/my-settings-modal.component';
import { NotificationSettingsModalComponent as NotificationSettingsModalComponent_1 } from './shared/layout/notifications/notification-settings-modal.component';
import { AddFriendModalComponent } from './shared/layout/chat/add-friend-modal.component';
import { ChatBarComponent } from './shared/layout/chat/chat-bar.component';
import { ThemeSelectionPanelComponent } from './shared/layout/theme-selection/theme-selection-panel.component';
import { SessionTimeoutComponent } from './shared/common/session-timeout/session-timeout.component';
import { UpdateNotificationComponent } from './shared/components/update-notification.component';
@Component({
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.less'],
    imports: [
        NgClass,
        DefaultLayoutComponent,
        Theme2LayoutComponent,
        Theme3LayoutComponent,
        Theme4LayoutComponent,
        Theme5LayoutComponent,
        Theme6LayoutComponent,
        Theme7LayoutComponent,
        Theme8LayoutComponent,
        Theme9LayoutComponent,
        Theme10LayoutComponent,
        Theme11LayoutComponent,
        Theme12LayoutComponent,
        Theme13LayoutComponent,
        ScrollTopComponent_1,
        LinkedAccountsModalComponent_1,
        UserDelegationsModalComponent_1,
        ChangePasswordModalComponent_1,
        ChangeProfilePictureModalComponent_1,
        MySettingsModalComponent_1,
        NotificationSettingsModalComponent_1,
        AddFriendModalComponent,
        ChatBarComponent,
        ThemeSelectionPanelComponent,
        SessionTimeoutComponent,
        UpdateNotificationComponent,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class AppComponent extends AppComponentBase implements OnInit {
    private _chatSignalrService = inject(ChatSignalrService);
    private _userNotificationHelper = inject(UserNotificationHelper);
    private _dateTimeService = inject(DateTimeService);
    @ViewChild('linkedAccountsModal') linkedAccountsModal: LinkedAccountsModalComponent;
    @ViewChild('userDelegationsModal', { static: true }) userDelegationsModal: UserDelegationsModalComponent;
    @ViewChild('changePasswordModal', { static: true }) changePasswordModal: ChangePasswordModalComponent;
    @ViewChild('changeProfilePictureModal', { static: true })
    changeProfilePictureModal: ChangeProfilePictureModalComponent;
    @ViewChild('mySettingsModal', { static: true }) mySettingsModal: MySettingsModalComponent;
    @ViewChild('notificationSettingsModal', { static: true })
    notificationSettingsModal: NotificationSettingsModalComponent;
    @ViewChild('chatBarComponent') chatBarComponent;
    subscriptionStartType = SubscriptionStartType;
    theme: string;
    installationMode = true;
    isQuickThemeSelectEnabled: boolean = this.setting.getBoolean('App.UserManagement.IsQuickThemeSelectEnabled');
    IsSessionTimeOutEnabled: boolean =
        this.setting.getBoolean('App.UserManagement.SessionTimeOut.IsEnabled') && this.appSession.userId != null;
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this._userNotificationHelper.settingsModal = this.notificationSettingsModal;
        this.theme = abp.setting.get('App.UiManagement.Theme').toLocaleLowerCase();
        this.installationMode = UrlHelper.isInstallUrl(location.href);
        this.registerModalOpenEvents();
        if (this.appSession.application) {
            SignalRHelper.initSignalR(() => {
                this._chatSignalrService.init();
            });
        }
        this.pluginsInitialization();
        if (this.appSession.tenantId) {
            this.getSetting();
        }
    }
    pluginsInitialized(): boolean {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        const menuItems = document.querySelectorAll('[data-kt-menu="true"]');
        for (let i = 0; i < menuItems.length; i++) {
            const el = menuItems[i];
            const menuItem = el as HTMLElement;
            const menuInstance = MenuComponent.getInstance(menuItem);
            if (menuInstance) {
                return true;
            }
        }
        return false;
    }
    pluginsInitialization() {
        abp.event.on('app.dynamic-styles-loaded', () => {
            KTUtil.resize();
        });
        setTimeout(() => {
            if (this.pluginsInitialized()) {
                return;
            }
            ToggleComponent.bootstrap();
            ScrollTopComponent.bootstrap();
            DrawerComponent.bootstrap();
            StickyComponent.bootstrap();
            MenuComponent.bootstrap();
            ScrollComponent.bootstrap();
        }, 200);
    }
    subscriptionStatusBarVisible(): boolean {
        return (
            this.appSession.tenantId > 0 &&
            (this.appSession.tenant.isInTrialPeriod || this.subscriptionIsExpiringSoon())
        );
    }
    subscriptionIsExpiringSoon(): boolean {
        if (this.appSession.tenant?.subscriptionEndDateUtc) {
            const today = this._dateTimeService.getUTCDate();
            const daysFromNow = this._dateTimeService.plusDays(today, AppConsts.subscriptionExpireNootifyDayCount);
            return daysFromNow >= this.appSession.tenant.subscriptionEndDateUtc;
        }
        return false;
    }
    registerModalOpenEvents(): void {
        this.subscribeToEvent('app.show.linkedAccountsModal', () => {
            this.linkedAccountsModal.show();
        });
        this.subscribeToEvent('app.show.userDelegationsModal', () => {
            this.userDelegationsModal.show();
        });
        this.subscribeToEvent('app.show.changePasswordModal', () => {
            this.changePasswordModal.show();
        });
        this.subscribeToEvent('app.show.changeProfilePictureModal', () => {
            this.changeProfilePictureModal.show();
        });
        this.subscribeToEvent('app.show.mySettingsModal', () => {
            this.mySettingsModal.show();
        });
    }
    getRecentlyLinkedUsers(): void {
        abp.event.trigger('app.getRecentlyLinkedUsers');
    }
    onMySettingsModalSaved(): void {
        abp.event.trigger('app.onMySettingsModalSaved');
    }
}
