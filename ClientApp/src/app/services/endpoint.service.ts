import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { Country } from '../models';
import { Subject } from 'rxjs/Subject';

export enum QueryType {
    'TweetsByHashtag',
    'LocationsByHashtag',
    'UserByScreenName',
    'UserConnectionsByScreenName'
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

@Injectable()
export class EndpointService {
    baseUrl: string;
    private hitBackend: Subject<any> = new Subject<any>();
    pushLatest = () => {
        this.hitBackend.next();
    };

    constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    public watchEndPoints() {
        return this.hitBackend;
    }
    public getAllRateLimits() {
        return this.http.get<RateInfo>(this.baseUrl + 'api/ratelimit/all');
    }
    public searchTweets(hashtag: string): Observable<any[]> {
        // TODO: Change type to properly match final Endpoint Version
        return this.http
            .get<any[]>(this.baseUrl + 'api/search/tweets?query=' + hashtag)
            .do(this.pushLatest);
    }

    public searchLocations(hashtag: string): Observable<Country[]> {
        return this.http
            .get<Country[]>(
                this.baseUrl + 'api/search/locations?query=' + hashtag
            )
            .do(this.pushLatest);
    }

    public getSelectedRateLimit(type: QueryType) {
        return this.http.get<AuthPair>(
            this.baseUrl + 'api/ratelimit/status?=' + type + '&forceupdate=true'
        );
    }
}
