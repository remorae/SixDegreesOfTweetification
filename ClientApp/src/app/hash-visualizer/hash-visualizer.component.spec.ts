import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { HashVisualizerComponent } from './hash-visualizer.component';

describe('HashVisualizerComponent', () => {
  let component: HashVisualizerComponent;
  let fixture: ComponentFixture<HashVisualizerComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ HashVisualizerComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HashVisualizerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
