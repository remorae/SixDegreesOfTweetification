import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { Router, NavigationEnd } from '@angular/router';
import { AlertService } from './alert.service';
import { filter } from 'rxjs/operators/filter';
import { map } from 'rxjs/operators/map';

export interface LoadingMap {
    [route: string]: boolean;
}
/**
 * @example Tracks whether a given route is waiting for data and should display a loading spinner.
 */
@Injectable()
export class LoaderService {
    loadingStatus: Subject<boolean> = new Subject<boolean>();
    currentRoute: string;
    loadMap: LoadingMap = {};
    constructor(private router: Router, private alerts: AlertService) {
        this.router.events
            .pipe(
                filter(event => event instanceof NavigationEnd),
                map((event: NavigationEnd) =>
                    event.urlAfterRedirects.split('/').join('')
                )
            )
            .subscribe(r => {
                this.currentRoute = r;
                const status = !!this.loadMap[r];
                this.loadingStatus.next(status);
            });
    }
    /**
     * @example Marks the current route as loading, and begins displaying the spinner.
     */
    startLoading(): void {
        this.loadMap[this.currentRoute] = true;
        this.loadingStatus.next(true);
    }
    /**
     * @example If the route waiting for data is the active one, turn off the spinner.
     *
     *          If it is not the active route, play an audio cue and show a success message on the currently active page.
     *
     * @param route The route that was active when an api call was made.
     */
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
    /**
     * @example Emits whether the current route should display a loading spinner or not.
     */
    getLoadingStatus(): Subject<boolean> {
        return this.loadingStatus;
    }
}
