using AdminWebsite.Models;
using AdminWebsite.Security;
using AdminWebsite.Services;
using AdminWebsite.Services.Models;
using AdminWebsite.UnitTests.Helper;
using AdminWebsite.UnitTests.Helpers;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationApi.Client;
using NotificationApi.Contract.Requests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AdminWebsite.Contracts.Requests;
using BookingsApi.Client;
using BookingsApi.Contract.Requests;
using BookingsApi.Contract.Requests.Enums;
using BookingsApi.Contract.Responses;
using NotificationApi.Contract;
using VideoApi.Client;
using LinkedParticipantRequest = BookingsApi.Contract.Requests.LinkedParticipantRequest;
using EndpointResponse = BookingsApi.Contract.Responses.EndpointResponse;
using LinkedParticipantResponse = BookingsApi.Contract.Responses.LinkedParticipantResponse;
using CaseResponse = BookingsApi.Contract.Responses.CaseResponse;
using LinkedParticipantType = BookingsApi.Contract.Enums.LinkedParticipantType;

namespace AdminWebsite.UnitTests.Controllers.HearingsController
{
    public class PostHearingTests
    {
        private Mock<IBookingsApiClient> _bookingsApiClient;
        private Mock<IUserIdentity> _userIdentity;
        private Mock<IUserAccountService> _userAccountService;
        private Mock<IValidator<EditHearingRequest>> _editHearingRequestValidator;
        private Mock<IVideoApiClient> _videoApiMock;
        private Mock<IPollyRetryService> _pollyRetryServiceMock;
        private Mock<INotificationApiClient> _notificationApiMock;

        private Mock<ILogger<HearingsService>> _participantGroupLogger;
        private IHearingsService _hearingsService;

        private AdminWebsite.Controllers.HearingsController _controller;

        [SetUp]
        public void Setup()
        {
            _bookingsApiClient = new Mock<IBookingsApiClient>();
            _userIdentity = new Mock<IUserIdentity>();
            _userAccountService = new Mock<IUserAccountService>();
            _editHearingRequestValidator = new Mock<IValidator<EditHearingRequest>>();
            _videoApiMock = new Mock<IVideoApiClient>();
            _notificationApiMock = new Mock<INotificationApiClient>();
            _pollyRetryServiceMock = new Mock<IPollyRetryService>();

            _participantGroupLogger = new Mock<ILogger<HearingsService>>();
            _hearingsService = new HearingsService(_pollyRetryServiceMock.Object,
                _userAccountService.Object, _notificationApiMock.Object, _videoApiMock.Object, _bookingsApiClient.Object, _participantGroupLogger.Object);

            _controller = new AdminWebsite.Controllers.HearingsController(_bookingsApiClient.Object,
                _userIdentity.Object,
                _userAccountService.Object,
                _editHearingRequestValidator.Object,
                new Mock<ILogger<AdminWebsite.Controllers.HearingsController>>().Object,
                _hearingsService,
                Mock.Of<IPublicHolidayRetriever>());

            _userAccountService
                .Setup(x => x.UpdateParticipantUsername(It.IsAny<BookingsApi.Contract.Requests.ParticipantRequest>()))
                .Callback<BookingsApi.Contract.Requests.ParticipantRequest>(p => { p.Username ??= p.ContactEmail; })
                .ReturnsAsync(new User());
        }

