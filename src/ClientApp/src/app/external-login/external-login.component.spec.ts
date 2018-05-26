import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ExternalLoginComponent } from './external-login.component';

describe('ExternalLoginComponent', () => {
    let component: ExternalLoginComponent;
    let fixture: ComponentFixture<ExternalLoginComponent>;

    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [ ExternalLoginComponent ]
        })
        .compileComponents();
    }));

    beforeEach(() => {
        fixture = TestBed.createComponent(ExternalLoginComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
