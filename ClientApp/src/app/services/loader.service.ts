import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { Router, NavigationEnd } from '@angular/router';
import { AlertService } from './alert.service';

export interface LoadingMap {
    [route: string]: boolean;
}
@Injectable()
export class LoaderService {
    loadingStatus: Subject<boolean> = new Subject<boolean>();
    currentRoute: string;
    loadMap: LoadingMap = {};
    constructor(private router: Router, private alerts: AlertService) {
        this.router.events
            .filter(event => event instanceof NavigationEnd)
            .map((event: NavigationEnd) =>
                event.urlAfterRedirects.split('/').join('')
            )
            .subscribe(r => {
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
        } else {
            const cue = new Audio('../../assets/SoundEffects/LoadingCue.m4a');
            cue.load();
            cue.play();
            this.alerts.addLoadingFinishedMessage(route);
        }
    }

    getLoadingStatus(): Subject<boolean> {
        return this.loadingStatus;
    }
}
