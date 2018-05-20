import {
    Component,
    OnInit,
    ViewChild,
    ElementRef,
    AfterViewInit
} from '@angular/core';

@Component({
    selector: 'app-canvas',
    templateUrl: './canvas.component.html',
    styleUrls: ['./canvas.component.scss']
})
export class CanvasComponent implements OnInit, AfterViewInit {
    @ViewChild('canvas') canvas: ElementRef;
    canvasHeight = 600;
    canvasWidth = 600;

    ctx: CanvasRenderingContext2D;

    constructor() {}

    ngOnInit() {
        this.ctx = this.canvas.nativeElement.getContext('2d'); // WARN: No drawing API calls will work here
    }

    ngAfterViewInit() {
        this.draw();
    }

    draw() {
        this.drawImage();
    }

    drawImage() {
        // example code for drawing SVGs
        const image = new Image();
        image.onload = () => {
            this.ctx.drawImage(image, 0, 0);
        };

        image.src = './../../assets/united-states-state.svg';
    }
}
