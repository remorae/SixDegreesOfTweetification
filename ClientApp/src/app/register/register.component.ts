import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';

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
    ) { }

    ngOnInit() { }

    register() {
        this.authService.register(this.email, this.password, this.confirmPassword).subscribe(
            val => {
                this.router.navigate(['home']);
            },
            error => {
                this.message = error.error;
            }
        );
    }

    navToLogin() {
        this.router.navigate(['login']);
    }
}
