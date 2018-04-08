import { Injectable } from '@angular/core';
import { EndpointService } from './endpoint.service';
import { TestData } from './testdata';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { SimulationNodeDatum, SimulationLinkDatum } from 'd3';
import { UserConnectionMap } from '../models/UserResult';

export interface Node extends SimulationNodeDatum {
    id: string;
    group: number;
    isShown: boolean;
    onPath: boolean;
}

export interface Link extends SimulationLinkDatum<Node> {
    source: string;
    target: string;
    value: number;
    onPath: boolean;
}

export interface Graph {
    links: Link[];
    nodes: Node[];
    metaData: ConnectionMetaData;
}

export interface SixDegreesConnection<T> {


    connections: { [hashtag: string]: T[] };
    paths: { [distance: number]: T }[];
    links: string[];
    metaData: ConnectionMetaData;

}
export interface ConnectionMetaData {
    time: string;
    calls: number;
}


export class TestPath {
    getPath() {
        return { path: { 'BarackObama': 0, 'Francis08563494': 1 }, "daddy_yankee": 2, "eliazaroxlaj": 3, "A24": 4, "MUKASAJAMES18": 5 };
    }
}


@Injectable()
export class GraphDataService {
    testData: TestData = new TestData();
    userGraphSub: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);
    hashGraphSub: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);
    constructor(private endpoint: EndpointService) {
        //this.userGraphSub.next(this.userConnectionsToGraph(this.testData.getUserData()));
    }


    getHashConnectionData(hashtag1: string, hashtag2: string) {
        this.endpoint
            .getHashSixDegrees(hashtag1, hashtag2)
            .map(this.hashDegreesToGraph)
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
                    this.userGraphSub.next(this.userGraphSub.value);
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
            .map((user) => (
                {
                    id: user,
                    group: data[user].distance,
                    isShown: data[user].distance <= 1,
                   // expandable: !!data[user].connections.length,
                   // opened: data[user].distance === 0,
                   // compacted: false
                   onPath: false
                }));

        // const expandables = this.trimMatchingEntries<Node>(nodes, (e: Node, i) => e.expandable);
        // let compactionFactor = 0;

        const links: Link[] = this.createUserLinks(data, nodes);

        return { nodes: nodes, links: links, metaData: null };
    }

    trimMatchingEntries<T>(array: T[], filter: (e, i) => boolean): T[] {
        let i = array.length;
        const deleted = [];
        while (i--) {
            if (filter(array[i], i)) {
                deleted.push(...array.splice(i, 1));
            }
        }

        return deleted;
    }

    hashDegreesToGraph(data: SixDegreesConnection<string>) {
        const nodes = Array.from(new Set([].concat(...Object.values(data.connections)).concat(Object.keys(data))))
                        .map((e): Node => ({ id: e, group: 0, isShown: true, onPath: false }));

        //TODO: include orphan purge

        const nodeMap = new Map<string, Node>();
        nodes.forEach((node) => {
            nodeMap.set(node.id, node);
        });

        const keys = Object.keys(data.connections);
        const links: Link[] = [];
        for (let index = keys.length - 1; index >= 0; index--) {
            const element = keys[index];
            data.connections[element].forEach((c) => {
                links.push({ source: element, target: c, value: 1, onPath: false });
            });
             nodeMap.get(element).group = index;
        }



        return { nodes: nodes, links: links, metaData: data.metaData };
    }

    sixDegreesToGraph = (data): Graph => {
        const nodes: Node[] = [];
        for (const [key, value] of Object.entries(data.path)) {
            nodes[+value] = { id: key, group: +value, isShown: true, onPath: false};
        }

        const links: Link[] = [];
        for (let i = 1; i < nodes.length; i++) {
            const source = nodes[i - 1];
            const target = nodes[i];
            links.push({ source: source.id, target: target.id, value: 10, onPath: false });
        }
        return { nodes: nodes, links: links, metaData: null };
    }


    createUserLinks(data, nodes: Node[]) { // TODO: look into bidirectional links
        const links: Link[] = [];

        nodes.forEach(element => {
            const user = data[element.id];
            const connections = user ? user.connections : [];
            connections.forEach(connection => {
                if (nodes.some((e: Node) => e.id === connection.screenName)) {
                    links.push({ source: element.id, target: connection.screenName, value: 1, onPath: false });
                }
            });
        });

        return links;
    }

}