        [Test]
        public async Task Should_update_participant_user_details()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "username",
                CaseRoleName = "Applicant",
                HearingRoleName = "Representative"
            };

            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                    .WithParticipant("Representative", "username");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await PostWithParticipants(participant);

            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Once);
        }

        [Test]
        public async Task Should_update_participant_username_to_aad_email_id()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "username@hmcts.net",
                CaseRoleName = "Applicant",
                HearingRoleName = "Representative",
                ContactEmail = "username@hmcts.net"
            };
            var participantList = new List<BookingsApi.Contract.Requests.ParticipantRequest> { participant };

            const string da = "username@hmcts.net";
            var endpoints = new EndpointRequest { DisplayName = "displayname", DefenceAdvocateUsername = da };
            var endpointList = new List<EndpointRequest> {endpoints};

            var hearing = new BookNewHearingRequest
            {
                Participants = participantList,
                Endpoints = endpointList
            };

            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = hearing
            };

            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                        .WithParticipant("Representative", participant.Username);
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await _controller.Post(bookingRequest);
            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Once);
        }

        [Test]
        public async Task Should_create_a_hearing_with_endpoints()
        {
            var newHearingRequest = new BookNewHearingRequest
            {
                Participants = new List<BookingsApi.Contract.Requests.ParticipantRequest>
                {
                    new BookingsApi.Contract.Requests.ParticipantRequest
                    {
                        CaseRoleName = "CaseRole", ContactEmail = "contact1@hmcts.net",
                        HearingRoleName = "HearingRole", DisplayName = "display name1",
                        FirstName = "fname", MiddleNames = "", LastName = "lname1", Username = "username1@hmcts.net",
                        OrganisationName = "", Representee = "", TelephoneNumber = ""
                    },
                    new BookingsApi.Contract.Requests.ParticipantRequest
                    {
                        CaseRoleName = "CaseRole", ContactEmail = "contact2@hmcts.net",
                        HearingRoleName = "HearingRole", DisplayName = "display name2",
                        FirstName = "fname2", MiddleNames = "", LastName = "lname2",
                        Username = "username2@hmcts.net", OrganisationName = "", Representee = "",
                        TelephoneNumber = ""
                    },
                },
                Endpoints = new List<EndpointRequest>
                {
                    new EndpointRequest
                        {DisplayName = "displayname1", DefenceAdvocateUsername = "username1@hmcts.net"},
                    new EndpointRequest
                        {DisplayName = "displayname2", DefenceAdvocateUsername = "username2@hmcts.net"},
                }
            };
            
            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = newHearingRequest
            };
            
            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                        .WithEndPoints(2)
                                        .WithParticipant("Representative", "username1@hmcts.net")
                                        .WithParticipant("Individual", "username2@hmcts.net");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(newHearingRequest))
                .ReturnsAsync(hearingDetailsResponse);

            var result = await _controller.Post(bookingRequest);

            result.Result.Should().BeOfType<CreatedResult>();
            var createdObjectResult = (CreatedResult)result.Result;
            createdObjectResult.StatusCode.Should().Be(201);
        }

        [Test]
        public async Task Should_create_a_hearing_with_LinkedParticipants()
        {
            // request.
            var newHearingRequest = new BookNewHearingRequest()
            {
                Participants = new List<BookingsApi.Contract.Requests.ParticipantRequest>
                {
                    new BookingsApi.Contract.Requests.ParticipantRequest { CaseRoleName = "CaseRole", ContactEmail = "firstName1.lastName1@email.com",
                        DisplayName = "firstName1 lastName1", FirstName = "firstName1", HearingRoleName = "Litigant in person", LastName = "lastName1", MiddleNames = "",
                        OrganisationName = "", Representee = "", TelephoneNumber = "1234567890", Title = "Mr.", Username = "firstName1.lastName1@email.net" },
                    new BookingsApi.Contract.Requests.ParticipantRequest { CaseRoleName = "CaseRole", ContactEmail = "firstName2.lastName2@email.com",
                        DisplayName = "firstName2 lastName2", FirstName = "firstName2", HearingRoleName = "Interpreter", LastName = "lastName2", MiddleNames = "",
                        OrganisationName = "", Representee = "", TelephoneNumber = "1234567890", Title = "Mr.", Username = "firstName2.lastName2@email.net" },

                },
                LinkedParticipants = new List<LinkedParticipantRequest>
                    {
                        new LinkedParticipantRequest { ParticipantContactEmail = "firstName1.lastName1@email.com",
                            LinkedParticipantContactEmail = "firstName2.lastName2@email.com", Type = LinkedParticipantType.Interpreter },
                        new LinkedParticipantRequest { ParticipantContactEmail = "firstName2.lastName2@email.com",
                            LinkedParticipantContactEmail = "firstName1.lastName1@email.com", Type = LinkedParticipantType.Interpreter }
                    }
            };
            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = newHearingRequest
            };
            // set response.
            var linkedParticipant1 = new List<LinkedParticipantResponse>() { new LinkedParticipantResponse() { LinkedId = Guid.NewGuid(), Type = LinkedParticipantType.Interpreter } };
            var participant1 = Builder<ParticipantResponse>.CreateNew().With(x => x.Id = Guid.NewGuid())
                .With(x => x.UserRoleName = "Individual").With(x => x.Username = "firstName1.lastName1@email.net")
                .With(x => x.LinkedParticipants = linkedParticipant1)
                .Build();
            var linkedParticipant2 = new List<LinkedParticipantResponse>() { new LinkedParticipantResponse() { LinkedId = Guid.NewGuid(), Type = LinkedParticipantType.Interpreter } };
            var participant2 = Builder<ParticipantResponse>.CreateNew().With(x => x.Id = Guid.NewGuid())
                .With(x => x.UserRoleName = "Individual").With(x => x.Username = "firstName1.lastName1@email.net")
                .With(x => x.LinkedParticipants = linkedParticipant2)
                .Build();
            var hearingDetailsResponse = Builder<HearingDetailsResponse>.CreateNew()
                .With(x => x.Cases = Builder<CaseResponse>.CreateListOfSize(2).Build().ToList())
                .With(x => x.Endpoints = Builder<EndpointResponse>.CreateListOfSize(2).Build().ToList())
                .With(x => x.Participants = new List<ParticipantResponse> { participant1, participant2 }).Build();
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(newHearingRequest))
                .ReturnsAsync(hearingDetailsResponse);
            var result = await _controller.Post(bookingRequest);
            result.Result.Should().BeOfType<CreatedResult>();
            var createdObjectResult = (CreatedResult)result.Result;
            createdObjectResult.StatusCode.Should().Be(201);
        }

        [Test]
        public async Task Should_not_update_user_details_for_judge()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "username",
                CaseRoleName = "Judge",
                HearingRoleName = "Judge"
            };

            // setup  response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                            .WithParticipant("Judge");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await PostWithParticipants(participant);

            _userAccountService.Verify(x => x.UpdateParticipantUsername(participant), Times.Never);
            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Never);
        }

        [Test]
        public async Task Should_not_update_user_details_for_panel_member()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "username",
                CaseRoleName = "",
                HearingRoleName = "Panel Member"
            };

            // setup  response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                            .WithParticipant("");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await PostWithParticipants(participant);

            _userAccountService.Verify(x => x.UpdateParticipantUsername(participant), Times.Never);
            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Never);            
        }

        [Test]
        public async Task Should_not_update_user_details_for_winger()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "username",
                CaseRoleName = "",
                HearingRoleName = "Winger"
            };

            // setup  response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                            .WithParticipant("");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await PostWithParticipants(participant);

            _userAccountService.Verify(x => x.UpdateParticipantUsername(participant), Times.Never);
            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Never);
        }

        [Test]
        public async Task Should_update_user_details_for_other_user_without_username()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "",
                CaseRoleName = "",
                HearingRoleName = ""
            };

            // setup  response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                            .WithParticipant("");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await PostWithParticipants(participant);

            _userAccountService.Verify(x => x.UpdateParticipantUsername(participant), Times.Once);
            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Never);
        }

        [Test]
        public async Task Should_get_user_details_for_other_user_with_username()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = "username",
                CaseRoleName = "",
                HearingRoleName = ""
            };

            // setup  response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                            .WithParticipant("");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            await PostWithParticipants(participant);

            _userAccountService.Verify(x => x.UpdateParticipantUsername(participant), Times.Never);
            _userAccountService.Verify(x => x.GetAdUserIdForUsername(participant.Username), Times.Once);
        }


        [Test]
        public async Task Should_pass_bad_request_from_bookings_api()
        {
            var hearing = new BookNewHearingRequest
            {
                Participants = new List<BookingsApi.Contract.Requests.ParticipantRequest>()
            };
            
            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = hearing
            };

            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .Throws(ClientException.ForBookingsAPI(HttpStatusCode.BadRequest));

            var result = await _controller.Post(bookingRequest);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
        
        [Test]
        public void Should_throw_BookingsApiException()
        {
            var hearing = new BookNewHearingRequest
            {
                Participants = new List<BookingsApi.Contract.Requests.ParticipantRequest>()
            };

            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = hearing
            };
            
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .Throws(ClientException.ForBookingsAPI(HttpStatusCode.InternalServerError));

            Assert.ThrowsAsync<BookingsApiException>(() => _controller.Post(bookingRequest));
        }
        
        [Test]
        public void Should_throw_Exception()
        {
            var hearing = new BookNewHearingRequest
            {
                Participants = new List<BookingsApi.Contract.Requests.ParticipantRequest>()
            };
            
            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = hearing
            };

            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .Throws(new Exception("Some internal error"));

            Assert.ThrowsAsync<Exception>(() => _controller.Post(bookingRequest));
        }

        [Test]
        public async Task Should_pass_current_user_as_created_by_to_service()
        {
            const string CURRENT_USERNAME = "test@hmcts.net";
            _userIdentity.Setup(x => x.GetUserIdentityName()).Returns(CURRENT_USERNAME);

            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                        .WithParticipant("Representative")
                                        .WithParticipant("Individual");
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);

            var result = await PostNewHearing();

            result.Result.Should().BeOfType<CreatedResult>();
            var createdResult = (CreatedResult)result.Result;
            createdResult.Location.Should().Be("");

            _bookingsApiClient.Verify(x => x.BookNewHearingAsync(It.Is<BookNewHearingRequest>(
                request => request.CreatedBy == CURRENT_USERNAME)), Times.Once);
            _userAccountService.Verify(x => x.AssignParticipantToGroup(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Should_update_booking_status()
        {
            _userIdentity.Setup(x => x.GetUserIdentityName()).Returns("admin@hmcts.net");
            _bookingsApiClient
                .Setup(x => x.UpdateBookingStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateBookingStatusRequest>()))
                .Verifiable();

            var vhExistingHearing = new HearingDetailsResponse
            {
                CaseTypeName = "Generic"
            };

            _bookingsApiClient.Setup(x => x.GetHearingDetailsByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(vhExistingHearing);

            var response = await _controller.UpdateBookingStatus(Guid.NewGuid(), new UpdateBookingStatusRequest{Status = UpdateBookingStatus.Created});

            response.Should().BeOfType<OkObjectResult>();

            _bookingsApiClient.Verify(
                x => x.UpdateBookingStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateBookingStatusRequest>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task Should_catch_BookingsApiException_by_updating_booking_status_and_returns_bad_result()
        {
            _userIdentity.Setup(x => x.GetUserIdentityName()).Returns("admin@hmcts.net");
            _bookingsApiClient.Setup(x =>
                    x.UpdateBookingStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateBookingStatusRequest>()))
                .Throws(new BookingsApiException("Error", 400, "response", null, null));

            var response = await _controller.UpdateBookingStatus(Guid.NewGuid(), new UpdateBookingStatusRequest());

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Should_catch_BookingsApiException_by_updating_booking_status_and_returns_not_found_result()
        {
            _userIdentity.Setup(x => x.GetUserIdentityName()).Returns("admin@hmcts.net");
            _bookingsApiClient.Setup(x =>
                    x.UpdateBookingStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateBookingStatusRequest>()))
                .Throws(new BookingsApiException("Error", 404, "response", null, null));

            var response = await _controller.UpdateBookingStatus(Guid.NewGuid(), new UpdateBookingStatusRequest());

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public async Task Should_clone_hearing()
        {
            var request = GetMultiHearingRequest();
            _bookingsApiClient
                .Setup(x => x.CloneHearingAsync(It.IsAny<Guid>(), It.IsAny<CloneHearingRequest>()))
                .Verifiable();

            var response = await _controller.CloneHearing(Guid.NewGuid(), request);

            response.Should().BeOfType<NoContentResult>();

            _bookingsApiClient.Verify(
                x => x.CloneHearingAsync(It.IsAny<Guid>(), It.IsAny<CloneHearingRequest>()),
                Times.Exactly(1));
        }

        [Test]
        public async Task Should_return_bad_request_status_if_no_items_in_the_date_list()
        {
            var startDate = new DateTime(2020, 10, 1);
            var endDate = new DateTime(2020, 10, 1);
            var request = new MultiHearingRequest { StartDate = startDate, EndDate = endDate};


            var response = await _controller.CloneHearing(Guid.NewGuid(), request);

            response.Should().BeOfType<BadRequestResult>();
        }

        [Test]
        public async Task Should_catch_BookingsApiException_by_clone_hearing()
        {
            var request = GetMultiHearingRequest();
            _bookingsApiClient
                .Setup(x => x.CloneHearingAsync(It.IsAny<Guid>(), It.IsAny<CloneHearingRequest>()))
                .Throws(new BookingsApiException("Error", (int)HttpStatusCode.BadRequest, "response", null, null));

            var response = await _controller.CloneHearing(Guid.NewGuid(), request);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Should_send_email_for_new_representative_participant_added()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = string.Empty, // New participant
                CaseRoleName = "Applicant",
                HearingRoleName = "Representative"
            };

            var newUserName = "some_new_user@hmcts.net";
            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                        .WithParticipant("Representative", newUserName);
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);
            _userAccountService
                .Setup(x => x.UpdateParticipantUsername(It.IsAny<BookingsApi.Contract.Requests.ParticipantRequest>()))
                .Callback<BookingsApi.Contract.Requests.ParticipantRequest>(p => { p.Username = newUserName; })
                .ReturnsAsync(new User() { UserName = newUserName, Password = "test123" });

            await PostWithParticipants(participant);

            _notificationApiMock.Verify(x => x.CreateNewNotificationAsync(It.Is<AddNotificationRequest>(request =>
                request.NotificationType == NotificationType.CreateRepresentative)), Times.Once);
        }

        [Test]
        public async Task Should_send_email_for_new_individual_participant_added()
        {
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = string.Empty, // New participant
                CaseRoleName = "Applicant",
                HearingRoleName = "Individual"
            };

            var newUserName = "some_new_user@hmcts.net";
            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                        .WithParticipant("Individual", newUserName);
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);
            _userAccountService
                .Setup(x => x.UpdateParticipantUsername(It.IsAny<BookingsApi.Contract.Requests.ParticipantRequest>()))
                .Callback<BookingsApi.Contract.Requests.ParticipantRequest>(p => { p.Username = newUserName; })
                .ReturnsAsync(new User { UserName = newUserName, Password = "test123" });

            await PostWithParticipants(participant);

            _notificationApiMock.Verify(x => x.CreateNewNotificationAsync(It.Is<AddNotificationRequest>(request =>
                request.NotificationType == NotificationType.CreateIndividual)), Times.Once);
        }

        [Test]
        public async Task Should_not_send_email_for_existing_participant_added()
        {
            var existingUserName = "some_new_user@hmcts.net";
            var participant = new BookingsApi.Contract.Requests.ParticipantRequest
            {
                Username = existingUserName,
                CaseRoleName = "Applicant",
                HearingRoleName = "Representative"
            };

            // setup response
            var hearingDetailsResponse = HearingResponseBuilder.Build()
                                         .WithParticipant("Representative", existingUserName);
            _bookingsApiClient.Setup(x => x.BookNewHearingAsync(It.IsAny<BookNewHearingRequest>()))
                .ReturnsAsync(hearingDetailsResponse);
                
            await PostWithParticipants(participant);

            _notificationApiMock.Verify(
                x => x.CreateNewNotificationAsync(It.Is<AddNotificationRequest>(request =>
                    request.NotificationType == NotificationType.CreateRepresentative)), Times.Never);
        }

        private static MultiHearingRequest GetMultiHearingRequest()
        {
            var startDate = new DateTime(2020, 10, 1);
            var endDate = new DateTime(2020, 10, 6);
            return new MultiHearingRequest { StartDate = startDate, EndDate = endDate };
        }

        private Task<ActionResult<HearingDetailsResponse>> PostNewHearing()
        {
            // without supplying participants
            return PostWithParticipants();
        }

        private async Task<ActionResult<HearingDetailsResponse>> PostWithParticipants(
            params BookingsApi.Contract.Requests.ParticipantRequest[] participants)
        {
            var hearing = new BookNewHearingRequest
            {
                Participants = new List<BookingsApi.Contract.Requests.ParticipantRequest>(participants)
            };
            
            var bookingRequest = new BookHearingRequest
            {
                BookingDetails = hearing
            };

            return await _controller.Post(bookingRequest);
        }
    }
}