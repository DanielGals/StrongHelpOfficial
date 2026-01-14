# Manual Testing Documentation for StrongHelpOfficial

## Test Plan Overview
**Project:** StrongHelpOfficial - Loan Management System  
**Tester:** [Your Name]  
**Date:** [Current Date]  
**Test Type:** Manual Functional Testing  
**Environment:** Development/Testing

---

## 1. TEST SCENARIOS

### Module 1: Home Page & Public Pages

#### TC-001: Verify Home Page Loads
**Priority:** High  
**Steps:**
1. Open browser
2. Navigate to http://localhost:5000/
3. Verify page loads successfully
4. Check all navigation links are visible

**Expected Result:** Home page displays with navigation menu  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-002: Verify About Page
**Priority:** Medium  
**Steps:**
1. Click "About" in navigation
2. Verify page loads
3. Check content is displayed

**Expected Result:** About page shows company information  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-003: Verify Contact Page
**Priority:** Medium  
**Steps:**
1. Click "Contact" in navigation
2. Verify contact form is displayed
3. Check all form fields are present

**Expected Result:** Contact page with form displayed  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

### Module 2: Authentication & Login

#### TC-004: Verify Login Page Access
**Priority:** High  
**Steps:**
1. Navigate to /Auth/Login
2. Verify login page loads
3. Check authentication status message

**Expected Result:** Login page displays  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-005: Verify Role Selection Page
**Priority:** High  
**Steps:**
1. Login as authenticated user
2. Navigate to /Auth/Selection
3. Verify role options are displayed

**Expected Result:** Selection page shows available roles  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-006: Verify Employee Role Access
**Priority:** High  
**Steps:**
1. Login as Employee
2. Click "Switch to Employee"
3. Verify redirect to Loaner Dashboard

**Expected Result:** Redirects to /Loaner/LoanerDashboard  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

### Module 3: Loan Application (Employee)

#### TC-007: Verify Apply for Loan Page
**Priority:** High  
**Steps:**
1. Login as Employee
2. Navigate to Apply for Loan
3. Verify form is displayed
4. Check required documents list

**Expected Result:** Loan application form with document requirements  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-008: Submit Loan Without Documents
**Priority:** High  
**Steps:**
1. Open Apply for Loan page
2. Enter loan amount: 50000
3. Click Submit without uploading documents
4. Verify error message

**Expected Result:** Error: "Your loan must at least have an amount, the 3 required documents, and an assigned co-maker!"  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-009: Submit Complete Loan Application
**Priority:** High  
**Steps:**
1. Enter loan amount: 50000
2. Upload 3 PDF documents
3. Select co-maker from search
4. Click Submit
5. Verify success message

**Expected Result:** Success message: "Loan request submitted successfully!"  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-010: Verify Co-Maker Search
**Priority:** Medium  
**Steps:**
1. In loan application form
2. Type name in co-maker search field
3. Verify search results appear
4. Select a co-maker

**Expected Result:** Search shows available co-makers, selection works  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-011: Upload Invalid File Type
**Priority:** Medium  
**Steps:**
1. Try to upload .docx file
2. Verify error message

**Expected Result:** Error: "Only PDF files are allowed for upload."  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

### Module 4: My Application (Employee)

#### TC-012: View My Application
**Priority:** High  
**Steps:**
1. Login as Employee with active loan
2. Navigate to My Application
3. Verify loan details are displayed

**Expected Result:** Shows loan amount, status, documents  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-013: View Approval Flow
**Priority:** Medium  
**Steps:**
1. Open My Application
2. Click "Approval Flow" tab
3. Verify approval stages are shown

**Expected Result:** Displays approval workflow with current stage  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

### Module 5: Benefits Assistant

#### TC-014: View Applications Dashboard
**Priority:** High  
**Steps:**
1. Login as Benefits Assistant
2. Navigate to Applications
3. Verify list of loan applications

**Expected Result:** Table showing all pending applications  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-015: View Application Details
**Priority:** High  
**Steps:**
1. Click on an application
2. Verify all details are displayed
3. Check documents are viewable

**Expected Result:** Full application details with documents  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-016: Assign Loan Information
**Priority:** High  
**Steps:**
1. Open application details
2. Fill in loan information fields
3. Click Save
4. Verify success message

**Expected Result:** Loan information saved successfully  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

### Module 6: Approver

#### TC-017: View Pending Applications
**Priority:** High  
**Steps:**
1. Login as Approver
2. Navigate to Applications
3. Verify pending applications list

**Expected Result:** Shows applications awaiting approval  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-018: Approve Application
**Priority:** High  
**Steps:**
1. Open application details
2. Select "Approve"
3. Enter comments
4. Click Submit
5. Verify status changes

**Expected Result:** Application status changes to "Approved"  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-019: Reject Application
**Priority:** High  
**Steps:**
1. Open application details
2. Select "Reject"
3. Enter rejection reason
4. Click Submit
5. Verify status changes

**Expected Result:** Application status changes to "Rejected"  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

### Module 7: Admin

