import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  HearingTypeResponse, BHClient, HearingRequest,
  HearingMediumResponse, ParticipantRoleResponse, HearingResponse
} from '../services/clients/api-client';

@Injectable({
  providedIn: 'root'
})
export class VideoHearingsService {

  private newRequestKey: string;
  private newHearing: HearingRequest;

  constructor(private bhClient: BHClient) {
    this.newRequestKey = 'bh-newRequest';
  }

  private checkForExistingHearing() {
    const localRequest = sessionStorage.getItem(this.newRequestKey);
    if (localRequest === null) {
      const initRequest = {
        cases: [],
        feeds: [],
        hearing_type_id: -1,
        hearing_medium_id: -1,
        court_id: -1,
        scheduled_duration: 0,
      };
      this.newHearing = new HearingRequest(initRequest);
    } else {
      this.newHearing = JSON.parse(localRequest);
    }
  }

  hasUnsavedChanges() {
    return sessionStorage.getItem(this.newRequestKey) !== null;
  }

  getHearingMediums(): Observable<HearingMediumResponse[]> {
    return this.bhClient.getHearingMediums();
  }

  getHearingTypes(): Observable<HearingTypeResponse[]> {
    return this.bhClient.getHearingTypes();
  }

  getCurrentRequest(): HearingRequest {
    this.checkForExistingHearing();
    return this.newHearing;
  }

  updateHearingRequest(updatedRequest: HearingRequest) {
    this.newHearing = updatedRequest;
    const localRequest = JSON.stringify(this.newHearing);
    sessionStorage.setItem(this.newRequestKey, localRequest);
  }

  getParticipantRoles(): Observable<ParticipantRoleResponse[]> {
    return this.bhClient.getParticipantRoles();
  }

  cancelRequest() {
    sessionStorage.removeItem(this.newRequestKey);
  }

  saveHearing(newRequest: HearingRequest): Observable<number> {
    return this.bhClient.bookNewHearing(newRequest);
  }

 getHearingById(hearingId: number): Observable<HearingResponse> {
    return this.bhClient.getHearingById(hearingId);
  }
}
