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
    Password: string;
    ConfirmPassword: string;
}

@Injectable()
export class AuthenticationService {
    constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
        this.loggedIn = !!localStorage.getItem('sixdegrees_auth');
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
            .post(this.baseUrl + 'account/Login', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                localStorage.setItem('sixdegrees_auth', 'true');
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
            .post(this.baseUrl + 'account/Register', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                localStorage.setItem('sixdegrees_auth', 'true');
                this.loggedIn = true;
            });
    }

    registerExternal(email: string, password: string, confirmPassword: string): Observable<Object> {
        const info: ExternalLogin = {
            Email: email,
            Password: password,
            ConfirmPassword: confirmPassword
        };
        return this.http
            .post(this.baseUrl + 'account/ExternalLoginConfirmation', info, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                localStorage.setItem('sixdegrees_auth', 'true');
                this.loggedIn = true;
            });
    }

    logout(onCompletion: () => void ): void {
        this.http.post(this.baseUrl + 'account/Logout', {
            headers: new HttpHeaders({
                'Content-Type': 'application/json; charset=utf-8'
            })
        }).subscribe(res => {
            localStorage.removeItem('auth_token');
            this.loggedIn = false;

        }, (error=>{}), onCompletion);
    }

    isLoggedIn(): boolean {
        this.updateLoginStatus()
        .subscribe(res => {
            if (res) {
                localStorage.setItem('sixdegrees_auth', 'true');
                this.loggedIn = true;
            }
        });
        return this.loggedIn;
    }

    updateLoginStatus(): Observable<boolean> {
        return this.http
            .post<boolean>(this.baseUrl + 'account/Authenticated', {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
    }

    hasTwitterLogin(): Observable<boolean> {
        return this.http.post<boolean>(this.baseUrl + 'account/TwitterAvailable', {
            headers: new HttpHeaders({
                'Content-Type': 'application/json; charset=utf-8'
            })
        });
    }

    removeTwitter(): Observable<Object> {
        return this.http.post(this.baseUrl + 'manage/RemoveExternal?provider=Twitter', {
            headers: new HttpHeaders({
                'Content-Type': 'application/json; charset=utf-8'
            })
        });
    }

    externalLoginUrl: string = 'account/ExternalLogin?provider=Twitter';
    linkLoginUrl: string = 'manage/LinkLogin?provider=Twitter';
}
