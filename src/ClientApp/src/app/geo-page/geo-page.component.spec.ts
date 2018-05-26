import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { GeoPageComponent } from './geo-page.component';

describe('GeoPageComponent', () => {
    let component: GeoPageComponent;
    let fixture: ComponentFixture<GeoPageComponent>;

    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [GeoPageComponent]
        }).compileComponents();
    }));

    beforeEach(() => {
        fixture = TestBed.createComponent(GeoPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
