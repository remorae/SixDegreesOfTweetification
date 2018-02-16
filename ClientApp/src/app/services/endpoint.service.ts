import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { CountryResult } from '../models';

@Injectable()
export class EndpointService {
    baseUrl: string;
    constructor(private http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    public searchTweets(hashtag: string): Observable<any> {
        // TODO: Change type to properly match final Endpoint Version
        return this.http.get<any[]>(
            this.baseUrl + 'api/search/tweets?query=' + hashtag
        );
    }

    public searchLocations(hashtag: string): Observable<any> {
        // TODO: Change type to properly match final Endpoint Version
        return this.http.get<CountryResult[]>(
            this.baseUrl + 'api/search/locations?query=' + hashtag
        );
    }
}
