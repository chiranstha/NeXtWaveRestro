import { ChangeDetectorRef, Component, Input, NgZone, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import { AbpSessionService } from 'abp-ng2-module';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { TooltipDirective } from 'ngx-bootstrap/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { FeatureCheckerPipe } from '@shared/common/pipes/feature-checker.pipe';
@Component({
    selector: 'chat-toggle-button',
    templateUrl: './chat-toggle-button.component.html',
    imports: [TooltipDirective, LocalizePipe, FeatureCheckerPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class ChatToggleButtonComponent extends ThemesLayoutBaseComponent implements OnInit {
    private _abpSessionService = inject(AbpSessionService);
    private cd = inject(ChangeDetectorRef);
    private ngZone = inject(NgZone);
    @Input() customStyle =
        'btn btn-active-color-primary btn-active-light btn-custom btn-icon btn-icon-muted h-35px h-md-40px position-relative w-35px w-md-40px';
    @Input() iconStyle = 'fa-duotone fa-regular fa-message-dots fs-4';
    unreadChatMessageCount: number = 0;
    chatConnected = false;
    isHost = false;
    public constructor() {
        const _dateTimeService = inject(DateTimeService);
        super();
    }
    ngOnInit(): void {
        this.registerToEvents();
        this.isHost = !this._abpSessionService.tenantId;
    }
    registerToEvents() {
        this.subscribeToEvent('app.chat.unreadMessageCountChanged', (messageCount) => {
            this.ngZone.run(() => {
                this.unreadChatMessageCount = parseInt(messageCount as string);
                this.cd.markForCheck();
            });
        });
        this.subscribeToEvent('app.chat.connected', () => {
            this.ngZone.run(() => {
                this.chatConnected = true;
                this.cd.markForCheck();
            });
        });
    }
}
