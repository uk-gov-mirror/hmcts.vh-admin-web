import { MomentModule } from 'ngx-moment';
import { SuitabilityModule } from './suitability/suitability.module';
import { DatePipe } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { APP_INITIALIZER, NgModule, ErrorHandler } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BookingModule } from './booking/booking.module';
import { BookingsListModule } from './bookings-list/bookings-list.module';
import { ChangesGuard } from './common/guards/changes.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AuthGuard } from './security/auth.guard';
import { AdminGuard } from './security/admin.guard';
import { VhOfficerAdminGuard } from './security/vh-officer-admin.guard';
import { LoginComponent } from './security/login.component';
import { LogoutComponent } from './security/logout.component';
import { BH_API_BASE_URL } from './services/clients/api-client';
import { ConfigService, ENVIRONMENT_CONFIG } from './services/config.service';
import { UserIdentityService } from './services/user-identity.service';
import { UnauthorisedComponent } from './error/unauthorised.component';
import { PopupModule } from './popups/popup.module';
import { SharedModule } from './shared/shared.module';
import { ErrorComponent } from './error/error.component';
import { ErrorService } from './services/error.service';
import { LoggerService, LOG_ADAPTER } from './services/logger.service';
import { PageTrackerService } from './services/page-tracker.service';
import { AppInsightsLogger } from './services/app-insights-logger.service';
import { Logger } from './services/logger';
import { ConsoleLogger } from './services/console-logger';
import { Config } from './common/model/config';
import { WindowRef } from './security/window-ref';
import { UnsupportedBrowserComponent } from './shared/unsupported-browser/unsupported-browser.component';
import { ChangePasswordComponent } from './change-password/change-password.component';
import { GetAudioFileModule } from './get-audio-file/get-audio-file.module';
import { DeleteParticipantModule } from './delete-participant/delete-participant.module';
import { EditParticipantModule } from './edit-participant/edit-participant.module';
import { AuthConfigModule } from './security/auth-config.module';

export function loadConfig(configService: ConfigService) {
    return () => configService.loadConfig();
}

@NgModule({
    declarations: [
        AppComponent,
        DashboardComponent,
        LoginComponent,
        LogoutComponent,
        UnauthorisedComponent,
        ErrorComponent,
        UnsupportedBrowserComponent,
        ChangePasswordComponent
    ],
    imports: [
        MomentModule,
        BookingModule,
        BookingsListModule,
        BrowserModule,
        SuitabilityModule,
        AppRoutingModule,
        SharedModule,
        PopupModule,
        GetAudioFileModule,
        DeleteParticipantModule,
        EditParticipantModule,
        AuthConfigModule
    ],
    providers: [
        HttpClientModule,
        ReactiveFormsModule,
        AppRoutingModule,
        { provide: APP_INITIALIZER, useFactory: loadConfig, deps: [ConfigService], multi: true },
        { provide: Config, useFactory: () => ENVIRONMENT_CONFIG },
        { provide: BH_API_BASE_URL, useFactory: () => '.' },
        { provide: LOG_ADAPTER, useClass: ConsoleLogger, multi: true },
        { provide: LOG_ADAPTER, useClass: AppInsightsLogger, multi: true },
        { provide: Logger, useClass: LoggerService },
        ConfigService,
        AuthGuard,
        ChangesGuard,
        DatePipe,
        UserIdentityService,
        AdminGuard,
        VhOfficerAdminGuard,
        { provide: ErrorHandler, useClass: ErrorService },
        LoggerService,
        ErrorService,
        PageTrackerService,
        AppInsightsLogger,
        WindowRef
    ],
    bootstrap: [AppComponent]
})
export class AppModule {}
