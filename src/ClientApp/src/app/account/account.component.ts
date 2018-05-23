import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';
/**
 * @example Provides the UI for:
 *      Logging a user out.
 *      Adding a Twitter Account authorization to an existing user
 *      Removing a Twitter Account authorization for an existing user.
 */
@Component({
    selector: 'app-account',
    templateUrl: './account.component.html',
    styleUrls: ['./account.component.scss']
})
export class AccountComponent implements OnInit {
    constructor(
        private router: Router,
        private authService: AuthenticationService
    ) {
        this.authService.hasTwitterLogin().subscribe(res => {
            this.hasTwitterLogin = res;
            this.canRemoveTwitter = this.hasTwitterLogin;
            this.canAddTwitter = !this.hasTwitterLogin;
        });
    }

    canAddTwitter = false;
    canRemoveTwitter = false;
    private hasTwitterLogin = false;

    ngOnInit() {}
    /**
     * @example Logs the user out on the server and in the AuthService, and removes the authorization token
     *  from localStorage before returning them to the login page.
     */
    logout(): void {
        this.authService.logout(() => {
            this.router.navigate(['login']);
        });
    }
    /**
     *  @example Redirects the user to Twitter, where they can log in with an existing account before being redirected back to the app.
     */
    addTwitter(): void {
        location.href = this.authService.linkLoginUrl;
    }

    /**
     * @example Removes the Twitter authorization attached to this account.
     */
    removeTwitter(): void {
        this.authService.removeTwitter().subscribe(res => {
            this.hasTwitterLogin = false;
            this.canRemoveTwitter = false;
            this.canAddTwitter = true;
        });
    }
}
