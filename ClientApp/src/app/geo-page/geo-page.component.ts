import { Component, OnInit } from '@angular/core';
import { Country, PlaceResult } from '../models';
import { EndpointService } from '../services/endpoint.service';
import { UserInput } from '../models/userInput';

@Component({
    selector: 'app-geo-page',
    templateUrl: './geo-page.component.html',
    styleUrls: ['./geo-page.component.scss']
})
export class GeoPageComponent implements OnInit {
    countries: Country[];

    constructor(private endpoint: EndpointService) {}

    ngOnInit() {}
    onUserSubmit(input: UserInput) {
        const hashtag = input.inputs[0];
        this.endpoint.searchLocations(hashtag).subscribe((val: Country[]) => {
            this.countries = val; // TODO: check for empty results.
        });
    }
}
