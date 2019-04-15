import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { ContactUsComponent } from './contact-us/contact-us.component';
import { FooterComponent } from './footer/footer.component';
import { HeaderComponent } from './header/header.component';
import { PaginationComponent } from './pagination/pagination.component';
import { SharedRoutingModule } from './shared-routing.module';
import { ScrollableDirective } from './directives/scrollable.directive';
import { BookingEditComponent } from './booking-edit/booking-edit.component';
import { LongDatetimePipe } from './directives/date-time.pipe';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    SharedRoutingModule
  ],
  declarations: [
    HeaderComponent,
    FooterComponent,
    ContactUsComponent,
    PaginationComponent,
    ScrollableDirective,
    BookingEditComponent,
    SignOutComponent,
    LongDatetimePipe
  ],
  exports: [
    HeaderComponent,
    FooterComponent,
    ContactUsComponent,
    PaginationComponent,
    BookingEditComponent,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    ScrollableDirective,
    SignOutComponent,
    LongDatetimePipe
  ]
})
export class SharedModule { }
