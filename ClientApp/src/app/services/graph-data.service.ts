import { Injectable } from '@angular/core';
import { EndpointService } from './endpoint.service';
import { TestData } from './testdata';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { SimulationNodeDatum, SimulationLinkDatum } from 'd3';
import { UserConnectionMap } from '../models/UserResult';

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

export interface SixDegreesConnection {
    path: {};
    links: string[];
    metadata: { time: string, calls: number };
}


@Injectable()
export class GraphDataService {
    testData: TestData = new TestData();
    userGraphSub: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);
    hashGraphSub: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);
    constructor(private endpoint: EndpointService) {
    }


    getHashConnectionData(hashtag1: string, hashtag2: string) {
        this.endpoint
            .getHashSixDegrees(hashtag1, hashtag2)
            .map(this.sixDegreesToGraph)
            .subscribe(
                (graph: Graph) => {
                    this.hashGraphSub.next(graph);
                },
                (error) => {
                    console.log(error);
                    this.hashGraphSub.next(this.hashGraphSub.value);
                });
    }

    getUserConnectionData(user1: string, user2: string) {
        this.endpoint
            .getUserSixDegrees(user1, user2)
            .map(this.sixDegreesToGraph)
            .subscribe(
                (graph: Graph) => {
                    this.userGraphSub.next(graph);
                },
                (error) => {
                    console.log(error);
                });
    }


    getSingleUserData(username: string) {
        this.endpoint
            .searchUserDegrees(username, 6)
            .map(this.userConnectionsToGraph)
            .subscribe(
                (graph: Graph) => {
                    this.userGraphSub.next(graph);
                },
                (error) => {
                    console.log(error);
                    this.userGraphSub.next(this.hashGraphSub.value);
                });
    }

    getLatestUserData(): BehaviorSubject<Graph> {
        return this.userGraphSub;
    }

    getLatestHashData(): BehaviorSubject<Graph> {
        return this.hashGraphSub;
    }
    userConnectionsToGraph = (data): Graph => {
        const nodes: Node[] = Object.keys(data)
            .map((user) => ({ id: user, group: data[user].distance }));

        const links: Link[] = this.createUserLinks(data, nodes);


        return { nodes: nodes, links: links };
    }

    sixDegreesToGraph = (data: SixDegreesConnection): Graph => {
        const nodes: Node[] = [];

        Object.keys(data.path).forEach((point) => {
            const position = data.path[point];
            nodes[position] = { id: point, group: position };
        });

        const links: Link[] = [];
        for (let i = 1; i < nodes.length; i++) {
            const source = nodes[i - 1];
            const target = nodes[i];
            links.push({ source: source.id, target: target.id, value: 10 });
        }
        return { nodes: nodes, links: links };
    }


    createUserLinks(data, nodes: Node[]) {
        const links: Link[] = [];

        nodes.forEach(element => {
            const user = data[element.id];
            const connections = user ? user.connections : [];
            connections.forEach(connection => {
                links.push({ source: element.id, target: connection.screenName, value: 1 });
            });
        });

        return links;
    }

}
