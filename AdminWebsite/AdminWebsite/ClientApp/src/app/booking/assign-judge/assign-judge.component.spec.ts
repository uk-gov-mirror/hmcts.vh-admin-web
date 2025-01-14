import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { Constants } from 'src/app/common/constants';
import { HearingModel } from '../../common/model/hearing.model';
import { ParticipantModel } from '../../common/model/participant.model';
import { CancelPopupComponent } from '../../popups/cancel-popup/cancel-popup.component';
import { DiscardConfirmPopupComponent } from '../../popups/discard-confirm-popup/discard-confirm-popup.component';
import { BookingService } from '../../services/booking.service';
import { ClientSettingsResponse, JudgeResponse } from '../../services/clients/api-client';
import { Logger } from '../../services/logger';
import { RecordingGuardService } from '../../services/recording-guard.service';
import { VideoHearingsService } from '../../services/video-hearings.service';
import { SharedModule } from '../../shared/shared.module';
import { MockValues } from '../../testing/data/test-objects';
import { BreadcrumbStubComponent } from '../../testing/stubs/breadcrumb-stub';
import { ParticipantsListStubComponent } from '../../testing/stubs/participant-list-stub';
import { JudgeDataService } from '../services/judge-data.service';
import { AssignJudgeComponent } from './assign-judge.component';
import { OtherInformationModel } from '../../common/model/other-information.model';
import { EmailValidationService } from 'src/app/booking/services/email-validation.service';
import { ConfigService } from '../../services/config.service';

function initHearingRequest(): HearingModel {
    const participants: ParticipantModel[] = [];
    const p1 = new ParticipantModel();
    p1.display_name = 'display name1';
    p1.email = 'test1@hmcts.net';
    p1.first_name = 'first';
    p1.last_name = 'last';
    p1.is_judge = true;
    p1.title = 'Mr.';
    p1.username = 'test1@hmcts.net';
    p1.hearing_role_name = 'Judge';

    const p2 = new ParticipantModel();
    p2.display_name = 'display name2';
    p2.email = 'test2@hmcts.net';
    p2.first_name = 'first2';
    p2.last_name = 'last2';
    p2.is_judge = false;
    p2.title = 'Mr.';
    p2.username = 'test2@hmcts.net';
    p2.hearing_role_name = 'Applicant';

    participants.push(p1);
    participants.push(p2);

    const newHearing = new HearingModel();
    newHearing.cases = [];
    newHearing.participants = participants;

    newHearing.hearing_type_id = -1;
    newHearing.hearing_venue_id = -1;
    newHearing.scheduled_date_time = null;
    newHearing.scheduled_duration = 0;
    newHearing.audio_recording_required = true;

    return newHearing;
}

function initHearingWithJOH() {
    const newHearing = initHearingRequest();
    const p1 = new ParticipantModel();
    p1.display_name = 'display name_pm';
    p1.email = 'testpm@hmcts.net';
    p1.first_name = 'firstpm';
    p1.last_name = 'lastpm';
    p1.is_judge = false;
    p1.title = 'Mr.';
    p1.username = 'testpm@hmcts.net';
    p1.hearing_role_name = 'Panel Member';

    const p2 = new ParticipantModel();
    p2.display_name = 'display name_w';
    p2.email = 'testw@hmcts.net';
    p2.first_name = 'firstw';
    p2.last_name = 'lastw';
    p2.is_judge = false;
    p2.title = 'Mr.';
    p2.username = 'testw@hmcts.net';
    p2.hearing_role_name = 'Winger';

    newHearing.participants.push(p1);
    newHearing.participants.push(p2);
    return newHearing;
}

let component: AssignJudgeComponent;
let fixture: ComponentFixture<AssignJudgeComponent>;

