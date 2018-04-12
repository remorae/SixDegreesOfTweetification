import { Component, OnInit } from '@angular/core';
import { Country } from '../models';
import { EndpointService } from '../services/endpoint.service';
import { GMapsService } from '../services/g-maps.service';
import { UserInput } from '../models/userInput';
import { LatLngLiteral } from '@agm/core/services/google-maps-types';

@Component({
    selector: 'app-geo-page',
    templateUrl: './geo-page.component.html',
    styleUrls: ['./geo-page.component.scss'],
    providers: [GMapsService]
})
export class GeoPageComponent implements OnInit {
    countries: Country[];
    latestSearch: string;
    testInput: string;
    latitude = 47.6588889;
    longitude = -117.425;

    constructor(private endpoint: EndpointService, private googleMap: GMapsService) {}

    ngOnInit() {}

    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.countries = undefined;
        this.endpoint.searchLocations(this.latestSearch).subscribe((val: Country[]) => {
            this.countries = val;
        });
    }

    // https://github.com/SebastianM/angular-google-maps/issues/689
    testSubmit() {
        this.googleMap.getLatLongFromAddress(this.testInput).subscribe((latLong: LatLngLiteral) => {
            console.log(latLong);
            // this.googleMap.setCenter(latLong);
            this.latitude = latLong.lat;
            this.longitude = latLong.lng;
        });
    }
}
