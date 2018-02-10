import { Component, OnInit } from '@angular/core';
import { NgModel, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { EventEmitter } from '@angular/core';

@Component({
    selector: 'app-single-input',
    templateUrl: './single-input.component.html',
    styleUrls: ['./single-input.component.scss']
})
export class SingleInputComponent implements OnInit {
    hashtag = '';
    tagModel: FormControl;
    hashSubmit: EventEmitter<string>;
    constructor(private router: Router) {
        this.hashSubmit = new EventEmitter<string>();
    }

    ngOnInit() {}

    submit() {
        if (this.hashtag.length) {
            this.hashSubmit.emit(this.hashtag);
        }
    }
}
