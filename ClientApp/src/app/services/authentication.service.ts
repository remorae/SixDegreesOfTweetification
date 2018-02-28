import { Injectable, Inject } from '@angular/core';
import { CanActivate } from '@angular/router/src/interfaces';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/Observable/of';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/delay';
import { Router } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import 'rxjs/operators/map';


export interface Creds {
    Email: string;
    Password: string;
    RememberMe: boolean;
}
@Injectable()
export class AuthenticationService {
    constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    isLoggedIn = false;
    redirectUrl: string;
    private baseUrl: string;
    login(user: string, pass: string): Observable<Object> {
        const creds: Creds = {
            Email: user,
            Password: pass,
            RememberMe: false
        };
        return this.http
            .post(this.baseUrl + 'api/Authentication/Login', creds, {
                headers: new HttpHeaders({
                    'Content-Type': 'application/json; charset=utf-8'
                })
            })
            .do(next => {
                this.isLoggedIn = true;
            });
    }
}
