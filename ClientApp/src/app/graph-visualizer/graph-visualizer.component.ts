import { Component, OnInit, Input, OnChanges, SimpleChange, SimpleChanges, HostListener } from '@angular/core';
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

    onDragEvent(e: Event) {
        e.preventDefault();
    }
    ngOnInit() {


    }

    ngOnChanges(changes: SimpleChanges) {
        const graphChange = changes['graph'];

        if (graphChange.currentValue) {
            this.deleteGraph();
            this.drawGraph();
        }
    }

    deleteGraph() {
        D3.select('svg.graph-container').selectAll('*').remove();
    }


    drawGraph() { // credit to https://bl.ocks.org/mbostock/4062045
        const svg = D3.select('svg.graph-container');
        const filteredNodes: Node[] = this.filterNodes();
        const filteredLinks: Link[] = this.filterLinks(filteredNodes);
        const simulation = this.createSimulation();

        simulation
            .nodes(filteredNodes)
            .on('tick', ticked);

        simulation.force<ForceLink<Node, Link>>('link').links(filteredLinks);

        // D3.timeout(() => {
        //     if (filteredNodes.length >= 1000) {
        //         simulation.stop();

        //         for (let i = 0; i < Math.ceil(Math.log(simulation.alphaMin()) / Math.log(1 - simulation.alphaDecay())); i++) {
        //             simulation.tick();
        //         }
        //     }
        // });

        const linkSelection = this.createLinksGroup(svg, filteredLinks);
        const nodeSelection = this.createNodesGroup(svg, simulation, filteredNodes);
        const textSelection = this.createTextGroup(svg, filteredNodes);
        nodeSelection.append('title')
            .text((d: Node) => `User: ${d.id} Distance: ${d.group}`);

        function ticked() {
            linkSelection
                .attr('x1', (d: { source }) => d.source.x)
                .attr('y1', (d: { source }) => d.source.y)
                .attr('x2', (d: { target }) => d.target.x)
                .attr('y2', (d: { target }) => d.target.y);

            nodeSelection
                .attr('cx', (d: { x }) => d.x)
                .attr('cy', (d: { y }) => d.y);

            textSelection
                .attr('x', (d) => d.x)
                .attr('y', (d) => d.y);
        }
    }

    filterNodes(): Node[] {
        return this.graph.nodes;
        //return this.graph.nodes.filter((n) => n.isShown);
    }

    filterLinks(filteredNodes: Node[]): Link[] {
        return this.graph.links;
        // return this.graph.links
        //     .filter((l) => filteredNodes.some((n) => n.id === l.source) && filteredNodes.some(n => n.id === l.target));
    }

    createSimulation(): D3.Simulation<Node, Link> {

        return D3.forceSimulation<Node, Link>()
            .force('link', D3.forceLink().id((d: { id }) => d.id))
            .force('charge', D3.forceManyBody().distanceMax(this.maxForceDistance))
            .force('center', D3.forceCenter(this.svgWidth / 2, this.svgHeight / 2));
    }

    createLinksGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, links: Link[]): D3.Selection<D3.BaseType, {}, D3.BaseType, {}> {

        return svg.append('g')
            .attr('class', 'links')
            .attr('stroke', '#999')
            .attr('stroke-opacity', '0.6')
            .selectAll('line')
            .data(links)
            .enter().append('line')
            .attr('stroke-width', (d: Link) => Math.sqrt(d.value));
    }

    createTextGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, nodes: Node[]) {
        return svg.append('g')
            .attr('class', 'node-text')
            .selectAll('text')
            .data(nodes)
            .enter().append('text')
            .attr('pointer-events', 'none')
            //.attr('class', 'unselectable')
            .attr('text-anchor', 'middle')
            .attr('dx', 0)
            .attr('dy', '.30rem')
            //.style("font-size", "50px")
            //.style("fill", "black")

            .text((d) => (d.expandable) ? '!' : '');
    }

    createNodesGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, simulation: D3.Simulation<Node, Link>, nodes) {
        const color = D3.scaleOrdinal(D3.schemeCategory10);
        return svg.append('g')
            .attr('class', 'nodes')
            .selectAll('circle')
            .data(nodes)
            .enter().append('circle')
            .attr('r', (d: Node) => d.group ? 5 : 10)
            .attr('fill', (d: Node) => color(d.group.toString()))
            .call(D3.drag()
                .on('start', dragbegin)
                .on('drag', dragging)
                .on('end', dragend));

        function dragbegin(d) {
            if (!D3.event.active) {
                simulation.alphaTarget(0.3).restart();
            }
            d.fx = d.x;
            d.fy = d.y;
        }

        function dragging(d) {
            d.fx = D3.event.x;
            d.fy = D3.event.y;
        }

        function dragend(d) {
            if (!D3.event.active) {
                simulation.alphaTarget(0);
            }
            d.fx = null;
            d.fy = null;
        }
    }

}
