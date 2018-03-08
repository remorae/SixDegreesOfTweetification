import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';

@Component({
  selector: 'app-hash-to-hash-page',
  templateUrl: './hash-to-hash-page.component.html',
  styleUrls: ['./hash-to-hash-page.component.scss']
})
export class HashToHashPageComponent implements OnInit {
    latestSearch: string[];
    constructor() { }

    ngOnInit() {}
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs;
    }
}
