# Interview Demonstration Script
## Tester Intern Position - 30 Minutes Total

---

## PART 1: INTRODUCTION (5 minutes)

### Opening (1 min)
"Good morning/afternoon. Thank you for this opportunity. My name is Axel Sacdal, and I'm excited to share my testing experience from my recent capstone project."

### Background (2 min)
"From June to September 2025, I worked on a loan management system called StrongHelp as my capstone project. My role focused on quality assurance and testing. The system handles loan applications, user authentication, and loan status tracking - so security and reliability were critical."

### Testing Approach Overview (2 min)
"I implemented a comprehensive testing strategy that included:
- 30 manual test cases covering functional, regression, integration, and negative testing
- 10 user acceptance tests to validate business requirements
- Bug tracking and documentation
- After the project, I independently learned automated testing with xUnit to expand my skillset"

---

## PART 2: TESTING DEMONSTRATION (10 minutes)

### Section A: Manual Test Cases (3 minutes)

**[OPEN: TEST_CASES.csv]**

"Let me show you my manual test cases. I created 20 test cases organized by testing type."

**[SCROLL TO TC-003]**

"Here's an example - Test Case 003: Loan Application Submission. This tests the complete workflow:
- User logs in
- Fills out the loan application form
- Submits the application
- Verifies the application is saved and confirmation is displayed

This is a critical business flow, so I tested it thoroughly with different loan amounts and purposes."

**[SCROLL TO TC-011]**

"I also did negative testing - like Test Case 011 where I tried submitting a loan application without logging in. The system should redirect to login, which it did correctly."

**[OPEN: UAT_TEST_CASES.csv]**

"I also conducted User Acceptance Testing with 10 test cases focused on usability and business requirements. For example, UAT-001 validates that users can easily understand the loan application process without technical knowledge."

---

### Section B: Bug Discovery & Documentation (4 minutes)

**[OPEN: BUG_REPORT.csv]**

"During testing, I discovered and documented 3 bugs. Let me walk you through one in detail."

**[POINT TO BUG-002]**

"Bug 002 was a critical issue. When users cleared their browser session and tried to submit a loan application, the system crashed with a NullReferenceException. 

Here's how I found it:
1. I was testing session handling scenarios
2. Cleared my browser cookies to simulate a new user
3. Filled out the loan form and clicked submit
4. The application crashed

I documented:
- Clear steps to reproduce
- Expected vs actual behavior
- Environment details
- Severity as High/P1 because it affects core functionality

The development team was able to fix it quickly because of the detailed reproduction steps."

**[POINT TO BUG-003]**

"Bug 003 was lower priority - the user status field showed blank instead of 'Active' or 'Inactive'. Not critical, but it affects user experience, so I logged it as Low/P3."

"Only finding 3 bugs might seem low, but it actually indicates good code quality. I still found real issues through thorough testing."

---

### Section C: Automated Testing (2 minutes)

**[OPEN: Visual Studio/VS Code - StrongHelpOfficial.Tests folder]**

"After completing the capstone, I wanted to expand my technical skills, so I taught myself automated testing using xUnit framework."

**[OPEN: HomeControllerTests.cs]**

"I created 24 automated tests covering unit tests, integration tests, and view model validation. For example, this test verifies the home page loads correctly."

**[OPEN TERMINAL]**

"Let me run the tests live."

**[TYPE: dotnet test]**

"As you can see, the tests execute automatically and provide immediate feedback. This is great for regression testing - ensuring new changes don't break existing functionality."

---

### Section D: Results & Metrics (1 minute)

**[OPEN: TEST_SUMMARY.csv]**

"Here's my testing summary:
- 54 total tests executed
- 89% pass rate
- 3 bugs discovered and documented

The 11% failure rate represents the bugs I found, which were then fixed by the development team."

---

## PART 3: CLOSING STATEMENT (5 minutes)

### Key Takeaways (2 min)
"What I learned from this experience:

1. **Documentation is crucial** - Clear test cases and bug reports save time and prevent miscommunication

2. **Different testing types serve different purposes** - Manual testing is essential for exploratory testing and user experience, while automation is great for repetitive regression tests

3. **Testing mindset** - I learned to think like an end user, trying unexpected scenarios that developers might not anticipate

4. **Continuous learning** - I didn't stop at manual testing. I independently learned automation because I wanted to be a more well-rounded tester"

