import { Component, OnInit } from '@angular/core';

export class Message {
    header: string;
    content: string;
    style: string;
    dismissed: boolean = false;

    constructor(header,content, style?) {
        this.header = header;
        this.content = content;
        this.style = style || 'info';
    }

}

@Component({
    selector: 'app-alert',
    templateUrl: './alert.component.html',
    styleUrls: ['./alert.component.scss']
})
export class AlertComponent implements OnInit {

    messages: Message[] = [];
    constructor() { }

    ngOnInit() {
        this.messages.push(new Message('Error', 'The server responded with: 404 The given user does not exist','danger'));
    }

    dismiss(itemKey) {
        //this.toast.dismissMessage(itemKey)
    }
    // https://angularfirebase.com/lessons/angular-toast-message-notifications-from-scratch/
}
