import { Component, OnInit } from '@angular/core';
import { AlertService, Message } from '../services/alert.service';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
/**
 * @example This component displays messages based on the properties in the passedIn message array.
 *          Each alert is a simple information display, except for successful loading messages, which
 *          contain a link to the page where loading was successful. Each alert is dismissable.
 */
@Component({
    selector: 'app-alert',
    templateUrl: './alert.component.html',
    styleUrls: ['./alert.component.scss']
})
export class AlertComponent implements OnInit {
    constructor(private alertService: AlertService) {}
    messages$: BehaviorSubject<Message[]> = this.alertService.getAlerts();
    ngOnInit() {}

    /**
     * @example Directly removes the array element inside the behaviorSubject, so it is not displayed inside the *ngFor in the template.
     * @param event Ignored
     * @param i The index of the message clicked.
     */
    dismiss(event, i: number) {
        this.messages$.value.splice(i, 1);
    }
    // https://angularfirebase.com/lessons/angular-toast-message-notifications-from-scratch/
}
