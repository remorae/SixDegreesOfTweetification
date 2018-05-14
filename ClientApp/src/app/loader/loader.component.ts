import { Component, OnInit } from '@angular/core';
import { LoaderService } from '../services/loader.service';
import { Observable } from 'rxjs/Observable';
/**
 * @example Displays a simple loading pop-up on any given route in the application,
 *          when the component associated with the route is awaiting server data.
 */
@Component({
    selector: 'app-loader',
    templateUrl: './loader.component.html',
    styleUrls: ['./loader.component.scss']
})
export class LoaderComponent implements OnInit {
    loading = false;
    constructor(private loader: LoaderService) {}

    ngOnInit(): void {
        this.loader.getLoadingStatus().subscribe(b => (this.loading = b));
    }
}