#### TC-020: View Admin Dashboard
**Priority:** High  
**Steps:**
1. Login as Admin
2. Navigate to Admin Dashboard
3. Verify statistics are displayed

**Expected Result:** Dashboard shows user count, loan stats  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-021: Add New User
**Priority:** High  
**Steps:**
1. Navigate to Admin > Add User
2. Fill in user details
3. Select role
4. Click Save
5. Verify success message

**Expected Result:** User created successfully  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-022: Manage User - Deactivate
**Priority:** High  
**Steps:**
1. Navigate to Admin > Users
2. Select a user
3. Click Deactivate
4. Verify user status changes

**Expected Result:** User status changes to "Inactive"  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-023: View System Logs
**Priority:** Medium  
**Steps:**
1. Navigate to Admin > Logs
2. Verify logs are displayed
3. Test filter functionality

**Expected Result:** Logs displayed with filter options  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

## 2. NEGATIVE TEST CASES

#### TC-024: Access Restricted Page Without Login
**Priority:** High  
**Steps:**
1. Logout from system
2. Try to access /Loaner/ApplyForLoan directly
3. Verify redirect to login

**Expected Result:** Redirects to login page  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-025: Submit Loan with Amount = 0
**Priority:** Medium  
**Steps:**
1. Enter loan amount: 0
2. Upload documents
3. Select co-maker
4. Click Submit

**Expected Result:** Error message or validation  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-026: Submit Loan with Negative Amount
**Priority:** Medium  
**Steps:**
1. Enter loan amount: -5000
2. Try to submit

**Expected Result:** Validation error  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

## 3. UI/UX TEST CASES

#### TC-027: Verify Responsive Design
**Priority:** Medium  
**Steps:**
1. Open application in browser
2. Resize window to mobile size
3. Verify layout adjusts properly

**Expected Result:** UI is responsive and usable on mobile  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-028: Verify Navigation Menu
**Priority:** Medium  
**Steps:**
1. Login as any role
2. Click each menu item
3. Verify correct page loads

**Expected Result:** All navigation links work correctly  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

#### TC-029: Verify Error Messages Display
**Priority:** Medium  
**Steps:**
1. Trigger various errors
2. Verify error messages are clear and visible

**Expected Result:** Error messages are user-friendly  
**Status:** [ ] Pass [ ] Fail  
**Comments:** _______________

---

## 4. BROWSER COMPATIBILITY

#### TC-030: Test on Chrome
**Browser:** Google Chrome  
**Version:** _______  
**Status:** [ ] Pass [ ] Fail  
**Issues:** _______________

#### TC-031: Test on Firefox
**Browser:** Mozilla Firefox  
**Version:** _______  
**Status:** [ ] Pass [ ] Fail  
**Issues:** _______________

#### TC-032: Test on Edge
**Browser:** Microsoft Edge  
**Version:** _______  
**Status:** [ ] Pass [ ] Fail  
**Issues:** _______________

---

## 5. BUG REPORT TEMPLATE

**Bug ID:** BUG-001  
**Title:** [Short description]  
**Severity:** [ ] Critical [ ] High [ ] Medium [ ] Low  
**Priority:** [ ] P1 [ ] P2 [ ] P3  

**Steps to Reproduce:**
1. 
2. 
3. 

**Expected Result:**  


**Actual Result:**  


**Screenshots:** [Attach if available]  
**Environment:** Windows/Browser/Version  
**Reported By:** [Your Name]  
**Date:** [Date]  

---

## 6. TEST EXECUTION SUMMARY

**Total Test Cases:** 32  
**Executed:** ___  
**Passed:** ___  
**Failed:** ___  
**Blocked:** ___  
**Pass Rate:** ____%  

**Critical Issues Found:** ___  
**High Priority Issues:** ___  
**Medium Priority Issues:** ___  
**Low Priority Issues:** ___  

**Overall Status:** [ ] Ready for Release [ ] Needs Fixes [ ] Major Issues  

**Tester Sign-off:** _______________  
**Date:** _______________

---

## 7. TESTING CHECKLIST

### Pre-Testing
- [ ] Test environment is set up
- [ ] Test data is prepared
- [ ] Access credentials obtained
- [ ] Browser versions verified

### During Testing
- [ ] Follow test cases step by step
- [ ] Document all findings
- [ ] Take screenshots of bugs
- [ ] Note any deviations

### Post-Testing
- [ ] Complete test execution summary
- [ ] Report all bugs found
- [ ] Update test case status
- [ ] Provide recommendations

---

## 8. NOTES FOR DEMONSTRATION

**For Interview, Explain:**
1. "I created 32 manual test cases covering all major features"
2. "Tests include functional, negative, and UI/UX scenarios"
3. "I documented expected vs actual results for each test"
4. "I created a bug report template for tracking issues"
5. "I tested across multiple browsers for compatibility"

**Show Your Process:**
- Systematic approach to testing
- Clear documentation
- Bug tracking methodology
- Understanding of test priorities
- Both positive and negative testing
