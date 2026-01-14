# Testing Demonstration Script (10 Minutes)
## What to Say While Showing Your Work

---

## TRANSITION FROM INTRO TO DEMO

"So today, I'd like to walk you through the testing work I did on my StrongHelp capstone project. But first, let me give you quick context about what StrongHelp is."

---

## PROJECT OVERVIEW (1 minute)

"StrongHelp is a loan management system I built for my capstone project from June to September 2025. It's designed for company employees to apply for loans and track their applications.

The system has four user roles:
- **Employees** can apply for loans, upload required documents, and track their application status
- **Approvers** review and approve or reject loan applications
- **Benefits Assistants** manage loan information and generate reports
- **Admins** manage users and system settings

The core workflow is: Employee submits a loan application with documents and a co-maker, Benefits Assistant assigns additional information, and an Approver makes the final decision.

Because this system handles financial data and has multiple user roles with different permissions, security and reliability were critical - which is why thorough testing was essential."

**[OPTIONAL: Show the running application homepage for 5 seconds if you have it running]**

"Now let me show you how I tested this system."

**[OPEN FILE EXPLORER - Have all files ready]**

---

## PART 1: MANUAL TEST CASES (2.5 minutes)

### Opening TEST_CASES.csv

**[OPEN: TEST_CASES.csv]**

"Let me start with my manual test cases. During the capstone, I created 20 manual test cases organized by testing type."

**[POINT TO TEST CASE ID COLUMN]**

"Each test has a unique ID that tells you what type of testing it is:
- FT means Functional Testing - testing if features work as designed
- RT is Regression Testing - making sure new changes didn't break existing features
- IT is Integration Testing - testing if different modules work together
- NT is Negative Testing - testing how the system handles invalid inputs
- UI is UI/UX Testing - testing the user interface and experience"

**[SCROLL TO FT-002]**

"Here's an example - FT-002 tests the loan application submission. This is marked as High priority because it's a core business function. If users can't submit loan applications, the entire system is useless."

**[POINT TO COLUMNS]**

"Each test case includes:
- The module being tested - in this case, Loan Application
- Preconditions - what needs to be true before starting
- Step-by-step test instructions
- Expected result - what should happen
- Actual result and status - what actually happened when I ran the test"

**[SCROLL TO NT-001]**

"I also did negative testing. NT-001 tests what happens when someone tries to access a restricted page without logging in. The system should redirect to the login page - which it did correctly. This kind of testing is important for security."

**[SCROLL TO PRIORITY COLUMN]**

"You'll notice I used High and Medium priority ratings. High priority tests cover critical workflows like authentication and loan submission. Medium priority tests cover important features like file validation and navigation. This helps me know which tests to run first when time is limited."

---

### Opening UAT_TEST_CASES.csv

**[OPEN: UAT_TEST_CASES.csv]**

"I also conducted User Acceptance Testing - 10 test cases focused on usability and business requirements."

**[POINT TO UAT-001]**

"The difference between functional tests and UAT is important. Functional tests ask: Does it work technically? UAT asks: Can users actually use this without confusion?

For example, UAT-001 tests if an employee can successfully apply for a loan. The expected result isn't just 'application submits' - it's 'employee can complete loan application without confusion.' This validates the user experience, not just technical correctness."

**[POINT TO USER ROLE COLUMN]**

"I tested from different user perspectives - Employee, Approver, Admin, Benefits Assistant - because each role has different needs and workflows."

---

## PART 2: BUG DISCOVERY (3 minutes)

**[OPEN: BUG_REPORT.csv]**

"During testing, I discovered and documented 3 bugs. This is where that 'detective work' I mentioned really came into play."

**[POINT TO COLUMN HEADERS]**

"Each bug report includes several key pieces of information. Let me explain two important fields: Severity and Priority.

**Severity** is the technical impact:
- Critical means system crash or data loss
- High means major functionality broken
- Medium means feature partially broken but there's a workaround
- Low means minor issue or cosmetic problem

**Priority** is the business urgency using P1, P2, P3 ratings:
- P1 means fix immediately, it's blocking critical work
- P2 means fix in the current sprint, it's important but not blocking
- P3 means fix when time allows, low business impact

