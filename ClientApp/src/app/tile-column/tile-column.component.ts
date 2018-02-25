import { Component, OnInit } from '@angular/core';

interface TileProps {
    iconPath?: string;
    title: string;
    description: string;
    route: string;
}

@Component({
    selector: 'app-tile-column',
    templateUrl: './tile-column.component.html',
    styleUrls: ['./tile-column.component.scss']
})
export class TileColumnComponent implements OnInit {
    pages: TileProps[] = [
        {
            title: 'Home',
            route: 'home',
            description: 'The place where the deer and the antelope play. Maybe Nebraska? Hard to say.',
        },
        {
            title: 'Counter',
            route: 'counter',
            description: 'Default component that we kept so our app looks less boring. Check it out!',
        },
        {
            title: 'Fetch Data',
            route: 'fetch-data',
            description: 'I\'m not entirely sure where the data comes from, but it\'s pretty magical.',
        },
        {
            title: 'Geographical Stats',
            route: 'geo',
            description: 'See where in the world certain hashtags are coming from!',
        },
    ];

    constructor() { }

    ngOnInit() {
    }

}
