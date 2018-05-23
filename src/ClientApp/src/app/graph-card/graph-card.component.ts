import {
    Component,
    OnInit,
    Input,
    OnChanges,
    SimpleChanges
} from '@angular/core';
import { EndpointService } from '../services/endpoint.service';
import { Subject } from 'rxjs/Subject';
import { UserResult } from '../models/UserResult';
import { switchMap } from 'rxjs/operators/switchMap';
import { catchError } from 'rxjs/operators/catchError';
import { Observable } from 'rxjs/Observable';
import { Link, Graph } from '../services/graph-data.service';
import { empty } from 'rxjs/observable/empty';
/**
 * @example Displays the data for the currently highlighted node in the Graph Visualizer
 */
@Component({
    selector: 'app-graph-card',
    templateUrl: './graph-card.component.html',
    styleUrls: ['./graph-card.component.scss']
})
export class GraphCardComponent implements OnInit, OnChanges {
    @Input() nodeData;
    @Input() whatToWhat;
    @Input() graph;
    cardTitle: string;
    cardBody = [];
    userData: Subject<any> = new Subject<any>();
    constructor(private endpoint: EndpointService) {}
    /**
     * @example Sets up a subscription to the component's own Subject. Whenever the highlighted node is a user node and does not have
     *      backing user data, the user's id can be fed into the subject and their data will be fetched from the server.
     *      The intent of the switchMap is to cancel in-progress network requests every time a new request is made. Clicking on multiple
     *      new nodes should not present any race conditions.
     */
    ngOnInit(): void {
        this.userData
            .pipe(
                switchMap((data: { id }) =>
                    this.endpoint
                        .getUserInfo(data.id)
                        .pipe(catchError(err => empty<UserResult>()))
                )
            )
            .subscribe((result: UserResult) => {
                this.nodeData.user = result;
                this.updateCardContent(this.nodeData);
            });
    }
    /**
     *  @example If there is no highlighted node, show the time and calls required to find the path between nodes.
     *      If there is a node, display its data.
     * @param changes Tracks the changes to the component's input variables.
     */
    ngOnChanges(changes: SimpleChanges) {
        const data = changes['nodeData'];
        if (data.currentValue) {
            this.updateCardContent(data.currentValue);
        } else {
            this.showTimeData(this.graph.metadata);
        }
    }
    /**
     * @example If the data has attached user data already, display it in a table format.
     *          The profileimage link is potentially obscene, and is excluded.
     *
     *          If the data node is a user node, but lacks backing data, fetch it.
     *
     *          If the node is a hashtag node, show all the words with connections to it.
     * @param data The attached metadata for the currently highlighted node element
     */
    updateCardContent(data) {
        this.cardBody = [];
        this.cardTitle = 'Loading...';

        if (data.user) {
            this.cardTitle = '@' + data.user.screenName;
            for (const [key, value] of Object.entries(data.user)) {
                if (key === 'profileImage') {
                    continue;
                }
                if (key && value) {
                    const keyTitle = this.toTitleCase(key.toString());
                    this.cardBody.push([keyTitle, value.toString()]);
                }
            }
        } else if (data.isUser) {
            this.userData.next(data);
        } else {
            const connectedWords: string[] = this.graph.links
                .filter(
                    link =>
                        link.source.id === data.id || link.target.id === data.id
                )
                .map(
                    link =>
                        link.source.id === data.id
                            ? link.target.id
                            : link.source.id
                );
            this.cardTitle = '#' + data.id;
            this.cardBody.push('Connected To:');
            connectedWords.forEach(hash => {
                this.cardBody.push('#' + hash);
            });
        }
    }
    /**
     *@example Converts camelCase to Title Case
     */
    toTitleCase(str: string) {
        return str
            .replace(/([A-Z])/g, match => ` ${match}`)
            .replace(/^./, match => match.toUpperCase());
    }

    showTimeData(metadata) {
        this.cardTitle = 'Time Taken';
        const { calls, time } = metadata;
        const [hours, minutes, seconds] = time.toLocaleString().split(':');
        this.cardBody.push(['Calls:', `${calls}`]);
        this.cardBody.push(['Hours', hours]);
        this.cardBody.push(['Minutes', minutes]);
        this.cardBody.push(['Seconds', seconds]);
    }
}
