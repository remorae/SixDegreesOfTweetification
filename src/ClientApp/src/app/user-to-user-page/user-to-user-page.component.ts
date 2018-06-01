import { Component, OnInit, OnDestroy } from '@angular/core';
import { UserInput } from '../models/userInput';
import {
    UserResult,
    UserConnectionInfo,
    UserConnectionMap
} from '../models/UserResult';
import { GraphDataService, Graph } from '../services/graph-data.service';
import { InputCacheService } from '../services/input-cache.service';
/**
 * @example Houses a dual input component, and requests data using the input user handles,
 *          which is then passed into a graph-visualizer component.
 */
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

    /**
     * @example When a graph is loaded from the GraphDataService, the graph-visualizer will only
     *          be immediately shown if the user has not navigated away from the page.
     *
     *          If there was navigation away from the page before the latest graph loaded,
     *          the previously searched terms are pulled from the InputCacheService.
     */
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
    /**
     *  @example Clears the current graph locally and in the GraphDataService,
     *           queries for a new graph with the latest pair of user handles,
     *           and caches said pair of user handles.
     *
     * @param input The pair of user handles input by the user in the dual-input component.
     */
    onUserSubmit(input: UserInput) {
        const [start, end] = input.inputs;
        this.userRelationshipGraph = undefined;
        this.graphData.getLatestUserData().next(null);
        this.graphData.getUserConnectionData(start, end);
        this.latestSearchStart = start;
        this.latestSearchEnd = end;
        this.inputCache.cachePreviousUsers(start, end);
    }
    /**
     * @example Displays the graph-visualizer modal window.
     */
    showModal() {
        this.modalActive = true;
    }
    /**
     *  @example Conditionally hides or displays the graph-visualizer modal.
     */
    changeModal(value) {
        this.modalActive = value;
    }
}
