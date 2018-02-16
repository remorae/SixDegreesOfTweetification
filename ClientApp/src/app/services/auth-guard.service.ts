import { Injectable, isDevMode } from '@angular/core';
import { AuthenticationService } from './authentication.service';
import {
    ActivatedRouteSnapshot,
    RouterStateSnapshot,
    CanActivate,
    Router
} from '@angular/router';

@Injectable()
export class AuthGuard implements CanActivate {
    constructor(
        private authService: AuthenticationService,
        private router: Router
    ) {}
    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
        const url: string = state.url;

        if (isDevMode()) {
            return true;
        }
        if (this.authService.isLoggedIn) {
            return true;
        }
        this.authService.redirectUrl = url;

        this.router.navigate(['/login']);
        return false;
    }
}
