import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, HostListener } from '@angular/core';
import * as D3 from 'd3';
import { ForceLink, forceCenter, forceX, forceY } from 'd3';
import { ConnectionMetaData, Link, Node } from '../services/graph-data.service';
@Component({
    selector: 'app-graph-visualizer',
    templateUrl: './graph-visualizer.component.html',
    styleUrls: ['./graph-visualizer.component.scss']
})
export class GraphVisualizerComponent implements OnInit, OnChanges, OnDestroy {

    @Input() graph: { links, nodes, metadata: ConnectionMetaData, nodeMap: Map<string, Node>, linkMap: Map<string, Link> };
    @Input() whatToWhat: string;
    headerContent = 'Left-click a node to view more information. Click and drag to move it around!';
    svgHeight = 900;
    svgWidth = 900;
    highlightedIndex = -1;
    @Output() modalOpen: EventEmitter<boolean> = new EventEmitter<boolean>(true);
    readonly maxForceDistance = 250;
    clickedDatum;
    simulation: D3.Simulation<Node, Link>;
    constructor() {

    }

    onDragEvent(e: Event) {
        e.preventDefault();
    }
    ngOnInit() {
    }

    ngOnDestroy() {
        this.deleteGraph();
        this.headerContent = '';
    }

    onXClick() {
        this.deleteGraph();
        this.modalOpen.emit(false);
    }

    ngOnChanges(changes: SimpleChanges) {
        const graphChange = changes['graph'];

        if (graphChange.currentValue) {
            this.deleteGraph();
            setTimeout(() => { this.drawGraph(); }, 500);
        }
    }

    deleteGraph() {
        D3.select('svg.graph-container').selectAll('*').remove();
    }

    onSVGClick(event: MouseEvent) {
        const e = event.target as SVGElement;
        const dim = e.getBoundingClientRect();
        const x = event.clientX - dim.left;
        const y = event.clientY - dim.top;
        this.simulation.alpha(0.3);
        this.simulation.force('clickX', forceX(x).strength(1.0));
        this.simulation.force('clickY', D3.forceY(y).strength(1.0));
        this.simulation.restart();
    }

    onSVGRelease(event: MouseEvent) {
        this.simulation.force('clickX', null);
        this.simulation.force('clickY', null);
    }


    drawGraph() { // credit to https://bl.ocks.org/mbostock/4062045
        const svg = D3.select('svg.graph-container');
        const temp = document.querySelector('svg.graph-container') as SVGElement;
        this.svgHeight = +temp.getBoundingClientRect().height;
        this.svgWidth = +temp.getBoundingClientRect().width;
        const filteredNodes: Node[] = this.filterNodes();
        const filteredLinks: Link[] = this.filterLinks(filteredNodes);
        this.simulation = this.createSimulation();

        this.simulation
            .nodes(filteredNodes)
            .on('tick', ticked);

        this.simulation.force<ForceLink<Node, Link>>('link').links(filteredLinks);

        const linkSelection = this.createLinksGroup(svg, filteredLinks);
        const nodeSelection = this.createNodesGroup(svg, this.simulation, filteredNodes);
        const textSelection = this.createTextGroup(svg, filteredNodes);
        nodeSelection.append('title')
            .text((d: Node) => (d.user) ? d.user.screenName : d.id);

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
    }

    filterLinks(filteredNodes: Node[]): Link[] {
        return this.graph.links;
    }

    createSimulation(): D3.Simulation<Node, Link> {

        return D3.forceSimulation<Node, Link>()
            .force('link', D3.forceLink().id((d: { id }) => d.id))
            .force('charge', D3.forceManyBody().distanceMax(this.maxForceDistance))
            .force('center', D3.forceCenter(this.svgWidth / 2, this.svgHeight / 2))
            .force('collision', D3.forceCollide().radius((d: Node) => d.onPath ? 10 : 5));
    }

    createLinksGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, links: Link[]): D3.Selection<D3.BaseType, {}, D3.BaseType, {}> {
        const color = D3.scaleOrdinal(D3.schemeCategory10);
        return svg.append('g')
            .attr('class', 'links')
            .selectAll('line')
            .data(links)
            .enter()
            // .append('a')
            // .attr('xlink:href', (l: Link) => l.linkUrl) // Allow this one day, when we have content filtering
            // .attr('target', '_blank')
            .append('line')
            .attr('stroke-width', (d: Link) => Math.sqrt(d.value))
            .attr('stroke', (d: Link) => d.onPath ? 'black' : '#999') // TODO: colorize based on target node.
            .attr('stroke-opacity', (d: Link) => d.onPath ? '1' : '0.6');
    }

    createTextGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, nodes: Node[]) {
        return svg.append('g')
            .attr('class', 'node-text')
            .selectAll('text')
            .data(nodes.filter((e: Node) => e.onPath))
            .enter().append('text')
            .attr('pointer-events', 'none')
            .attr('text-anchor', 'middle')
            .attr('dx', 0)
            .attr('dy', '5px')
            .text((d) => (d.onPath) ? d.group : '');
    }



    createNodesGroup(svg: D3.Selection<D3.BaseType, {}, HTMLElement, any>, simulation: D3.Simulation<Node, Link>, nodes) {
        const color = D3.scaleOrdinal(D3.schemeCategory10);
        return svg.append('g')
            .attr('class', 'nodes')
            .selectAll('circle')
            .data(nodes)
            .enter().append('circle')
            .attr('r', (d: Node) => d.onPath ? 10 : 5)
            .attr('fill', (d: Node) => color(d.group.toString()))
            .on('click', (d, i, selection) => {
                this.highlightNode(d, i, selection, color);
            })
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

    highlightNode(d, i: number, selection, color) {
        if (this.highlightedIndex >= 0) {
            const reference = selection[this.highlightedIndex];
            const data = reference.__data__;
            D3.select(reference)
                .attr('stroke', '')
                .attr('stroke-width', 0)
                .attr('fill', color(data.group.toString()));
        }

        this.highlightedIndex = i;
        const newHighlight = selection[i];
        const newData = newHighlight.__data__;
        D3.select(newHighlight)
            .attr('stroke', '#1dcaff')
            .attr('fill', '#1dcaff')
            .attr('stroke-width', '3');

        this.updateCardContent(newData);
    }

    updateCardContent(data) {
        this.clickedDatum = data;
    }

}
