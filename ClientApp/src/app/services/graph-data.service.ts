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
    linkUrl?: string;
}

export interface Graph {
    links: Link[];
    nodes: Node[];
    metaData: ConnectionMetaData;
}

export interface SixDegreesConnection<T> {


    connections: { [hashtag: string]: T[] };
    paths: LinkPath<T>[];
    metaData: ConnectionMetaData;

}

export interface LinkPath<T> {
    path: { [key: number]: T };
    links: string[];

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



    hashDegreesToGraph = (data: SixDegreesConnection<string>) => {
        const nodes = Array.from(new Set([].concat(...Object.values(data.connections)).concat(Object.keys(data.connections))))
            .map((e): Node => ({ id: e, group: 1, isShown: true, onPath: false }));

        // TODO: Change size of nodes based on OnPath, click handler for highlighting, highlighting data, and closing the modal
        const nodeMap = new Map<string, Node>();
        const linkMap = new Map<string, Link>();
        nodes.forEach((node) => {
            nodeMap.set(node.id, node);
        });

        const keys = Object.keys(data.connections);
        const links: Link[] = [];
        for (let index = 0; index < keys.length; index++) {
            const element = keys[index];
            data.connections[element].forEach((c) => {
                const link = { source: element, target: c, value: 1, onPath: false };
                links.push(link);
                linkMap.set(`${link.source} ${link.target}`, link);
                const reverse = `${link.source} ${link.target}`;
                // if(!linkMap.has(reverse)){
                //     const reverseLink = { source: c, target: element, value: 1, onPath: false };
                //     linkMap.set(reverse, reverseLink);
                //     links.push(reverseLink);
                // }
            });
        }
        const paths = data.paths;
        let start = null;
        let end = null;
        paths.forEach(linkPath => {
            const array: string[] = Object.values(linkPath.path);

            start = array[0];
            end = array[array.length - 1];
            for (let i = 1; i < array.length; i++) {
                const sourceNode = array[i - 1];
                const targetNode = array[i];
                nodeMap.get(sourceNode).onPath = true;
                nodeMap.get(targetNode).onPath = true;
                const link = linkMap.get(`${sourceNode} ${targetNode}`);
                link.onPath = true;
                link.value = 5;
                link.linkUrl = linkPath.links[i - 1];

            }
        });

        const queue: string[] = [];
        const visited = new Set<string>();
        const startNode = nodeMap.get(start);
        startNode.group = 0;
        visited.add(startNode.id);
        queue.push(startNode.id);

        while (queue.length > 0) {

            const curr = queue.shift();
            const currNode = nodeMap.get(curr);
            const neighbors = links.filter((e) => e.source === curr);
            neighbors.forEach(n => {

                if (!visited.has(n.target)) {
                    visited.add(n.target);
                    queue.push(n.target);
                    nodeMap.get(n.target).group = currNode.group + 1;
                }
            });
        }

        return { nodes: nodes, links: links, metaData: data.metaData };
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


    sixDegreesToGraph = (data): Graph => {
        return this.hashDegreesToGraph(data);
    }


    // const nodes: Node[] = [];
    // for (const [key, value] of Object.entries(data.path)) {
    //     nodes[+value] = { id: key, group: +value, isShown: true, onPath: false };
    // }

    // const links: Link[] = [];
    // for (let i = 1; i < nodes.length; i++) {
    //     const source = nodes[i - 1];
    //     const target = nodes[i];
    //     links.push({ source: source.id, target: target.id, value: 10, onPath: false });
    // }
    // return { nodes: nodes, links: links, metaData: null };


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
