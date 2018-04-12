import { Injectable, NgZone } from '@angular/core';
import { GoogleMapsAPIWrapper, MapsAPILoader } from '@agm/core';
import { LatLng, LatLngLiteral } from '@agm/core/services/google-maps-types';
import { Observable } from 'rxjs/Observable';
import { Observer } from 'rxjs/Observer';

declare var google: any;

@Injectable()
export class GMapsService extends GoogleMapsAPIWrapper {
    // geocoder: any;

    constructor(private __loader: MapsAPILoader, private __zone: NgZone) {
        super(__loader, __zone);
        // this.geocoder = new google.maps.Geocoder();
    }

    getLatLongFromAddress(address: string): Observable<LatLngLiteral> {
        console.log('Getting Address - ', address);
        const geocoder = new google.maps.Geocoder();
        return Observable.create(observer => {
            geocoder.geocode( { 'address': address}, function(results, status) {
                if (status === google.maps.GeocoderStatus.OK) {
                    const resultLocation: LatLng = results[0].geometry.location;
                    const latLong: LatLngLiteral = {
                        lat: resultLocation.lat(),
                        lng: resultLocation.lng(),
                    };
                    observer.next(latLong);
                    observer.complete();
                } else {
                    console.log('Error - ', results, ' & Status - ', status);
                    observer.next({});
                    observer.complete();
                }
            });
        });
    }
}
