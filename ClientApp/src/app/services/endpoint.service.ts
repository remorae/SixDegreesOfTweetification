import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { Country } from '../models';
import { Subject } from 'rxjs/Subject';
import 'rxjs/add/operator/finally';
import { UserConnectionInfo, UserConnectionMap } from '../models/UserResult';
import { HashConnectionMap } from '../models/HashConnectionInfo';

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
        return this.http.get<HashConnectionMap>(this.baseUrl + 'api/search/degrees/hashtags?query=' + hashtag).finally(this.pushLatest);
    }

    public searchUserDegrees(user: string, numDegrees: number) {
        return this.http.get<UserConnectionMap>(this.baseUrl +
            'api/search/degrees/users/single?query='
            + user + '&numberOfDegrees=' + numDegrees).finally(this.pushLatest);
    }

    public getUserSixDegrees(user1: string, user2: string) {
        return this.http.get<any>(this.baseUrl + 'api/search/degrees/users?user1='
            + user1 + '&user2=' + user2).finally(this.pushLatest);
    }

    public getHashSixDegrees(hashtag1: string, hashtag2: string) {
        return this.http.get<any>(this.baseUrl + 'api/search/degrees/hashtags?hashtag1='
            + hashtag1 + '&hashtag2=' + hashtag2).finally(this.pushLatest);
    }
}
