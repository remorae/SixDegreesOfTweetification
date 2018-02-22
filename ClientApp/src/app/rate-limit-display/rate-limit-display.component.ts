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
        this.router.events
            .filter(event => event instanceof NavigationEnd)
            .map((event: NavigationEnd) =>
                event.urlAfterRedirects.split('/').join('')
            )
            .subscribe((componentName: string) => {
                this.currentComponentName = componentName;
                this.requestRelatedRates(componentName);
            });

        this.endpoint.getAllRateLimits().subscribe(rates => {
            this.translateRates(rates);
        });

        this.endpoint.watchEndPoints().subscribe(() => {
            this.endpoint.getAllRateLimits().subscribe(rates => {
                this.translateRates(rates);
            });
            this.requestRelatedRates(this.currentComponentName);
        });
    }

    public mapComponentToQueryType(componentName: string): [QueryType] {
        switch (componentName) {
            case 'geo':
                return [QueryType.LocationsByHashtag];

            default:
                return null;
        }
    }

    public requestRelatedRates(componentName: string) {
        const types: QueryType[] = this.mapComponentToQueryType(componentName);

        if (types) {
            this.currentPagesRates = types.map(type => {
                const rateName = type.replace(/([A-Z])/g, ' $1').substring(1);
                this.endpoint
                    .getSelectedRateLimit(type)
                    .subscribe((auths: AuthPair) => {
                        this.currentPagesRates.find(
                            element => element[rateName]
                        )[rateName] = auths;
                    });

                return { [rateName]: 'placeholder' };
            });
        } else {
        }
    }

    public translateRates(rates: RateInfo) {
        this.allRates = rates;
    }
}
