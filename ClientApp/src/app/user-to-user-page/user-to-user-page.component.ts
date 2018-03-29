import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { UserResult, UserConnectionInfo, UserConnectionMap } from '../models/UserResult';
import { GraphDataService, Graph } from '../services/graph-data.service';

@Component({
    selector: 'app-user-to-user-page',
    templateUrl: './user-to-user-page.component.html',
    styleUrls: ['./user-to-user-page.component.scss']
})
export class UserToUserPageComponent implements OnInit {
    latestSearch;
    userRelationshipGraph: Graph;
    constructor(private graphService: GraphDataService) { }

    ngOnInit() {
        this.graphService.getLatestGraphData()
            .subscribe((g: Graph) => { this.userRelationshipGraph = g; });
    }
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.userRelationshipGraph = undefined;
        this.graphService.getSingleUserData(this.latestSearch);
    }
}
