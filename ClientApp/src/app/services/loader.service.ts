import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';

@Injectable()
export class LoaderService {

    loadingStatus: Subject<boolean> = new Subject<boolean>();

    constructor() { }

    startLoading() {
        this.loadingStatus.next(true);
    }

    endLoading() {
        this.loadingStatus.next(false);
    }

    getLoadingStatus(): Subject<boolean> {
        return this.loadingStatus;
    }

}
