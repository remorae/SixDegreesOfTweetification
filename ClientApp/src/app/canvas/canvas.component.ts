import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import * as D3 from 'd3';
declare let d3: any;

export interface WeightedWord {

    text: string;
    size: number;
}
@Component({
    selector: 'app-canvas',
    templateUrl: './canvas.component.html',
    styleUrls: ['./canvas.component.scss']
})
export class CanvasComponent implements OnInit {
    @ViewChild('canvas') canvas: ElementRef;
    // canvasHeight = 600;
    //canvasWidth = 600;

    // [width]="canvasWidth" [height]="canvasHeight"
    ctx: CanvasRenderingContext2D;

    constructor() {

    }

    ngOnInit() {
        this.ctx = this.canvas.nativeElement.getContext('2d'); // WARN: No drawing API calls will work here

    }


    example() { // https://github.com/jasondavies/d3-cloud
        const words = ['Hello', 'world', 'normally', 'you', 'want', 'more', 'words', 'than', 'this',
            'lorem', 'ipsum', 'dolor', 'sit', 'amet', 'consectetur',
            'adipiscing', 'elit', 'curabitur', 'vel', 'hendrerit', 'libero',
            'eleifend', 'blandit', 'nunc', 'ornare', 'odio', 'ut',
            'orci', 'gravida', 'imperdiet', 'nullam', 'purus', 'lacinia',
            'a', 'pretium', 'quis', 'congue', 'praesent', 'sagittis',
            'laoreet', 'auctor', 'mauris', 'non', 'velit', 'eros',
            'dictum', 'proin', 'accumsan', 'sapien', 'nec', 'massa',
            'volutpat', 'venenatis', 'sed', 'eu', 'molestie', 'lacus',
            'quisque', 'porttitor', 'ligula', 'dui', 'mollis', 'tempus',
            'at', 'magna', 'vestibulum', 'turpis', 'ac', 'diam',
            'tincidunt', 'id', 'condimentum', 'enim', 'sodales', 'in',
            'hac', 'habitasse', 'platea', 'dictumst', 'aenean', 'neque',
            'fusce', 'augue', 'leo', 'eget', 'semper', 'mattis',
            'tortor', 'scelerisque', 'nulla', 'interdum', 'tellus', 'malesuada',
            'rhoncus', 'porta', 'sem', 'aliquet', 'et', 'nam',
            'suspendisse', 'potenti', 'vivamus', 'luctus', 'fringilla', 'erat',
            'donec', 'justo', 'vehicula', 'ultricies', 'varius', 'ante',
            'primis', 'faucibus', 'ultrices', 'posuere', 'cubilia', 'curae',
            'etiam', 'cursus', 'aliquam', 'quam', 'dapibus', 'nisl',
            'feugiat', 'egestas', 'class', 'aptent', 'taciti', 'sociosqu',
            'ad', 'litora', 'torquent', 'per', 'conubia', 'nostra',
            'inceptos', 'himenaeos', 'phasellus', 'nibh', 'pulvinar', 'vitae',
            'urna', 'iaculis', 'lobortis', 'nisi', 'viverra', 'arcu',
            'morbi', 'pellentesque', 'metus', 'commodo', 'ut', 'facilisis',
            'felis', 'tristique', 'ullamcorper', 'placerat', 'aenean', 'convallis',
            'sollicitudin', 'integer', 'rutrum', 'duis', 'est', 'etiam',
            'bibendum', 'donec', 'pharetra', 'vulputate', 'maecenas', 'mi',
            'fermentum', 'consequat', 'suscipit', 'aliquam', 'habitant', 'senectus',
            'netus', 'fames', 'quisque', 'euismod', 'curabitur', 'lectus',
            'elementum', 'tempor', 'risus', 'cras']
            .map(function (d): WeightedWord {
                return { text: d, size: 10 + Math.random() * 90 };
            });

        let fill: D3.ScaleOrdinal<string, string> = D3.scaleOrdinal(D3.schemeCategory10);
        d3.layout.cloud().size([600, 600])
            // .canvas(() => this.canvas.nativeElement)
            .words(words)
            .padding(5)
            .rotate(function () { return ~~(Math.random() * 2) * 45; })
            // turns out ~~ just chops off everything to the right of the decimal
            .font('Impact')
            .fontSize(function (d) { return d.size; })
            .on('end', (input) => {
                console.log(JSON.stringify(words));
                D3.select('body').append('svg')
                    .attr('width', 600)
                    .attr('height', 600)
                    .append('g')
                    .attr('transform', 'translate(' + 600 / 2 + ',' + 600 / 2 + ')')
                    .selectAll('text')
                    .data(words)
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

    drawWords(words) {

    }

    draw() {
        this.example();

    }


    drawImage() { // example code for drawing SVGs
        let image = new Image();
        image.onload = () => {

            this.ctx.drawImage(image, 0, 0);
        };

        image.src = './../../assets/united-states-state.svg';
    }




}
