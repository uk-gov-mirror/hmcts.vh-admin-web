﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using AcceptanceTests.Common.Api.Helpers;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Driver.Drivers;
using AcceptanceTests.Common.Driver.Helpers;
using AcceptanceTests.Common.Test.Steps;
using AdminWebsite.AcceptanceTests.Data;
using AdminWebsite.AcceptanceTests.Helpers;
using AdminWebsite.AcceptanceTests.Pages;
using BookingsApi.Contract.Responses;
using TestApi.Contract.Dtos;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace AdminWebsite.AcceptanceTests.Steps
{
    [Binding]
    public class SummarySteps : ISteps
    {
        private const int TIMEOUT = 60;
        private readonly TestContext _c;
        private readonly Dictionary<UserDto, UserBrowser> _browsers;
        private readonly BookingDetailsSteps _bookingDetailsSteps;
        private readonly HearingDetailsSteps _hearingDetailsSteps;
        private readonly HearingScheduleSteps _hearingScheduleSteps;
        private readonly AssignJudgeSteps _assignJudgeSteps;
        private readonly AddParticipantSteps _addParticipantSteps;
        private readonly OtherInformationSteps _otherInformationSteps;
        private readonly VideoAccessPointsSteps _videoAccessPointsSteps;
        private UserAccount _newUserToEdit;

        public SummarySteps(
            TestContext testContext,
            Dictionary<UserDto, UserBrowser> browsers,
            BookingDetailsSteps bookingDetailsSteps,
            HearingDetailsSteps hearingDetailsSteps,
            HearingScheduleSteps hearingScheduleSteps,
            AssignJudgeSteps assignJudgeSteps,
            AddParticipantSteps addParticipantSteps,
            OtherInformationSteps otherInformationSteps,
            VideoAccessPointsSteps videoAccessPointsSteps)
        {
            _c = testContext;
            _browsers = browsers;
            _bookingDetailsSteps = bookingDetailsSteps;
            _hearingDetailsSteps = hearingDetailsSteps;
            _hearingScheduleSteps = hearingScheduleSteps;
            _assignJudgeSteps = assignJudgeSteps;
            _addParticipantSteps = addParticipantSteps;
            _otherInformationSteps = otherInformationSteps;
            _videoAccessPointsSteps = videoAccessPointsSteps;
        }

        [When(@"the user views the information on the summary form")]
        public void ProgressToNextPage()
        {
            VerifyHearingDetails();
            VerifyHearingSchedule();
            VerifyAudioRecording();
            VerifyVideoAccessPoints();
            VerifyOtherInformation();
            ClickBook();
            VerifyBookingsCreated();
            VerifyNewUsersCreatedInAad();
        }

        [Then(@"the user views the information on the summary form")]
        public void ThenTheUserViewsTheInformationOnTheSummaryForm()
        {
            ClickBook();
            VerifyBookingsCreated();
            VerifyNewUsersCreatedInAad();
        }

        private void ClickBook()
        {
            _browsers[_c.CurrentUser].Click(SummaryPage.BookButton);
            _browsers[_c.CurrentUser].Driver.WaitUntilElementNotVisible(SummaryPage.WaitPopUp);
            _c.Test.CreatedBy = _c.CurrentUser.Username;
        }

        [When(@"the user edits the (.*)")]
        public void WhenTheUserEditsTheHearing(string screen)
        {
            _bookingDetailsSteps.ClickEdit();
            _browsers[_c.CurrentUser].Click(SummaryPage.EditScreenLink(screen));

            if (screen.Equals("hearing details"))
            {
                _hearingDetailsSteps.EditHearingDetails();
            }
            else if (screen.Equals("hearing schedule"))
            {
                _hearingScheduleSteps.EditHearingSchedule();
            }
            else if (screen.Equals("audio recording"))
            {
                _assignJudgeSteps.EditAudioRecording();
            }
            else if (screen.Equals("other information"))
            {
                _otherInformationSteps.ProgressToNextPage();
            }
        }

        [When(@"the user edits a new participant")]
        public void WhenTheUserEditsANewParticipant()
        {
            _bookingDetailsSteps.ClickEdit();
            _newUserToEdit = UserManager.GetUserFromDisplayName(_c.Test.HearingParticipants, _c.Test.AddParticipant.Participant.NewUserPrefix);
            _browsers[_c.CurrentUser].Click(SummaryPage.EditParticipantLink(_newUserToEdit.Firstname));
            _addParticipantSteps.EditANewParticipant(_newUserToEdit.AlternativeEmail);
        }

        [When(@"the user edits an Interpreter")]
        public void WhenTheUserEditsAnInterpreter()
        { 
            var user = GetParticipantBy("Interpreter");
            _browsers[_c.CurrentUser].Click(SummaryPage.EditParticipantLink(user.Firstname));
            _addParticipantSteps.EditAnInterpreter(user.AlternativeEmail,false);
        }

        [When(@"the user edits a saved Interpreter")]
        public void WhenTheUserEditsASavedInterpreter()
        {
            var user = GetParticipantBy("Interpreter");
            _browsers[_c.CurrentUser].Click(SummaryPage.EditParticipantLink(user.Firstname));
            _addParticipantSteps.EditAnInterpreter(user.AlternativeEmail);
        }



        [When(@"the user removes participant")]
        public void WhenTheUserRemovesParticipant()
        {
            var participant = GetParticipantBy("Litigant in person");
            _browsers[_c.CurrentUser].Click(SummaryPage.RemoveParticipantLink(participant.Firstname));
            _browsers[_c.CurrentUser].Driver.WaitUntilVisible(SummaryPage.RemoveInterpreterMessage).Displayed.Should().BeTrue();
            _browsers[_c.CurrentUser].Click(SummaryPage.RemoveInterpreter);
            RemoveParticipant(participant);
            RemoveParticipant(GetParticipantBy("Interpreter"));
        }

        [When(@"the user removes Interpreter")]
        public void WhenTheUserRemovesInterpreter()
        {
            var role = "Interpreter";
            _browsers[_c.CurrentUser].Click(SummaryPage.RemoveParticipantLink(GetParticipantBy(role).Firstname));
            _browsers[_c.CurrentUser].Click(SummaryPage.RemoveParticipant);
            RemoveParticipant(GetParticipantBy(role));
        }

        private void RemoveParticipant(UserAccount user)
        { 
            _c.Test.HearingParticipants.Remove(user);
            ParticipantListContains(user).Should().BeFalse();
        }

        private bool ParticipantListContains(UserAccount interpreter)
        {
            var participantDetails = _addParticipantSteps.GetAllParticipantsDetails();
            return participantDetails.Any(p => p.Contains(interpreter.Firstname));
        }

        private UserAccount GetParticipantBy(string role)
        {
            return _c.Test.HearingParticipants.Where(h => h.HearingRoleName == role).FirstOrDefault();
        }

        [When(@"the user edits an endpoint display name")]
        public void WhenTheUserEditsAnEndpointDisplayName()
        {
            _bookingDetailsSteps.ClickEdit();
            _browsers[_c.CurrentUser].Click(SummaryPage.EditScreenLink("video access points"));
            _videoAccessPointsSteps.ProgressToNextPage();
        }

        [Then(@"the details are updated")]
        public void ThenTheHearingIsUpdated()
        {
            VerifyHearingDetails();
            VerifyHearingSchedule();
            VerifyAudioRecording();
            VerifyVideoAccessPoints();
            VerifyOtherInformation();
            ClickBook();
            VerifyBookingUpdated();
        }

        [Then(@"the participant details are updated")]
        public void ThenTheParticipantDetailsAreUpdated()
        {
            ClickBook();
            _c.Api.PollForParticipantNameUpdated(Users.GetJudgeUser(_c.Users).Username, _c.Test.AddParticipant.Participant.NewUserPrefix).Should().BeTrue();
        }

        [Then(@"the questionnaires have been sent")]
        public void ThenTheQuestionnairesHaveBeenSent()
        {
            ProgressToNextPage();
        }

        [Then(@"audio recording is set to No")]
        public void ThenAudioRecordingIsSetToNo()
        {
            VerifyAudioRecording(false);
        }


        private void VerifyHearingDetails()
        {
            _browsers[_c.CurrentUser].TextOf(SummaryPage.CaseNumber).Should().Be(_c.Test.HearingDetails.CaseNumber);
            _browsers[_c.CurrentUser].TextOf(SummaryPage.CaseName).Should().Be(_c.Test.HearingDetails.CaseName);
            _browsers[_c.CurrentUser].TextOf(SummaryPage.CaseType).Should().Be(_c.Test.HearingDetails.CaseType.Name);
            _browsers[_c.CurrentUser].TextOf(SummaryPage.HearingType).Should().Be(_c.Test.HearingDetails.HearingType.Name);
        }

        private void VerifyHearingSchedule()
        {
            if (!_c.Test.HearingSchedule.MultiDays)
            {
                var scheduleDate = _c.Test.HearingSchedule.ScheduledDate.ToString(DateFormats.HearingSummaryDate);
                _browsers[_c.CurrentUser].TextOf(SummaryPage.HearingDate).ToLower().Should().Be(scheduleDate.ToLower());
                var listedFor = $"listed for {_c.Test.HearingSchedule.DurationMinutes} minutes";
                _browsers[_c.CurrentUser].TextOf(SummaryPage.HearingDuration).Should().Be(listedFor);
            }
            else
            {
                var startDate = _c.Test.HearingSchedule.ScheduledDate.ToString(DateFormats.HearingSummaryDateMultiDays);
                var endDate = _c.Test.HearingSchedule.EndHearingDate.ToString(DateFormats.HearingSummaryDateMultiDays);
                var startTime = _c.Test.HearingSchedule.ScheduledDate.ToString(DateFormats.HearingSummaryTimeMultiDays);
                var textDateStart = $"{startDate.ToLower()} -";
                var textDateEnd = $"{endDate.ToLower()}, {startTime.ToLower()}";

                _browsers[_c.CurrentUser].TextOf(SummaryPage.HearingStartDateMultiDays).ToLower().Should().Be(textDateStart);
                _browsers[_c.CurrentUser].TextOf(SummaryPage.HearingEndDateMultiDays).ToLower().Should().Be(textDateEnd);
            }

            var courtAddress = $"{_c.Test.HearingSchedule.HearingVenue}, {_c.Test.HearingSchedule.Room}";
            _browsers[_c.CurrentUser].TextOf(SummaryPage.CourtAddress).Should().Be(courtAddress);
        }

        private void VerifyAudioRecording(bool audioFlag=true)
        {
            _browsers[_c.CurrentUser].TextOf(SummaryPage.AudioRecording).Should().Be(audioFlag ? "Yes":"No");
        }

        private void VerifyOtherInformation()
        {
            var otherInformation = OtherInformationSteps.GetOtherInfo(_c.Test.TestData.OtherInformationDetails.OtherInformation);
            _browsers[_c.CurrentUser].TextOf(SummaryPage.OtherInformation).Should().Be(otherInformation);
        }

        private void VerifyVideoAccessPoints()
        {
            var videoAccessPoints = _c.Test.VideoAccessPoints.DisplayName;
            _browsers[_c.CurrentUser].TextOf(SummaryPage.VideoAccessPoints(0)).Should().Be(videoAccessPoints);
        }

        private void VerifyBookingsCreated()
        {
            var hearings = PollForAllHearings();

            foreach (var hearing in hearings)
            {
                var expectedScheduledDate = _c.TimeZone.AdjustAdminWeb(_c.Test.HearingSchedule.ScheduledDate);
                AssertHearing.AssertHearingDetails(hearing, _c.Test);
                AssertHearing.AssertHearingParticipants(hearing.Participants, _c.Test.HearingParticipants, _c.Test.AddParticipant.Participant.Organisation);
                AssertHearing.AssertCreatedBy(hearing.CreatedBy, _c.CurrentUser.Username);
                var day = GetDayOfHearing(hearing.Cases.First().Name);
                AssertHearing.AssertScheduledDate(day, hearing.ScheduledDateTime, expectedScheduledDate, _c.Test.HearingSchedule.MultiDays, _c.WebConfig.SauceLabsConfiguration.RunningOnSauceLabs());
                AssertHearing.AssertTimeSpansMatch(hearing.ScheduledDuration, _c.Test.HearingSchedule.DurationHours, _c.Test.HearingSchedule.DurationMinutes, _c.Test.HearingSchedule.MultiDays);
            }
        }

        private void VerifyBookingUpdated()
        {
            var hearings = PollForHearingUpdated();

            foreach (var hearing in hearings)
            {
                var expectedScheduledDate = _c.TimeZone.AdjustAdminWeb(_c.Test.HearingSchedule.ScheduledDate);
                AssertHearing.AssertHearingDetails(hearing, _c.Test);
                AssertHearing.AssertHearingParticipants(hearing.Participants, _c.Test.HearingParticipants, _c.Test.AddParticipant.Participant.Organisation);
                AssertHearing.AssertCreatedBy(hearing.CreatedBy, _c.CurrentUser.Username);
                var day = GetDayOfHearing(hearing.Cases.First().Name);
                AssertHearing.AssertScheduledDate(day, hearing.ScheduledDateTime, expectedScheduledDate, _c.Test.HearingSchedule.MultiDays, _c.WebConfig.SauceLabsConfiguration.RunningOnSauceLabs());
                AssertHearing.AssertTimeSpansMatch(hearing.ScheduledDuration, _c.Test.HearingSchedule.DurationHours, _c.Test.HearingSchedule.DurationMinutes, _c.Test.HearingSchedule.MultiDays);
                AssertHearing.AssertUpdatedStatus(hearing, _c.CurrentUser.Username, DateTime.Now);
            }
        }

        private static int GetDayOfHearing(string caseName)
        {
            if (!caseName.Contains("Day") || !caseName.Contains("of")) return 1;
            var daysPart = caseName[^("x of x").Length..];
            var day = daysPart.Substring(0, 1);
            return int.Parse(day);
        }

        private IEnumerable<HearingDetailsResponse> PollForHearingUpdated()
        {
            const int RETRIES = 10;
            const int DELAY = 2;

            for (var i = 0; i < RETRIES; i++)
            {
                var hearings = PollForAllHearings();

                var pollForHearingUpdated = hearings as HearingDetailsResponse[] ?? hearings.ToArray();
                if (pollForHearingUpdated.All(x => x.UpdatedBy != null))
                {
                    return pollForHearingUpdated;
                }

                Thread.Sleep(TimeSpan.FromSeconds(DELAY));
            }

            throw new DataException($"Hearings not updated after {RETRIES * DELAY} seconds");
        }

        private IEnumerable<HearingDetailsResponse> PollForAllHearings()
        {
            const int RETRIES = 10;
            const int DELAY = 2;

            for (var i = 0; i < RETRIES; i++)
            {
                var response = _c.Api.PollForHearingByUsername(Users.GetJudgeUser(_c.Users).Username, _c.Test.HearingDetails.CaseName);
                var allHearings = RequestHelper.Deserialise<List<HearingDetailsResponse>>(response.Content);
                var hearings = GetHearingFromHearings(allHearings);

                if (!_c.Test.HearingSchedule.MultiDays)
                {
                    return hearings;
                }

                var pollForAllHearings = hearings as HearingDetailsResponse[] ?? hearings.ToArray();
                if (pollForAllHearings.Count().Equals(_c.Test.HearingSchedule.NumberOfMultiDays))
                {
                    return pollForAllHearings;
                }

                Thread.Sleep(TimeSpan.FromSeconds(DELAY));
            }
            
            throw new DataException($"All hearings not created after {RETRIES * DELAY} seconds");
        }

        private IEnumerable<HearingDetailsResponse> GetHearingFromHearings(IEnumerable<HearingDetailsResponse> allHearings)
        {
            var hearings = allHearings.Where(hearing => hearing.Cases.First().Name.Contains(_c.Test.HearingDetails.CaseName)).ToList();

            if (hearings.Count == 0)
            {
                throw new DataException("Created hearing could not be found in the bookings api");
            }

            return hearings;
        }

        private void VerifyNewUsersCreatedInAad()
        {
            foreach (var participant in _c.Test.HearingParticipants.Where(participant => participant.DisplayName.Contains(_c.Test.TestData.AddParticipant.Participant.NewUserPrefix)))
            {
                _c.Api.PollForParticipantExistsInAD(participant.Username, TIMEOUT);
            }
            _c.Test.SubmittedAndCreatedNewAadUsers = true;
        }
    }
}
