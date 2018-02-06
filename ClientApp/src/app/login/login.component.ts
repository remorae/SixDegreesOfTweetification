import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
    username = '';
    password = '';
    message = '';
    constructor(
        private router: Router,
        private authService: AuthenticationService
    ) {}

    ngOnInit() {}

    login() {
        this.authService.login().subscribe(
            val => {
                console.log(val);
                if (this.authService.isLoggedIn) {
                    this.router.navigate(['home']);
                }
            },
            error => {
                this.message = 'Login failed due to' + JSON.stringify(error);
            }
        );
    }
}
