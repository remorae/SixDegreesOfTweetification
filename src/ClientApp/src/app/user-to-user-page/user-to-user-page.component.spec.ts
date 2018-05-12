import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { UserToUserPageComponent } from './user-to-user-page.component';

describe('UserToUserPageComponent', () => {
    let component: UserToUserPageComponent;
    let fixture: ComponentFixture<UserToUserPageComponent>;

    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [UserToUserPageComponent]
        }).compileComponents();
    }));

    beforeEach(() => {
        fixture = TestBed.createComponent(UserToUserPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
