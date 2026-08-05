import { Injectable, inject } from '@angular/core';
import { TokenService, RefreshTokenService } from 'abp-ng2-module';
import { TokenAuthServiceProxy, RefreshTokenResult } from '@shared/service-proxies/service-proxies';
import { Observable, Subject, of } from 'rxjs';
import { AppConsts } from '@shared/AppConsts';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { SignalRHelper } from '@shared/helpers/SignalRHelper';
@Injectable({
    providedIn: 'root',
})
export class ZeroRefreshTokenService implements RefreshTokenService {
    private _tokenAuthService = inject(TokenAuthServiceProxy);
    private _tokenService = inject(TokenService);
    private _localStorageService = inject(LocalStorageService);
    constructor() {}
    tryAuthWithRefreshToken(): Observable<boolean> {
        const refreshTokenObservable = new Subject<boolean>();
        const token = this._tokenService.getRefreshToken();
        if (!token || token.trim() === '') {
            return of(false);
        }
        this._tokenAuthService.refreshToken(token).subscribe(
            (tokenResult: RefreshTokenResult) => {
                if (tokenResult?.accessToken) {
                    const tokenExpireDate = new Date(new Date().getTime() + 1000 * tokenResult.expireInSeconds);
                    this._tokenService.setToken(tokenResult.accessToken, tokenExpireDate);
                    SignalRHelper.updateQueryString(tokenResult.encryptedAccessToken);
                    this._localStorageService.setItem(
                        AppConsts.authorization.encrptedAuthTokenName,
                        {
                            token: tokenResult.encryptedAccessToken,
                            expireDate: tokenExpireDate,
                        },
                        () => refreshTokenObservable.next(true),
                    );
                } else {
                    refreshTokenObservable.next(false);
                }
            },
            () => {
                refreshTokenObservable.next(false);
            },
        );
        return refreshTokenObservable;
    }
}
