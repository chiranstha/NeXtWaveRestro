import { Pipe, PipeTransform, inject } from '@angular/core';
import { PermissionCheckerService } from 'abp-ng2-module';
@Pipe({
    name: 'permissionAll',
    standalone: true,
})
export class PermissionAllPipe implements PipeTransform {
    permission = inject(PermissionCheckerService);
    transform(arrPermissions: string[]): boolean {
        if (!arrPermissions) {
            return false;
        }
        for (const permission of arrPermissions) {
            if (!this.permission.isGranted(permission)) {
                return false;
            }
        }
        return true; //all permissions are granted
    }
}
