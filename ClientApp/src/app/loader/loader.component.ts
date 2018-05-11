import { Component, OnInit } from '@angular/core';
import { LoaderService } from '../services/loader.service';
import { Observable } from 'rxjs/Observable';

@Component({
    selector: 'app-loader',
    templateUrl: './loader.component.html',
    styleUrls: ['./loader.component.scss']
})
export class LoaderComponent implements OnInit {
    loading = false;
    constructor(private loader: LoaderService) {}

    ngOnInit() {
        this.loader.getLoadingStatus().subscribe(b => (this.loading = b));
    }
}
