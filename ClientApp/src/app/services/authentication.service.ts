import { Injectable, Inject } from '@angular/core';
import { CanActivate } from '@angular/router/src/interfaces';
import { Observable } from 'rxjs/Observable';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { tap } from 'rxjs/operators/tap';

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

    private loggedIn = false;
    redirectUrl: string;
    private baseUrl: string;

    externalLoginUrl = 'account/ExternalLogin?provider=Twitter';
    linkLoginUrl = 'manage/LinkLogin?provider=Twitter';

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
            .pipe(
                tap(
                    next => {
                        localStorage.setItem('sixdegrees_auth', 'true');
                        this.loggedIn = true;
                    },
                    error => {
                        localStorage.removeItem('sixdegrees_auth');
                        this.loggedIn = false;
                    }
                )
            );
    }

    register(
        email: string,
        password: string,
        confirmPassword: string
    ): Observable<Object> {
        if (password !== confirmPassword) {
            throw new Error('Passwords do not match.');
        }
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
            .pipe(
                tap(next => {
                    localStorage.setItem('sixdegrees_auth', 'true');
                    this.loggedIn = true;
                })
            );
    }

    registerExternal(
        email: string,
        password: string,
        confirmPassword: string
    ): Observable<Object> {
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
            .pipe(
                tap(next => {
                    localStorage.setItem('sixdegrees_auth', 'true');
                    this.loggedIn = true;
                })
            );
    }

    logout(onCompletion: () => void): void {
        this.http
            .post(this.baseUrl + 'account/Logout', {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .subscribe(
                () => {
                    localStorage.removeItem('auth_token');
                    this.loggedIn = false;
                },
                error => {},
                onCompletion
            );
    }
    /**
     * @example Look at rewriting this method if you have issues logging in.
     * @returns Whether the user is logged in or not.
     */
    isLoggedIn(): boolean {
        this.getUpdatedLoginStatus().subscribe(res => {
            if (res) {
                localStorage.setItem('sixdegrees_auth', 'true');
                this.loggedIn = true;
            } else {
                localStorage.removeItem('sixdegrees_auth');
                this.loggedIn = false;
            }
        });
        return this.loggedIn;
    }

    getUpdatedLoginStatus(): Observable<boolean> {
        return this.http.post<boolean>(this.baseUrl + 'account/Authenticated', {
            headers: new HttpHeaders({
                'Content-Type': 'application/json; charset=utf-8'
            })
        });
    }

    hasTwitterLogin(): Observable<boolean> {
        return this.http.post<boolean>(
            this.baseUrl + 'account/TwitterAvailable',
            {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            }
        );
    }

    removeTwitter(): Observable<Object> {
        return this.http.post(
            this.baseUrl + 'manage/RemoveExternal?provider=Twitter',
            {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            }
        );
    }
}
