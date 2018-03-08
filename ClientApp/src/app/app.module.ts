import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule, Routes } from '@angular/router';

import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { NavbarComponent } from './navbar/navbar.component';
import { TileColumnComponent } from './tile-column/tile-column.component';
import { LoginComponent } from './login/login.component';
import { AuthenticationService } from './services/authentication.service';
import { AuthGuard } from './services/auth-guard.service';
import { GeoPageComponent } from './geo-page/geo-page.component';
import { DualInputComponent } from './dual-input/dual-input.component';
import { SingleInputComponent } from './single-input/single-input.component';
import { EndpointService } from './services/endpoint.service';
import { SectionTileComponent } from './section-tile/section-tile.component';
import { SelectGeoFilterComponent } from './select-geo-filter/select-geo-filter.component';
import { RateLimitDisplayComponent } from './rate-limit-display/rate-limit-display.component';
import { HashToHashPageComponent } from './hash-to-hash-page/hash-to-hash-page.component';
import { UserToUserPageComponent } from './user-to-user-page/user-to-user-page.component';
import { WordCloudPageComponent } from './word-cloud-page/word-cloud-page.component';


const appRoutes: Routes = [
    { path: '', component: LoginComponent, pathMatch: 'full' },
    { path: 'login', component: LoginComponent, pathMatch: 'full' },
    { path: 'home', component: HomeComponent, canActivate: [AuthGuard] },
    { path: 'hash-to-hash', component: HashToHashPageComponent, canActivate: [AuthGuard] },
    { path: 'geo', component: GeoPageComponent, canActivate: [AuthGuard] },
    { path: 'user-to-user', component: UserToUserPageComponent, canActivate: [AuthGuard] },
    { path: 'word-cloud', component: WordCloudPageComponent, canActivate: [AuthGuard] },
];
@NgModule({
    declarations: [
        AppComponent,
        HomeComponent,
        NavbarComponent,
        TileColumnComponent,
        LoginComponent,
        GeoPageComponent,
        DualInputComponent,
        SingleInputComponent,
        SectionTileComponent,
        SelectGeoFilterComponent,
        RateLimitDisplayComponent,
        HashToHashPageComponent,
        UserToUserPageComponent,
        WordCloudPageComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        RouterModule.forRoot(appRoutes) // , { enableTracing: true })
    ],
    providers: [AuthenticationService, AuthGuard, EndpointService],
    bootstrap: [AppComponent]
})
export class AppModule {}
