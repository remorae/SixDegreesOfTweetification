import { Component, OnInit, EventEmitter, Output, Input } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UserInput, HashOrHandle } from '../models/userInput';

@Component({
    selector: 'app-single-input',
    templateUrl: './single-input.component.html',
    styleUrls: ['./single-input.component.scss']
})
export class SingleInputComponent implements OnInit {
    userForm: FormGroup;
    labelText: '#' | '@';
    @Input() inputType: HashOrHandle = 'hashtag';
    @Output()
    userSubmit: EventEmitter<UserInput> = new EventEmitter<UserInput>();
    constructor(private builder: FormBuilder) {}

    get firstSub() {
        return this.userForm.get('firstSub');
    }

    ngOnInit() {
        this.createForm();
        this.labelText = this.inputType === 'hashtag' ? '#' : '@';
    }

    createForm() {
        const regex: string =
            this.inputType === 'hashtag'
                ? '[A-Za-z0-9]+\\w{0,138}'
                : '\\w{1,15}';
        this.userForm = this.builder.group({
            firstSub: ['', [Validators.required, Validators.pattern(regex)]]
        });
    }

    onSubmit() {
        const input = new UserInput(this.inputType, this.firstSub.value);
        this.userSubmit.emit(input);
    }
}
