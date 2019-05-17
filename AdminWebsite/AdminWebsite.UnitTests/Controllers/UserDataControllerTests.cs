﻿using AdminWebsite.Contracts.Responses;
using AdminWebsite.Controllers;
using AdminWebsite.Services;
using AdminWebsite.UserAPI.Client;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdminWebsite.UnitTests.Controllers
{
    public class UserDataControllerTests
    {
        private UserDataController _controller;
        private Mock<IUserApiClient> _apiClient;
        protected Mock<IUserAccountService> _userAccountService { get; private set; }

        private readonly List<JudgeResponse> judgeResponse = new List<JudgeResponse>();

        [SetUp]
        public void Setup()
        {
            _userAccountService = new Mock<IUserAccountService>();
            _controller = new UserDataController(_userAccountService.Object);

            _apiClient = new Mock<IUserApiClient>();
            GroupsResponse groupResponse = new GroupsResponse() { Display_name = "VirtualRoomJudge", Group_id = "431f50b2-fb30-4937-9e91-9b9eeb54097f" };
            _apiClient.Setup(x => x.GetGroupByName("VirtualRoomJudge")).Returns(groupResponse);

            GroupsResponse groupResponseTest = new GroupsResponse() { Display_name = "TestAccount", Group_id = "63b60a06-874f-490d-8acb-56a88a125078" };
            _apiClient.Setup(x => x.GetGroupByName("TestAccount")).Returns(groupResponseTest);

            JudgeResponse judgeData = new JudgeResponse()
            {
                Email = "Test.Judge01@hearings.reform.hmcts.net",
                DisplayName = "Test Judge01",
                FirstName = "Test",
                LastName = "Judge01"
            };
            judgeResponse.Add(judgeData);
            judgeData = new JudgeResponse()
            {
                Email = "Test.Judge02@hearings.reform.hmcts.net",
                DisplayName = "Test Judge02",
                FirstName = "Test",
                LastName = "Judge021"
            };
            judgeResponse.Add(judgeData);
        }

        [Test]
        public void should_return_a_list_of_judges()
        {
            _userAccountService.Setup(x => x.GetJudgeUsers()).Returns(judgeResponse);

            _controller = new UserDataController(_userAccountService.Object);
            ActionResult result = _controller.GetJudges().Result;
            OkObjectResult okObjectResult = (OkObjectResult)result;
            okObjectResult.StatusCode.Should().Be(200);

            List<JudgeResponse> judges = (List<JudgeResponse>)okObjectResult.Value;
            JudgeResponse testJudge = judges.Single(j =>
                j.Email.Equals("Test.Judge01@hearings.reform.hmcts.net", StringComparison.CurrentCultureIgnoreCase));

            testJudge.LastName.Should().Be("Judge01");
            testJudge.FirstName.Should().Be("Test");
            testJudge.DisplayName.Should().Be("Test Judge01");
        }
    }
}