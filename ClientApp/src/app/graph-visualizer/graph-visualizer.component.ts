import { Component, OnInit, Input, OnChanges, SimpleChange, SimpleChanges } from '@angular/core';
import * as D3 from 'd3';
import { Node, Link, Graph, GraphDataService } from '../services/graph-data.service';
import { ForceLink } from 'd3';
@Component({
    selector: 'app-graph-visualizer',
    templateUrl: './graph-visualizer.component.html',
    styleUrls: ['./graph-visualizer.component.scss']
})
export class GraphVisualizerComponent implements OnInit, OnChanges {

    @Input() graph: { links, nodes };
    readonly svgHeight = 900;
    readonly svgWidth = 900;
    readonly maxForceDistance = 250;
    constructor() {

    }

    ngOnInit() {


    }

    ngOnChanges(changes: SimpleChanges) {
        const graphChange = changes['graph'];
        if (graphChange.currentValue) {
            this.drawGraph();
           // this.deleteGraph();
        }
    }

    deleteGraph() {
            D3.select('svg.graph-container').selectAll('*').remove();
    }

    createSimulation(): D3.Simulation<Node, Link> {

        return D3.forceSimulation<Node, Link>()
            .force('link', D3.forceLink().id((d: { id }) => d.id))
            .force('charge', D3.forceManyBody().distanceMax(this.maxForceDistance))
            .force('center', D3.forceCenter(this.svgWidth / 2, this.svgHeight / 2));
    }

    createLinksGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>): D3.Selection<D3.BaseType, {}, D3.BaseType, {}> {
        return svg.append('g')
            .attr('class', 'links')
            .attr('stroke', '#999')
            .attr('stroke-opacity', '0.6')
            .selectAll('line')
            .data(this.graph.links)
            .enter().append('line')
            .attr('stroke-width', (d: Link) => Math.sqrt(d.value));
    }

    createNodesGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, simulation: D3.Simulation<Node, Link>) {
        const color = D3.scaleOrdinal(D3.schemeCategory10);
        return svg.append('g')
            .attr('class', 'nodes')
            .selectAll('circle')
            .data(this.graph.nodes)
            .enter().append('circle')
            .attr('r', (d: Node) => d.group ? 5 : 10)
            .attr('fill', (d: Node) => color(d.group.toString()))
            .call(D3.drag()
                .on('start', dragstarted)
                .on('drag', dragged)
                .on('end', dragended));

        function dragstarted(d) {
            if (!D3.event.active) {
                simulation.alphaTarget(0.3).restart();
            }
            d.fx = d.x;
            d.fy = d.y;
        }

        function dragged(d) {
            d.fx = D3.event.x;
            d.fy = D3.event.y;
        }

        function dragended(d) {
            if (!D3.event.active) {
                simulation.alphaTarget(0);
            }
            d.fx = null;
            d.fy = null;
        }
    }
    drawGraph() {
        const svg = D3.select('svg.graph-container');
        const simulation = this.createSimulation();
        const links = this.createLinksGroup(svg);
        const nodes = this.createNodesGroup(svg, simulation);

        nodes.append('title')
            .text((d: Node) => `User: ${d.id} Distance: ${d.group}`);

        simulation
            .nodes(this.graph.nodes)
            .on('tick', ticked);

        simulation.force<ForceLink<Node, Link>>('link').links(this.graph.links);

        function ticked() {
            links
                .attr('x1', (d: { source }) => d.source.x)
                .attr('y1', (d: { source }) => d.source.y)
                .attr('x2', (d: { target }) => d.target.x)
                .attr('y2', (d: { target }) => d.target.y);

            nodes
                .attr('cx', (d: { x }) => d.x)
                .attr('cy', (d: { y }) => d.y);
        }


    }



}