### Why I'm a Good Fit (2 min)
"I believe I'd be a strong fit for this intern position because:
- I have hands-on testing experience with a real application
- I understand both manual and automated testing approaches
- I'm detail-oriented and thorough in documentation
- I take initiative to learn new skills independently
- I understand the business impact of bugs, not just the technical details"

### Enthusiasm (1 min)
"I'm genuinely passionate about quality assurance. Finding that NullReferenceException bug and preventing users from experiencing a crash - that felt rewarding. I'd love to bring that same attention to detail and enthusiasm to your team."

---

## PART 4: Q&A PREPARATION (10 minutes)

### Expected Questions & Answers

**Q: "What testing tools are you familiar with?"**
A: "I've worked with xUnit for automated testing in .NET, and I'm comfortable with manual testing using spreadsheets for test case management. I've also used Chrome DevTools for debugging. I'm eager to learn industry-standard tools like Jira, TestRail, or Selenium if your team uses them."

**Q: "How do you prioritize which bugs to fix first?"**
A: "I use severity and priority ratings. Severity measures the impact - does it crash the system or just affect UI? Priority considers business impact - does it block critical workflows? For example, BUG-002 was High/P1 because it crashed the loan application process, while BUG-003 was Low/P3 because it was just a display issue."

**Q: "Tell me about a challenging bug you found."**
A: "BUG-002 was challenging because it only occurred under specific conditions - when session data was missing. It didn't happen during normal testing. I found it by thinking about edge cases: what if a user's session expires? What if they clear cookies? This taught me to test beyond the happy path."

**Q: "What's the difference between functional and regression testing?"**
A: "Functional testing verifies that features work as designed - like testing if the loan application form submits correctly. Regression testing ensures that new changes don't break existing functionality - like re-testing the login feature after adding a new user profile page."

**Q: "Why only 3 bugs? Shouldn't there be more?"**
A: "Three bugs actually indicates good code quality, which is realistic for a well-developed project. I focused on thorough testing rather than logging minor issues. Each bug I reported was legitimate and impacted functionality. Quality over quantity."

**Q: "Do you prefer manual or automated testing?"**
A: "Both have their place. Manual testing is essential for exploratory testing, usability validation, and testing new features. Automated testing is great for regression testing and repetitive scenarios. The best approach uses both strategically."

**Q: "What would you do if developers disagreed with your bug report?"**
A: "I'd provide clear evidence - reproduction steps, screenshots if available, and explain the user impact. If they still disagree, I'd ask for clarification on why it's working as intended. Maybe I misunderstood the requirements. Good communication is key."

**Q: "How do you stay organized with multiple test cases?"**
A: "I use structured documentation with clear IDs, categories, and status tracking. My CSV files organize tests by type - functional, regression, integration, UAT. I also prioritize based on risk - testing critical features like authentication and loan submission first."

**Q: "What did you learn from this capstone project?"**
A: "I learned that testing isn't just about finding bugs - it's about understanding user needs and business requirements. I also learned the importance of clear documentation and communication. And I discovered I really enjoy the problem-solving aspect of testing."

**Q: "Where do you see yourself growing as a tester?"**
A: "I want to deepen my automation skills - learning Selenium for web testing and API testing tools. I'm also interested in performance testing and security testing. Long-term, I'd like to contribute to test strategy and mentor other testers."

---

## TIPS FOR DELIVERY

### Body Language
- Maintain eye contact
- Speak clearly and confidently
- Use hand gestures when explaining workflows
- Show enthusiasm when discussing bugs you found

### Screen Sharing
- Have all files open in separate tabs/windows beforehand
- Practice switching between files smoothly
- Zoom in on text so it's readable
- Don't spend too long scrolling

### Timing
- Practice this script to stay within 10 minutes for demo
- If running short, add more detail to bug explanation
- If running long, skip showing multiple test cases - just show 1-2 examples

### Confidence Boosters
- "I'm proud of the thoroughness of my testing"
- "This bug could have affected real users"
- "I took initiative to learn automation on my own"
- "I understand the business impact of quality"

---

## FINAL CHECKLIST BEFORE INTERVIEW

- [ ] All CSV files open and ready
- [ ] Visual Studio/VS Code open with test project
- [ ] Terminal ready to run `dotnet test`
- [ ] Practice the script at least 3 times
- [ ] Time yourself - aim for 9-10 minutes
- [ ] Prepare 2-3 questions to ask them
- [ ] Test your screen sharing setup
- [ ] Have water nearby
- [ ] Smile and be yourself!

---

**Good luck! You've got this! 🚀**
