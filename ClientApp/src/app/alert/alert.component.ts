import { Component, OnInit } from '@angular/core';
import { AlertService, Message } from '../services/alert.service';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';



@Component({
    selector: 'app-alert',
    templateUrl: './alert.component.html',
    styleUrls: ['./alert.component.scss']
})
export class AlertComponent implements OnInit {


    constructor(private alertService: AlertService) { }

    messages$: BehaviorSubject<Message[]> = this.alertService.getAlerts();
    ngOnInit() {
    }

    dismiss(event, i) {

        this.messages$.value.splice(i, 1);
    }
    // https://angularfirebase.com/lessons/angular-toast-message-notifications-from-scratch/
}
