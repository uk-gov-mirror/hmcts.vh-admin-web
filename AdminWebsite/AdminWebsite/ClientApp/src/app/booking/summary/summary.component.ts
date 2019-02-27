import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';

import { Constants } from '../../common/constants';
import { CanDeactiveComponent } from '../../common/guards/changes.guard';
import {
  CourtResponse,
  HearingTypeResponse,
} from '../../services/clients/api-client';
import { HearingModel} from '../../common/model/hearing.model';
import {ParticipantModel } from '../../common/model/participant.model';

import { ReferenceDataService } from '../../services/reference-data.service';
import { VideoHearingsService } from '../../services/video-hearings.service';

@Component({
  selector: 'app-summary',
  templateUrl: './summary.component.html',
  styleUrls: ['./summary.component.css']
})

export class SummaryComponent implements OnInit, CanDeactiveComponent {

  constants = Constants;
  hearing: HearingModel;
 // newhearing: HearingRequest;
  attemptingCancellation: boolean;
  canNavigate = true;
  hearingForm: FormGroup;
  failedSubmission: boolean;
  bookingsSaving: boolean = false;

  caseNumber: string;
  caseName: string;
  caseHearingType: string;
  hearingDate: Date;
  courtRoomAddress: string;
  hearingDuration: string;
  otherInformation: string;
  errors: any;

  selectedHearingTypeName: HearingTypeResponse[];
 // newparticipants: ParticipantRequest[] = [];
  participants: ParticipantModel[] = [];
  selectedHearingType: HearingTypeResponse[];
  saveFailed: boolean;

  constructor(private hearingService: VideoHearingsService, private router: Router, private referenceDataService: ReferenceDataService) {
    this.attemptingCancellation = false;
    this.saveFailed = false;
  }

  ngOnInit() {
    this.checkForExistingRequest();
    this.retrieveHearingSummary();
  }

  private checkForExistingRequest() {
    this.hearing = this.hearingService.getCurrentRequest();
  }

  private retrieveHearingSummary() {
    this.caseNumber = this.hearing.cases[0].number;
    this.caseName = this.hearing.cases[0].name;
    this.getCaseHearingTypeName(this.hearing.hearing_type_id);
    this.hearingDate = this.hearing.scheduled_date_time;
    this.getCourtRoomAndAddress(this.hearing.court_id);
    this.hearingDuration = this.getHearingDuration(this.hearing.scheduled_duration);
    this.participants = this.getAllParticipants();
    this.otherInformation = this.hearing.other_information;
  }

  private getAllParticipants(): ParticipantModel[] {
    let participants: ParticipantModel[] = [];
    this.hearing.feeds.forEach(x => {
      if (x.participants && x.participants.length >= 1) {
        participants = participants.concat(x.participants);
      }
    });
    return participants;
  }

  private getCaseHearingTypeName(hearing_type_id: number): void {
    this.hearingService.getHearingTypes()
      .subscribe(
        (data: HearingTypeResponse[]) => {
          const selectedHearingType = data.filter(h => h.id === hearing_type_id);
          this.caseHearingType = selectedHearingType[0].name;
        },
        error => console.error(error)
      );
  }

  private getCourtRoomAndAddress(courtId: number): void {
    this.referenceDataService.getCourts()
      .subscribe(
        (data: CourtResponse[]) => {
          const selectedCourt = data.filter(c => c.id === courtId);
          const selectedCourtRoom = this.hearing.court_room;
          this.courtRoomAddress = selectedCourt[0].address + ', ' + selectedCourtRoom;
        },
        error => console.error(error)
      );
  }

  private getHearingDuration(duration: number): string {
    return 'listed for ' + (duration === null ? 0 : duration) + ' minutes';
  }

  continueBooking() {
    this.attemptingCancellation = false;
  }

  confirmCancelBooking() {
    this.attemptingCancellation = true;
  }

  cancelBooking() {
    this.attemptingCancellation = false;
    this.hearingService.cancelRequest();
    this.router.navigate(['/dashboard']);
  }

  hasChanges(): Observable<boolean> | boolean {
    return true;
  }

  bookHearing(): void {
    this.bookingsSaving = true;
    this.hearingService.saveHearing(this.hearing)
      .subscribe(
        (data: number) => {
          this.hearingService.cancelRequest();
          this.router.navigate(['/booking-confirmation']);
        },
        error => {
          console.error(error);
          this.saveFailed = true;
          this.errors = error;
        }
      );

    if (this.errors) {
      this.saveFailed = true;
    }
  }
}
