import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-section-tile',
  templateUrl: './section-tile.component.html',
  styleUrls: ['./section-tile.component.scss']
})
export class SectionTileComponent implements OnInit {
    @Input() iconPath: string;
    @Input() title: string;
    @Input() description: string;
    @Input() route: string;
    isCurrentPage: boolean;

    constructor() { const thing = document; }

    ngOnInit() { }
}
