import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import { EndpointService } from '../services/endpoint.service';
import { Subject } from 'rxjs/Subject';
import { UserResult } from '../models/UserResult';
import 'rxjs/add/operator/switchMap';

@Component({
    selector: 'app-graph-card',
    templateUrl: './graph-card.component.html',
    styleUrls: ['./graph-card.component.scss']
})
export class GraphCardComponent implements OnInit, OnChanges {

    @Input() nodeData;
    @Input() whatToWhat;
    cardTitle: string;
    cardBody: string[][] = [];
    userData: Subject<any> = new Subject<any>();
    newestUser: UserResult;
    constructor(private endpoint: EndpointService) { }

    ngOnInit() {
        this.userData.switchMap((data) => this.endpoint.getUserInfo(data.id)).subscribe(
            (result) => {
                this.newestUser = result;
                this.nodeData.user = result;
                this.updateCardContent(this.nodeData);
            } // TODO: kills entire stream on Error, need to isolate with pipe calls
        );
    }

    ngOnChanges(changes: SimpleChanges) {
        const data = changes['nodeData'];
        if (data.currentValue) {
            this.updateCardContent(data.currentValue);
        }
    }

    updateCardContent(data) {
        this.cardBody = [];

        if (data.user) {
            this.cardTitle = data.user.screenName;
            for (const [key, value] of Object.entries(data.user)) {
                if (key === 'profileImage') {
                    continue;
                }
                if (key && value) {
                    this.cardBody.push([key.toString(), value.toString()]);
                }

            }
        } else if (data.isUser) {

            this.userData.next(data);
            // this.cardTitle = data.id;
            // //const { calls, time } = this.graph.metadata;
            // // const [hours, minutes, seconds] = time.toLocaleString().split(':');
            // //this.cardBody.push(['Calls:', `${this.graph.metadata.calls}`]);
            // this.cardBody.push(['Time Taken:', '']);
            // this.cardBody.push(['Hours', hours]);
            // this.cardBody.push(['Minutes', minutes]);
            // this.cardBody.push(['Seconds', seconds]);
        } else {
            console.log('Didn\'t hit guard');
            this.endpoint.getUserInfo(data.id).subscribe(e => console.log(e));
        }
    }

}
