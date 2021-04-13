import { Component, EventEmitter, Output, OnInit, Input, OnDestroy } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { JudgeResponse, PersonResponse } from '../../services/clients/api-client';
import { Constants } from '../../common/constants';
import { ParticipantModel } from '../../common/model/participant.model';
import { SearchService } from '../../services/search.service';
import { ConfigService } from 'src/app/services/config.service';
import { Logger } from '../../services/logger';
import { debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs/operators';
import { JudgeDataService } from '../services/judge-data.service';

@Component({
    selector: 'app-search-email',
    templateUrl: './search-email.component.html',
    styleUrls: ['./search-email.component.css'],
    providers: [SearchService]
})
export class SearchEmailComponent implements OnInit, OnDestroy {
    private readonly loggerPrefix = '[SearchEmail] -';
    constants = Constants;
    participantDetails: ParticipantModel;
    searchTerm = new Subject<string>();
    results: ParticipantModel[] = [];
    isShowResult = false;
    notFoundParticipant = false;
    email = '';
    isValidEmail = true;
    $subscriptions: Subscription[] = [];
    invalidPattern: string;
    isErrorEmailAssignedToJudge = false;
    isJoh = false;
    notFoundEmailEvent = new Subject<boolean>();
    notFoundEmailEvent$ = this.notFoundEmailEvent.asObservable();

    @Input() disabled = true;

    @Input() hearingRoleParticipant = '';

    @Output() findParticipant = new EventEmitter<ParticipantModel>();

    @Output() emailChanged = new EventEmitter<string>();

    @Input() includeJudges = false;

    constructor(private searchService: SearchService, private configService: ConfigService, private logger: Logger) {}

    ngOnInit() {
        this.$subscriptions.push(
            this.searchTerm
                .pipe(debounceTime(500))
                .pipe(distinctUntilChanged())
                .pipe(
                    switchMap(term => {
                        return this.searchService.participantSearch(term, this.hearingRoleParticipant);
                    })
                )
                .subscribe(personsFound => {
                    if (personsFound && personsFound.length > 0) {
                        this.getData(personsFound);
                    } else {
                        if (this.email.length > 2) {
                            this.noDataFound();
                        } else {
                            this.lessThanThreeLetters();
                        }
                        this.isShowResult = false;
                        this.results = undefined;
                    }
                })
        );

        this.$subscriptions.push(this.searchTerm.subscribe(s => (this.email = s)));
        this.getEmailPattern();
    }

    async getEmailPattern() {
        this.$subscriptions.push(
            this.configService
                .getClientSettings()
                .pipe(map(x => x.test_username_stem))
                .subscribe(x => {
                    this.invalidPattern = x;
                    if (!this.invalidPattern || this.invalidPattern.length === 0) {
                        this.logger.error(`${this.loggerPrefix} Pattern to validate email is not set`, new Error('Email validation error'));
                    } else {
                        this.logger.info(`${this.loggerPrefix} Pattern to validate email is set with length ${this.invalidPattern.length}`);
                    }
                })
        );
    }

    getData(data: ParticipantModel[]) {
        this.results = data;
        this.isShowResult = true;
        this.isValidEmail = true;
        this.notFoundParticipant = false;
        this.notFoundEmailEvent.next(false);
    }

    noDataFound() {
        this.isErrorEmailAssignedToJudge = this.hearingRoleParticipant === 'Panel Member' || this.hearingRoleParticipant === 'Winger';
        this.isShowResult = false;
        this.notFoundParticipant = !this.isErrorEmailAssignedToJudge;
        this.notFoundEmailEvent.next(true);
    }

    lessThanThreeLetters() {
        this.isShowResult = false;
        this.notFoundParticipant = false;
        this.notFoundEmailEvent.next(false);
    }

    selectItemClick(result: ParticipantModel) {
        this.email = result.email;

        const selectedResult = new ParticipantModel();
        selectedResult.email = result.email;
        selectedResult.first_name = result.first_name;
        selectedResult.last_name = result.last_name;
        selectedResult.title = result.title;
        selectedResult.phone = result.phone;
        selectedResult.company = result.company;
        selectedResult.is_exist_person = true;
        selectedResult.username = result.username;
        this.isShowResult = false;
        this.findParticipant.emit(selectedResult);
    }

    validateEmail() {
        const pattern = Constants.EmailPattern;
        this.isValidEmail =
            this.email &&
            this.email.length > 0 &&
            this.email.length < 256 &&
            pattern.test(this.email) &&
            this.email.indexOf(this.invalidPattern) < 0;

        return this.isValidEmail;
    }

    blur() {
        this.isShowResult = false;
    }

    clearEmail() {
        this.email = '';
        this.isValidEmail = true;
        this.notFoundParticipant = false;
    }

    blurEmail() {
        if (!this.results || this.results.length === 0) {
            this.validateEmail();
            this.emailChanged.emit(this.email);
        }
    }

    onChange() {
        this.isErrorEmailAssignedToJudge = false;
    }

    ngOnDestroy() {
        this.$subscriptions.forEach(subscription => {
            if (subscription) {
                subscription.unsubscribe();
            }
        });
    }

    populateParticipantInfo(email: string) {
        if (this.results && this.results.length > 0) {
            const participant = this.results.find(p => p.email === email);
            if (participant) {
                this.selectItemClick(participant);
            }
        }
    }
}
