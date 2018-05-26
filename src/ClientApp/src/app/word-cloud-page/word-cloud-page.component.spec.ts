import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WordCloudPageComponent } from './word-cloud-page.component';

describe('WordCloudPageComponent', () => {
    let component: WordCloudPageComponent;
    let fixture: ComponentFixture<WordCloudPageComponent>;

    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [WordCloudPageComponent]
        }).compileComponents();
    }));

    beforeEach(() => {
        fixture = TestBed.createComponent(WordCloudPageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
