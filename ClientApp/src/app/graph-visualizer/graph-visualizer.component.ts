import { Component, OnInit } from '@angular/core';
import * as D3 from 'd3';
import { GraphDataService } from '../services/graph-data.service';
@Component({
    selector: 'app-graph-visualizer',
    templateUrl: './graph-visualizer.component.html',
    styleUrls: ['./graph-visualizer.component.scss']
})
export class GraphVisualizerComponent implements OnInit {

    constructor(private graphData: GraphDataService) {

    }

    ngOnInit() {

        this.drawGraph();
    }

    drawGraph() {
        const maxForceDistance = 250;
        const svg = D3.select('svg.target'),
            width = 900, // +svg.attr('width'),
            height = 900; // +svg.attr('height');

        const color = D3.scaleOrdinal(D3.schemeCategory10);

        const simulation: any = D3.forceSimulation()  // may need to remove
            .force('link', D3.forceLink().id((d: { id }) => d.id))
            .force('charge', D3.forceManyBody().distanceMax(maxForceDistance))

            .force('center', D3.forceCenter(width / 2, height / 2));
        let graph: { links, nodes } = this.graphData.mapUserDataToLinks(); // this probably needs to go back to D3.json
        let link = svg.append('g')
            //  .attr('class', 'links')
            .attr('stroke', '#999')
            .attr('stroke-opacity', '0.6')
            .selectAll('line')
            .data(graph.links)
            .enter().append('line')
            .attr('stroke-width', (d: { value }) => Math.sqrt(d.value));

        let node = svg.append('g')
            //   .attr('class', 'nodes')
            .selectAll('circle')
            .data(graph.nodes)
            .enter().append('circle')
            .attr('r', (d: { group }) => { return d.group !== 0 ? 5 : 10})
            .attr('fill', (d: { group }) => color(d.group))
            .call(D3.drag()
                .on('start', dragstarted)
                .on('drag', dragged)
                .on('end', dragended));

        node.append('title')
            .text((d: { id }) => d.id);

        simulation
            .nodes(graph.nodes)
            .on('tick', ticked);

        simulation.force('link').links(graph.links);




        function ticked() {
            link
                .attr('x1', (d: { source }) => d.source.x)
                .attr('y1', (d: { source }) => d.source.y)
                .attr('x2', (d: { target }) => d.target.x)
                .attr('y2', (d: { target }) => d.target.y);

            node
                .attr('cx', (d: { x }) => d.x)
                .attr('cy', (d: { y }) => d.y);
        }

        function dragstarted(d) {
            if (!D3.event.active) simulation.alphaTarget(0.3).restart();
            d.fx = d.x;
            d.fy = d.y;
        }

        function dragged(d) {
            d.fx = D3.event.x;
            d.fy = D3.event.y;
        }

        function dragended(d) {
            if (!D3.event.active) simulation.alphaTarget(0);
            d.fx = null;
            d.fy = null;
        }

    }



}
