// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:3.0.0.0
//      SpecFlow Generator Version:3.0.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace AdminWebsite.AcceptanceTests.Features
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("Case Admin or VH Officer signs out of VH Bookings")]
    public partial class CaseAdminOrVHOfficerSignsOutOfVHBookingsFeature
    {
        
        private TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "SignOut.feature"
#line hidden
        
        [NUnit.Framework.OneTimeSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Case Admin or VH Officer signs out of VH Bookings", "\t\tAs a Case Admin or VH Officer\r\n\t\tI want to sign out of the Book a VH applicatio" +
                    "n\r\n\t\tSo that I can ensure that no-one else accesses the Book a VH application wi" +
                    "th my account", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.OneTimeTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [NUnit.Framework.SetUpAttribute()]
        public virtual void TestInitialize()
        {
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void ScenarioTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<NUnit.Framework.TestContext>(NUnit.Framework.TestContext.CurrentContext);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("User is not in the process of booking hearing")]
        [NUnit.Framework.CategoryAttribute("VIH-2072")]
        [NUnit.Framework.TestCaseAttribute("Case Admin", null)]
        [NUnit.Framework.TestCaseAttribute("VhOfficerCivilMoneyclaims", null)]
        public virtual void UserIsNotInTheProcessOfBookingHearing(string user, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "VIH-2072"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("User is not in the process of booking hearing", null, @__tags);
#line 7
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line 8
 testRunner.Given("Admin user is on microsoft login page", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 9
 testRunner.And(string.Format("{0} logs into Vh-Admin website", user), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 10
 testRunner.And("user is on dashboard page", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 11
 testRunner.When("user attempts to sign out of Vh-Admin website", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 12
 testRunner.Then("user should be navigated to sign in screen", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Case Admin signs out in the process of booking hearing")]
        [NUnit.Framework.CategoryAttribute("VIH-2072")]
        [NUnit.Framework.TestCaseAttribute("Hearing details", null)]
        [NUnit.Framework.TestCaseAttribute("Hearing sjudge", null)]
        [NUnit.Framework.TestCaseAttribute("Add participants", null)]
        [NUnit.Framework.TestCaseAttribute("Other information", null)]
        [NUnit.Framework.TestCaseAttribute("Summary", null)]
        [NUnit.Framework.TestCaseAttribute("Dashboard", null)]
        [NUnit.Framework.TestCaseAttribute("Bookings List", null)]
        public virtual void CaseAdminSignsOutInTheProcessOfBookingHearing(string booking, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "VIH-2072"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Case Admin signs out in the process of booking hearing", null, @__tags);
#line 19
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line 20
 testRunner.Given("Admin user is on microsoft login page", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 21
 testRunner.And("Case Admin logs into Vh-Admin website", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 22
 testRunner.And(string.Format("user is in the process of {0} Hearing", booking), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 23
 testRunner.When("user attempts to sign out of Vh-Admin website", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 24
 testRunner.Then("warning message should be displayed as You will lose all your booking details if " +
                    "you sign out.", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Vh Officer signs out in the process of booking hearing")]
        [NUnit.Framework.CategoryAttribute("VIH-2072")]
        [NUnit.Framework.TestCaseAttribute("Hearing details", null)]
        [NUnit.Framework.TestCaseAttribute("Hearing sjudge", null)]
        [NUnit.Framework.TestCaseAttribute("Add participants", null)]
        [NUnit.Framework.TestCaseAttribute("Other information", null)]
        [NUnit.Framework.TestCaseAttribute("Summary", null)]
        [NUnit.Framework.TestCaseAttribute("Dashboard", null)]
        [NUnit.Framework.TestCaseAttribute("Bookings List", null)]
        public virtual void VhOfficerSignsOutInTheProcessOfBookingHearing(string booking, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "VIH-2072"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Vh Officer signs out in the process of booking hearing", null, @__tags);
#line 36
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line 37
 testRunner.Given("Admin user is on microsoft login page", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line 38
 testRunner.And("VhOfficerCivilMoneyclaims logs into Vh-Admin website", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 39
 testRunner.And(string.Format("user is in the process of {0} Hearing", booking), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line 40
 testRunner.When("user attempts to sign out of Vh-Admin website", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line 41
 testRunner.Then("warning message should be displayed as You will lose all your booking details if " +
                    "you sign out.", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
