import { Component, OnInit, Query } from '@angular/core';
import {
    EndpointService,
    QueryType,
    RateInfo,
    AuthPair
} from '../services/endpoint.service';
import { Router, NavigationEnd } from '@angular/router';
import 'rxjs/add/operator/filter';
import 'rxjs/add/operator/map';

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
    ngOnInit() {
        this.endpoint.getAllRateLimits().subscribe(rates => {
            this.translateRates(rates);
            this.displayCurrentRates(this.currentComponentName);
        });
        this.router.events
            .filter(event => event instanceof NavigationEnd)
            .map((event: NavigationEnd) =>
                event.urlAfterRedirects.split('/').join('')
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

    public mapComponentToQueryType(componentName: string): QueryType[] {
        switch (componentName) {
            case 'geo':
                return [QueryType.LocationsByHashtag]; // TODO: update when adding components
            case 'word-cloud':
            return [QueryType.HashtagsFromHashtag];
            case 'user-to-user':
            return [QueryType.UserConnectionsByScreenName];
            case 'hash-to-hash':
            return [QueryType.HashtagConnectionsByHashtag];
            default:
                return null;
        }
    }

    public displayCurrentRates(componentName: string) {
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
