using System;
using AdminWebsite.Models;
using BookingsApi.Contract.Responses;
using Newtonsoft.Json;

namespace AdminWebsite.Extensions
{
    public static class HearingDetailsResponseExtensions
    {
        public static bool IsGenericHearing(this HearingDetailsResponse hearing)
        {
            return hearing.CaseTypeName.Equals("Generic", StringComparison.CurrentCultureIgnoreCase);
        }
        
        public static bool HasScheduleAmended(this HearingDetailsResponse hearing, HearingDetailsResponse anotherHearing)
        {
            return hearing.ScheduledDateTime.Ticks != anotherHearing.ScheduledDateTime.Ticks;
        }
        
        public static bool HasJudgeEmailChanged(this HearingDetailsResponse hearing, HearingDetailsResponse anotherHearing)
        {
            if (string.IsNullOrWhiteSpace(anotherHearing.OtherInformation) && string.IsNullOrWhiteSpace(hearing.OtherInformation))
            {
                return false;
            }
            return hearing.GetJudgeEmail() != anotherHearing.GetJudgeEmail();
        }

        public static bool DoesJudgeEmailExist(this HearingDetailsResponse hearing)
        {
            if (hearing.OtherInformation != null)
            {
                var otherInformationDetails = GetOtherInformationObject(hearing.OtherInformation);
                if (otherInformationDetails.JudgeEmail != "")
                {
                    return true;
                }
            }
            return false;
        }
        
        public static bool DoesJudgePhoneExist(this HearingDetailsResponse hearing)
        {
            if (hearing.OtherInformation != null)
            {
                var otherInformationDetails = GetOtherInformationObject(hearing.OtherInformation);
                if (otherInformationDetails.JudgePhone != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetJudgeEmail(this HearingDetailsResponse hearing)
        {
            var email = GetOtherInformationObject(hearing.OtherInformation)?.JudgeEmail;
            if (email == string.Empty)
            {
                return null;
            }
            return email;
        }
        
        public static string GetJudgePhone(this HearingDetailsResponse hearing)
        {
            var phone = GetOtherInformationObject(hearing.OtherInformation).JudgePhone;
            if (phone == string.Empty)
            {
                return null;
            }
            return phone;
        }

        public static string ToOtherInformationString(this OtherInformationDetails otherInformationDetailsObject)
        {
            return
                $"|JudgeEmail|{otherInformationDetailsObject.JudgeEmail}" +
                $"|JudgePhone|{otherInformationDetailsObject.JudgePhone}" +
                $"|OtherInformation|{otherInformationDetailsObject.OtherInformation}";
        }

        public static HearingDetailsResponse Duplicate(this HearingDetailsResponse hearingDetailsResponse)
        {
            var json = JsonConvert.SerializeObject(hearingDetailsResponse);
            return JsonConvert.DeserializeObject<HearingDetailsResponse>(json);
        }

        private static OtherInformationDetails GetOtherInformationObject(string otherInformation)
        {
            try
            {
                var properties = otherInformation.Split("|");
                var otherInfo = new OtherInformationDetails
                {
                    JudgeEmail = Array.IndexOf(properties, "JudgeEmail") > -1
                        ? properties[Array.IndexOf(properties, "JudgeEmail") + 1]
                        : "",
                    JudgePhone = Array.IndexOf(properties, "JudgePhone") > -1
                        ? properties[Array.IndexOf(properties, "JudgePhone") + 1]
                        : "",
                    OtherInformation = Array.IndexOf(properties, "OtherInformation") > -1
                        ? properties[Array.IndexOf(properties, "OtherInformation") + 1]
                        : ""
                };
                return otherInfo;
            }
            catch (Exception)
            {
                if(string.IsNullOrWhiteSpace(otherInformation)){
                {
                    return new OtherInformationDetails {OtherInformation = otherInformation};
                }}
                var properties = otherInformation.Split("|");
                if (properties.Length > 2)
                {
                    return new OtherInformationDetails {OtherInformation = properties[2]};
                }

                return new OtherInformationDetails {OtherInformation = otherInformation};
            }
        }
    }
}