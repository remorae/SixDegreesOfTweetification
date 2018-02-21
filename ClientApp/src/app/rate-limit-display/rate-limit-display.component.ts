import { Component, OnInit } from '@angular/core';
import {
    EndpointService,
    QueryType,
    RateInfo
} from '../services/endpoint.service';

@Component({
    selector: 'app-rate-limit-display',
    templateUrl: './rate-limit-display.component.html',
    styleUrls: ['./rate-limit-display.component.scss']
})
export class RateLimitDisplayComponent implements OnInit {
    rates: RateInfo;

    constructor(private endpoint: EndpointService) {}

    ngOnInit() {
        this.endpoint.watchEndPoints().subscribe(() => {
            this.endpoint.getAllRateLimits().subscribe(rates => {
                this.translateRates(rates);
            });
        });
    }

    public translateRates(rates: RateInfo) {
        this.rates = rates;
    }
}
