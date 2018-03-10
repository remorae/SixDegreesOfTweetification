import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { CloudDataService } from '../services/cloud-data.service';
import { WeightedWord } from '../cloud-bottle/cloud-bottle.component';
import * as D3 from 'd3';
@Component({
    selector: 'app-word-cloud-page',
    templateUrl: './word-cloud-page.component.html',
    styleUrls: ['./word-cloud-page.component.scss']
})
export class WordCloudPageComponent implements OnInit {
    latestSearch: string;
    wordMap: D3.Map<WeightedWord> = D3.map();
    cloudWords: WeightedWord[] = [];
    newlyAdded: number;
    constructor(private cloudData: CloudDataService) { }

    ngOnInit() {
    }
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
        this.cloudData.getRelatedHashes(this.latestSearch).subscribe((newWords: WeightedWord[]) => {

            newWords.forEach((word) => {
                if (this.wordMap.has(word.text)) {
                    this.wordMap.get(word.text).size++;
                } else {
                    this.wordMap.set(word.text, word);
                }
            });
            const fresh: WeightedWord[] = this.wordMap.values();

            if (!this.cloudWords.length) {
                this.newlyAdded = fresh.length;
            } else {
                this.newlyAdded = fresh.length - this.cloudWords.length;
            }
            this.cloudWords = fresh;
        });

    }
}
