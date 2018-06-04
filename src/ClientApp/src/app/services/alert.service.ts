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
/**
 * @example Manages an array of Messages that are displayed as alerts by the Alert Component.
 *      Other components can have this service injected if they need to alert the user to something.
 */
@Injectable()
export class AlertService {
    messages: Message[] = [];
    alerts: BehaviorSubject<Message[]> = new BehaviorSubject<Message[]>(
        this.messages
    );
    constructor() {}
    /**
     * @returns The active messages, wrapped in a BehaviorSubject.
     */
    getAlerts(): BehaviorSubject<Message[]> {
        return this.alerts;
    }
    /**
     * @example Adds an error message to the list.
     * @param message The message that should be displayed with the error.
     */
    addError(message: string): void {
        this.messages.push(new Message('Error', message, 'danger'));
    }
    /**
     * @example Adds a success message to the list, with a router-usable link.
     * @param route The route fragment corresponding to a component that finished loading a resource.
     */
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
