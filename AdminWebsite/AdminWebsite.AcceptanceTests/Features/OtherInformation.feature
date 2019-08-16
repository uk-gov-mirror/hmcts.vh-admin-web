﻿Feature: Other Information
		As a Case Admin
		I want to be able to add any other information to the Video Hearing booking
		So that any other details that might be relevant to the booking can be included

@VIH-2641
Scenario: Case Admin adds Other information to the Video Hearing booking
	Given Case Admin is on the other information page 
	When user adds other information to the Video Hearing booking  
	And user proceeds to the next page 
	Then user should be on the summary page 
	
@VIH-2641
Scenario: Case Admin continues booking without adding Other information
	Given Case Admin is on the other information page 
	When user continues booking without adding other information  
	And user proceeds to the next page 
	Then user should be on the summary page 