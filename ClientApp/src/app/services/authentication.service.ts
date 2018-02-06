import { Injectable } from '@angular/core';
import { CanActivate } from '@angular/router/src/interfaces';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/Observable/of';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/delay';
import { Router } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

@Injectable()
export class AuthenticationService {
    constructor() {}

    isLoggedIn = false;
    redirectUrl: string;
    login(): Observable<boolean> {
        // this does nothing but make the user wait and log them in.
        return Observable.of(true)
            .delay(1000)
            .do(val => (this.isLoggedIn = true));
    }
}