They're usually aligned, but not always. For example, a typo on the CEO's dashboard might be Low severity but High priority."

**[POINT TO BUG-002]**

"Let me walk you through BUG-002 in detail. This was a NullReferenceException that crashed the application when users submitted loan applications.

Here's how I found it: I was testing session handling scenarios - what happens if a user's session expires or they clear their cookies? So I cleared my browser session, navigated to the loan application page, filled out the form, and clicked submit. The application crashed.

I rated it Medium severity because it crashed the application, and P2 priority because it affected the core loan submission workflow - a critical business function.

The key to a good bug report is clear reproduction steps. I documented:
1. Clear browser cookies to simulate session loss
2. Navigate to Apply for Loan page
3. Fill out the form
4. Click Submit
5. Observe the crash

With these detailed steps, the development team could reproduce the issue immediately and fix it quickly."

**[POINT TO BUG-001]**

"BUG-001 was an authentication configuration issue. After users successfully logged in, when they tried to access the loan status page, they would get a 500 Internal Server Error. 

This happened because of how the authentication was configured - the system was trying to use a specific authentication method that wasn't properly set up for that page. Users could log in fine, but then couldn't view their loan status, which is a critical feature.

I rated it Medium severity because it blocked access to an important feature, and P2 priority because users needed to check their loan status regularly. It wasn't as critical as BUG-002 which crashed the entire application, but it still affected a key workflow."

**[POINT TO BUG-003]**

"BUG-003 was much simpler - a data display issue. In the user management section, when viewing user profiles, the 'IsActive' status field would show blank or null instead of displaying 'Active' or 'Inactive'.

This was Low severity because it's purely a display problem - the system still worked, users could still do their jobs, it just looked unprofessional. P3 priority because it could be fixed whenever there was time. It's the kind of bug that doesn't block anyone but should still be fixed for a polished user experience."

"So to summarize the three bugs:
- **BUG-001**: Authentication error blocking loan status page - Medium/P2
- **BUG-002**: Application crash on loan submission - Medium/P2  
- **BUG-003**: Display issue showing null status - Low/P3

All three bugs were fixed before project completion. 

To be honest, during the capstone we prioritized fixing bugs quickly over documentation. When we found a bug, we'd communicate it to the team, fix it immediately, and move on. These CSV files you're seeing - I actually created them after the project to properly document the testing work I did. It's something I learned: in a fast-paced student project, you focus on delivery, but in a professional setting, documentation is just as important as the fix itself."

---

## PART 3: AUTOMATED TESTING (2 minutes)

**[OPEN: Visual Studio/VS Code - Show StrongHelpOfficial.Tests folder]**

"During the capstone project itself, we didn't use automated testing - we focused on manual testing because we were learning as we went and needed to deliver quickly.

But after the project ended, during my Christmas break, I realized I wanted to learn more about testing. So I taught myself automated testing using the xUnit framework with the help of AI tools and online resources. I went back to my StrongHelp project and wrote automated tests for it."

**[SHOW FOLDER STRUCTURE]**

"I created 24 automated tests organized into different test files:
- HomeControllerTests - testing the home page functionality
- AuthControllerTests - testing authentication
- ApplyForLoanControllerTests - testing loan application logic
- ViewModelTests - testing data models
- BasicIntegrationTests - testing how components work together"

**[OPEN: HomeControllerTests.cs - just show it briefly]**

"Here's an example test file. Each test method verifies a specific behavior. For instance, this test checks if the home page loads correctly and returns the right view."

**[OPEN TERMINAL]**

"Let me run these tests live so you can see how automation works."

**[TYPE: dotnet test]**

**[WHILE TESTS ARE RUNNING]**

"Automated tests execute in seconds and provide immediate feedback. This is incredibly valuable for regression testing - when you make changes to the code, you can quickly verify that you didn't break anything."

**[AFTER TESTS FINISH]**

"As you can see, the tests ran in about 4 seconds. Compare that to the 4 hours it took to run my manual tests. That's a 99.97% time savings. Of course, manual testing is still essential for exploratory testing and user experience validation, but automation is perfect for repetitive regression tests."

