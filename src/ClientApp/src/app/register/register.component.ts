import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';
/**
 * @example Displays the UI for registering a user after they have logged in.
 */
@Component({
    selector: 'app-register',
    templateUrl: './register.component.html',
    styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
    email = '';
    password = '';
    confirmPassword = '';
    message = '';
    constructor(
        private router: Router,
        private authService: AuthenticationService
    ) {}

    ngOnInit(): void {}

    /**
     * @example Registers the email and password with the backend, then navigates
     *          to the homepage on success.
     */
    register(): void {
        this.authService
            .register(this.email, this.password, this.confirmPassword)
            .subscribe(
                val => {
                    this.router.navigate(['home']);
                },
                error => {
                    this.message = error
                        .replace(new RegExp(';', 'g'), '\n')
                        .replace(new RegExp('"', 'g'), '');
                }
            );
    }

    navToLogin() {
        this.router.navigate(['login']);
    }
}
