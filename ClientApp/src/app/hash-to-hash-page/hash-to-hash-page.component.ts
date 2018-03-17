import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { EndpointService } from '../services/endpoint.service';

@Component({
    selector: 'app-hash-to-hash-page',
    templateUrl: './hash-to-hash-page.component.html',
    styleUrls: ['./hash-to-hash-page.component.scss']
})
export class HashToHashPageComponent implements OnInit {
    latestSearch;
    results;
    constructor(private endpoint: EndpointService) { }

    ngOnInit() { }
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.endpoint.searchHashDegrees(this.latestSearch).subscribe((values) => {
            this.results = values;
        });
    }
}
