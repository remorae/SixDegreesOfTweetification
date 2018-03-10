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
            title: 'Hash to Hash',
            route: 'hash-to-hash',
            description: 'How many tweets does it take to get from one hashtag to another?',
            iconPath: './../../assets/Bandaid.svg'
        },
        {
            title: 'Geo Stats',
            route: 'geo',
            description: 'See where in the world certain hashtags are coming from!',
            iconPath: './../../assets/Bandaid.svg'
        },
        {
            title: 'User to User',
            route: 'user-to-user',
            description: 'How many user connections does it take to get from one person to another?',
            iconPath: './../../assets/Bandaid.svg'
        },
        {
            title: 'Word Cloud',
            route: 'word-cloud',
            description: 'See what hashtags are often found together!',
            iconPath: './../../assets/Bandaid.svg'
        },
    ];

    constructor() { }

    ngOnInit() {
    }

}
