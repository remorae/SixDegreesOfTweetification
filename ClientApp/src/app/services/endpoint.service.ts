import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/empty';
import { Country } from '../models';
import { Subject } from 'rxjs/Subject';
import 'rxjs/add/operator/finally';
import 'rxjs/add/operator/catch';
import { UserConnectionInfo, UserConnectionMap, UserResult } from '../models/UserResult';
import { HashConnectionMap } from '../models/HashConnectionInfo';
import { SixDegreesConnection } from './graph-data.service';
import { AlertService } from './alert.service';
import { LoaderService } from './loader.service';
import { Router, NavigationEnd } from '@angular/router';

export enum QueryType {
    TweetsByHashtag = 'TweetsByHashtag',
    LocationsByHashtag = 'LocationsByHashtag',
    UserByScreenName = 'UserByScreenName',
    UserConnectionsByScreenName = 'UserConnectionsByScreenName',
    HashtagsFromHashtag = 'HashtagsFromHashtag',
    HashtagConnectionsByHashtag = 'HashtagConnectionsByHashtag'
}
export class AuthPair {
    Application: number;
    User: number;
}
export class RateInfo {
    TweetsByHashtag: AuthPair;
    LocationsByHashtag: AuthPair;
    UserByScreenName: AuthPair;
    UserConnectionsByScreenName: AuthPair;
}

export class LocationData {
    countries: Country[];
    coordinateInfo: CoordinateInfo[];
}

export class CoordinateInfo {
    coordinates: GeoCoordinates;
    hashtags: string[];
    sources: string[];
}

export class GeoCoordinates {
    type: string;
    x: number;
    y: number;
}

@Injectable()
export class EndpointService {
    baseUrl: string;
    route: string;
    private hitBackend: Subject<any> = new Subject<any>();
    pushLatest = () => {
        this.hitBackend.next();
    }

    constructor(private http: HttpClient,
        @Inject('BASE_URL') baseUrl: string,
        private alerts: AlertService,
        private loader: LoaderService,
        private router: Router) {
        this.baseUrl = baseUrl;
        this.router.events
            .filter(event => event instanceof NavigationEnd)
            .map((event: NavigationEnd) =>
                event.urlAfterRedirects.split('/').join('')
            )
            .subscribe((r) => {
                this.route = r;
            });
    }

    public watchEndPoints() {
        return this.hitBackend;
    }

    public showLoader() {
        this.loader.startLoading();
    }

    // Purpose

    // Abstract

    // Why it interests you

    //How it would benefit people

    //Algorithm


    callAPI<T>(urlChunks: string[]) {
        const currentRoute: string = this.route;
        this.showLoader();
        return this.http.get<T>(`${this.baseUrl}${urlChunks.join('')}`)
            .finally(() => {
                this.loader.endLoading(currentRoute);
            })
            .finally(this.pushLatest)
            .catch((err: HttpErrorResponse, caught: Observable<T>) => {
                this.alerts.addError(err.error);
                return Observable.empty<T>();
            });
    }
    public getAllRateLimits() {
        return this.http.get<RateInfo>(this.baseUrl + 'api/ratelimit/all'); // can't use pushLatest
    }

    public searchLocations(hashtag: string): Observable<Country[]> {

        return this.callAPI<LocationData>(['api/search/locations?query=', hashtag]).map(val => val.countries);
    }

    public searchRelatedHashtags(hashtag: string): Observable<string[]> {

        return this.callAPI<string[]>(['api/search/hashtags?query=', hashtag]);
    }


    public getUserSixDegrees(user1: string, user2: string) {
        return this.callAPI<SixDegreesConnection<UserResult>>(['api/search/degrees/users?user1=', user1, '&user2=', user2]);

    }

    public getHashSixDegrees(hashtag1: string, hashtag2: string) {
        return this.callAPI<SixDegreesConnection<string>>(['api/search/degrees/hashtags?hashtag1=', hashtag1, '&hashtag2=', hashtag2]);
    }
}
