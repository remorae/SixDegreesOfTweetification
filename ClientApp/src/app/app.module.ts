import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HttpClientXsrfModule, HTTP_INTERCEPTORS } from '@angular/common/http';
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
import { CanvasComponent } from './canvas/canvas.component';

import { HttpXsrfInterceptorService } from './services/http-xsrfinterceptor.service';
import { RegisterComponent } from './register/register.component';
import { ExternalLoginComponent } from './external-login/external-login.component';

const appRoutes: Routes = [
    { path: '', component: LoginComponent, pathMatch: 'full' },
    { path: 'login', component: LoginComponent, pathMatch: 'full' },
    { path: 'externallogin', component: ExternalLoginComponent, pathMatch: 'full' },
    { path: 'register', component: RegisterComponent, pathMatch: 'full' },
    { path: 'geo', component: GeoPageComponent, canActivate: [AuthGuard] },
    { path: 'home', component: HomeComponent, canActivate: [AuthGuard] },
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
        CanvasComponent,
        RegisterComponent,
        ExternalLoginComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        HttpClientModule,
        HttpClientXsrfModule.withOptions({
            cookieName: 'XSRF-TOKEN',
            headerName: 'X-XSRF-TOKEN',
        }),
        FormsModule,
        ReactiveFormsModule,
        RouterModule.forRoot(appRoutes) // , { enableTracing: true })
    ],
    providers: [
        AuthenticationService,
        AuthGuard,
        EndpointService,
        { provide: HTTP_INTERCEPTORS, useClass: HttpXsrfInterceptorService, multi: true }
    ],
    bootstrap: [AppComponent]
})
export class AppModule {}
