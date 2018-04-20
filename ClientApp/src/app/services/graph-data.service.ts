import { Injectable } from '@angular/core';
import { SimulationLinkDatum, SimulationNodeDatum } from 'd3';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { UserResult } from '../models/UserResult';
import { EndpointService } from './endpoint.service';
import { TestData } from './testdata';

export interface Node extends SimulationNodeDatum {
    id: string;
    group: number;
    isShown: boolean;
    onPath: boolean;
    isUser: boolean;
    user?: UserResult;

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
    metadata: ConnectionMetaData;
}

export interface SixDegreesConnection<T> {


    connections: { [hashtag: string]: T[] };
    paths: LinkPath<T>[];
    metadata: ConnectionMetaData;

}

export interface LinkPath<T> {
    path: { [key: number]: T };
    links: string[];

}
export interface ConnectionMetaData {
    time: string;
    calls: number;
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
            .map(this.hashDegreesToGraph)
            .subscribe(
                (graph: Graph) => {
                    this.hashGraphSub.next(graph);
                },
                (error) => {
                    console.log(error);
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

    getLatestUserData(): BehaviorSubject<Graph> {
        return this.userGraphSub;
    }

    getLatestHashData(): BehaviorSubject<Graph> {
        return this.hashGraphSub;
    }


    hashDegreesToGraph = (data: SixDegreesConnection<string>) => {
        const nodeMap = new Map<string, Node>();
        const linkMap = new Map<string, Link>();

        const nodes = this.createNodes(data, nodeMap);
        const links: Link[] = this.createLinks(data, linkMap);
        const [start, end] = this.markPath(data, nodeMap, linkMap);
        this.colorizeGraph(nodeMap, links, start);
        return { nodes, links, metadata: data.metadata, nodeMap, linkMap };
    }

    createNodes(data: SixDegreesConnection<UserResult | string>, nodeMap: Map<string, Node>) {
        const values = [].concat(...(Object.values(data.connections).concat(Object.keys(data.connections))));
        const nodes: Node[] = values.map((e) => {
            if (!e.screenName) {
                return { id: e, group: 1, isShown: true, onPath: false, isUser: false };
            } else {
                return { id: e.id, group: 1, isShown: true, onPath: false, user: e, isUser: true };
            }
        });
        nodes.forEach((e: Node, i: number) => {
            if (!nodeMap.has(e.id)) {
                nodeMap.set(e.id, e);
            }
        });
        return Array.from(nodeMap.values());

    }

    createLinks(data: SixDegreesConnection<string | UserResult>, linkMap: Map<string, Link>) {
        const links: Link[] = [];
        for (const element of Object.keys(data.connections)) {
            data.connections[element].forEach((c: any) => {
                let end: string = null;
                if (!c.screenName) {
                    end = c;
                } else {
                    end = c.id;
                }

                const link = { source: element, target: end, value: 1, onPath: false };
                const linkKey = `${link.source} ${link.target}`;
                const reverse = `${link.target} ${link.source}`;

                if (!linkMap.has(linkKey)) {
                    linkMap.set(linkKey, link);
                    links.push(link);

                }

                if (!linkMap.has(reverse)) {
                    linkMap.set(reverse, link);
                }
            });
        }
        return links;
    }

    pointToNodeID(nodeMap: Map<string, Node>, pathPoint: any) {


        const node = nodeMap.get(pathPoint);
        return node.id;

    }


    markPath(data: SixDegreesConnection<string | UserResult>, nodeMap: Map<string, Node>, linkMap: Map<string, Link>) {
        const paths = data.paths;
        let start: string = null;
        let end: string = null;
        paths.forEach(linkPath => {
            const pathPoints: string[] = Object.values(linkPath.path)
                .map((e: any) => {
                    if (e.screenName) {
                        return e.id;
                    } else {
                        return e;
                    }
                });

            start = this.pointToNodeID(nodeMap, pathPoints[0]);
            end = this.pointToNodeID(nodeMap, pathPoints[pathPoints.length - 1]);

            for (let i = 1; i < pathPoints.length; i++) {
                const sourceNode = pathPoints[i - 1];
                const targetNode = pathPoints[i];
                const linkKey = `${sourceNode} ${targetNode}`;
                const reverseKey = `${targetNode} ${sourceNode}`;
                const link = linkMap.get(linkKey) || linkMap.get(reverseKey);
                const url = (linkPath.links[i - 1]) ? (linkPath.links[i - 1]) : null;

                if (nodeMap.has(sourceNode) && nodeMap.has(targetNode)) {
                    nodeMap.get(sourceNode).onPath = true;
                    nodeMap.get(targetNode).onPath = true;
                }
                if (link) {
                    link.onPath = true;
                    link.value = 4;
                    link.linkUrl = url;
                }
            }
        });
        return [start, end];
    }

    colorizeGraph(nodeMap: Map<string, Node>, links: Link[], start: string) {
        const queue: string[] = [];
        const visited = new Set<string>();
        const startNode = nodeMap.get(start);
        if (!startNode) {
            return;
        }
        startNode.group = 0;
        visited.add(startNode.id);
        queue.push(startNode.id);

        while (queue.length > 0) {

            const curr = queue.shift();
            const currNode = nodeMap.get(curr);
            const neighbors = links.filter((e) => e.source === curr || e.target === curr);
            neighbors.forEach(n => {
                const target = nodeMap.get(n.target);
                if (!visited.has(n.target)) {
                    visited.add(n.target);
                    queue.push(n.target);
                    target.group = currNode.group + 1;
                }
                const source = nodeMap.get(n.source);
                if (!visited.has(n.source)) {
                    visited.add(n.source);
                    queue.push(n.source);
                    source.group = currNode.group + 1;
                }
            });
        }
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
}
