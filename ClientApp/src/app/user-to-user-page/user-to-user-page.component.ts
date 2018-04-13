import { Component, OnInit, OnDestroy } from '@angular/core';
import { UserInput } from '../models/userInput';
import { UserResult, UserConnectionInfo, UserConnectionMap } from '../models/UserResult';
import { GraphDataService, Graph } from '../services/graph-data.service';

@Component({
    selector: 'app-user-to-user-page',
    templateUrl: './user-to-user-page.component.html',
    styleUrls: ['./user-to-user-page.component.scss']
})
export class UserToUserPageComponent implements OnInit {
    latestSearchStart: string;
    latestSearchEnd: string;
    userRelationshipGraph: Graph;
    modalActive = false;
    constructor(private graphData: GraphDataService) { }

    ngOnInit() {
        this.graphData.getLatestUserData()
            .subscribe((g: Graph) => {
                this.userRelationshipGraph = g;
                if (this.userRelationshipGraph) {
                    this.showModal();
                }
            });
    }

    onUserSubmit(input: UserInput) {
        const [start, end] = input.inputs;
        this.userRelationshipGraph = undefined;
        this.graphData.getUserConnectionData(start, end);
        this.latestSearchStart = start;
        this.latestSearchEnd = end;
        // this.graphData.getSingleUserData(this.latestSearch);
    }

    showModal() {
        this.modalActive = true;
    }

    changeModal(value) {
        this.modalActive = value;
    }
}
