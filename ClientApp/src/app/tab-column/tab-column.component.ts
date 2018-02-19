import { Component, OnInit } from '@angular/core';

interface PageRoute {
    name: string;
    route: string;
}

@Component({
    selector: 'app-tab-column',
    templateUrl: './tab-column.component.html',
    styleUrls: ['./tab-column.component.scss']
})
export class TabColumnComponent implements OnInit {
    pages: PageRoute[] = [
        { name: 'Home', route: 'home' },
        { name: 'Counter', route: 'counter' },
        { name: 'Fetch Data', route: 'fetch-data' },
        { name: 'Geographical Stats', route: 'geo' },
    ];

    constructor() { }

    ngOnInit() {
    }

}
