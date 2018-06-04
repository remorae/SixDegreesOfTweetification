import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { HashConnectionMap } from '../models/HashConnectionInfo';
import { GraphDataService, Graph } from '../services/graph-data.service';
import { InputCacheService } from '../services/input-cache.service';
import { DualInputComponent } from '../dual-input/dual-input.component';
/**
 * @example Houses a dual input component, and requests data using the input hashtags,
 *          which is then passed into a graph-visualizer component.
 */
@Component({
    selector: 'app-hash-to-hash-page',
    templateUrl: './hash-to-hash-page.component.html',
    styleUrls: ['./hash-to-hash-page.component.scss']
})
export class HashToHashPageComponent implements OnInit {
    latestSearchStart;
    latestSearchEnd;
    hashGraph;
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
    ngOnInit(): void {
        this.graphData.getLatestHashData().subscribe((g: Graph) => {
            this.hashGraph = g;
            if (this.hashGraph && this.freshNavigation) {
                this.showModal();
            }
        });

        this.inputCache.getPreviousHashes().subscribe((s: string[]) => {
            this.latestSearchStart = s[0];
            this.latestSearchEnd = s[1];
        });

        this.freshNavigation = true;
    }
    /**
     *  @example Clears the current graph locally and in the GraphDataService,
     *           queries for a new graph with the latest pair of hashtags,
     *           and caches said pair of hashtags.
     *
     * @param input The pair of hashtags input by the user in the dual-input component.
     */
    onUserSubmit(input: UserInput) {
        const [hashtag1, hashtag2] = input.inputs;
        this.hashGraph = undefined;
        this.graphData.hashGraphSub.next(null);
        this.graphData.getHashConnectionData(hashtag1, hashtag2);
        this.latestSearchStart = hashtag1;
        this.latestSearchEnd = hashtag2;

        this.inputCache.cachePreviousHashes(hashtag1, hashtag2);
    }
    /**
     * @example Displays the graph-visualizer modal window.
     */
    showModal(): void {
        this.modalActive = true;
    }

    /**
     *  @example Conditionally hides or displays the graph-visualizer modal.
     */
    changeModal(value): void {
        this.modalActive = value;
    }
}
