import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { empty } from 'rxjs/observable/empty';
import { Country } from '../models';
import { Subject } from 'rxjs/Subject';
import { catchError } from 'rxjs/operators/catchError';
import { finalize } from 'rxjs/operators/finalize';
import {
    UserConnectionInfo,
    UserConnectionMap,
    UserResult
} from '../models/UserResult';
import { HashConnectionMap } from '../models/HashConnectionInfo';
import { SixDegreesConnection } from './graph-data.service';
import { AlertService } from './alert.service';
import { LoaderService } from './loader.service';
import { Router, NavigationEnd } from '@angular/router';
import { map } from 'rxjs/operators/map';
import { filter } from 'rxjs/operators/filter';

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
/**
 * @example Handles data-fetching requests to the server.
 */
@Injectable()
export class EndpointService {
    baseUrl: string;
    route: string;
    private hitBackend: Subject<any> = new Subject<any>();
    pushLatest = () => {
        this.hitBackend.next();
    };

    constructor(
        private http: HttpClient,
        @Inject('BASE_URL') baseUrl: string,
        private alerts: AlertService,
        private loader: LoaderService,
        private router: Router
    ) {
        this.baseUrl = baseUrl;
        this.router.events
            .pipe(
                filter(event => event instanceof NavigationEnd),
                map((event: NavigationEnd) =>
                    event.urlAfterRedirects.split('/').join('')
                )
            )
            .subscribe(r => {
                this.route = r;
            });
    }

    /**
     * @example Returns a Subject that can be subscribed to in order to watch for API calls to the server.
     */
    public watchEndPoints(): Subject<any> {
        return this.hitBackend;
    }
    /**
     * @example Tells the LoaderService that it should display the loading icon.
     */
    public showLoader(): void {
        this.loader.startLoading();
    }

    /**
     * @example Queries the server at the given url, and whether that query is successful or has an error:
     *          Hides the loading spinner for that route,
     *          alerts any watchEndPoints() subscribers that an API call was made.
     *
     *          On an error, it will display an error message using the alert Service, and return an Observable that emits no items.
     * @param urlChunks The pieces of the url that will be queried.
     */
    callAPI<T>(urlChunks: string[]): Observable<T> {
        const currentRoute: string = this.route;
        this.showLoader();
        return this.http.get<T>(`${this.baseUrl}${urlChunks.join('')}`).pipe(
            finalize(() => {
                this.loader.endLoading(currentRoute);
            }),
            finalize(this.pushLatest),
            catchError((err: HttpErrorResponse, caught: Observable<T>) => {
                this.alerts.addError(err.error);
                return empty<T>();
            })
        );
    }

    /**
     * @example Queries the server for the latest Twitter rate limits.
     */
    public getAllRateLimits() {
        return this.http.get<RateInfo>(this.baseUrl + 'api/ratelimit/all'); // can't use pushLatest
    }

    /**
     * @example Queries for locations associated with a hashtag.
     * @param hashtag
     */
    public searchLocations(hashtag: string): Observable<Country[]> {
        return this.callAPI<LocationData>([
            'api/search/locations?query=',
            hashtag
        ]).pipe(map(val => val.countries));
    }
    /**
     *  @example Queries for hashtags associated with a hashtag.
     * @param hashtag
     */
    public searchRelatedHashtags(hashtag: string): Observable<string[]> {
        return this.callAPI<string[]>(['api/search/hashtags?query=', hashtag]);
    }

    /**
     *  @example Queries for a connected graph of users.
     * @param user1
     * @param user2
     */
    public getUserSixDegrees(
        user1: string,
        user2: string
    ): Observable<SixDegreesConnection<UserResult>> {
        return this.callAPI<SixDegreesConnection<UserResult>>([
            'api/search/degrees/users?user1=',
            user1,
            '&user2=',
            user2
        ]);
    }
    /**
     * @example Queries for a connected graph of hashtags.
     * @param hashtag1
     * @param hashtag2
     */
    public getHashSixDegrees(
        hashtag1: string,
        hashtag2: string
    ): Observable<SixDegreesConnection<string>> {
        return this.callAPI<SixDegreesConnection<string>>([
            'api/search/degrees/hashtags?hashtag1=',
            hashtag1,
            '&hashtag2=',
            hashtag2
        ]);
    }

    /**
     * @example Queries for an individual user's associated user data from Twitter.
     * @param id A Twitter user's id
     */
    public getUserInfo(id: string) {
        return this.callAPI<UserResult>(['api/search/userID?user_id=', id]);
    }
}
