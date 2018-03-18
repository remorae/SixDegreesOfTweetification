import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { EndpointService } from '../services/endpoint.service';
import { HashConnectionMap } from '../models/HashConnectionInfo';

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
        this.results = undefined;
        this.endpoint.searchHashDegrees(this.latestSearch).subscribe((values: HashConnectionMap) => {
            this.results = values;
        });
    }
}
