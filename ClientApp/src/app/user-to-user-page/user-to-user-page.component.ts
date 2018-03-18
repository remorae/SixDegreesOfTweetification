import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { EndpointService } from '../services/endpoint.service';
import { UserResult, UserConnectionInfo, UserConnectionMap } from '../models/UserResult';

@Component({
    selector: 'app-user-to-user-page',
    templateUrl: './user-to-user-page.component.html',
    styleUrls: ['./user-to-user-page.component.scss']
})
export class UserToUserPageComponent implements OnInit {
    latestSearch;
    results: UserConnectionMap;
    constructor(private endpoint: EndpointService) { }

    ngOnInit() {
    }
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.results = undefined;
        this.endpoint.searchUserDegrees(this.latestSearch).subscribe((values: UserConnectionMap) => {
            this.results = values;
        });
    }
}
