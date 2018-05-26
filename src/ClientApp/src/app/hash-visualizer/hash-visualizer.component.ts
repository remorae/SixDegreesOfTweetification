import { Component, OnInit, AfterViewInit } from '@angular/core';
import { select } from 'd3-selection';
import { selectAll } from 'd3-selection';
import { timer } from 'd3';
@Component({
    selector: 'app-hash-visualizer',
    templateUrl: './hash-visualizer.component.html',
    styleUrls: ['./hash-visualizer.component.scss']
})
export class HashVisualizerComponent implements OnInit, AfterViewInit {
    constructor() {}
    ngOnInit() {}

    ngAfterViewInit() {
        const width = 960,
            height = 500,
            radius = 80,
            x = Math.sin(2 * Math.PI / 3),
            y = Math.cos(2 * Math.PI / 3);

        let offset = 0;
        const speed = 4;
        const start = Date.now();

        const svg = select('div.gearbox')
            .append('svg')
            .attr('width', width)
            .attr('height', height)
            .append('g')
            .attr(
                'transform',
                'translate(' + width / 2 + ',' + height / 2 + ')scale(.55)'
            )
            .append('g');

        const frame = svg.append('g').datum({ radius: Infinity });

        frame
            .append('g')
            .attr('class', 'annulus')
            .datum({ teeth: 80, radius: -radius * 5, annulus: true })
            .append('path')
            .attr('d', gear);

        frame
            .append('g')
            .attr('class', 'sun')
            .datum({ teeth: 16, radius: radius })
            .append('path')
            .attr('d', gear);

        frame
            .append('g')
            .attr('class', 'planet')
            .attr('transform', 'translate(0,-' + radius * 3 + ')')
            .datum({ teeth: 32, radius: -radius * 2 })
            .append('path')
            .attr('d', gear);

        frame
            .append('g')
            .attr('class', 'planet')
            .attr(
                'transform',
                'translate(' + -radius * 3 * x + ',' + -radius * 3 * y + ')'
            )
            .datum({ teeth: 32, radius: -radius * 2 })
            .append('path')
            .attr('d', gear);

        frame
            .append('g')
            .attr('class', 'planet')
            .attr(
                'transform',
                'translate(' + radius * 3 * x + ',' + -radius * 3 * y + ')'
            )
            .datum({ teeth: 32, radius: -radius * 2 })
            .append('path')
            .attr('d', gear);

        selectAll('input[name=reference]')
            .data([radius * 5, Infinity, -radius])
            .on('change', function(radius1) {
                const radius0 = frame.datum().radius,
                    angle = (Date.now() - start) * speed;
                frame.datum({ radius: radius1 });
                svg.attr(
                    'transform',
                    'rotate(' +
                        (offset += angle / radius0 - angle / radius1) +
                        ')'
                );
            });

        // D3.selectAll('input[name=speed]')
        //     .on('change', function () { speed = +this.value; });

        function gear(d) {
            let r3;
            const n = d.teeth,
                r2 = Math.abs(d.radius);
            let r0 = r2 - 8;
            let r1 = r2 + 8;
            const da = Math.PI / n;
            let a0 = -Math.PI / 2 + (d.annulus ? Math.PI / n : 0),
                i = -1;
            r3 = d.annulus ? ((r3 = r0), (r0 = r1), (r1 = r3), r2 + 20) : 20;
            const path = ['M', r0 * Math.cos(a0), ',', r0 * Math.sin(a0)];
            while (++i < n) {
                path.push(
                    'A',
                    r0,
                    ',',
                    r0,
                    ' 0 0,1 ',
                    r0 * Math.cos((a0 += da)),
                    ',',
                    r0 * Math.sin(a0),
                    'L',
                    r2 * Math.cos(a0),
                    ',',
                    r2 * Math.sin(a0),
                    'L',
                    r1 * Math.cos((a0 += da / 3)),
                    ',',
                    r1 * Math.sin(a0),
                    'A',
                    r1,
                    ',',
                    r1,
                    ' 0 0,1 ',
                    r1 * Math.cos((a0 += da / 3)),
                    ',',
                    r1 * Math.sin(a0),
                    'L',
                    r2 * Math.cos((a0 += da / 3)),
                    ',',
                    r2 * Math.sin(a0),
                    'L',
                    r0 * Math.cos(a0),
                    ',',
                    r0 * Math.sin(a0)
                );
            }
            path.push(
                'M0,',
                -r3,
                'A',
                r3,
                ',',
                r3,
                ' 0 0,0 0,',
                r3,
                'A',
                r3,
                ',',
                r3,
                ' 0 0,0 0,',
                -r3,
                'Z'
            );
            return path.join('');
        }

        timer(function() {
            const angle = (Date.now() - start) * speed,
                transform = function(d) {
                    return 'rotate(' + angle / d.radius + ')';
                };
            frame.selectAll('path').attr('transform', transform);
            frame.attr('transform', transform); // frame of reference
        });
    }
}
