import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { HashToHashPageComponent } from './hash-to-hash-page.component';

describe('HashToHashPageComponent', () => {
    let component: HashToHashPageComponent;
    let fixture: ComponentFixture<HashToHashPageComponent>;

    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [HashToHashPageComponent]
        }).compileComponents();
    }));

    beforeEach(() => {
        fixture = TestBed.createComponent(HashToHashPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
