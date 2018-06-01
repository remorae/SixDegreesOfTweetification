import {
    Component,
    OnInit,
    Input,
    OnChanges,
    SimpleChanges,
    EventEmitter,
    Output
} from '@angular/core';
import * as D3 from 'd3';
import { CloudDataService } from '../services/cloud-data.service';
import { CloudState } from '../word-cloud-page/word-cloud-page.component';

declare let d3: any;

export interface WeightedWord {
    text: string;
    size: number;
    occurrence: number;
}
/**
 *  @example Draws the word cloud, based on the input array of WeightedWords. Words will be oriented and sized in the cloud based on the word-fitting library
 *      written by Jason Davies, and then rendered as SVG elements with D3.js
 */
@Component({
    selector: 'app-cloud-bottle',
    templateUrl: './cloud-bottle.component.html',
    styleUrls: ['./cloud-bottle.component.scss']
})
export class CloudBottleComponent implements OnInit, OnChanges {
    @Input() words: WeightedWord[];
    cloudWords: WeightedWord[];
    cloudWidth: number;
    cloudHeight: number;
    @Output() cloudDrawn: EventEmitter<string> = new EventEmitter<string>(true);
    constructor() {}

    /**
     * @example Acquires the dimensions of the svg element on component initialization
     */
    ngOnInit(): void {
        const temp = document.querySelector('div.bottle') as HTMLDivElement;
        this.cloudHeight = temp.getBoundingClientRect().height;
        this.cloudWidth = temp.getBoundingClientRect().width;
    }
    /**
     * @example Whenever the input array of WeightedWords is changed, delete the previous wordcloud and render a new one.
     *      Broadcast whether any work was done via an EventEmitter.
     * @param changes Tracks changes made to the @Input array of words
     */
    ngOnChanges(changes: SimpleChanges): void {
        const wordChange = changes['words'];
        let drawn: CloudState = 'unchanged';
        if (
            wordChange.previousValue &&
            wordChange.previousValue.length !== wordChange.currentValue.length
        ) {
            this.cloudWords = this.words.map(word => ({ ...word }));
            this.dropCloud();
            this.buildLayout();
            drawn = 'empty';
            if (
                wordChange.currentValue.length < wordChange.previousValue.length
            ) {
                drawn = 'empty';
            }
        }

        if (wordChange.isFirstChange()) {
            drawn = 'empty';
        }
        this.cloudDrawn.emit(drawn);
    }

    /**
     * @example Fit as many words as possible into the cloud. Once the orientation data is processed, render the cloud.
     */
    buildLayout(): void {
        // https://github.com/jasondavies/d3-cloud for cloud generator
        d3.layout
            .cloud()
            .size([this.cloudWidth, this.cloudHeight])
            .words(this.cloudWords)
            .padding(1)
            //   .rotate(() => ~~(Math.random() * 2) * 45) // the default rotate function may be more visually appealing
            // turns out ~~ just chops off everything to the right of the decimal
            .font('Impact')
            .fontSize(d => d.size)
            .on('end', _ => {
                // doesn't work without this arrow function
                this.createCloud();
            })
            .start();
    }
    /**
     * @example Delete all elements corresponding to the wordcloud, then broadcast that the cloud has been cleared.
     */
    dropCloud(): void {
        D3.select('div.bottle')
            .selectAll('*')
            .remove();
        this.cloudDrawn.emit('empty');
    }
    /**
     * @example Convert the word orientation data into SVG text elements. Elements are colored based on a D3 Categorical Color Scale.
     */
    createCloud(): void {
        const fill: D3.ScaleOrdinal<string, string> = D3.scaleOrdinal(
            D3.schemeCategory10
        );
        D3.select('div.bottle')
            .append('svg')
            .attr('class', 'stylable')
            .attr('height', '100%')
            .attr('width', '100%')
            .append('g')
            .attr(
                'transform',
                `translate(${this.cloudWidth / 2},${this.cloudHeight / 2})`
            )
            .selectAll('text')
            .data(this.cloudWords)
            .enter()
            .append('text')
            .style('font-size', (d: WeightedWord) => d.size + 'px')
            .style('font-family', 'Impact')
            .style('fill', (d, i) => fill(i.toString()))
            .attr('text-anchor', 'middle')
            .attr(
                'transform',
                (d: any) =>
                    'translate(' + [d.x, d.y] + ')rotate(' + d.rotate + ')'
            )
            .text((d: WeightedWord) => d.text);

        const cloudy = document.querySelector('svg.stylable') as SVGElement;
        cloudy.setAttribute(
            'style',
            'animation: grow-fade-in 0.75s cubic-bezier(0.17, 0.67, 0, 1)'
        ); // cubic-bezier(0.17, 0.67, 0.2, 1)
    }
}
