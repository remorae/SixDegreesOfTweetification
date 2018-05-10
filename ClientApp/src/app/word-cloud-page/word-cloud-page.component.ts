import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { CloudDataService } from '../services/cloud-data.service';
import { WeightedWord } from '../cloud-bottle/cloud-bottle.component';

export type CloudState = 'empty' | 'new' | 'unchanged' | 'loading';
@Component({
    selector: 'app-word-cloud-page',
    templateUrl: './word-cloud-page.component.html',
    styleUrls: ['./word-cloud-page.component.scss']
})
export class WordCloudPageComponent implements OnInit {
    latestSearch: string;
    cloudWords: WeightedWord[] = [];
    newlyAdded: number;
    cloudState: CloudState;
    constructor(private cloudData: CloudDataService) {}

    ngOnInit() {
        this.cloudState = 'empty';
    }
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.cloudState = 'loading';
        this.cloudData
            .getRelatedHashes(this.latestSearch)
            .subscribe((newWords: WeightedWord[]) => {
                this.cloudWords = newWords;
                this.newlyAdded = this.cloudData.getWordsAdded();
            });
    }

    clearCloud() {
        this.cloudData.dropCachedWords();
        this.cloudWords = [];
        this.latestSearch = '';
        this.newlyAdded = 0;
        this.cloudState = 'empty';
    }

    checkCloudDrawn(drawn: CloudState) {
        this.cloudState = drawn;
    }
}
