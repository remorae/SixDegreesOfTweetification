import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import * as D3 from 'd3';
import * as cloud from 'd3-cloud';
import { CloudDataService } from '../services/cloud-data.service';
declare let d3: any;

export interface WeightedWord {
    text: string;
    size: number;
}


@Component({
    selector: 'app-cloud-bottle',
    templateUrl: './cloud-bottle.component.html',
    styleUrls: ['./cloud-bottle.component.scss']
})
export class CloudBottleComponent implements OnInit, OnChanges {

    @Input() words: WeightedWord[];
    cloudWidth: string;
    cloudHeight: string;
    constructor() { }

    ngOnInit() {
        this.cloudHeight = '600';
        this.cloudWidth = '940';
    }
    ngOnChanges(changes: SimpleChanges) {
        const wordChange = changes['words'];
        if (wordChange.previousValue && wordChange.previousValue.length !== wordChange.currentValue.length) {
            this.dropCloud();
            this.buildLayout();
        }
    }

    buildLayout() {  // https://github.com/jasondavies/d3-cloud for cloud generator
        d3.layout.cloud().size([this.cloudWidth, this.cloudHeight])
            .words(this.words)
            .padding(1)
            //   .rotate(() => ~~(Math.random() * 2) * 45) // the default rotate function may be more visually appealing
            // turns out ~~ just chops off everything to the right of the decimal
            .font('Impact')
            .fontSize((d) => d.size)
            .on('end', (input) => { // doesn't work without this arrow function
                this.createCloud(input);

            })
            .start();
    }

    dropCloud() {
        D3.select('svg.removable').remove();
        //  map is necessary to avoid subsequent calls messing with the attached sprite masks
        this.words = this.words.map((word) => ({ text: word.text, size: word.size }));
    }
    createCloud(input) {
        const fill: D3.ScaleOrdinal<string, string> = D3.scaleOrdinal(D3.schemeCategory10);
        D3.select('.bottle')
            .append('svg')
            .attr('class', 'removable')
            .attr('width', this.cloudWidth)
            .attr('height', this.cloudHeight)
            .append('g')
            .attr('transform', 'translate(' + parseInt(this.cloudWidth, 10) / 2 + ',' + parseInt(this.cloudHeight, 10) / 2 + ')')
            .selectAll('text')
            .data(this.words)
            .enter().append('text')
            .style('font-size', (d: WeightedWord) => d.size + 'px')
            .style('font-family', 'Impact')
            .style('fill', (d, i) => fill(i.toString())) // changed 'i' to 'i.toString()'
            .attr('text-anchor', 'middle')
            .attr('transform', (d: any) => // changed type to any
                'translate(' + [d.x, d.y] + ')rotate(' + d.rotate + ')'
            )
            .text((d: WeightedWord) => d.text);
    }
}
