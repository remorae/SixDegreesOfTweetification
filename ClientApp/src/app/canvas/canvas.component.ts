import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import * as D3 from 'd3';
declare let d3: any;
@Component({
    selector: 'app-canvas',
    templateUrl: './canvas.component.html',
    styleUrls: ['./canvas.component.scss']
})
export class CanvasComponent implements OnInit {
    @ViewChild('canvas') canvas: ElementRef;
    canvasHeight = 600;
    canvasWidth = 600;
    ctx: CanvasRenderingContext2D;

    constructor() {

    }

    ngOnInit() {
        this.ctx = this.canvas.nativeElement.getContext('2d'); // WARN: No drawing API calls will work here

    }


    example() {
        const words = ['Hello', 'world', 'normally', 'you', 'want', 'more', 'words', 'than', 'this']
            .map(function (d) {
                return { text: d, size: 10 + Math.random() * 90 };
            });

        d3.layout.cloud().size([600, 600])
            .canvas(() => this.canvas.nativeElement)
            .words(words)
            .padding(5)
            .rotate(function () { return ~~(Math.random() * 2) * 90; }) //turns out ~~ just chops off everything to the right of the decimal
            .font('Impact')
            .fontSize(function (d) { return d.size; })
            .on('end', (words) => { console.log(JSON.stringify(words)); })
            .start();


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
