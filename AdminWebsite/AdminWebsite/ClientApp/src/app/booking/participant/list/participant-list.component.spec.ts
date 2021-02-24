import { DebugElement } from '@angular/core';
import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { ParticipantModel } from 'src/app/common/model/participant.model';
import { Logger } from 'src/app/services/logger';
import { ParticipantListComponent } from './participant-list.component';

const loggerSpy = jasmine.createSpyObj<Logger>('Logger', ['error', 'debug', 'warn']);
const router = {
    navigate: jasmine.createSpy('navigate'),
    url: '/summary'
};

describe('ParticipantListComponent', () => {
    let component: ParticipantListComponent;
    let fixture: ComponentFixture<ParticipantListComponent>;
    let debugElement: DebugElement;
    const pat1 = new ParticipantModel();
    pat1.title = 'Mrs';
    pat1.first_name = 'Sam';
    const participants: any[] = [pat1, pat1];

    beforeEach(
        waitForAsync(() => {
            TestBed.configureTestingModule({
                declarations: [ParticipantListComponent],
                providers: [
                    { provide: Logger, useValue: loggerSpy },
                    { provide: Router, useValue: router }
                ],
                imports: [RouterTestingModule]
            }).compileComponents();
        })
    );

    beforeEach(() => {
        fixture = TestBed.createComponent(ParticipantListComponent);
        debugElement = fixture.debugElement;
        component = debugElement.componentInstance;

        fixture.detectChanges();
    });

    it('should create participants list component', () => {
        expect(component).toBeTruthy();
    });

    it('should display participants', done => {
        component.participants = participants;
        component.ngOnInit();
        fixture.whenStable().then(() => {
            fixture.detectChanges();
            const elementArray = debugElement.queryAll(By.css('app-participant-item'));
            expect(elementArray.length).toBeGreaterThan(0);
            expect(elementArray.length).toBe(2);
            done();
        });
    });
    it('previous url summary', () => {
        component.ngOnInit();
        expect(component.isSummaryPage).toBeTruthy();
        expect(component.isEditRemoveVisible).toBeTruthy();
    });
    it('should emit on remove', () => {
        spyOn(component.$selectedForRemove, 'emit');
        component.removeParticipant({ email: 'email@hmcts.net', is_exist_person: false, is_judge: false });
        expect(component.$selectedForRemove.emit).toHaveBeenCalled();
    });
});
