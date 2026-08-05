import { Component, Input, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { ChatMessageDto, ChatServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppConsts } from 'shared/AppConsts';
import { LocalStorageService } from '@shared/utils/local-storage.service';
@Component({
    selector: 'chat-message',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './chat-message.component.html',
})
export class ChatMessageComponent implements OnInit {
    private _chatService = inject(ChatServiceProxy);
    private _localStorageService = inject(LocalStorageService);
    @Input()
    message: ChatMessageDto;
    chatMessage: string;
    chatMessageType: string;
    fileName: string;
    fileContentType: string;
    constructor() {}
    ngOnInit(): void {
        this.setChatMessageType();
    }
    private setChatMessageType(): void {
        const self = this;
        this._localStorageService.getItem(AppConsts.authorization.encrptedAuthTokenName, (err, value) => {
            const encryptedAuthToken = value?.token;
            if (self.message.message.startsWith('[image]')) {
                self.chatMessageType = 'image';
                const image = JSON.parse(self.message.message.substring('[image]'.length));
                self.chatMessage = `${AppConsts.remoteServiceBaseUrl}/Chat/GetUploadedObject?fileId=${
                    image.id
                }&fileName=${image.name}&contentType=${image.contentType}&${
                    AppConsts.authorization.encrptedAuthTokenName
                }=${encodeURIComponent(encryptedAuthToken)}`;
            } else if (self.message.message.startsWith('[file]')) {
                self.chatMessageType = 'file';
                const file = JSON.parse(self.message.message.substring('[file]'.length));
                self.chatMessage = `${AppConsts.remoteServiceBaseUrl}/Chat/GetUploadedObject?fileId=${
                    file.id
                }&fileName=${file.name}&contentType=${file.contentType}&${
                    AppConsts.authorization.encrptedAuthTokenName
                }=${encodeURIComponent(encryptedAuthToken)}`;
                self.fileName = file.name;
            } else if (self.message.message.startsWith('[link]')) {
                self.chatMessageType = 'link';
                const linkMessage = JSON.parse(self.message.message.substring('[link]'.length));
                self.chatMessage = linkMessage.message == null ? '' : linkMessage.message;
            } else {
                self.chatMessageType = 'text';
                self.chatMessage = self.message.message;
            }
        });
    }
}
