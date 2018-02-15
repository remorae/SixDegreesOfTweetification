import { Component, OnInit } from '@angular/core';
import { CountryResult, PlaceResult } from '../../models';

@Component({
    selector: 'app-geo-page',
    templateUrl: './geo-page.component.html',
    styleUrls: ['./geo-page.component.scss']
})
export class GeoPageComponent implements OnInit {
    australiaCities: PlaceResult[] = [
        {
            country: 'Australia',
            hashtags: ['monitorLizard', 'blueRingOctopus', 'funnelWebSpiders'],
            name: 'Melbourne',
            sources: [],
            type: 'City',
        },
        {
            country: 'Australia',
            hashtags: ['blackMamba', 'kangaroo', 'koala'],
            name: 'Perth',
            sources: [],
            type: 'City',
        },
        {
            country: 'Australia',
            hashtags: ['pSherman', '42WallabeeWay', 'taipan'],
            name: 'Sydney',
            sources: [],
            type: 'City',
        },
    ];
    canadaCities: PlaceResult[] = [
        {
            country: 'Canada',
            hashtags: ['iHeartAlberta', 'moveOverBC', 'equestrianOrBust'],
            name: 'Calgary',
            sources: [],
            type: 'City',
        },
        {
            country: 'Canada',
            hashtags: ['iCanSeeNewYorkFromMyHouse', 'yeahEhDereHoser', 'zed'],
            name: 'Toronto',
            sources: [],
            type: 'City',
        },
        {
            country: 'Canada',
            hashtags: ['nowBobsYourUncle', 'orcaRiding', 'likeSeattleButMoreNorth'],
            name: 'Vancouver',
            sources: [],
            type: 'City',
        },
    ];
    usaCities: PlaceResult[] = [
        {
            country: 'United States',
            hashtags: ['floatTheRiver', 'sawtoothMountains', 'cityOfTrees'],
            name: 'Boise',
            sources: [],
            type: 'City',
        },
        {
            country: 'United States',
            hashtags: ['pikesPeak', 'thunderStormAt1pm', 'hangGliding'],
            name: 'Colorado Springs',
            sources: [],
            type: 'City',
        },
        {
            country: 'United States',
            hashtags: ['spokaneDoesntSuck', 'spokompton', 'helloWorld'],
            name: 'Spokane',
            sources: [],
            type: 'City',
        },
    ];
    testCountries: CountryResult[] = [
        {
            countryName: 'Australia',
            places: this.australiaCities,
            sources: [],
        },
        {
            countryName: 'Canada',
            places: this.canadaCities,
            sources: [],
        },
        {
            countryName: 'USA',
            places: this.usaCities,
            sources: [],
        },
    ];

    constructor() { }

    ngOnInit() {
    }

}
