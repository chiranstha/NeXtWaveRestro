import { Component, Input, OnChanges, SimpleChanges, inject, ChangeDetectionStrategy } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { ProfileServiceProxy } from '@shared/service-proxies/service-proxies';
@Component({
    selector: 'friend-profile-picture',
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `
        <img [src]="profilePicture" alt="..." />
    `,
})
export class FriendProfilePictureComponent implements OnChanges {
    private _profileService = inject(ProfileServiceProxy);
    @Input() userId: number;
    @Input() tenantId: number;
    profilePicture = `${AppConsts.appBaseUrl}/assets/common/images/default-profile-picture.png`;
    private static profilePictureCache = new Map<string, string>();
    private static ongoingRequests = new Map<string, Promise<any>>();
    constructor() {}
    ngOnChanges(changes: SimpleChanges): void {
        if (changes.userId || changes.tenantId) {
            this.setProfileImage();
        }
    }
    private setProfileImage(): void {
        if (!this.userId) {
            return;
        }
        const cacheKey = `${this.userId}_${this.tenantId || 'null'}`;
        const cachedPicture = FriendProfilePictureComponent.profilePictureCache.get(cacheKey);
        if (cachedPicture) {
            this.profilePicture = cachedPicture;
            return;
        }
        // Check if there's already an ongoing request for this user
        const ongoingRequest = FriendProfilePictureComponent.ongoingRequests.get(cacheKey);
        if (ongoingRequest) {
            ongoingRequest.then((result) => {
                if (result?.profilePicture) {
                    const pictureUrl = `data:image/jpeg;base64,${result.profilePicture}`;
                    this.profilePicture = pictureUrl;
                    FriendProfilePictureComponent.profilePictureCache.set(cacheKey, pictureUrl);
                }
            });
            return;
        }
        if (!this.tenantId) {
            this.tenantId = undefined;
        }
        // Create and cache the ongoing request
        const requestPromise = this._profileService.getFriendProfilePicture(this.userId, this.tenantId).toPromise();
        FriendProfilePictureComponent.ongoingRequests.set(cacheKey, requestPromise);
        requestPromise
            .then((result) => {
                // Remove from ongoing requests
                FriendProfilePictureComponent.ongoingRequests.delete(cacheKey);
                if (result && result.profilePicture) {
                    const pictureUrl = `data:image/jpeg;base64,${result.profilePicture}`;
                    this.profilePicture = pictureUrl;
                    FriendProfilePictureComponent.profilePictureCache.set(cacheKey, pictureUrl);
                }
            })
            .catch(() => {
                // Remove from ongoing requests on error
                FriendProfilePictureComponent.ongoingRequests.delete(cacheKey);
            });
    }
}
