import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App (Jest)', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App]
    }).compileComponents();
  });

  it('creates the app component', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const h1: HTMLHeadingElement | null = fixture.nativeElement.querySelector('h1');
    expect(h1).not.toBeNull();
    expect(h1!.textContent).toMatch(/hello, front/i);
  });
});
