import { async, ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { CancelPopupComponent } from '../../popups/cancel-popup/cancel-popup.component';
import { DiscardConfirmPopupComponent } from '../../popups/discard-confirm-popup/discard-confirm-popup.component';
import { SharedModule } from '../../shared/shared.module';
import { BreadcrumbStubComponent } from '../../testing/stubs/breadcrumb-stub';
import { Router } from '@angular/router';

import { VideoHearingsService } from '../../services/video-hearings.service';
import { BookingService } from '../../services/booking.service';
import { AssignJudgeComponent } from './assign-judge.component';
import { of } from 'rxjs';
import { MockValues } from '../../testing/data/test-objects';
import { JudgeDataService } from '../services/judge-data.service';
import { ParticipantsListStubComponent } from '../../testing/stubs/participant-list-stub';
import { HearingModel } from '../../common/model/hearing.model';
import { ParticipantModel } from '../../common/model/participant.model';
import { By } from '@angular/platform-browser';

function initHearingRequest(): HearingModel {

  const participants: ParticipantModel[] = [];
  const p1 = new ParticipantModel();
  p1.display_name = 'display name1';
  p1.email = 'test1@TestBed.com';
  p1.first_name = 'first';
  p1.last_name = 'last';
  p1.is_judge = true;
  p1.title = 'Mr.';

  const p2 = new ParticipantModel();
  p2.display_name = 'display name2';
  p2.email = 'test2@TestBed.com';
  p2.first_name = 'first2';
  p2.last_name = 'last2';
  p2.is_judge = false;
  p2.title = 'Mr.';


  participants.push(p1);
  participants.push(p2);

  const newHearing = new HearingModel();
  newHearing.cases = [];
  newHearing.participants = participants;

  newHearing.hearing_type_id = -1;
  newHearing.hearing_venue_id = -1;
  newHearing.scheduled_date_time = null;
  newHearing.scheduled_duration = 0;

  return newHearing;
}

let component: AssignJudgeComponent;
let fixture: ComponentFixture<AssignJudgeComponent>;

let videoHearingsServiceSpy: jasmine.SpyObj<VideoHearingsService>;
let judgeDataServiceSpy: jasmine.SpyObj<JudgeDataService>;
let routerSpy: jasmine.SpyObj<Router>;
let bookingServiseSpy: jasmine.SpyObj<BookingService>;

describe('AssignJudgeComponent', () => {

  beforeEach(async(() => {
    const newHearing = initHearingRequest();
    routerSpy = jasmine.createSpyObj('Router', ['navigate', 'url']);
    routerSpy.url.and.returnValue('/summary');

    videoHearingsServiceSpy = jasmine.createSpyObj<VideoHearingsService>('VideoHearingsService',
      ['getHearingMediums', 'getHearingTypes', 'getCurrentRequest',
        'updateHearingRequest', 'cancelRequest', 'setBookingHasChanged']);
    videoHearingsServiceSpy.getCurrentRequest.and.returnValue(newHearing);

    bookingServiseSpy = jasmine.createSpyObj<BookingService>('BookingService', ['resetEditMode', 'isEditMode']);

    judgeDataServiceSpy = jasmine.createSpyObj<JudgeDataService>(['JudgeDataService', 'getJudges']);
    judgeDataServiceSpy.getJudges.and.returnValue(of(MockValues.Judges));

    TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
      providers: [
        { provide: VideoHearingsService, useValue: videoHearingsServiceSpy },
        { provide: JudgeDataService, useValue: judgeDataServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: BookingService, useValue: bookingServiseSpy },
      ],
      declarations: [AssignJudgeComponent, BreadcrumbStubComponent,
        CancelPopupComponent, ParticipantsListStubComponent, DiscardConfirmPopupComponent]
    })
      .compileComponents();

    fixture = TestBed.createComponent(AssignJudgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.ngOnInit();
  }));

  it('should fail validation if a judge is not selected', () => {
    component.cancelAssignJudge();
    component.saveJudge();
    expect(component.form.valid).toBeFalsy();
  });

  it('is valid and has updated selected judge after selecting judge in dropdown', () => {
    const dropDown = fixture.debugElement.query(By.css('#judgeName')).nativeElement;
    dropDown.value = dropDown.options[2].value;
    dropDown.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(component.judge.email).toBe('John2.Doe@hearings.reform.hmcts.net');
    expect(component.form.valid).toBeTruthy();
  });

  it('should get current booking and judge details', () => {
    component.ngOnInit();
    expect(component.failedSubmission).toBeFalsy();
    expect(videoHearingsServiceSpy.getCurrentRequest).toHaveBeenCalled();
    expect(component.canNavigate).toBeTruthy();
    expect(component.judge.first_name).toBe('first');
    expect(component.judge.display_name).toBe('display name1');
    expect(component.judge.email).toBe('test1@TestBed.com');
    expect(component.judge.last_name).toBe('last');
  });
  it('should get available judges', () => {
    component.ngOnInit();
    expect(component.availableJudges.length).toBeGreaterThan(1);
    expect(component.availableJudges[0].email).toBe('Please select');
    expect(component.availableJudges[0].display_name).toBe('');

  });
  it('should hide cancel and discard pop up confirmation', () => {
    component.attemptingCancellation = true;
    component.attemptingDiscardChanges = true;
    fixture.detectChanges();
    component.continueBooking();
    expect(component.attemptingCancellation).toBeFalsy();
    expect(component.attemptingDiscardChanges).toBeFalsy();
  });
  it('should show discard pop up confirmation', () => {
    component.editMode = true;
    component.form.markAsDirty();
    fixture.detectChanges();
    component.confirmCancelBooking();
    expect(component.attemptingDiscardChanges).toBeTruthy();
  });
  it('should navigate to summary page if no changes', () => {
    component.editMode = true;
    component.form.markAsPristine();
    fixture.detectChanges();
    component.confirmCancelBooking();
    expect(routerSpy.navigate).toHaveBeenCalled();
  });
  it('should show cancel booking confirmation pop up', () => {
    component.editMode = false;
    fixture.detectChanges();
    component.confirmCancelBooking();
    expect(component.attemptingCancellation).toBeTruthy();
  });
  it('should cancel booking, hide pop up and navigate to dashboard', () => {
    fixture.detectChanges();
    component.cancelAssignJudge();
    expect(component.attemptingCancellation).toBeFalsy();
    expect(videoHearingsServiceSpy.cancelRequest).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalled();
  });
  it('should cancel current changes, hide pop up and navigate to summary', () => {
    fixture.detectChanges();
    component.cancelChanges();
    expect(component.attemptingDiscardChanges).toBeFalsy();
    expect(routerSpy.navigate).toHaveBeenCalled();
  });
  it('should check if the judge display name was entered and return true', () => {
    component.judge.display_name = 'New Name Set';
    const result = component.isJudgeDisplayNameSet();
    expect(result).toBeTruthy();
  });
  it('should check if the judge display name was entered and return false', () => {
    component.judge.display_name = 'John Doe';
    const result = component.isJudgeDisplayNameSet();
    expect(result).toBeFalsy();
  });
});

