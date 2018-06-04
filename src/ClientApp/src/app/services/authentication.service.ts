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
/**
 * @example Handles the API calls for various flavors of user authentication.
 */
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
    /**
     * @example Logs the user in and sets the authentication item in localStorage.
     * @param email The user's email
     * @param password The user's password.
     */
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
    /**
     * @example Registers the user with no external login credentials.
     * @param email the user's email
     * @param password the user's password
     * @param confirmPassword  the user's password, reentered as confirmation.
     */
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
    /**
     * @example Registers the user with external login credentials.
     * @param email the user's email
     * @param password the user's password
     * @param confirmPassword  the user's password, reentered as confirmation.
     */
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
    /**
     * @example Logs the user out and removes the authentication token the backend is looking for.
     * @param onCompletion A callback that will be invoked on the successful completition of the post to the backend.
     */
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

    /**
     * @example Verifies whether the user is authenticated or not.
     */
    getUpdatedLoginStatus(): Observable<boolean> {
        return this.http.post<boolean>(this.baseUrl + 'account/Authenticated', {
            headers: new HttpHeaders({
                'Content-Type': 'application/json; charset=utf-8'
            })
        });
    }
    /**
     * @example Checks whether the user has attached Twitter credentials.
     */
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
    /**
     * @example Removes the associated Twitter credentials for the currently logged-in user.
     */
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
