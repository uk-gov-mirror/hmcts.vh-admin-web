import { NgModule, Type } from '@angular/core';
import { PopupModule } from '../popups/popup.module';
import { SharedModule } from '../shared/shared.module';
import { AddParticipantComponent } from './add-participant/add-participant.component';
import { AssignJudgeComponent } from './assign-judge/assign-judge.component';
import { BookingConfirmationComponent } from './booking-confirmation/booking-confirmation.component';
import { BookingRoutingModule } from './booking-routing.module';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';
import { CreateHearingComponent } from './create-hearing/create-hearing.component';
import { HearingScheduleComponent } from './hearing-schedule/hearing-schedule.component';
import { OtherInformationComponent } from './other-information/other-information.component';
import { SearchEmailComponent } from './search-email/search-email.component';
import { SummaryComponent } from './summary/summary.component';
import { RemovePopupComponent } from '../popups/remove-popup/remove-popup.component';
import { EndpointsComponent } from './endpoints/endpoints.component';
import { ParticipantItemComponent, ParticipantListComponent } from './participant';

export const Components: Type<any>[] = [
    CreateHearingComponent,
    HearingScheduleComponent,
    AssignJudgeComponent,
    AddParticipantComponent,
    RemovePopupComponent,
    OtherInformationComponent,
    SummaryComponent,
    BookingConfirmationComponent,
    BreadcrumbComponent,
    SearchEmailComponent,
    ParticipantListComponent,
    ParticipantItemComponent,
    EndpointsComponent
];

@NgModule({
    imports: [SharedModule, BookingRoutingModule, PopupModule],
    declarations: Components,
    exports: Components
})
export class BookingModule {}
