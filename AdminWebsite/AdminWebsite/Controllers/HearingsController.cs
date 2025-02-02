using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AdminWebsite.Attributes;
using AdminWebsite.Contracts.Requests;
using AdminWebsite.Extensions;
using AdminWebsite.Helper;
using AdminWebsite.Mappers;
using AdminWebsite.Models;
using AdminWebsite.Security;
using AdminWebsite.Services;
using AdminWebsite.Services.Models;
using BookingsApi.Client;
using BookingsApi.Contract.Enums;
using BookingsApi.Contract.Requests;
using BookingsApi.Contract.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using VideoApi.Client;

namespace AdminWebsite.Controllers
{
    /// <summary>
    ///     Responsible for retrieving and storing hearing information
    /// </summary>
    [Produces("application/json")]
    [Route("api/hearings")]
    [ApiController]
    public class HearingsController : ControllerBase
    {
        private readonly IBookingsApiClient _bookingsApiClient;
        private readonly IValidator<EditHearingRequest> _editHearingRequestValidator;
        private readonly IHearingsService _hearingsService;
        private readonly ILogger<HearingsController> _logger;
        private readonly IUserAccountService _userAccountService;
        private readonly IUserIdentity _userIdentity;
        private readonly IPublicHolidayRetriever _publicHolidayRetriever;

        /// <summary>
        ///     Instantiates the controller
        /// </summary>
        public HearingsController(IBookingsApiClient bookingsApiClient, IUserIdentity userIdentity,
            IUserAccountService userAccountService, IValidator<EditHearingRequest> editHearingRequestValidator,
            ILogger<HearingsController> logger, IHearingsService hearingsService, IPublicHolidayRetriever publicHolidayRetriever)
        {
            _bookingsApiClient = bookingsApiClient;
            _userIdentity = userIdentity;
            _userAccountService = userAccountService;
            _editHearingRequestValidator = editHearingRequestValidator;
            _logger = logger;
            _hearingsService = hearingsService;
            _publicHolidayRetriever = publicHolidayRetriever;
        }

