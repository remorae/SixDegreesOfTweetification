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
    }

    isLoggedIn = false;
    redirectUrl: string;
    private baseUrl: string;

    login(email: string, password: string): Observable<Object> {
        const info: Login = {
            Email: email,
            Password: password,
            RememberMe: false
        };
        return this.http
            .post(this.baseUrl + 'api/authentication/Login', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                this.isLoggedIn = true;
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
            .post(this.baseUrl + 'api/authentication/Register', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                this.isLoggedIn = true;
            });
    }

    registerExternal(email: string): Observable<Object> {
        const info: ExternalLogin = {
            Email: email
        };
        return this.http
            .post(this.baseUrl + 'api/authentication/ExternalLoginConfirmation', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                this.isLoggedIn = true;
            });
    }
}
