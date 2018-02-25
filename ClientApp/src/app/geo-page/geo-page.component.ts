import { Component, OnInit } from '@angular/core';
import { Country } from '../models';
import { EndpointService } from '../services/endpoint.service';
import { UserInput } from '../models/userInput';

@Component({
    selector: 'app-geo-page',
    templateUrl: './geo-page.component.html',
    styleUrls: ['./geo-page.component.scss']
})
export class GeoPageComponent implements OnInit {
    countries: Country[];
    latestSearch: string;
    constructor(private endpoint: EndpointService) {}

    ngOnInit() {}
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.countries = undefined;
        this.endpoint.searchLocations(this.latestSearch).subscribe((val: Country[]) => {
            this.countries = val;
        });
    }
}
