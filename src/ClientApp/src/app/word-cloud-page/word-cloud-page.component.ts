import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { CloudDataService } from '../services/cloud-data.service';
import { WeightedWord } from '../cloud-bottle/cloud-bottle.component';

export type CloudState = 'empty' | 'new' | 'unchanged' | 'loading';
/**
 * @example Contains a single input component and a cloud bottle component. It runs queries using information from the former
 *          and feeds the resulting data into the latter.
 *
 */
export type AllOrCurrent = 'all' | 'current';
@Component({
    selector: 'app-word-cloud-page',
    templateUrl: './word-cloud-page.component.html',
    styleUrls: ['./word-cloud-page.component.scss']
})
export class WordCloudPageComponent implements OnInit {
    latestSearch: string;
    allOrCurrent: AllOrCurrent = 'current';
    allCloudWords: WeightedWord[] = [];
    currentHashCloudWords: WeightedWord[] = [];
    newlyAdded: number;
    cloudState: CloudState;
    constructor(private cloudData: CloudDataService) {}
    /**
     *  @example The page defaults to showing an empty cloud.
     */
    ngOnInit() {
        this.cloudState = 'empty';
    }
    /**
     * @example Fetches a new array of associated hashtags from the word entered in the input component.
     * @param input The input hashtag from the single input component.
     */

    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.cloudState = 'loading';
        this.cloudData
            .getRelatedHashes(this.latestSearch)
            .subscribe((increasedSet: WeightedWord[]) => {
                this.allCloudWords = increasedSet;
                this.currentHashCloudWords = this.cloudData.getCurrentWords();
            });
    }

    onRadioClick(event: any) {
        this.allOrCurrent = event.target.value;

        /* Current things:
            - "No new results" shows when clicking All (rerender changes 'empty' and 'new' cloudState to 'unchanged')
            - The above ^ bullet shows "Words added" if search performed on All, switch to Current, then back to all (if that's a problem)
            - Want a loading message (since the word swap takes a sec) & rerender animation when switching between Current and All
        */
    }

    clearCloud() {
        this.cloudData.dropCachedWords();
        this.allCloudWords = [];
        this.currentHashCloudWords = [];
        this.latestSearch = '';
        this.newlyAdded = 0;
        this.cloudState = 'empty';
    }

    checkCloudDrawn(drawn: CloudState) {
        this.cloudState = drawn;
    }
}
