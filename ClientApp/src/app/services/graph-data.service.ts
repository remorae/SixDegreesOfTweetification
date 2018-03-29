import { Injectable } from '@angular/core';
import { EndpointService } from './endpoint.service';
import { TestData } from './testdata';


export interface Node {
    id: string;
    group: number;
}

export interface Link {
    source: string;
    target: string;
    value: number;
}

@Injectable()
export class GraphDataService {
    testData: TestData = new TestData();

    constructor(private endpoint: EndpointService) {
    }

    mapUserDataToLinks() {
        const data = this.testData.getUserData();
        const nodes: Node[] = Object.keys(data)
                                .map((user) => ({ id: user, group: data[user].distance + 1 }));

        const links: Link[] = this.createLinks(data, nodes);
        return { nodes: nodes, links: links };
    }


    createLinks(data, nodes: Node[]) {
        let links: Link[] = [];
        //const root: string = nodes.find((a)=> a.group === 0);

        nodes.forEach(element => {
            const connections = data[element.id].connections || [];

            connections.forEach(connection => {
                links.push({ source: element.id, target: connection.screenName, value: 1 })
            });

        });

        return links;
    }

}