---

## PART 4: TESTING METRICS (1.5 minutes)

**[OPEN: TEST_SUMMARY.csv]**

"Finally, here's my testing summary with key metrics."

**[POINT TO TOTAL TEST CASES]**

"I executed 54 total tests - 30 manual test cases and 24 automated tests."

**[POINT TO PASS RATE]**

"I achieved an 89% pass rate. The 11% failure rate represents the bugs I discovered and documented. This is actually a good sign - it shows the code quality was high, but I still found real issues through thorough testing."

**[POINT TO DEFECTS]**

"Zero critical defects - meaning no system-breaking bugs. The 3 bugs I found were Medium and Low severity, which indicates the application was well-developed."

**[POINT TO TEST EXECUTION TIME]**

"The execution time comparison really highlights the value of automation: 4 hours for manual tests versus 4 seconds for automated tests. This is why modern QA teams use both approaches strategically."

**[POINT TO CODE COVERAGE]**

"I achieved 67% code coverage with my automated tests, which is solid for a capstone project. In a professional setting, teams often aim for 70-80% coverage on critical modules."

---

## CLOSING TRANSITION (0.5 minutes)

"So to summarize what I've shown you:

**Manual Testing**: 30 test cases covering functional, regression, integration, negative, and user acceptance testing - all documented with clear steps and priorities.

**Bug Discovery**: 3 bugs found through thorough testing, including edge cases like session handling. Each bug documented with severity, priority, and detailed reproduction steps.

**Automation**: 24 automated tests I learned independently after the capstone, demonstrating my initiative to grow beyond the curriculum.

**Results**: 89% pass rate, zero critical defects, and all bugs fixed before project completion.

This experience taught me that testing isn't just about finding bugs - it's about understanding user needs, thinking through edge cases, and communicating clearly with developers. That's the foundation I want to build on in this internship."

**[CLOSE FILES - LOOK AT CAMERA]**

"I'm ready for any questions you might have about my testing approach or the work I've shown you."

---

## QUICK TIPS FOR DELIVERY

### Pacing
- Don't rush through the CSV files
- Pause after explaining each bug
- Let them absorb the information

### Pointing
- Use your cursor to highlight specific rows/columns
- Don't just scroll - point to what you're talking about
- Zoom in if text is small

### Confidence
- Speak clearly about your decisions (why you chose priorities)
- Own the bugs you found - you prevented users from experiencing them
- Show enthusiasm when discussing the "detective work"

### If They Interrupt
- Welcome questions anytime
- Don't say "I'll get to that later" - answer immediately
- Use their questions to show deeper knowledge

### If Running Short on Time
- Skip showing multiple test case examples
- Focus more on BUG-002 (the most interesting one)
- Briefly mention automation instead of running tests live

### If Running Long
- Add more detail about how you found BUG-002
- Explain your testing strategy (why you chose these test types)
- Discuss what you'd do differently next time

---

## BACKUP ANSWERS IF THEY ASK DURING DEMO

**"Why only 3 bugs?"**
"Three bugs indicates good code quality. I focused on thorough testing of critical workflows rather than logging every minor cosmetic issue. Each bug had real business impact."

**"Why are test priorities High/Medium but bug priorities P1/P2/P3?"**
"Different teams use different systems. Test case priority determines which tests to run first. Bug priority determines fix order for developers. Both are valid - the important thing is consistency within each document."

**"Did you use any testing tools?"**
"For manual testing, I used CSV files for documentation and Chrome DevTools for debugging. For automation, I used xUnit framework with Moq for mocking. I'm eager to learn industry tools like Jira, TestRail, or Selenium."

**"How long did testing take?"**
"Manual testing took about 4 hours to execute all 30 test cases. Creating the test cases and documentation took additional time upfront, but it made execution much faster and more organized."

**"What would you do differently?"**
"I'd capture screenshots of the bugs before fixing them - that's something I learned is important for documentation. I'd also implement automation earlier for regression testing instead of waiting until after the project."

---

**You've got this! Show them your detective skills! 🔍**
