import { Component, OnInit } from '@angular/core';
import { HttpClient} from '@angular/common/http';

@Component({
   selector: 'app-root',
   templateUrl: './app.component.html',
   styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {

   constructor(private _httpService: HttpClient) { }

   apiValues: string[] = [];

   ngOnInit() {
      this._httpService.get<string[]>('/api/values').subscribe(values => {
         this.apiValues = values;
      });
   }

}