let videoHearingsServiceSpy: jasmine.SpyObj<VideoHearingsService>;
let judgeDataServiceSpy: jasmine.SpyObj<JudgeDataService>;
let routerSpy: jasmine.SpyObj<Router>;
let bookingServiseSpy: jasmine.SpyObj<BookingService>;
let loggerSpy: jasmine.SpyObj<Logger>;
let emailValidationServiceSpy: jasmine.SpyObj<EmailValidationService>;
const configSettings = new ClientSettingsResponse();
configSettings.test_username_stem = '@hmcts.net';
let configServiceSpy: jasmine.SpyObj<ConfigService>;

describe('AssignJudgeComponent', () => {
    beforeEach(
        waitForAsync(() => {
            const newHearing = initHearingRequest();
            loggerSpy = jasmine.createSpyObj<Logger>('Logger', ['error', 'debug', 'warn']);
            configServiceSpy = jasmine.createSpyObj<ConfigService>('ConfigService', ['getClientSettings']);
            emailValidationServiceSpy = jasmine.createSpyObj<EmailValidationService>('EmailValidationService', [
                'hasCourtroomAccountPattern',
                'validateEmail'
            ]);
            videoHearingsServiceSpy = jasmine.createSpyObj<VideoHearingsService>('VideoHearingsService', [
                'getHearingTypes',
                'getCurrentRequest',
                'updateHearingRequest',
                'cancelRequest',
                'setBookingHasChanged'
            ]);
            videoHearingsServiceSpy.getCurrentRequest.and.returnValue(newHearing);
            emailValidationServiceSpy.validateEmail.and.returnValue(true);
            emailValidationServiceSpy.hasCourtroomAccountPattern.and.returnValue(true);

            bookingServiseSpy = jasmine.createSpyObj<BookingService>('BookingService', ['resetEditMode', 'isEditMode', 'removeEditMode']);

            judgeDataServiceSpy = jasmine.createSpyObj<JudgeDataService>(['JudgeDataService', 'getJudges']);
            judgeDataServiceSpy.getJudges.and.returnValue(of(MockValues.Judges));
            configServiceSpy = jasmine.createSpyObj<ConfigService>('CongigService', ['getClientSettings']);
            configServiceSpy.getClientSettings.and.returnValue(of(configSettings));

            TestBed.configureTestingModule({
                imports: [SharedModule, RouterTestingModule],
                providers: [
                    { provide: VideoHearingsService, useValue: videoHearingsServiceSpy },
                    { provide: JudgeDataService, useValue: judgeDataServiceSpy },
                    { provide: EmailValidationService, useValue: emailValidationServiceSpy },
                    { provide: ConfigService, useValue: configServiceSpy },
                    {
                        provide: Router,
                        useValue: {
                            url: '/summary',
                            navigate: jasmine.createSpy('navigate')
                        }
                    },
                    { provide: BookingService, useValue: bookingServiseSpy },
                    { provide: Logger, useValue: loggerSpy },
                    RecordingGuardService,
                    { provide: ConfigService, useValue: configServiceSpy }
                ],
                declarations: [
                    AssignJudgeComponent,
                    BreadcrumbStubComponent,
                    CancelPopupComponent,
                    ParticipantsListStubComponent,
                    DiscardConfirmPopupComponent
                ]
            }).compileComponents();

            fixture = TestBed.createComponent(AssignJudgeComponent);

            /* tslint:disable */
            routerSpy = TestBed.get(Router);
            /* tslint:enable */

            component = fixture.componentInstance;
            fixture.detectChanges();
            component.ngOnInit();
        })
    );

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

        expect(component.judge.email).toBe('John2.Doe@hmcts.net');
        expect(component.form.valid).toBeTruthy();
    });
    it('should initialize form and create judgeDisplayName control', () => {
        component.ngOnInit();
        expect(component.judgeDisplayNameFld).toBeTruthy();
        expect(component.judgeDisplayNameFld.updateOn).toBe('blur');
    });
    it('judge display name field validity required', () => {
        let errors: {};
        component.form.controls['judgeDisplayNameFld'].setValue('');
        const judge_display_name = component.form.controls['judgeDisplayNameFld'];
        errors = judge_display_name.errors || {};
        expect(errors['required']).toBeTruthy();
    });
    it('judge display name field validity pattern', () => {
        let errors: {};
        component.form.controls['judgeDisplayNameFld'].setValue('%');
        const judge_display_name = component.form.controls['judgeDisplayNameFld'];
        errors = judge_display_name.errors || {};
        expect(errors['pattern']).toBeTruthy();
    });
    it('should fail validation if a judge display name is not entered', () => {
        component.ngOnInit();
        expect(component.judgeDisplayNameFld).toBeTruthy();
        expect(component.judgeDisplayNameFld.validator).toBeTruthy();
        component.judgeDisplayNameFld.setValue('');
        expect(component.form.valid).toBeFalsy();
    });
    it('should succeeded validation if a judge display name is entered', () => {
        component.ngOnInit();
        component.judgeDisplayNameFld.setValue('judge name');
        expect(component.judgeDisplayNameInvalid).toBeFalsy();
    });
    it('should not succeeded validation if a judge display name' + 'is entered with not allowed characters', () => {
        component.ngOnInit();
        component.judgeDisplayNameFld.setValue('%');
        component.failedSubmission = true;
        expect(component.judgeDisplayNameInvalid).toBeTruthy();
    });
    it('should return judgeDisplayNameInvalid is false if form is valid', () => {
        component.ngOnInit();
        component.judgeDisplayNameFld.setValue('a');
        component.judgeDisplayNameFld.markAsUntouched();
        component.judgeDisplayNameFld.markAsPristine();
        component.failedSubmission = false;
        expect(component.judgeDisplayNameInvalid).toBeFalsy();
    });

    it('should get current booking and judge details', () => {
        component.ngOnInit();
        expect(component.failedSubmission).toBeFalsy();
        expect(videoHearingsServiceSpy.getCurrentRequest).toHaveBeenCalled();
        expect(component.canNavigate).toBeTruthy();
        expect(component.judge.first_name).toBe('first');
        expect(component.judge.display_name).toBe('display name1');
        expect(component.judge.email).toBe('test1@hmcts.net');
        expect(component.judge.last_name).toBe('last');
    });
    it('should get available judges', () => {
        component.ngOnInit();
        expect(component.availableJudges.length).toBeGreaterThan(1);
        expect(component.availableJudges[0].email).toBe(Constants.PleaseSelect);
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
    it('should add judge with display name was entered', () => {
        component.judge.display_name = 'New Name Set';
        component.hearing = new HearingModel();
        component.hearing.participants = [];
        component.availableJudges = [new JudgeResponse({ display_name: 'New Name Set', email: 'email@hmcts.net' })];
        component.addJudge('email@hmcts.net');
        expect(component.hearing.participants.length).toBeGreaterThan(0);
    });
    it('should sanitize display name of the judge if it was entered', () => {
        component.judgeDisplayNameFld.setValue('<script>text</script>');
        component.changeDisplayName();
        expect(component.judgeDisplayNameFld.value).toBe('text');
    });
    it('should change display name of the judge if it was selected', () => {
        component.judge.display_name = 'John Dall';
        component.changeDisplayName();
        expect(component.hearing.participants[0].display_name).toBe('John Dall');
    });
    it('should not save judge if courtroom account is null', () => {
        component.judge.email = null;
        component.saveJudge();
        expect(component.isJudgeSelected).toBeFalsy();
    });
    it('should not save judge if courtroom account is not selected', () => {
        component.judge.email = Constants.PleaseSelect;
        component.saveJudge();
        expect(component.isJudgeSelected).toBeFalsy();
    });
    it('should save judge if courtroom account is selected and form is valid', () => {
        const dropDown = fixture.debugElement.query(By.css('#judgeName')).nativeElement;
        dropDown.value = dropDown.options[2].value;
        dropDown.dispatchEvent(new Event('change'));
        fixture.detectChanges();
        expect(component.form.valid).toBeTruthy();

        component.saveJudge();
        expect(videoHearingsServiceSpy.updateHearingRequest).toHaveBeenCalled();
    });
    it('should log error message if no judges to load', () => {
        component.onErrorLoadJudges(new Error());
        expect(loggerSpy.error).toHaveBeenCalled();
    });
    it('should unsubscribe all subcriptions on destroy component', () => {
        component.ngOnDestroy();
        expect(component.$subscriptions[0].closed).toBeTruthy();
        expect(component.$subscriptions[1].closed).toBeTruthy();
    });
    it('should navigate if judge is saved', () => {
        const savedHearing = initHearingRequest();
        expect(savedHearing.participants.length > 0).toBe(true);
        videoHearingsServiceSpy.getCurrentRequest.calls.reset();
        component.canNavigate = true;
        videoHearingsServiceSpy.getCurrentRequest.and.returnValue(savedHearing);
        expect(component.canNavigateNext).toBe(true);
    });
    it('should not navigate if judge is not saved', () => {
        const savedHearing = initHearingRequest();
        savedHearing.participants = [];
        videoHearingsServiceSpy.getCurrentRequest.calls.reset();
        component.canNavigate = true;
        videoHearingsServiceSpy.getCurrentRequest.and.returnValue(savedHearing);
        expect(component.canNavigateNext).toBe(false);
    });
    it('should change email address when entered in judge email input', () => {
        component.judgeEmailFld.setValue('judge@judges.com');
        component.changeEmail();
        expect(component.hearing.participants[0].email).toBe('test1@hmcts.net');
    });
    it('should display error when email address is not valid', () => {
        component.ngOnInit();
        component.judgeEmailFld.setValue('judge@');
        component.failedSubmission = true;
        expect(component.judgeEmailInvalid).toBeTruthy();
    });
    it('should change telephone number when entered in judge telephone input', () => {
        const judgePhone = '01234567890';
        component.judgePhoneFld.setValue(judgePhone);
        component.changeTelephone();
        const otherInformationDetails = OtherInformationModel.init(component.hearing.other_information);
        expect(otherInformationDetails.JudgePhone).toBe(judgePhone);
    });
    it('should display error when telephone address is not valid', () => {
        component.ngOnInit();
        component.judgePhoneFld.setValue('+123456ABCD');
        component.failedSubmission = true;
        expect(component.judgePhoneInvalid).toBeTruthy();
    });
    it('should create a new judge if one does not already exist', () => {
        component.ngOnInit();
        component.populateFormFields(null);
        expect(component.judge).toBeTruthy();
        expect(component.judge).not.toBeNull();
    });
    it('should return nothing if judge is not available', () => {
        component.ngOnInit();
        component.addJudge('fakejudge@notavailable.com');
        expect().nothing();
        expect(component.isJudgeParticipantError).toBe(false);
    });
    it('should set validation error to true if judge account has the same account as panel member', () => {
        const savedHearing = initHearingWithJOH();
        const panelMember = savedHearing.participants.find(x => x.hearing_role_name === 'Panel Member');
        const judge = savedHearing.participants.find(x => x.is_judge);
        panelMember.username = judge.username;
        component.hearing = savedHearing;

        expect(component.isJudgeParticipantError).toBe(false);

        component.saveJudge();

        expect(component.isJudgeParticipantError).toBe(true);
    });
    it('should set validation error to true if judge account has the same account as winger', () => {
        const savedHearing = initHearingWithJOH();
        const panelMember = savedHearing.participants.find(x => x.hearing_role_name === 'Winger');
        const judge = savedHearing.participants.find(x => x.is_judge);
        panelMember.username = judge.username;
        component.hearing = savedHearing;

        expect(component.isJudgeParticipantError).toBe(false);

        component.saveJudge();

        expect(component.isJudgeParticipantError).toBe(true);
    });
    it('should set validation error to false if judge account has not the same account with winger or panel member', () => {
        const savedHearing = initHearingWithJOH();
        component.hearing = savedHearing;

        expect(component.isJudgeParticipantError).toBe(false);

        component.saveJudge();

        expect(component.isJudgeParticipantError).toBe(false);
    });
});
