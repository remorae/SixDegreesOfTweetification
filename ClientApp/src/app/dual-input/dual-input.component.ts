import { Component, OnInit, Input, Output } from '@angular/core';
import {
    NgModel,
    FormControl,
    Validators,
    NgForm,
    FormGroup,
    FormBuilder
} from '@angular/forms';
import { EventEmitter } from '@angular/core';
import { UserInput, HashOrHandle } from '../models/userInput';
@Component({
    selector: 'app-dual-input',
    templateUrl: './dual-input.component.html',
    styleUrls: ['./dual-input.component.scss']
})
export class DualInputComponent implements OnInit {
    userForm: FormGroup;
    labelText: '#' | '@';
    @Input() inputType: HashOrHandle = 'hashtag';
    @Output()
    userSubmit: EventEmitter<UserInput> = new EventEmitter<UserInput>();
    constructor(private builder: FormBuilder) {}
    get firstSub() {
        return this.userForm.get('firstSub');
    }
    get secondSub() {
        return this.userForm.get('secondSub');
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
            firstSub: ['', [Validators.required, Validators.pattern(regex)]],
            secondSub: ['', [Validators.required, Validators.pattern(regex)]]
        });
    }

    onSubmit() {
        const input = new UserInput(
            this.inputType,
            this.firstSub.value,
            this.secondSub.value
        );
        this.userSubmit.emit(input);
    }
}
