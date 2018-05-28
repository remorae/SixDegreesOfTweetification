import { Injectable, isDevMode } from '@angular/core';
import { AuthenticationService } from './authentication.service';
import {
    ActivatedRouteSnapshot,
    RouterStateSnapshot,
    CanActivate,
    Router
} from '@angular/router';
/**
 * @example Prevents the user from activating particular routes in the application unless they've already been logged in.
 */
@Injectable()
export class AuthGuard implements CanActivate {
    constructor(
        private authService: AuthenticationService,
        private router: Router
    ) {}
    /**
     * @example Verifies that the user is logged-in, and routes them appropriately. You'll notice that this should actually be
     *      two separate guards, based on the explicit url checks being done in CanActivate.
     * @returns Whether the user can or cannot be routed to the requested route.
     * @param route Unused
     * @param state Information about the route the user is attempting to activate
     */
    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean {
        const url: string = state.url;

        if (
            url === '/login' ||
            url === '/register' ||
            url === '/externallogin'
        ) {
            if (!this.authService.isLoggedIn()) {
                return true;
            }
            this.authService.redirectUrl = url;

            this.router.navigate(['/account']);
            return false;
        } else {
            if (this.authService.isLoggedIn()) {
                return true;
            }
            this.authService.redirectUrl = url;

            this.router.navigate(['/login']);
            return false;
        }
    }
}
