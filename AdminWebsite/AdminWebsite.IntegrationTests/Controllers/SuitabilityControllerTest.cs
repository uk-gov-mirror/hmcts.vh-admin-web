﻿using AdminWebsite.BookingsAPI.Client;
using AdminWebsite.Controllers;
using AdminWebsite.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Net;

namespace AdminWebsite.IntegrationTests.Controllers
{
    public class SuitabilityControllerTest : ControllerTestsBase
    {
        private SuitabilityAnswersController _controller;
        private Mock<IBookingsApiClient> _bookingsApiClientMock;
        private Mock<IUserIdentity> _userIdentityMock;

        [SetUp]
        public void Setup()
        {
            _bookingsApiClientMock = new Mock<IBookingsApiClient>();
            _userIdentityMock = new Mock<IUserIdentity>();
            _controller = new SuitabilityAnswersController(_bookingsApiClientMock.Object, _userIdentityMock.Object);
        }

        [Test]
        public void Should_retrieve_the_suitability_answers()
        {
            var response = new SuitabilityAnswersResponse();

            _userIdentityMock.Setup(s => s.IsVhOfficerAdministratorRole()).Returns(true);
            _bookingsApiClientMock.Setup(s => s.GetSuitabilityAnswers("", 1)).Returns(response);


            var result = _controller.GetSuitabilityAnswersList("", 1);

            result.Should().NotBeNull();
            var objectResult = (ObjectResult)result;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Test]
        public void Should_return_unauthorized()
        {
            _userIdentityMock.Setup(s => s.IsVhOfficerAdministratorRole()).Returns(false);

            var result = _controller.GetSuitabilityAnswersList("", 1);

            result.Should().NotBeNull();
            var objectResult = (UnauthorizedResult)result;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        }
    }
}
