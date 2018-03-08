import { Component, OnInit } from '@angular/core';
import { UserInput } from '../models/userInput';

@Component({
  selector: 'app-user-to-user-page',
  templateUrl: './user-to-user-page.component.html',
  styleUrls: ['./user-to-user-page.component.scss']
})
export class UserToUserPageComponent implements OnInit {
    latestSearch: string[];
    constructor() { }

    ngOnInit() {}
    onUserSubmit(input: UserInput) {
        this.latestSearch = input.inputs;
    }
}
