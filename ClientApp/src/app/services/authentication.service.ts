import { Injectable, Inject } from '@angular/core';
import { CanActivate } from '@angular/router/src/interfaces';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/Observable/of';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/delay';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import 'rxjs/operators/map';


export interface Login {
    Email: string;
    Password: string;
    RememberMe: boolean;
}

export interface Registration {
    Email: string;
    Password: string;
    ConfirmPassword: string;
}

export interface ExternalLogin {
    Email: string;
}

@Injectable()
export class AuthenticationService {
    constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
        this.loggedIn = !!localStorage.getItem('auth_token');
    }

    private loggedIn: boolean = false;
    redirectUrl: string;
    private baseUrl: string;

    login(email: string, password: string): Observable<Object> {
        const info: Login = {
            Email: email,
            Password: password,
            RememberMe: false
        };
        return this.http
            .post(this.baseUrl + 'authentication/Login', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                localStorage.setItem('auth_token', JSON.parse(JSON.stringify(next)).auth_token);
                this.loggedIn = true;
            });
    }

    register(email: string, password: string, confirmPassword: string): Observable<Object> {
        if (password !== confirmPassword)
            throw new Error("Passwords do not match.");
        const info: Registration = {
                Email: email,
                Password: password,
                ConfirmPassword: confirmPassword
            };
        return this.http
            .post(this.baseUrl + 'authentication/Register', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                localStorage.setItem('auth_token', JSON.parse(JSON.stringify(next)).auth_token);
                this.loggedIn = true;
            });
    }

    registerExternal(email: string): Observable<Object> {
        const info: ExternalLogin = {
            Email: email
        };
        return this.http
            .post(this.baseUrl + 'authentication/ExternalLoginConfirmation', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                localStorage.setItem('auth_token', this.readCookie("SixDegrees.Identity"));
                this.loggedIn = true;
            });
    }

    private readCookie(key: string): string {
        let cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            let cookie = cookies[i].trim().split('=');
            if (cookie[0] === key)
                return cookie[1];
        }
    }

    logout() {
        localStorage.removeItem('auth_token');
        this.loggedIn = false;
    }

    isLoggedIn(): boolean {
        return this.loggedIn;
    }
}