        /// <summary>
        ///     Create a hearing
        /// </summary>
        /// <param name="request">Hearing Request object</param>
        /// <returns>VideoHearingId</returns>
        [HttpPost]
        [SwaggerOperation(OperationId = "BookNewHearing")]
        [ProducesResponseType(typeof(HearingDetailsResponse), (int) HttpStatusCode.Created)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [HearingInputSanitizer]
        public async Task<ActionResult<HearingDetailsResponse>> Post([FromBody] BookHearingRequest request)
        {
            var newBookingRequest = request.BookingDetails;

            var usernameAdIdDict = new Dictionary<string, User>();
            try
            {
                var nonJudgeParticipants = newBookingRequest.Participants
                    .Where(p => p.CaseRoleName != "Judge" && p.HearingRoleName != "Panel Member" && p.HearingRoleName != "Winger")
                    .ToList();
                await PopulateUserIdsAndUsernames(nonJudgeParticipants, usernameAdIdDict);

                if (newBookingRequest.Endpoints != null && newBookingRequest.Endpoints.Any())
                {
                    var endpointsWithDa = newBookingRequest.Endpoints
                        .Where(x => !string.IsNullOrWhiteSpace(x.DefenceAdvocateUsername)).ToList();
                    _hearingsService.AssignEndpointDefenceAdvocates(endpointsWithDa,
                        newBookingRequest.Participants.AsReadOnly());
                }

                newBookingRequest.CreatedBy = _userIdentity.GetUserIdentityName();

                _logger.LogInformation("BookNewHearing - Attempting to send booking request to Booking API");
                var hearingDetailsResponse = await _bookingsApiClient.BookNewHearingAsync(newBookingRequest);
                _logger.LogInformation("BookNewHearing - Successfully booked hearing {Hearing}", hearingDetailsResponse.Id);

                _logger.LogInformation("BookNewHearing - Sending email notification to the participants");
                await _hearingsService.SendNewUserEmailParticipants(hearingDetailsResponse, usernameAdIdDict);
                _logger.LogInformation("BookNewHearing - Successfully sent emails to participants- {Hearing}",
                    hearingDetailsResponse.Id);

                _logger.LogInformation("BookNewHearing - Attempting assign participants to the correct group");
                await _hearingsService.AssignParticipantToCorrectGroups(hearingDetailsResponse, usernameAdIdDict);
                _logger.LogInformation("BookNewHearing - Successfully assigned participants to the correct group");


                if (request.IsMultiDay)
                {
                    var publicHolidays = await _publicHolidayRetriever.RetrieveUpcomingHolidays();
                    var listOfDates = DateListMapper.GetListOfWorkingDates(request.MultiHearingDetails.StartDate,
                        request.MultiHearingDetails.EndDate, publicHolidays);
                    var totalDays = listOfDates.Select(x => x.DayOfYear).Distinct().Count() + 1; // include start date
                    await _hearingsService.SendMultiDayHearingConfirmationEmail(hearingDetailsResponse, totalDays);
                }
                else
                {
                    await _hearingsService.SendHearingConfirmationEmail(hearingDetailsResponse);
                }

                return Created("", hearingDetailsResponse);
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e,
                    "BookNewHearing - There was a problem saving the booking. Status Code {StatusCode} - Message {Message}",
                    e.StatusCode, e.Response);
                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);

                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "BookNewHearing - Failed to save hearing - {Message} -  for request: {RequestBody}",
                    e.Message, JsonConvert.SerializeObject(newBookingRequest));
                throw;
            }
        }

        /// <summary>
        ///     Clone hearings with the details of a given hearing on given dates
        /// </summary>
        /// <param name="hearingId">Original hearing to clone</param>
        /// <param name="hearingRequest">The dates range to create the new hearings on</param>
        /// <returns></returns>
        [HttpPost("{hearingId}/clone")]
        [SwaggerOperation(OperationId = "CloneHearing")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CloneHearing(Guid hearingId, MultiHearingRequest hearingRequest)
        {
            _logger.LogDebug("Attempting to clone hearing {Hearing}", hearingId);
            var publicHolidays = await _publicHolidayRetriever.RetrieveUpcomingHolidays();
            var listOfDates = DateListMapper.GetListOfWorkingDates(hearingRequest.StartDate, hearingRequest.EndDate, publicHolidays);
            if (listOfDates.Count == 0)
            {
                _logger.LogWarning("No working dates provided to clone to");
                return BadRequest();
            }

            var cloneHearingRequest = new CloneHearingRequest {Dates = listOfDates};
            try
            {
                _logger.LogDebug("Sending request to clone hearing to Bookings API");
                await _bookingsApiClient.CloneHearingAsync(hearingId, cloneHearingRequest);
                _logger.LogDebug("Successfully cloned hearing {Hearing}", hearingId);
                return NoContent();
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e,
                    "There was a problem cloning the booking. Status Code {StatusCode} - Message {Message}",
                    e.StatusCode, e.Response);
                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);
                throw;
            }
        }

        /// <summary>
        ///     Edit a hearing
        /// </summary>
        /// <param name="hearingId">The id of the hearing to update</param>
        /// <param name="request">Hearing Request object for edit operation</param>
        /// <returns>VideoHearingId</returns>
        [HttpPut("{hearingId}")]
        [SwaggerOperation(OperationId = "EditHearing")]
        [ProducesResponseType(typeof(HearingDetailsResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [HearingInputSanitizer]
        public async Task<ActionResult<HearingDetailsResponse>> EditHearing(Guid hearingId,
            [FromBody] EditHearingRequest request)
        {
            var usernameAdIdDict = new Dictionary<string, User>();
            if (hearingId == Guid.Empty)
            {
                _logger.LogWarning("No hearing id found to edit");
                ModelState.AddModelError(nameof(hearingId), $"Please provide a valid {nameof(hearingId)}");
                return BadRequest(ModelState);
            }

            _logger.LogDebug("Attempting to edit hearing {Hearing}", hearingId);

            var result = _editHearingRequestValidator.Validate(request);

            if (!result.IsValid)
            {
                _logger.LogWarning("Failed edit hearing validation");
                ModelState.AddFluentValidationErrors(result.Errors);
                return BadRequest(ModelState);
            }

            HearingDetailsResponse originalHearing;
            try
            {
                originalHearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e,
                    "Failed to get hearing {Hearing}. Status Code {StatusCode} - Message {Message}",
                    hearingId, e.StatusCode, e.Response);
                if (e.StatusCode != (int) HttpStatusCode.NotFound)
                    throw;

                return NotFound($"No hearing with id found [{hearingId}]");
            }

            try
            {
                //Save hearing details
                var updateHearingRequest =
                    HearingUpdateRequestMapper.MapTo(request, _userIdentity.GetUserIdentityName());
                await _bookingsApiClient.UpdateHearingDetailsAsync(hearingId, updateHearingRequest);

                var newParticipantList = new List<ParticipantRequest>();

                foreach (var participant in request.Participants)
                    if (!participant.Id.HasValue)
                        await _hearingsService.ProcessNewParticipants(hearingId, participant, originalHearing,
                            usernameAdIdDict, newParticipantList);
                    else
                        await _hearingsService.ProcessExistingParticipants(hearingId, originalHearing, participant);

                // Delete existing participants if the request doesn't contain any update information
                originalHearing.Participants ??= new List<ParticipantResponse>();
                await RemoveParticipantsFromHearing(hearingId, request, originalHearing);

                // Add new participants
                await _hearingsService.SaveNewParticipants(hearingId, newParticipantList);
                var addedParticipantToHearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
                await _hearingsService.UpdateParticipantLinks(hearingId, request, addedParticipantToHearing);

                // endpoints
                await _hearingsService.ProcessEndpoints(hearingId, request, originalHearing, newParticipantList);

                var updatedHearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
                await _hearingsService.AddParticipantLinks(hearingId, request, updatedHearing);
                _logger.LogDebug("Attempting assign participants to the correct group");
                await _hearingsService.AssignParticipantToCorrectGroups(updatedHearing, usernameAdIdDict);
                _logger.LogDebug("Successfully assigned participants to the correct group");

                // Send a notification email to newly created participants
                var newParticipantEmails = newParticipantList.Select(p => p.ContactEmail).ToList();
                await SendEmailsToParticipantsAddedToHearing(newParticipantList, updatedHearing, usernameAdIdDict, newParticipantEmails);

                await SendJudgeEmailIfNeeded(updatedHearing, originalHearing);
                if (!updatedHearing.HasScheduleAmended(originalHearing)) return Ok(updatedHearing);


                var participantsForAmendment = updatedHearing.Participants
                    .Where(p => !newParticipantEmails.Contains(p.ContactEmail)).ToList();
                await _hearingsService.SendHearingUpdateEmail(originalHearing, updatedHearing,
                    participantsForAmendment);


                return Ok(updatedHearing);
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e,
                    "Failed to edit hearing {Hearing}. Status Code {StatusCode} - Message {Message}",
                    hearingId, e.StatusCode, e.Response);
                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);

                throw;
            }
        }

        private async Task RemoveParticipantsFromHearing(Guid hearingId, EditHearingRequest request,
            HearingDetailsResponse originalHearing)
        {
            var deleteParticipantList =
                originalHearing.Participants.Where(p => request.Participants.All(rp => rp.Id != p.Id));
            foreach (var participantToDelete in deleteParticipantList)
            {
                _logger.LogDebug("Removing existing participant {Participant} from hearing {Hearing}",
                    participantToDelete.Id, hearingId);
                await _bookingsApiClient.RemoveParticipantFromHearingAsync(hearingId, participantToDelete.Id);
            }
        }

        private async Task SendJudgeEmailIfNeeded(HearingDetailsResponse updatedHearing, HearingDetailsResponse originalHearing)
        {
            if (updatedHearing.HasJudgeEmailChanged(originalHearing) &&
                updatedHearing.Status == BookingStatus.Created)
                await _hearingsService.SendJudgeConfirmationEmail(updatedHearing);
        }

        private async Task SendEmailsToParticipantsAddedToHearing(List<ParticipantRequest> newParticipantList,
            HearingDetailsResponse updatedHearing, Dictionary<string, User> usernameAdIdDict, IEnumerable<string> newParticipantEmails)
        {
            if (newParticipantList.Any())
            {
                _logger.LogInformation("Sending email notification to the participants");
                await _hearingsService.SendNewUserEmailParticipants(updatedHearing, usernameAdIdDict);

                var participantsForConfirmation = updatedHearing.Participants
                    .Where(p => newParticipantEmails.Contains(p.ContactEmail)).ToList();
                await _hearingsService.SendHearingConfirmationEmail(updatedHearing, participantsForConfirmation);
                _logger.LogInformation("Successfully sent emails to participants - {Hearing}", updatedHearing.Id);
            }
        }


        /// <summary>
        ///     Gets bookings hearing by Id.
        /// </summary>
        /// <param name="hearingId">The unique sequential value of hearing ID.</param>
        /// <returns> The hearing</returns>
        [HttpGet("{hearingId}")]
        [SwaggerOperation(OperationId = "GetHearingById")]
        [ProducesResponseType(typeof(HearingDetailsResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetHearingById(Guid hearingId)
        {
            try
            {
                var hearingResponse = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
                return Ok(hearingResponse);
            }
            catch (BookingsApiException e)
            {
                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);

                throw;
            }
        }

        /// <summary>
        ///     Get hearings by case number.
        /// </summary>
        /// <param name="caseNumber">The case number.</param>
        /// <param name="date">The date to filter by</param>
        /// <returns> The hearing</returns>
        [HttpGet("audiorecording/search")]
        [SwaggerOperation(OperationId = "SearchForAudioRecordedHearings")]
        [ProducesResponseType(typeof(List<HearingsForAudioFileSearchResponse>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SearchForAudioRecordedHearingsAsync([FromQuery] string caseNumber,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var decodedCaseNumber = string.IsNullOrWhiteSpace(caseNumber) ? null : WebUtility.UrlDecode(caseNumber);
                var hearingResponse = await _bookingsApiClient.SearchForHearingsAsync(decodedCaseNumber, date);

                return Ok(hearingResponse.Select(HearingsForAudioFileSearchMapper.MapFrom));
            }
            catch (BookingsApiException ex)
            {
                if (ex.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(ex.Response);

                throw;
            }
        }

        /// <summary>
        ///     Update the hearing status.
        /// </summary>
        /// <param name="hearingId">The hearing id</param>
        /// <param name="updateBookingStatusRequest"></param>
        /// <returns>Success status</returns>
        [HttpPatch("{hearingId}")]
        [SwaggerOperation(OperationId = "UpdateBookingStatus")]
        [ProducesResponseType(typeof(UpdateBookingStatusResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateBookingStatus(Guid hearingId,
            UpdateBookingStatusRequest updateBookingStatusRequest)
        {
            var errorMessage =
                $"Failed to get the conference from video api, possibly the conference was not created or the kinly meeting room is null - hearingId: {hearingId}";

            try
            {
                _logger.LogDebug("Attempting to update hearing {Hearing} to booking status {BookingStatus}", hearingId,
                    updateBookingStatusRequest.Status);
                updateBookingStatusRequest.UpdatedBy = _userIdentity.GetUserIdentityName();
                await _bookingsApiClient.UpdateBookingStatusAsync(hearingId, updateBookingStatusRequest);
                _logger.LogDebug("Updated hearing {Hearing} to booking status {BookingStatus}", hearingId,
                    updateBookingStatusRequest.Status);
                if (updateBookingStatusRequest.Status != BookingsApi.Contract.Requests.Enums.UpdateBookingStatus.Created)
                    return Ok(new UpdateBookingStatusResponse {Success = true});

                try
                {
                    _logger.LogDebug("Hearing {Hearing} is confirmed. Polling for Conference in VideoApi", hearingId);
                    var conferenceDetailsResponse =
                        await _hearingsService.GetConferenceDetailsByHearingIdWithRetry(hearingId, errorMessage);
                    _logger.LogInformation("Found conference for hearing {Hearing}", hearingId);
                    if (conferenceDetailsResponse.HasValidMeetingRoom())
                    {
                        var hearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);

                        _logger.LogInformation("Sending a reminder email for hearing {Hearing}", hearingId);
                        await _hearingsService.SendHearingReminderEmail(hearing);

                        return Ok(new UpdateBookingStatusResponse
                        {
                            Success = true,
                            TelephoneConferenceId = conferenceDetailsResponse.MeetingRoom.TelephoneConferenceId
                        });
                    }
                }
                catch (VideoApiException ex)
                {
                    _logger.LogError(ex, "Failed to confirm a hearing. {ErrorMessage}", errorMessage);
                }

                _logger.LogError("There was an unknown error for hearing {Hearing}. Updating status to failed",
                    hearingId);
                // Set the booking status to failed as the video api failed
                await _bookingsApiClient.UpdateBookingStatusAsync(hearingId, new UpdateBookingStatusRequest
                {
                    Status = BookingsApi.Contract.Requests.Enums.UpdateBookingStatus.Failed,
                    UpdatedBy = "System",
                    CancelReason = string.Empty
                });

                return Ok(new UpdateBookingStatusResponse {Success = false, Message = errorMessage});
            }
            catch (BookingsApiException e)
            {
                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);

                if (e.StatusCode == (int) HttpStatusCode.NotFound) return NotFound(e.Response);

                _logger.LogError(e, "There was an unknown error updating status for hearing {Hearing}", hearingId);
                throw;
            }
        }

        /// <summary>
        ///     Gets for confirmed booking the telephone conference Id by hearing Id.
        /// </summary>
        /// <param name="hearingId">The unique sequential value of hearing ID.</param>
        /// <returns> The telephone conference Id</returns>
        [HttpGet("{hearingId}/telephoneConferenceId")]
        [SwaggerOperation(OperationId = "GetTelephoneConferenceIdById")]
        [ProducesResponseType(typeof(PhoneConferenceResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetTelephoneConferenceIdById(Guid hearingId)
        {
            try
            {
                var conferenceDetailsResponse = await _hearingsService.GetConferenceDetailsByHearingId(hearingId);

                if (conferenceDetailsResponse.HasValidMeetingRoom())
                    return Ok(new PhoneConferenceResponse
                        {TelephoneConferenceId = conferenceDetailsResponse.MeetingRoom.TelephoneConferenceId});

                return NotFound();
            }
            catch (VideoApiException e)
            {
                if (e.StatusCode == (int) HttpStatusCode.NotFound) return NotFound();

                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);

                throw;
            }
        }

        private async Task PopulateUserIdsAndUsernames(IList<ParticipantRequest> participants,
            Dictionary<string, User> usernameAdIdDict)
        {
            _logger.LogDebug("Assigning HMCTS usernames for participants");
            foreach (var participant in participants)
            {
                // set the participant username according to AD
                User user;
                if (string.IsNullOrWhiteSpace(participant.Username))
                {
                    _logger.LogDebug(
                        "No username provided in booking for participant {Email}. Checking AD by contact email",
                        participant.ContactEmail);
                    user = await _userAccountService.UpdateParticipantUsername(participant);
                }
                else
                {
                    // get user
                    _logger.LogDebug(
                        "Username provided in booking for participant {Email}. Getting id for username {Username}",
                        participant.ContactEmail, participant.Username);
                    var adUserId = await _userAccountService.GetAdUserIdForUsername(participant.Username);
                    user = new User {UserName = adUserId};
                }

                // username's participant will be set by this point
                usernameAdIdDict[participant.Username!] = user;
            }
        }
    }
}