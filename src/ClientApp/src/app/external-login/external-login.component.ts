import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';
/**
 * @example Provides the UI for registering the user with an external source of authentication. Twitter, in this case.
 */
@Component({
    selector: 'app-external-login',
    templateUrl: './external-login.component.html',
    styleUrls: ['./external-login.component.scss']
})
export class ExternalLoginComponent implements OnInit {
    email = '';
    password = '';
    confirmPassword = '';
    message = '';
    constructor(
        private router: Router,
        private authService: AuthenticationService
    ) {}

    ngOnInit() {}

    register(): void {
        this.authService
            .registerExternal(this.email, this.password, this.confirmPassword)
            .subscribe(
                val => {
                    this.router.navigate(['home']);
                },
                error => {
                    this.message = JSON.stringify(error.error)
                        .replace(new RegExp(';', 'g'), '\n')
                        .replace(new RegExp('"', 'g'), '');
                }
            );
    }
}
