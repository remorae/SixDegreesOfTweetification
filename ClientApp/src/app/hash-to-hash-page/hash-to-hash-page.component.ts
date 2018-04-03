import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';
import { HashConnectionMap } from '../models/HashConnectionInfo';
import { GraphDataService, Graph } from '../services/graph-data.service';

@Component({
    selector: 'app-hash-to-hash-page',
    templateUrl: './hash-to-hash-page.component.html',
    styleUrls: ['./hash-to-hash-page.component.scss']
})
export class HashToHashPageComponent implements OnInit {
    latestSearchStart;
    latestSearchEnd;
    hashGraph;
    constructor(private graphData: GraphDataService) { }

    ngOnInit() {
        this.graphData.getLatestHashData().subscribe((g: Graph) => {
            this.hashGraph = g;
        });
    }
    onUserSubmit(input: UserInput) {
        const [hashtag1, hashtag2] = input.inputs;
        this.hashGraph = undefined;
        this.graphData.getHashConnectionData(hashtag1, hashtag2);
        this.latestSearchStart = hashtag1;
        this.latestSearchEnd = hashtag2;
    }
}
