import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { Router, NavigationEnd } from '@angular/router';

export interface LoadingMap {
    [route: string]: boolean;
}
@Injectable()
export class LoaderService {

    loadingStatus: Subject<boolean> = new Subject<boolean>();
    currentRoute: string;
    loadMap: LoadingMap = {};
    constructor(private router: Router) {
        this.router.events
            .filter(event => event instanceof NavigationEnd)
            .map((event: NavigationEnd) => event.urlAfterRedirects.split('/').join(''))
            .subscribe((r) => {
                this.currentRoute = r;
                const status = !!this.loadMap[r];
                this.loadingStatus.next(status);
            });
    }

    startLoading() {
        this.loadMap[this.currentRoute] = true;
        this.loadingStatus.next(true);
    }

    endLoading(route: string) {
        this.loadMap[route] = false;
        if (this.currentRoute === route) {
            this.loadingStatus.next(false);
        }
        //TODO: Fix this killing an active loader when another returns;
    }

    getLoadingStatus(): Subject<boolean> {
        return this.loadingStatus;
    }

}
