import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';

@Component({
  selector: 'app-word-cloud-page',
  templateUrl: './word-cloud-page.component.html',
  styleUrls: ['./word-cloud-page.component.scss']
})
export class WordCloudPageComponent implements OnInit {
    latestSearch: string;
    constructor() { }

    ngOnInit() {}
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs[0];
    }
}
