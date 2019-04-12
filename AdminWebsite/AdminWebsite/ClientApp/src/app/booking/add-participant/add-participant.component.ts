import { Component, OnInit, ViewChild, AfterViewInit, OnDestroy, AfterContentInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { Constants } from '../../common/constants';
import { CanDeactiveComponent } from '../../common/guards/changes.guard';
import { IDropDownModel } from '../../common/model/drop-down.model';
import { HearingModel } from '../../common/model/hearing.model';
import { ParticipantModel } from '../../common/model/participant.model';

import { SearchService } from '../../services/search.service';
import { VideoHearingsService } from '../../services/video-hearings.service';
import { SearchEmailComponent } from '../search-email/search-email.component';
import { ParticipantsListComponent } from '../participants-list/participants-list.component';
import { BookingBaseComponent } from '../booking-base/booking-base.component';
import { BookingService } from '../../services/booking.service';
import { ParticipantService } from '../services/participant.service';
import { CaseAndHearingRolesResponse } from '../../services/clients/api-client';
import { PartyModel } from '../../common/model/party.model';


@Component({
  selector: 'app-add-participant',
  templateUrl: './add-participant.component.html',
  styleUrls: ['./add-participant.component.css'],
})
export class AddParticipantComponent extends BookingBaseComponent implements OnInit,
  AfterViewInit, AfterContentInit, OnDestroy, CanDeactiveComponent {
  canNavigate = true;
  constants = Constants;

  participantDetails: ParticipantModel;
  notFound: boolean;
  hearing: HearingModel;
  titleList: IDropDownModel[] = [];
  roleList: string[];
  hearingRoleList: string[];
  caseAndHearingRoles: PartyModel[] = [];
  selectedParticipantEmail: string = null;
  participantForm: FormGroup;
  private role: FormControl;
  private party: FormControl;
  private title: FormControl;
  private firstName: FormControl;
  private lastName: FormControl;
  private phone: FormControl;
  private displayName: FormControl;
  private companyName: FormControl;
  private houseNumber: FormControl;
  private street: FormControl;
  private city: FormControl;
  private county: FormControl;
  private postcode: FormControl;
  isRoleSelected = true;
  isPartySelected = true;
  isTitleSelected = true;
  isShowErrorSummary = false;
  showDetails = false;

  showCancelPopup = false;
  showConfirmationPopup = false;
  attemptingDiscardChanges = false;
  confirmationMessage: string;
  showConfirmationRemoveParticipant = false;
  removerFullName: string;

  displayNextButton = true;
  displayAddButton = false;
  displayClearButton = false;
  displayUpdateButton = false;
  displayErrorNoParticipants = false;
  localEditMode = false;
  existingPerson: boolean;
  existingParticipant: boolean;
  bookingHasParticipants: boolean;
  showAddress = false;


  @ViewChild(SearchEmailComponent)
  searchEmail: SearchEmailComponent;

  @ViewChild(ParticipantsListComponent)
  participantsListComponent: ParticipantsListComponent;

  constructor(
    private searchService: SearchService,
    private videoHearingService: VideoHearingsService,
    private participantService: ParticipantService,
    protected router: Router,
    protected bookingService: BookingService) {

    super(bookingService, router);
    this.titleList = searchService.TitleList;
  }

  ngOnInit() {
    super.ngOnInit();
    this.checkForExistingRequest();
    this.initializeForm();

    if (this.participantsListComponent) {
      const self = this;
      self.participantsListComponent.selectedParticipant.subscribe((participantEmail) => {
        self.selectedParticipantEmail = participantEmail;
        self.showDetails = true;
        setTimeout(() => {
          self.repopulateParticipantToEdit();
          self.displayUpdate();
          self.localEditMode = true;
          if (self.searchEmail) {
            self.setParticipantEmail();
          }
        }, 500);
      });

      this.participantsListComponent.selectedParticipantToRemove.subscribe((participantEmail) => {
        self.selectedParticipantEmail = participantEmail;
        this.confirmRemoveParticipant();
      });
    }
  }

  ngAfterViewInit() {
    setTimeout(() => {
      const self = this;
      this.videoHearingService.getParticipantRoles(this.hearing.case_type)
        .subscribe(
          (data: CaseAndHearingRolesResponse[]) => {
            self.setupRoles(data);
            if (self.editMode) {
              self.selectedParticipantEmail = self.bookingService.getParticipantEmail();
              if (!self.selectedParticipantEmail || self.selectedParticipantEmail.length === 0) {
                // no participants, we need to add one
                self.showDetails = false;
                self.displayAdd();
              } else {
                self.showDetails = true;
                setTimeout(() => {
                  if (this.searchEmail && this.participantDetails) {
                    this.setParticipantEmail();
                  }
                }, 500);

                self.displayNext();
              }
              self.repopulateParticipantToEdit();
            }
          }, error => console.error(error));
    }, 500);
  }

  ngAfterContentInit() {
    if (this.editMode) {
      if (this.searchEmail && this.participantDetails) {
        this.setParticipantEmail();
      }
    }
  }

  private setParticipantEmail() {
    this.searchEmail.email = this.participantDetails.email;
    this.searchEmail.isValidEmail = true;
    this.searchEmail.setEmailDisabled(this.participantDetails.id && this.participantDetails.id.length > 0);
  }

  initializeForm() {
    this.role = new FormControl(this.constants.PleaseSelect, [
      Validators.required,
      Validators.pattern(this.constants.PleaseSelectPattern)
    ]);
    this.party = new FormControl(this.constants.PleaseSelect, [
      Validators.required,
      Validators.pattern(this.constants.PleaseSelectPattern)
    ]);
    this.title = new FormControl(this.constants.PleaseSelect);
    this.firstName = new FormControl('', Validators.required);
    this.lastName = new FormControl('', Validators.required);
    this.phone = new FormControl('', [Validators.required, Validators.pattern(/^[0-9) -.]+$/)]);
    this.displayName = new FormControl('', Validators.required);
    this.companyName = new FormControl('');
    this.houseNumber = new FormControl('');
    this.street = new FormControl('');
    this.city = new FormControl('');
    this.county = new FormControl('');
    this.postcode = new FormControl('');
    this.participantForm = new FormGroup({
      role: this.role,
      party: this.party,
      title: this.title,
      firstName: this.firstName,
      lastName: this.lastName,
      phone: this.phone,
      displayName: this.displayName,
      companyName: this.companyName,
      houseNumber: this.houseNumber,
      street: this.street,
      city: this.city,
      county: this.county,
      postcode: this.postcode,
    });
    const self = this;
    this.participantForm.valueChanges.subscribe(
      result => {
        setTimeout(() => {
          if (self.showDetails && (self.role.value === self.constants.PleaseSelect &&
            self.party.value === self.constants.PleaseSelect &&
            self.title.value === self.constants.PleaseSelect &&
            self.firstName.value === '' &&
            self.lastName.value === '' &&
            self.phone.value === '' &&
            self.displayName.value === '') || self.editMode) {
            self.displayNext();
          } else if (!self.showDetails && (self.role.value === self.constants.PleaseSelect
            && self.party.value === self.constants.PleaseSelect)) {
            self.displayNext();
          } else if (self.showDetails && self.participantForm.valid && (self.searchEmail && self.searchEmail.validateEmail())) {
            if (self.localEditMode) {
              self.displayUpdate();
            } else {
              self.displayAdd();
            }
          } else {
            self.displayClear();
          }
        }, 500);
      });
  }

  private repopulateParticipantToEdit() {
    const selectedParticipant = this.hearing.participants.find(s => s.email === this.selectedParticipantEmail);
    if (selectedParticipant) {
      this.getParticipant(selectedParticipant);
    }
  }

  private checkForExistingRequest() {
    this.hearing = this.videoHearingService.getCurrentRequest();
    if (this.hearing) {
      const anyParticipants = this.hearing.participants.find(x => !x.is_judge);
      if (this.editMode) {
        this.bookingHasParticipants = anyParticipants && !anyParticipants.is_judge;
      }
    }
  }

  setupRoles(data: CaseAndHearingRolesResponse[]) {
    this.caseAndHearingRoles = this.participantService.mapParticipantsRoles(data);
    this.roleList = this.caseAndHearingRoles.filter(x => x.name !== 'Judge').map(x => x.name);
    this.roleList.unshift(this.constants.PleaseSelect);
    this.caseAndHearingRoles.forEach(x => {
      this.setupHearingRoles(x.name);
    });
  }

  setupHearingRoles(caseRoleName: string) {
    const list = this.caseAndHearingRoles.find(x => x.name === caseRoleName && x.name !== 'Judge');
    this.hearingRoleList = list ? list.hearingRoles : [];
    if (!this.hearingRoleList.find(s => s === this.constants.PleaseSelect)) {
      this.hearingRoleList.unshift(this.constants.PleaseSelect);
    }
  }

  public getParticipant(participantDetails) {
    this.displayErrorNoParticipants = false;
    this.displayAdd();
    this.participantDetails = Object.assign({}, participantDetails);

    if (participantDetails.is_exist_person) {
      this.disableLastFirstNames();
    }
    this.existingParticipant = participantDetails.id && participantDetails.id.length > 0;
    if (this.existingParticipant) {
      this.disableCaseAndHearingRoles();
      this.disableLastFirstNames();
    }
    // if it's added in the existing hearing participant, then allowed all fields to edit.
    this.resetPartyAndRole();

    if (this.constants.IndividualRoles.indexOf(this.participantDetails.hearing_role_name) > -1) {
      this.showAddress = true;
    }
    this.participantForm.setValue({
      party: this.participantDetails.case_role_name,
      role: this.participantDetails.hearing_role_name,
      title: this.participantDetails.title,
      firstName: this.participantDetails.first_name,
      lastName: this.participantDetails.last_name,
      phone: this.participantDetails.phone || '',
      displayName: this.participantDetails.display_name || '',
      companyName: this.participantDetails.company ? this.participantDetails.company : '',
      houseNumber: this.participantDetails.housenumber ? this.participantDetails.housenumber : '',
      street: this.participantDetails.street ? this.participantDetails.street : '',
      city: this.participantDetails.city ? this.participantDetails.city : '',
      county: this.participantDetails.county ? this.participantDetails.county : '',
      postcode: this.participantDetails.postcode ? this.participantDetails.postcode : ''
    });

    setTimeout(() => {
      this.participantForm.get('role').setValue(this.participantDetails.hearing_role_name);
    }, 500);

  }

  resetPartyAndRole() {
    if (this.participantDetails.case_role_name) {
      this.setupHearingRoles(this.participantDetails.case_role_name);
    }
    if (this.isPartySelected && !this.existingParticipant &&
      (!this.participantDetails.case_role_name || this.participantDetails.case_role_name.length === 0)) {
      this.participantDetails.case_role_name = this.party.value;
    }
    if (this.isRoleSelected && !this.existingParticipant &&
      (!this.participantDetails.hearing_role_name || this.participantDetails.hearing_role_name.length === 0)) {
      this.participantDetails.hearing_role_name = this.role.value;
    }
  }

  notFoundParticipant() {
    this.displayErrorNoParticipants = false;
    this.displayClear();
  }

  emailChanged() {
    if (this.participantForm.valid && this.showDetails && this.searchEmail.validateEmail()) {
      if (this.editMode) {
        this.displayNext();
      } else {
        this.displayAdd();
      }
    }
  }

  private displayAdd() {
    this.displayNextButton = false;
    this.displayClearButton = true;
    this.displayAddButton = true;
    this.displayUpdateButton = false;
  }

  private displayUpdate() {
    this.displayNextButton = false;
    this.displayClearButton = true;
    this.displayUpdateButton = true;
    this.displayAddButton = false;
  }

  private displayNext() {
    this.displayNextButton = true;
    this.displayClearButton = false;
    this.displayAddButton = false;
    this.displayUpdateButton = false;
  }

  private displayClear() {
    this.displayNextButton = false;
    this.displayClearButton = true;
    this.displayAddButton = false;
    this.displayUpdateButton = false;
  }
  get firstNameInvalid() {
    return this.firstName.invalid && (this.firstName.dirty || this.firstName.touched || this.isShowErrorSummary);
  }

  get lastNameInvalid() {
    return this.lastName.invalid && (this.lastName.dirty || this.lastName.touched || this.isShowErrorSummary);
  }

  get phoneInvalid() {
    return this.phone.invalid && (this.phone.dirty || this.phone.touched || this.isShowErrorSummary);
  }

  get partyInvalid() {
    return this.party.invalid && (this.party.dirty || this.party.touched || this.isShowErrorSummary);
  }

  get roleInvalid() {
    return this.role.invalid && (this.role.dirty || this.role.touched || this.isShowErrorSummary);
  }

  get displayNameInvalid() {
    return this.displayName.invalid && (this.displayName.dirty || this.displayName.touched || this.isShowErrorSummary)
      || (this.displayName.touched && this.displayName.value === '');
  }

  get houseNumberInvalid() {
    return this.houseNumber.invalid && (this.houseNumber.dirty || this.houseNumber.touched || this.isShowErrorSummary);
  }

  get streetInvalid() {
    return this.street.invalid && (this.street.dirty || this.street.touched || this.isShowErrorSummary);
  }
  get cityInvalid() {
    return this.city.invalid && (this.city.dirty || this.city.touched || this.isShowErrorSummary);
  }
  get countyInvalid() {
    return this.county.invalid && (this.county.dirty || this.county.touched || this.isShowErrorSummary);
  }

  get postcodeInvalid() {
    return this.postcode.invalid && (this.postcode.dirty || this.postcode.touched || this.isShowErrorSummary);
  }

  partySelected() {
    this.isPartySelected = this.party.value !== this.constants.PleaseSelect;
    this.setupHearingRoles(this.party.value);
  }

  roleSelected() {
    this.isRoleSelected = this.role.value !== this.constants.PleaseSelect;
    if (this.role.value !== this.constants.Solicitor) {
      this.showAddress = true;

      this.houseNumber.setValidators([Validators.required]);
      this.street.setValidators([Validators.required]);
      this.city.setValidators([Validators.required]);
      this.county.setValidators([Validators.required]);
      this.postcode.setValidators([Validators.required]);

      this.houseNumber.updateValueAndValidity();
      this.street.updateValueAndValidity();
      this.city.updateValueAndValidity();
      this.county.updateValueAndValidity();
      this.postcode.updateValueAndValidity();
    } else {
      this.showAddress = false;

      this.houseNumber.setValue('');
      this.street.setValue('');
      this.city.setValue('');
      this.county.setValue('');
      this.postcode.setValue('');
    }
    this.showDetails = true;
  }

  titleSelected() {
    this.isTitleSelected = this.title.value !== this.constants.PleaseSelect;
  }

  emailInvalid() {
    setTimeout(() => {
      return this.showDetails && this.searchEmail ? this.searchEmail.validateEmail() : true;
    });
  }

  showErrorSummary() {
    return !this.participantForm.valid || !this.isRoleSelected;
  }

  saveParticipant() {
    this.actionsBeforeSave();
    const validEmail = this.showDetails && (this.searchEmail ? this.searchEmail.validateEmail() : true);
    if (this.participantForm.valid && validEmail && this.isRoleSelected && this.isPartySelected && this.isTitleSelected) {
      this.isShowErrorSummary = false;
      this.participantForm.markAsUntouched();
      this.participantForm.markAsPristine();
      this.participantForm.updateValueAndValidity();
      const newParticipant = new ParticipantModel();
      this.mapParticipant(newParticipant);
      if (!this.participantService.checkDuplication(newParticipant.email, this.hearing.participants)) {
        this.hearing.participants.push(newParticipant);
        this.videoHearingService.updateHearingRequest(this.hearing);
        this.clearForm();
        this.displayNext();
        this.participantForm.markAsPristine();
        this.showDetails = false;
      } else {
        this.showConfirmationPopup = true;
        this.confirmationMessage = `You have already added ${newParticipant.first_name} ${newParticipant.last_name} to this hearing`;
      }
    } else {
      this.isShowErrorSummary = true;
    }
  }

  updateParticipantAction() {
    this.updateParticipant();
    this.videoHearingService.updateHearingRequest(this.hearing);
    this.displayNext();
    this.localEditMode = false;
  }


  updateParticipant() {
    if (!this.existingParticipant && !this.participantDetails) {
      this.saveParticipant();
      this.bookingHasParticipants = true;
    } else {
      const validEmail = this.showDetails && this.searchEmail ? this.searchEmail.validateEmail() : true;
      this.actionsBeforeSave();
      if (this.participantForm.valid && validEmail && this.isRoleSelected && this.isTitleSelected) {
        this.isShowErrorSummary = false;
        this.hearing.participants.forEach(newParticipant => {
          if (newParticipant.email === this.selectedParticipantEmail) {
            this.mapParticipant(newParticipant);
          }
        });
        this.clearForm();
        this.participantDetails = null;
        this.participantForm.markAsPristine();
      } else {
        this.isShowErrorSummary = true;
      }
    }
  }

  actionsBeforeSave() {
    this.roleSelected();
    this.role.markAsTouched();
    this.firstName.markAsTouched();
    this.lastName.markAsTouched();
    this.phone.markAsTouched();
    this.displayName.markAsTouched();
  }

  confirmRemoveParticipant() {
    if (this.selectedParticipantEmail) {
      const participant = this.hearing.participants.find(x => x.email.toLowerCase() === this.selectedParticipantEmail.toLowerCase());
      this.removerFullName = participant ? `${participant.title} ${participant.first_name} ${participant.last_name}` : '';
      const anyParticipants = this.hearing.participants.filter(x => !x.is_judge);
      this.bookingHasParticipants = anyParticipants && anyParticipants.length < 2;
      this.showConfirmationRemoveParticipant = true;
    }
  }

  removeParticipant() {
    // check if participant details were populated, if yes then clean form.
    if (this.searchEmail && this.searchEmail.email === this.selectedParticipantEmail) {
      this.clearForm();
    }
    this.participantService.removeParticipant(this.hearing, this.selectedParticipantEmail);
    this.videoHearingService.updateHearingRequest(this.hearing);
  }

  mapParticipant(newParticipant: ParticipantModel) {
    newParticipant.first_name = this.firstName.value;
    newParticipant.last_name = this.lastName.value;
    newParticipant.phone = this.phone.value;
    newParticipant.title = this.title.value;
    newParticipant.case_role_name = this.party.value;
    newParticipant.hearing_role_name = this.role.value;
    newParticipant.email = this.searchEmail ? this.searchEmail.email : '';
    newParticipant.display_name = this.displayName.value;
    newParticipant.company = this.companyName.value;
    newParticipant.username = this.searchEmail ? this.searchEmail.email : '';
    newParticipant.housenumber = this.houseNumber.value;
    newParticipant.street = this.street.value;
    newParticipant.city = this.city.value;
    newParticipant.county = this.county.value;
    newParticipant.postcode = this.postcode.value;
  }

  addParticipantCancel() {
    if (this.editMode) {
      if (this.participantForm.dirty || this.participantForm.touched) {
        this.attemptingDiscardChanges = true;
      } else {
        this.navigateToSummary();
      }
    } else {
      this.showCancelPopup = true;
    }
  }

  handleContinueBooking() {
    this.showCancelPopup = false;
    this.attemptingDiscardChanges = false;
  }

  handleCancelBooking() {
    this.showCancelPopup = false;
    this.participantForm.reset();
    if (this.editMode) {
      this.navigateToSummary();
    } else {
      this.videoHearingService.cancelRequest();
      this.router.navigate(['/dashboard']);
    }
  }

  cancelChanges() {
    this.attemptingDiscardChanges = false;
    this.participantForm.reset();
    this.navigateToSummary();
  }

  handleConfirmation() {
    this.showConfirmationPopup = false;
  }

  handleContinueRemove() {
    this.showConfirmationRemoveParticipant = false;
    this.removeParticipant();
  }

  handleCancelRemove() {
    this.showConfirmationRemoveParticipant = false;
  }

  clearForm() {
    this.enableFields();
    this.participantForm.setValue(
      {
        role: this.constants.PleaseSelect,
        party: this.constants.PleaseSelect,
        title: this.constants.PleaseSelect,
        firstName: '',
        lastName: '',
        phone: '',
        displayName: '',
        companyName: '',
        houseNumber: '',
        street: '',
        city: '',
        county: '',
        postcode: '',
      });
    this.participantForm.markAsUntouched();
    this.participantForm.markAsPristine();
    this.participantForm.updateValueAndValidity();
    if (this.showDetails && this.searchEmail) {
      this.searchEmail.clearEmail();
    }
    this.showDetails = false;
    this.resetEditMode();
    this.localEditMode = false;
    this.isShowErrorSummary = false;
    this.isRoleSelected = true;
    this.isPartySelected = true;

    if (this.hearing.participants.length > 1) {
      this.displayNext();
    }
  }

  next() {
    if (this.checkParticipants()) {
      if (this.editMode) {
        this.updateParticipant();
        this.videoHearingService.updateHearingRequest(this.hearing);
        if (this.isShowErrorSummary) {
          return;
        }
        this.navigateToSummary();
      } else {
        this.router.navigate(['/other-information']);
      }
    } else {
      this.displayErrorNoParticipants = true;
    }
  }

  private checkParticipants(): boolean {
    let participantsValid = false;
    if (this.hearing.participants && this.hearing.participants.length > 0) {
      const anyParticipants = this.hearing.participants.filter(x => !x.is_judge);
      participantsValid = anyParticipants && anyParticipants.length > 0;
    }
    return participantsValid;
  }

  hasChanges(): Observable<boolean> | boolean {
    if (this.participantForm.dirty) {
      this.showCancelPopup = true;
    }
    return this.participantForm.dirty;
  }

  goToDiv(fragment: string): void {
    window.document.getElementById(fragment).parentElement.parentElement.scrollIntoView();
  }


  disableLastFirstNames() {
    this.participantForm.get('lastName').disable();
    this.participantForm.get('firstName').disable();
  }
  disableCaseAndHearingRoles() {
    this.participantForm.get('party').disable();
    this.participantForm.get('role').disable();
  }
  enableFields() {
    this.participantForm.get('lastName').enable();
    this.participantForm.get('firstName').enable();
    this.participantForm.get('party').enable();
    this.participantForm.get('role').enable();
  }

  ngOnDestroy() {
    this.clearForm();
  }
}
