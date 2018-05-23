import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    Validators
} from '@angular/forms';
import { HashOrHandle, UserInput } from '../models/userInput';
/**
 * @example A pair of validated input forms that accept only valid Twitter user handles, or valid Twitter hashtags.
 */
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
    /**
     * @returns The form control for the first input
     */
    get firstSub(): AbstractControl {
        return this.userForm.get('firstSub');
    }
    /**
     * @returns The form control for the second input
     */
    get secondSub(): AbstractControl {
        return this.userForm.get('secondSub');
    }

    ngOnInit(): void {
        this.createForm();
        this.labelText = this.inputType === 'hashtag' ? '#' : '@';
    }
    /**
     * @example Creates grouped FormControls using custom validators.
     *      Both inputs must have a value that matches the input regular expression before submission is allowed.
     */
    createForm(): void {
        const regex: string =
            this.inputType === 'hashtag'
                ? '[A-Za-z0-9]+\\w{0,138}'
                : '\\w{1,15}';
        this.userForm = this.builder.group({
            firstSub: ['', [Validators.required, Validators.pattern(regex)]],
            secondSub: ['', [Validators.required, Validators.pattern(regex)]]
        });
    }
    /**
     * @example Emits the submitted values in both input elements to the parent component.
     */
    onSubmit(): void {
        const input = new UserInput(
            this.inputType,
            this.firstSub.value,
            this.secondSub.value
        );
        this.userSubmit.emit(input);
    }
}
