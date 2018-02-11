import { Component, OnInit, Input } from '@angular/core';
import { NgModel, FormControl, Validators, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { EventEmitter } from '@angular/core';
import { UserInput } from '../models/userInput';
@Component({
    selector: 'app-user-input',
    templateUrl: './user-input.component.html',
    styleUrls: ['./user-input.component.scss']
})
export class UserInputComponent implements OnInit {
    @Input() inputType: string;
    regex: string; // [A-Za-z0-9]+\w* for hashtags
    labelText: string;
    @Input() secondForm = false;
    firstSubmission = '';
    secondSubmission = '';
    userSubmit: EventEmitter<UserInput>;
    constructor(private router: Router) {
        this.userSubmit = new EventEmitter<UserInput>();
    }

    ngOnInit() {
        this.regex =
            this.inputType === 'hashtag' ? '[A-Za-z0-9]+w{0,138}' : 'w{1,15}';
        this.labelText = this.inputType === 'hashtag' ? '#' : '@';
    }

    submit() {
        let ui: UserInput;
        if (this.secondForm) {
            ui = new UserInput(
                this.labelText,
                this.firstSubmission,
                this.secondSubmission
            );
        } else {
            ui = new UserInput(this.labelText, this.firstSubmission);
        }
        this.userSubmit.emit(ui);
    }
}
