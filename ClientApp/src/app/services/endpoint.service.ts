import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { Country } from '../models';
import { Subject } from 'rxjs/Subject';
import 'rxjs/add/operator/finally';

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

@Injectable()
export class EndpointService {
    baseUrl: string;
    private hitBackend: Subject<any> = new Subject<any>();
    pushLatest = () => {
        this.hitBackend.next();
    }

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
            .finally(this.pushLatest);
    }

    public searchLocations(hashtag: string): Observable<Country[]> {
        return this.http
            .get<Country[]>(
                this.baseUrl + 'api/search/locations?query=' + hashtag
            )
            .finally(this.pushLatest);
    }

    public getSelectedRateLimit(type: QueryType) {
        return this.http.get<AuthPair>(
            this.baseUrl + 'api/ratelimit/status?=' + type + '&forceupdate=true'
        );
    }

    public searchRelatedHashtags(hashtag: string): Observable<string[]> {
        return this.http.get<string[]>(this.baseUrl + 'api/search/hashtags?query=' + hashtag).finally(this.pushLatest);
    }

    public searchHashDegrees(hashtag: string) {
        return this.http.get(this.baseUrl + 'api/search/degrees/hashtags?query=' + hashtag).finally(this.pushLatest);
    }

    public searchUserDegrees(user: string) {
        return this.http.get(this.baseUrl + 'api/search/degrees/users?query=' + user).finally(this.pushLatest);
    }
}
