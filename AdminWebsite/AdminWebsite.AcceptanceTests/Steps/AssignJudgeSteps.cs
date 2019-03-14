﻿using AdminWebsite.AcceptanceTests.Helpers;
using AdminWebsite.AcceptanceTests.Pages;
using TechTalk.SpecFlow;

namespace AdminWebsite.AcceptanceTests.Steps
{
    [Binding]
    public sealed class AssignJudgeSteps
    {
        private readonly AssignJudge _assignJudge;

        public AssignJudgeSteps(AssignJudge assignJudge)
        {
            _assignJudge = assignJudge;
        }
        [When(@"judge is assigned to hearing")]
        public void AssignJudgeToHearing()
        {
            AssignJudgePage();
            SelectJudge();
        }
        [Then(@"user should be on assign judge page")]
        [When(@"Admin user is on assign judge page")]
        public void AssignJudgePage()
        {
            _assignJudge.PageUrl(PageUri.AssignJudgePage);
        }
        [When(@"select judge")]
        public void SelectJudge()
        {
            _assignJudge.Judge();
        }
        [When(@"hearing booking is assigned to a different judge")]
        public void WhenHearingBookingIsAssignedToADifferentJudge()
        {
            AssignJudgePage();
            _assignJudge.AddItems<string>("Judge", _assignJudge.GetSelectedJudge());
        }

    }
}