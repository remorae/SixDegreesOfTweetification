import { Component, OnInit, OnDestroy } from '@angular/core';
import { UserInput } from '../models/userInput';
import {
    UserResult,
    UserConnectionInfo,
    UserConnectionMap
} from '../models/UserResult';
import { GraphDataService, Graph } from '../services/graph-data.service';
import { InputCacheService } from '../services/input-cache.service';

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
    freshNavigation = false;
    constructor(
        private graphData: GraphDataService,
        private inputCache: InputCacheService
    ) {}

    ngOnInit() {
        this.graphData.getLatestUserData().subscribe((g: Graph) => {
            this.userRelationshipGraph = g;
            if (this.userRelationshipGraph && this.freshNavigation) {
                this.showModal();
            }
        });

        this.inputCache.getPreviousUsers().subscribe((s: string[]) => {
            [this.latestSearchStart, this.latestSearchEnd] = s;
        });

        this.freshNavigation = true;
    }

    onUserSubmit(input: UserInput) {
        const [start, end] = input.inputs;
        this.userRelationshipGraph = undefined;
        this.graphData.getLatestUserData().next(null);
        this.graphData.getUserConnectionData(start, end);
        this.latestSearchStart = start;
        this.latestSearchEnd = end;
        this.inputCache.cachePreviousUsers(start, end);
    }

    showModal() {
        this.modalActive = true;
    }

    changeModal(value) {
        this.modalActive = value;
    }
}
