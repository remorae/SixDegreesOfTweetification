import { Injectable } from '@angular/core';
import { EndpointService } from './endpoint.service';
import { TestData } from './testdata';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { SimulationNodeDatum, SimulationLinkDatum } from 'd3';

export interface Node extends SimulationNodeDatum {
    id: string;
    group: number;
}

export interface Link extends SimulationLinkDatum<Node> {
    source: string;
    target: string;
    value: number;
}

export interface Graph {
    links: Link[];
    nodes: Node[];
}

@Injectable()
export class GraphDataService {
    testData: TestData = new TestData();
    graphData: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);

    constructor(private endpoint: EndpointService) {
    }


    getSingleUserData(username: string) {
         this.graphData.next(this.mapUserData(this.testData.getUserData()));
        // this.endpoint.searchUserDegrees(username)
        //     .map(this.mapUserData)
        //     .subscribe(
        //         (graph: Graph) =>
        //          this.graphData.next(graph),
        //         (error) => {
        //             console.log(error);
        //         this.graphData.next(this.graphData.value);
        //     });
    }

    getLatestGraphData() {
        return this.graphData;
    }
    mapUserData = (data): Graph => {
        // const data = this.testData.getUserData();
        const nodes: Node[] = Object.keys(data)
            .map((user) => ({ id: user, group: data[user].distance }));

        const links: Link[] = this.createLinks(data, nodes);
        return { nodes: nodes, links: links };
    }


    createLinks(data, nodes: Node[]) {
        const links: Link[] = [];

        nodes.forEach(element => {
            const user = data[element.id];
            const connections = user ? user.connections : [];
            connections.forEach(connection => {
                links.push({ source: element.id, target: connection.screenName, value: 1 })
            });
        });

        return links;
    }

}
