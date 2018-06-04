import { Injectable } from '@angular/core';
import { SimulationLinkDatum, SimulationNodeDatum } from 'd3';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { UserResult } from '../models/UserResult';
import { EndpointService } from './endpoint.service';
import { map } from 'rxjs/operators/map';
import { AlertService } from './alert.service';
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
/**
 * @example Transforms the data for a connected graph into a format that D3.js can render as a force vertlet simulation.
 */
@Injectable()
export class GraphDataService {
    userGraphSub: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);
    hashGraphSub: BehaviorSubject<Graph> = new BehaviorSubject<Graph>(null);
    constructor(
        private endpoint: EndpointService,
        private alerts: AlertService
    ) {}

    /**
     * @example Fetches and runs the mapper function on a graph of hashtags.
     * @param hashtag1
     * @param hashtag2
     */
    getHashConnectionData(hashtag1: string, hashtag2: string): void {
        this.endpoint
            .getHashSixDegrees(hashtag1, hashtag2)
            .pipe(map(this.hashDegreesToGraph))
            .subscribe(
                (graph: Graph) => {
                    this.hashGraphSub.next(graph);
                },
                error => {
                    console.log(error);
                }
            );
    }
    /**
     * @example Fetches and runs the mapper function on a graph of users.
     * @param user1
     * @param user2
     */
    getUserConnectionData(user1: string, user2: string): void {
        this.endpoint
            .getUserSixDegrees(user1, user2)
            .pipe(map(this.sixDegreesToGraph))
            .subscribe(
                (graph: Graph) => {
                    this.userGraphSub.next(graph);
                },
                error => {
                    console.log(error);
                }
            );
    }
    /**
     * @example Provides a cached copy of the latest requested UserGraph
     */
    getLatestUserData(): BehaviorSubject<Graph> {
        return this.userGraphSub;
    }
    /**
     * @example Provides a cached copy of the latest requested HashGraph.
     */
    getLatestHashData(): BehaviorSubject<Graph> {
        return this.hashGraphSub;
    }

    hashDegreesToGraph = (data: SixDegreesConnection<string>) => {
        return this.mapToGraph(data, false);
    };
    mapToGraph = (
        data: SixDegreesConnection<string | UserResult>,
        isUserGraph: boolean
    ) => {
        const nodeMap = new Map<string, Node>();
        const linkMap = new Map<string, Link>();

        const nodes = this.createNodes(data, nodeMap, isUserGraph);
        const links: Link[] = this.createLinks(data, linkMap);
        if (!this.hasGraphPath(data)) {
            this.alerts.addError(
                'Unable to create path between start and destination!'
            );
            return null;
        }
        const start: string = isUserGraph
            ? (data.paths[0].path[0] as UserResult).id
            : data.paths[0].path[0].toString();
        this.colorizeGraph(nodeMap, links, start);
        return { nodes, links, metadata: data.metadata, nodeMap, linkMap };
    };

    /**
     * @example Checks if the returned graph has at least one path connecting the start and end.
     * @param data The graph data
     */
    hasGraphPath(data: SixDegreesConnection<string | UserResult>): boolean {
        return !!data.paths[0];
    }
    /**
     * @example Creates nodes from the graph data's connections entries, adds them to the nodeMap, and then
     *          marks any nodes in the graph data's path entries as being on the path.
     * @param data The graph data
     * @param nodeMap A Map of Nodes where the keys are the nodes's id property.
     * @param isUserGraph Whether or not the graph data corresponds to a graph of users or hashtags.
     */
    createNodes(
        data: SixDegreesConnection<UserResult | string>,
        nodeMap: Map<string, Node>,
        isUserGraph
    ): Node[] {
        const values = [].concat(
            ...Object.values(data.connections).concat(
                Object.keys(data.connections)
            )
        );
        const nodes: Node[] = values.map(e => {
            return {
                id: e,
                group: 1,
                isShown: true,
                onPath: false,
                isUser: isUserGraph
            };
        });
        nodes.forEach((e: Node, i: number) => {
            if (!nodeMap.has(e.id)) {
                nodeMap.set(e.id, e);
            }
        });

        this.createNodesFromPath(isUserGraph, data, nodeMap);
        return Array.from(nodeMap.values());
    }
    /**
     * @example Creates nodes out of the path portion of the graph data if they don't already exist, and marks all
     *          nodes in the path as such.
     * @param isUserGraph Whether the graph is a graph of users or not.
     * @param data The graph data
     * @param nodeMap The map of already existing nodes.
     */
    createNodesFromPath(
        isUserGraph: boolean,
        data: SixDegreesConnection<string | UserResult>,
        nodeMap: Map<string, Node>
    ): void {
        data.paths.forEach(linkPath => {
            const pathPoints: string[] = Object.values(linkPath.path).map(
                (e: any) => {
                    if (e.screenName) {
                        return e.id;
                    } else {
                        return e;
                    }
                }
            );

            for (let i = 0; i < pathPoints.length; i++) {
                const nodeName = pathPoints[i];
                if (nodeMap.has(nodeName)) {
                    const node = nodeMap.get(nodeName);
                    node.onPath = true;
                    node.user = isUserGraph
                        ? (linkPath.path[i] as UserResult)
                        : null;
                } else {
                    const node: Node = {
                        id: nodeName,
                        group: 1,
                        isShown: true,
                        onPath: true,
                        isUser: isUserGraph
                    };
                    node.user = isUserGraph
                        ? (linkPath.path[i] as UserResult)
                        : null;

                    nodeMap.set(node.id, node);
                }
            }
        });
    }
    /**
     * @example Creates Links between Nodes in the data connections, adds them to the map, then creates Links from the path data.
     * @param data data for the graph
     * @param linkMap map of Links
     */
    createLinks(
        data: SixDegreesConnection<string | UserResult>,
        linkMap: Map<string, Link>
    ): Link[] {
        for (const element of Object.keys(data.connections)) {
            data.connections[element].forEach((c: any) => {
                let end: string = null;
                if (!c.screenName) {
                    end = c;
                } else {
                    end = c.id;
                }

                const link = {
                    source: element,
                    target: end,
                    value: 1,
                    onPath: false
                };
                const linkKey = `${link.source} ${link.target}`;
                const reverse = `${link.target} ${link.source}`;

                if (!linkMap.has(linkKey) && !linkMap.has(reverse)) {
                    linkMap.set(linkKey, link);
                }
            });
        }
        this.addLinksFromPath(data, linkMap);
        return Array.from(linkMap.values());
    }
    /**
     * @param data the graph data
     * @param linkMap the map of Links
     */
    addLinksFromPath(
        data: SixDegreesConnection<string | UserResult>,
        linkMap: Map<string, Link>
    ): void {
        data.paths.forEach(linkPath => {
            const route: string[] = Object.values(linkPath.path).map(
                (e: any) => {
                    if (e.screenName) {
                        return e.id;
                    } else {
                        return e;
                    }
                }
            );

            for (let i = 1; i < route.length; i++) {
                const sourceNode = route[i - 1];
                const targetNode = route[i];
                this.createLinkIfAbsent(sourceNode, targetNode, linkMap);
            }
        });
    }
    /**
     *  @example Creates a path Link from the path data if it does not exist, and marks existing links as on the path.
     * @param sourceNode The previous Node in the path.
     * @param targetNode The next node in the path.
     * @param linkMap The map of existing links.
     */
    createLinkIfAbsent(
        sourceNode: string,
        targetNode: string,
        linkMap: Map<string, Link>
    ): void {
        const linkKey = `${sourceNode} ${targetNode}`;
        const reverseKey = `${targetNode} ${sourceNode}`;
        const link = linkMap.get(linkKey) || linkMap.get(reverseKey);
        if (link) {
            link.onPath = true;
            link.value = 4;
        } else {
            const newLink: Link = {
                source: sourceNode,
                target: targetNode,
                value: 4,
                onPath: true
            };

            linkMap.set(linkKey, newLink);
        }
    }
    /**
     * @example Marks how distant each node is from the start node via the group property
     * @param nodeMap The map of nodes
     * @param links The links in the graph
     * @param start The node id corresponding to the first search term.
     */
    colorizeGraph(
        nodeMap: Map<string, Node>,
        links: Link[],
        start: string
    ): void {
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
            const neighbors = links.filter(
                e => e.source === curr || e.target === curr
            );
            neighbors.forEach(n => {
                const target = nodeMap.get(n.target);
                if (!visited.has(n.target)) {
                    visited.add(n.target);
                    queue.push(n.target);
                    target.group = currNode.group + 1;
                } else if (target.group > currNode.group + 1) {
                    target.group = currNode.group + 1;
                }
                const source = nodeMap.get(n.source);
                if (!visited.has(n.source)) {
                    visited.add(n.source);
                    queue.push(n.source);
                    source.group = currNode.group + 1;
                } else if (source.group > currNode.group + 1) {
                    source.group = currNode.group + 1;
                }
            });
        }
    }
    /**
     *  @example An unused utility function that removes elements that match a predicate from an array.
     * @param array
     * @param filter
     */
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
        return this.mapToGraph(data, true);
    };
}
