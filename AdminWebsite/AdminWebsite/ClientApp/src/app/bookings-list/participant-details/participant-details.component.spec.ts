import { DebugElement } from '@angular/core';
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RouterTestingModule } from '@angular/router/testing';
import { ParticipantDetailsComponent } from './participant-details.component';
import { ParticipantDetailsModel } from '../../common/model/participant-details.model';

describe('ParticipantDetailsComponent', () => {

  let component: ParticipantDetailsComponent;
  let fixture: ComponentFixture<ParticipantDetailsComponent>;
  let debugElement: DebugElement;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ParticipantDetailsComponent],
      imports: [RouterTestingModule],
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ParticipantDetailsComponent);
    debugElement = fixture.debugElement;
    component = debugElement.componentInstance;
  });

  it('should display participant details', () => {
    const pr = new ParticipantDetailsModel('1', 'Mrs', 'Alan', 'Brake', 'Citizen', 'email.p1@email.com',
      'email@ee.ee', 'Defendant', 'Defendant LIP', 'Alan Brake', '', 'ABC Solicitors', 'new Solicitor', 'defendant');
    pr.IndexInList = 0;
    component.participant = pr;

    fixture.detectChanges();
    const divElementRole = debugElement.queryAll(By.css('#participant_role0'));
    expect(divElementRole.length).toBeGreaterThan(0);
    expect(divElementRole.length).toBe(1);
    const el = divElementRole[0].nativeElement as HTMLElement;
    expect(el.innerHTML).toContain('Defendant');
  });
});
