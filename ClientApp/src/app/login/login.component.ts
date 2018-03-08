import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';

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
    ) {}

    ngOnInit() {}

    login() {
        this.authService.login(this.email, this.password).subscribe(
            val => {
                if (this.authService.isLoggedIn) {
                    this.router.navigate(['home']);
                } else {
                    this.message = 'Login failed due to improper credentials.';
                }
            },
            error => {
                this.message = JSON.stringify(error.error).replace(new RegExp(';', 'g'), '\n').replace(new RegExp('"', 'g'), '');
            }
        );
    }

    navToRegister() {
        this.router.navigate(['register']);
    }

    loginWithTwitter() {
        location.href = 'api/authentication/ExternalLogin?provider=Twitter';
    }
}
