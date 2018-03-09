import { Component, OnInit } from '@angular/core';
import * as D3 from 'd3';
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
export class CloudBottleComponent implements OnInit {

    words: WeightedWord[];
    cloudWidth: string;
    cloudHeight: string;
    constructor(private cloudData: CloudDataService) { }

    ngOnInit() {
        this.cloudHeight = '600';
        this.cloudWidth = '940';
        this.words = this.cloudData.init();
        this.buildLayout();
    }

    buildLayout() {

        d3.layout.cloud().size([this.cloudWidth, this.cloudHeight])
            .words(this.words)
            .padding(1)
            .rotate(function () { return ~~(Math.random() * 2) * 45; })
            // turns out ~~ just chops off everything to the right of the decimal
            .font('Impact')
            .fontSize(function (d) { return d.size; })
            .on('end', (input) => { // doesn't work without this arrow function, dunno why
                this.createCloud(input);

            })
            .start();
    }
    createCloud(input) {
        const fill: D3.ScaleOrdinal<string, string> = D3.scaleOrdinal(D3.schemeCategory10);
        D3.select('.bottle')
            .append('svg')
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
            .text((d: WeightedWord) =>  d.text);
    }
    example() { // https://github.com/jasondavies/d3-cloud

        //let blues : D3.ScaleOrdinal<string, string> = D3.scaleOrdinal(D3.schemeBlues.);
        //  D3.schemeBlues
        //  D3.schemeCategory10

        let fill: D3.ScaleOrdinal<string, string> = D3.scaleOrdinal(D3.schemeCategory10);
        d3.layout.cloud().size([600, 600])
            // .canvas(() => this.canvas.nativeElement)
            .words(this.words)
            .padding(5)
            .rotate(function () { return ~~(Math.random() * 2) * 45; })
            // turns out ~~ just chops off everything to the right of the decimal
            .font('Impact')
            .fontSize(function (d) { return d.size; })
            .on('end', (input) => {
                // console.log(JSON.stringify(words));
                D3.select('.bottle')
                    .append('svg')
                    .attr('width', 600)
                    .attr('height', 600)
                    .append('g')
                    .attr('transform', 'translate(' + 600 / 2 + ',' + 600 / 2 + ')')
                    .selectAll('text')
                    .data(this.words)
                    .enter().append('text')
                    .style('font-size', function (d: WeightedWord) { return d.size + 'px'; })
                    .style('font-family', 'Impact')
                    .style('fill', function (d, i) { return fill(i.toString()); }) // changed 'i' to 'i.toString()'
                    .attr('text-anchor', 'middle')
                    .attr('transform', function (d: any) { //changed type to any
                        return 'translate(' + [d.x, d.y] + ')rotate(' + d.rotate + ')';
                    })
                    .text(function (d: WeightedWord) { return d.text; });
            })
            .start();






    }


}
