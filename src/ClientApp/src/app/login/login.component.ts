import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';
/**
 * @example Provides the UI for the initial login page of the app.
 */
@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
    email = '';
    password = '';
    message = '';

    constructor(
        private router: Router,
        private authService: AuthenticationService
    ) {
        this.authService.getUpdatedLoginStatus().subscribe(res => {
            if (res) {
                this.router.navigate(['home']);
            }
        });
    }

    ngOnInit() {}
    /**
     * @example Logs the user in if they are registered on the server, and navigates them
     *          to the home page if they are.
     */
    login() {
        this.authService.login(this.email, this.password).subscribe(
            val => {
                if (this.authService.isLoggedIn()) {
                    this.router.navigate(['home']);
                } else {
                    this.message = 'Login failed due to improper credentials.';
                }
            },
            error => {
                this.message = JSON.stringify(error.error)
                    .replace(new RegExp(';', 'g'), '\n')
                    .replace(new RegExp('"', 'g'), '');
            }
        );
    }

    navToRegister() {
        this.router.navigate(['register']);
    }

    loginWithTwitter() {
        location.href = this.authService.externalLoginUrl;
    }
}
