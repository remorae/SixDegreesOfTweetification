import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';

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

    register() {
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
