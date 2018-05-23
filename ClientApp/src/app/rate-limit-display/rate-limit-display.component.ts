import { Component, OnInit, Query } from '@angular/core';
import {
    EndpointService,
    QueryType,
    RateInfo,
    AuthPair
} from '../services/endpoint.service';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators/filter';
import { map } from 'rxjs/operators/map';
/**
 * @example Component responsible for displaying the Twitter API rate limits that would be
 *          associated with a given page.
 */
@Component({
    selector: 'app-rate-limit-display',
    templateUrl: './rate-limit-display.component.html',
    styleUrls: ['./rate-limit-display.component.scss']
})
export class RateLimitDisplayComponent implements OnInit {
    allRates: RateInfo;
    currentComponentName: string;
    currentPagesRates: any[];
    constructor(private endpoint: EndpointService, private router: Router) {}
    /**
     * @example Queries the backend for all the Twitter API rate limits being tracked by the server.
     *
     *          Depending on the current route, displays a different slice of the rate limits in the nav-bar.
     *          It watches said changes via the Router events Observable.
     *
     *          Since the component is never reinstantiated, it listens to a Subject in the
     *          EndpointService that triggers every time an API call is made (except for its own rate limit
     *          requests. When the Subject triggers, new rate limits are requested.)
     */
    ngOnInit(): void {
        this.endpoint.getAllRateLimits().subscribe(rates => {
            this.translateRates(rates);
            this.displayCurrentRates(this.currentComponentName);
        });
        this.router.events
            .pipe(
                filter(event => event instanceof NavigationEnd),
                map((event: NavigationEnd) =>
                    event.urlAfterRedirects.split('/').join('')
                )
            )
            .subscribe((componentName: string) => {
                this.currentComponentName = componentName;
                this.displayCurrentRates(componentName);
            });

        this.endpoint.watchEndPoints().subscribe(() => {
            this.endpoint.getAllRateLimits().subscribe(rates => {
                this.translateRates(rates);
                this.displayCurrentRates(this.currentComponentName);
            });
        });
    }
    /**
     *  @example Maps a route fragment to an array of names associated with a given API endpoint.
     * @param componentName The route fragment associated with a given component.
     * @returns The associated list of Endpoint names for a component.
     */
    public mapComponentToQueryType(componentName: string): QueryType[] {
        switch (componentName) {
            case 'geo':
                return [QueryType.LocationsByHashtag]; // TODO: update when adding components
            case 'word-cloud':
                return [QueryType.HashtagsFromHashtag];
            case 'user-to-user':
                return [
                    QueryType.UserConnectionsByScreenName,
                    QueryType.UserByScreenName
                ];
            case 'hash-to-hash':
                return [QueryType.HashtagConnectionsByHashtag];
            default:
                return null;
        }
    }
    /**
     *  @examples Converts the returned QueryTypes into a Title Case display
     * @param componentName The route fragment corresponding to the currently active component.
     */
    public displayCurrentRates(componentName: string): void {
        const types: QueryType[] = this.mapComponentToQueryType(componentName);

        if (types && this.allRates) {
            this.currentPagesRates = types.map(type => {
                const rateName = type.replace(/([A-Z])/g, ' $1').substring(1);
                return { name: rateName, auths: this.allRates[type] };
            });
        } else {
            this.currentPagesRates = [];
        }
    }

    public translateRates(rates: RateInfo) {
        this.allRates = rates;
    }
}
