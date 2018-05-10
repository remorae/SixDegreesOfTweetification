import {
    Component,
    OnInit,
    OnDestroy,
    ViewChild,
    AfterViewInit
} from '@angular/core';
import { UserInput } from '../models/userInput';
import { HashConnectionMap } from '../models/HashConnectionInfo';
import { GraphDataService, Graph } from '../services/graph-data.service';
import { InputCacheService } from '../services/input-cache.service';
import { DualInputComponent } from '../dual-input/dual-input.component';

@Component({
    selector: 'app-hash-to-hash-page',
    templateUrl: './hash-to-hash-page.component.html',
    styleUrls: ['./hash-to-hash-page.component.scss']
})
export class HashToHashPageComponent implements OnInit, AfterViewInit {
    @ViewChild(DualInputComponent) dualInput;
    latestSearchStart;
    latestSearchEnd;
    hashGraph;
    modalActive = false;
    freshNavigation = false;
    constructor(
        private graphData: GraphDataService,
        private inputCache: InputCacheService
    ) {}

    ngOnInit() {
        this.graphData.getLatestHashData().subscribe((g: Graph) => {
            this.hashGraph = g;
            if (this.hashGraph && this.freshNavigation) {
                this.showModal(); //TODO: Potential Errors if they navigate back while a query is ongoing.
            }
        });

        this.inputCache.getPreviousHashes().subscribe((s: string[]) => {
            this.latestSearchStart = s[0];
            this.latestSearchEnd = s[1];
        });

        this.freshNavigation = true;
    }

    ngAfterViewInit() {
        // if (this.latestSearchStart && this.latestSearchEnd) {
        //     this.dualInput.firstSub().value = this.latestSearchStart;
        //     this.dualInput.SecondSub().value = this.latestSearchEnd;
        // }
    }

    onUserSubmit(input: UserInput) {
        const [hashtag1, hashtag2] = input.inputs;
        this.hashGraph = undefined;
        this.graphData.getHashConnectionData(hashtag1, hashtag2);
        this.latestSearchStart = hashtag1;
        this.latestSearchEnd = hashtag2;

        this.inputCache.cachePreviousHashes(hashtag1, hashtag2);
    }

    showModal() {
        this.modalActive = true;
    }

    changeModal(value) {
        this.modalActive = value;
    }
}
