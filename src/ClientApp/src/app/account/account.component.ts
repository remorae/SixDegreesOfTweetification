import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../services/authentication.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-account',
    templateUrl: './account.component.html',
    styleUrls: ['./account.component.scss']
})
export class AccountComponent implements OnInit {
    constructor(
        private router: Router,
        private authService: AuthenticationService
    ) {
        this.authService.hasTwitterLogin().subscribe(res => {
            this.hasTwitterLogin = res;
            this.canRemoveTwitter = this.hasTwitterLogin;
            this.canAddTwitter = !this.hasTwitterLogin;
        });
    }

    canAddTwitter = false;
    canRemoveTwitter = false;
    private hasTwitterLogin = false;

    ngOnInit() {}

    logout(): void {
        this.authService.logout(() => {
            this.router.navigate(['login']);
        });
    }

    addTwitter(): void {
        location.href = this.authService.linkLoginUrl;
    }

    removeTwitter(): void {
        this.authService.removeTwitter().subscribe(res => {
            this.hasTwitterLogin = false;
            this.canRemoveTwitter = false;
            this.canAddTwitter = true;
        });
    }
}
