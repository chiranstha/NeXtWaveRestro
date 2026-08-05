import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    SubscriptionPaymentType,
    SubscriptionStartType,
    TenantLoginInfoDto,
} from '@shared/service-proxies/service-proxies';
@Component({ changeDetection: ChangeDetectionStrategy.Eager, template: '' })
export class ThemesLayoutBaseComponent extends AppComponentBase {
    private _dateTimeService = inject(DateTimeService);
    tenant: TenantLoginInfoDto = new TenantLoginInfoDto();
    subscriptionStartType = SubscriptionStartType;
    installationMode = true;
    defaultLogo = `${AppConsts.appBaseUrl}/assets/common/images/app-logo-on-${
        this.currentTheme.baseSettings.menu.asideSkin
    }.svg`;

    subscriptionStatusBarVisible(): boolean {
        return (
            this.appSession.tenantId > 0 &&
            this.appSession.tenant.subscriptionPaymentType !== SubscriptionPaymentType.RecurringAutomatic &&
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
    getSubscriptionExpiringDayCount(): number {
        if (!this.appSession.tenant.subscriptionEndDateUtc) {
            return 0;
        }
        const todayUTC = this._dateTimeService.getUTCDate();
        const duration = this.appSession.tenant.subscriptionEndDateUtc.diff(todayUTC, 'days');
        return Math.round(duration.days);
    }
    getTrialSubscriptionNotification(): string {
        return this.l(
            'TrialSubscriptionNotification',
            `<strong>${this.appSession.tenant.edition.displayName}</strong>`,
            `<a href="/app/admin/subscription-management">${this.l('ClickHere')}</a>`,
        );
    }
    getExpireNotification(localizationKey: string): string {
        return this.l(localizationKey, this.getSubscriptionExpiringDayCount());
    }
    isMobileDevice(): boolean {
        return KTUtil.isMobileDevice();
    }
    isDarkModeActive(): boolean {
        return this.appSession.theme.baseSettings.layout.darkMode;
    }
    getSkin(): string {
        return this.isDarkModeActive() ? 'dark' : 'light';
    }
}
