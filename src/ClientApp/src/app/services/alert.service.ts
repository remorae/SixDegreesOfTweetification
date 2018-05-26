import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';

export class Message {
    header: string;
    content: string;
    style: string;
    link?: string;

    constructor(header, content, style?, link?) {
        this.header = header;
        this.content = content;
        this.style = style || 'info';
        this.link = link;
    }
}
@Injectable()
export class AlertService {
    messages: Message[] = [];
    alerts: BehaviorSubject<Message[]> = new BehaviorSubject<Message[]>(
        this.messages
    );
    constructor() {}

    getAlerts(): BehaviorSubject<Message[]> {
        return this.alerts;
    }

    addError(message: string) {
        this.messages.push(new Message('Error', message, 'danger'));
    }
    addLoadingFinishedMessage(route: string) {
        this.messages.push(
            new Message(
                'Finished!',
                route + ' has finished loading!',
                'success',
                '/' + route
            )
        );
    }
}
