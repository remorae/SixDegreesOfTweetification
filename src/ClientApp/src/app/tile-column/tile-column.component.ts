import { Component, OnInit } from '@angular/core';

interface TileProps {
    iconPath?: string;
    title: string;
    description: string;
    route: string;
}
/**
 * @example Contains and specifies a selection of navigable section tiles that can be clicked to navigate to another component.
 */
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
            description:
                'How many tweets does it take to get from one hashtag to another?',
            iconPath: './../../assets/hash_to_hash.svg'
        },
        {
            title: 'Geo Stats',
            route: 'geo',
            description:
                'See where in the world certain hashtags are coming from!',
            iconPath: './../../assets/geo_stats.svg'
        },
        {
            title: 'User to User',
            route: 'user-to-user',
            description:
                'How many user connections does it take to get from one person to another?',
            iconPath: './../../assets/user_to_user.svg'
        },
        {
            title: 'Word Cloud',
            route: 'word-cloud',
            description: 'See what hashtags are often found together!',
            iconPath: './../../assets/word_cloud.svg'
        }
    ];

    constructor() {}

    ngOnInit() {}
}
