import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { Country } from '../models';
import { Subject } from 'rxjs/Subject';

export type QueryType =
    |'TweetsByHashtag'
    | 'LocationsByHashtag'
    | 'UserByScreenName'
    | 'UserConnectionsByScreenName';
@Injectable()
export class EndpointService {
    baseUrl: string;
    latestEndpoint: Subject<number[]> = new Subject<number[]>();
    constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    public searchTweets(hashtag: string): Observable<any[]> {
        // TODO: Change type to properly match final Endpoint Version
        return this.http.get<any[]>(
            this.baseUrl + 'api/search/tweets?query=' + hashtag
        );
    }

    public searchLocations(hashtag: string): Observable<Country[]> {
        return this.http.get<Country[]>(
            this.baseUrl + 'api/search/locations?query=' + hashtag
        );
    }

    public pushLatest(type: QueryType) {
        this.http
            .get<number[]>(this.baseUrl + 'api/ratelimit/status?=' + type)
            .subscribe(val => this.latestEndpoint.next(val));
    }
}
