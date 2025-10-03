# Lab 4 - Review New Code - Jose Manuel Morales Patty

## VSVendorDetails Module

### VSVendorDetails Overview

The VSVendorDetails module is a comprehensive vendor management system built on
the DotNetNuke (DNN) platform that enables Vision Source to maintain a directory
of approved vendors with detailed profiles, reviews, and marketing materials.
The module implements a multi-tier approval workflow where vendors can
self-manage their profiles, but changes require administrative review before
going live. This ensures content quality while reducing administrative overhead
through selective automation.

### VSVendorDetails Location

```plaintext
insight-dnn
|__VisionSourceDNN
   |__DesktopModules
      |__VisionSource
          |__VSVendorDetails
              |__App
                 |__Pages
                 |__Scripts
                 |__Lib
              |__README.md
              |__View.ascx
              |__View.ascx.cs
```

### VSVendorDetails Role-Based Access Control

The system uses role-based access control (RBAC) to manage five distinct user
types, each with specific permissions ranging from read-only access to full
administrative control. All vendor-initiated changes enter a pending approval
queue visible to administrative users who can selectively approve or decline
modifications with feedback.

| Role                   | Primary Function                    | Key Capabilities                                                                                                                   | Restrictions                                                                                                          |
| ---------------------- | ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| **Super User**         | System Administration               | • Unrestricted access to all features<br>• Bypass all validation rules<br>• Delete any content<br>• Manage all vendors             | None                                                                                                                  |
| **Vendor Relations**   | Vendor Liaison & Content Moderation | • Approve/decline vendor changes<br>• Edit any vendor profile<br>• Manage vendor programs<br>• Reply to reviews<br>• Email vendors | Cannot delete reviews                                                                                                 |
| **Administrator**      | System-Level Management             | • Full edit access<br>• Approval workflows<br>• Manage vendor types/categories<br>• Review moderation                              | Cannot delete reviews                                                                                                 |
| **Vendor User**        | Self-Service Profile Management     | • Edit own vendor profile<br>• Upload media & documents<br>• Manage social media links<br>• View analytics                         | • Changes require approval<br>• Cannot edit type/category<br>• Cannot access other vendors<br>• Cannot delete reviews |
| **Member Services**    | Information Support                 | • View all vendor details<br>• Bookmark vendors<br>• Submit reviews                                                                | No edit privileges (read-only)                                                                                        |
| **Authenticated User** | General Access                      | • View vendor profiles<br>• Read reviews<br>• Submit reviews<br>• Create bookmarks                                                 | Cannot edit vendor information                                                                                        |

### VSVendorDetails API Endpoints Architecture

The module consumes data from three distinct API layers, each serving specific
functional areas:

```plaintext
┌─────────────────────────────────────────────────────────────┐
│                    API ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. VENDOR DETAILS API (Primary) - Rest API                 │
│     Base: /VendorDetails/api                                │
│     Purpose: Core vendor profile management & approval      │
│                                                             │
│  2. VENDOR DIRECTORY SERVICE (Bookmarks) - WCF service      │
│     Base: /VendorDirectory/service.svc                      │
│     Purpose: User bookmark functionality                    │
│                                                             │
│  3. LEGACY WEB SERVICES API (Reviews) - DNN Module API      │
│     Base: /DesktopModules/VSWebServicesAPI/API              │
│     Purpose: Review and rating system                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Vendor Information Management Endpoints

- **Repository**: visionsource-webapi
- **Source**: VendorDetails API (`/VendorDetails/api`)
- **Databases**: VSBackOffice
- **DB Tables**: Vendor, VendorInformationAdminApproval
- **DB Procedures**: usp_vendor_sel, ManageVendorInformationAdminApproval

| Endpoint                             | Method | URL Pattern                                              | Purpose                                                                                                                                                                                                 |
| ------------------------------------ | ------ | -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GETALLVENDORINFO**                 | GET    | `/VendorDetails/api/users/{userId}/vendors/{vendorId}`   | Retrieves complete vendor profile including basic information, ratings, programs, documents, and pending changes. Returns comprehensive vendor object with all related data.                            |
| **UPDATEVENDORINFO**                 | PUT    | `/VendorDetails/api/vendor`                              | Accepts vendor profile updates via FormData payload. Can handle incremental updates (changed fields only) or complete profile replacements. Enforces role-based validation and approval workflow rules. |
| **GET_VENDOR_PENDING_APPROVAL_DATA** | GET    | `/VendorDetails/api/pending-approval/vendors/{vendorId}` | Fetches all pending changes for a specific vendor with field-level granularity. Returns side-by-side comparison data showing current live values versus proposed changes. Used by approval interface.   |
| **APPROVE_PENDING_INFORMATION**      | POST   | `/VendorDetails/api/{vendorId}/approve-pending`          | Processes selective approval of pending changes. Accepts array of approved field names, merges approved data into live profile, and clears approved items from pending queue.                           |
| **REJECT_PENDING_INFORMATION**       | POST   | `/VendorDetails/api/{vendorId}/reject-pending`           | Declines pending changes with required explanation. Sends notification email to vendor with decline reasons, clears rejected data from pending queue.                                                   |

#### Document Management Endpoints

- **Repository**: visionsource-webapi
- **Source**: VendorDetails API (`/VendorDetails/api`)
- **Databases**: VSBackOffice
- **DB Tables**: Vendor_Documents_AprovalPending, Vendor_Documents
- **DB Procedures**: spSaveVendorDocument, spSaveVendorDocumentForApproval,
  spGetVendorDocumentsForApproval

| Endpoint                           | Method | URL Pattern                                                        | Purpose                                                                                                                                                                                                                                                                                     |
| ---------------------------------- | ------ | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **SAVE_DOCUMENT**                  | POST   | `/VendorDetails/api/document/upload`                               | Uploads individual vendor documents with comprehensive metadata (name, visibility dates, expiration, display order). Validates file extensions (.doc, .docx, .xls, .xlsx, .pdf, .txt, .ppt, .pptx, .png, .jpeg, .jpg) and enforces 10MB size limit. Documents enter pending approval state. |
| **REMOVE_DOCUMENT**                | POST   | `/VendorDetails/api/remove/document`                               | Deletes a vendor document by document ID. Physically removes file from storage and removes database record. Requires ownership validation or administrative privileges.                                                                                                                     |
| **GET_PENDING_APPROVAL_DOCUMENTS** | GET    | `/VendorDetails/api/pending-approval/vendors/{vendorId}/documents` | Retrieves list of documents awaiting administrative approval. Returns document metadata including upload date, file size, and submitter information. Used by approval interface.                                                                                                            |

#### Bookmark System Endpoints

- **Repository**: dnn-modules
- **Source**: VendorDirectory Service (`/VendorDirectory/service.svc`)
- **Databases**: VSBackOffice
- **DB Tables**: user_vendor
- **DB Procedures**: usp_user_vendor_ins, usp_user_vendor_del, spGetBookMark

| Endpoint           | Method | URL Pattern                                                             | Purpose                                                                                                                                                               |
| ------------------ | ------ | ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GETBOOKMARK**    | GET    | `/VendorDirectory/service.svc/BookMark/user/{userId}/vendor/{vendorId}` | Checks if current user has bookmarked the specific vendor. Returns boolean status used to render correct bookmark button state (Add vs Remove).                       |
| **UPDATEBOOKMARK** | PUT    | `/VendorDirectory/service.svc/BookMark`                                 | Toggles vendor bookmark status for the user. Adds vendor to user's "Vendor Book" if not present, removes if already bookmarked. Payload includes userId and vendorId. |

#### Review System Endpoints

- **Repository**: insight-dnn
- **Source**: Legacy Web Services API (`/DesktopModules/VSWebServicesAPI/API`)
- **Databases**: Royalty
- **DB Tables**: VendorReviews, User_Vendor_ratting
- **DB Procedures**: UpsertVendorReview, ManageUserWiseVendorRReview,
  RemoveReviewByID

| Endpoint             | Method | URL Pattern                                                                    | Purpose                                                                                                                                                                            |
| -------------------- | ------ | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **POSTREVIEWRATING** | POST   | `/DesktopModules/VSWebServicesAPI/API/Vendor/ReviewRating`                     | Submits new vendor review with star rating (1-5 stars) and optional text comment. Validates user hasn't already reviewed this vendor. Review appears immediately without approval. |
| **POSTREPLY**        | POST   | `/DesktopModules/VSWebServicesAPI/API/Vendor/ReplyReview?reviewId={reviewId}`  | Posts official vendor response to a customer review. Restricted to Vendor Relations and Super User roles. Reply appears beneath original review showing organization's response.   |
| **DELETEREVIEW**     | DELETE | `/DesktopModules/VSWebServicesAPI/API/Vendor/DeleteReview?reviewId={reviewId}` | Permanently removes a review from the system. Exclusive to Super User role. Used for inappropriate content or spam reviews. Action is irreversible.                                |

### VSVendorDetails API Interaction Patterns

#### Initialization Flow

When a user lands on a vendor detail page, the system executes parallel AJAX
calls to load all necessary data:

```plaintext
Page Load
    ├─→ GETALLVENDORINFO (Vendor profile, programs, documents)
    ├─→ GETBOOKMARK (User's bookmark status)
    ├─→ GET_VENDOR_PENDING_APPROVAL_DATA (Pending changes - if admin)
    └─→ GET_PENDING_APPROVAL_DOCUMENTS (Pending docs - if admin)
```

#### Edit & Save Flow

Vendor users or administrators making profile changes follow this pattern:

```plaintext
User Edits Profile
    └─→ Client-side validation executes
        └─→ User clicks "Save"
            ├─→ SAVE_IMAGES (if images changed)
            ├─→ SAVE_DOCUMENT (for each new document)
            └─→ UPDATEVENDORINFO (all text/metadata changes)
                └─→ Data enters pending approval queue
                    └─→ Notification triggers for Vendor Relations
```

#### Approval Flow

Administrative users reviewing vendor changes:

```plaintext
Vendor Relations Enters Approval Mode
    └─→ GET_VENDOR_PENDING_APPROVAL_DATA (loads pending changes)
    └─→ GET_PENDING_APPROVAL_DOCUMENTS (loads pending documents)
        └─→ Admin reviews side-by-side comparison
            ├─→ APPROVE_PENDING_INFORMATION (selected fields)
            │   └─→ Changes merge to live profile
            └─→ REJECT_PENDING_INFORMATION (unwanted changes)
                └─→ Vendor receives email notification with reasons
```

## VSMyOfficesDashboard Module

### VSMyOfficesDashboard Overview

The VSMyOfficesDashboard module is a comprehensive multi-practice management
platform built on the DotNetNuke (DNN) platform that provides Vision Source
members with centralized access to critical business operations across multiple
office locations. The module serves as a unified dashboard for managing practice
performance, financial reporting, staff administration, business metrics, and
inter-office communications. It features role-based access control with
extensive permission granularity, supporting various user roles from practice
owners and corporate administrators to staff members with specific operational
responsibilities. The system integrates with multiple external APIs including
Power BI for embedded analytics, Azure Storage for vendor resources, and custom
Vision Source services for member-specific data. The module implements a
sophisticated URL routing system that enables deep-linking to specific offices,
tabs, and data views, while maintaining state through query parameters for
enhanced navigation and bookmarking capabilities.

### VSMyOfficesDashboard Location

```plaintext
insight-dnn
|__VisionSourceDNN
   |__DesktopModules
      |__VisionSource
          |__VSMyOfficesDashboard
              |__App
                 |__Content
                    |__Styles
                       |__main.css
                       |__tab-accountHistory.css
                       |__tab-BOO.css
                       |__tab-communication.css
                       |__tab-managePersonnel.css
                       |__tab-officeInformation.css
                       |__tab-summary.css
                       |__tab-vsNext.css
                 |__Pages
                    |__[Various pages]
                 |__Scripts
                    |__Common.js (Shared configuration & utilities)
                    |__Main.js (Primary application controller)
                    |__AccountHistory.js (Financial management)
                    |__BOO.js (Business of Optometry metrics)
                    |__Communication.js (Inter-office messaging)
                    |__ManagePersonnel.js (Staff administration)
                    |__OfficeInformation.js (Practice details)
                    |__Search.js (Office/doctor search)
                    |__Summary.js (Performance dashboard)
                    |__VSNext.js (VSNext program management)
              |__Main.ascx (Main Module View)
```

### VSMyOfficesDashboard API Endpoints Architecture

The module consumes data from multiple distinct API services organized by
functional domain:

```plaintext
┌─────────────────────────────────────────────────────────────────────────┐
│                         API ARCHITECTURE                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. MY OFFICES DASHBOARD API (Primary) - REST API                       │
│     Base: /myofficesdashboard/api                                       │
│     Purpose: Office search, summary metrics, financial data             │
│                                                                         │
│  2. MEMBER DASHBOARD SERVICE API - WCF Service                          │
│     Base: /memberdashboard/service.svc                                  │
│     Purpose: Legacy member dashboard data integration                   │
│                                                                         │
│  3. STAFF MANAGEMENT API - WCF Service                                  │
│     Base: /staffmanagement/api                                          │
│     Purpose: Personnel management, role assignments, office linking     │
│                                                                         │
│  4. POWER BI EMBED API - WCF Service                                    │
│     Base: /powerbi/api/embed/report                                     │
│     Purpose: Power BI report embedding for analytics dashboards         │
│                                                                         │
│  5. DATA DRIVEN SURVEY API - REST API                                   │
│     Base: /VSDataDrivenSurveyV2/api                                     │
│     Purpose: Survey form management and submission (VSNext)             │
│                                                                         │
│  6. PORTAL API - DNN Services API                                       │
│     Base: /DesktopModules/Services/API/Personnel                        │
│     Purpose: DNN user account management, password resets               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Office Search & Selection Endpoints

- **Repository**: visionsource-webapi
- **Source**: My Offices Dashboard API (`/myofficesdashboard/api`)
- **Database**: VSSqlServerCRM
- **DB Tables**: vStaffOffices, Company
- **DB Procedures**: spGetOfficesByDid, spSearchOwners

| Endpoint                               | Method | URL Pattern                                                                | Purpose                                                                                                                                                                                                                                                                                                                                                                                                            |
| -------------------------------------- | ------ | -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **GET_ALL_OFFICES**                    | GET    | `/myofficesdashboard/api/search/offices?staff={staffId}`                   | Retrieves all offices accessible to the specified staff member. Returns array of office objects containing: `officeId`, `officeName`, `accountKey`, `city`, `state`, `ownerName`, `contactInfo`. Response is filtered based on user's role - superusers and administrators see all offices, while regular members see only their associated practices. Used in office selection dropdown and search functionality. |
| **GET_ALL_OFFICES_BY_SELECTED_DOCTOR** | GET    | `/myofficesdashboard/api/search/offices?staff={staffId}`                   | Dynamic variant of GET_ALL_OFFICES that accepts a different staffId parameter. Enables viewing offices from another doctor's perspective when user has appropriate permissions (superuser, DNN admin, or corporate role). Commonly used when switching between different practice owner views in multi-office scenarios.                                                                                           |
| **SEARCH_DOCTOR**                      | GET    | `/myofficesdashboard/api/search/owners?staff={staffId}&query={searchTerm}` | Performs autocomplete search for practice owners/doctors. The `staff` parameter determines search scope: `staff=0` for superusers/admins (searches all doctors), or `staff={currentStaffId}` for regular users (searches within their network). Returns array of staff objects with: `staffId`, `fullName`, `email`, `officeCount`. Used in doctor search typeahead functionality for practice switching.          |

#### Summary Dashboard Endpoints

- **Repository**: visionsource-webapi
- **Source**: My Offices Dashboard API (`/myofficesdashboard/api/summary`)
- **Database**: VSSqlServerVSDW
- **DB Tables**: bi_dds.dimLocation, bi_dds.dimCRMProgramMembership, CTE
- **DB Procedures**: SP_VSInsight_memberdashboard_SUM_Education_InPerson,
  SP_VSInsight_memberdashboard_SUM_Education_InPerson

| Endpoint                            | Method | URL Pattern                                                                                                | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                            |
| ----------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GET_PP_PARTICIPATION_PERCENTAGE** | GET    | `/myofficesdashboard/api/summary/totals?staff={staffId}&office={officeId}&category=program-participations` | Retrieves aggregate program participation metrics for dashboard KPI cards. Returns: `totalPrograms`, `activeParticipations`, `participationPercentage`, `trendDirection` (up/down/flat). Used to display overall engagement with Vision Source programs (Frame Dream, TOD, VSLearning, etc.). Calculated based on active enrollments vs. available programs.                                                                       |
| **GET_PP_GROWTH**                   | GET    | `/myofficesdashboard/api/summary/totals?staff={staffId}&office={officeId}&category=12-month-growths`       | Retrieves 12-month growth metrics summary for dashboard overview. Returns: `currentYearSales`, `priorYearSales`, `growthPercentage`, `growthAmount`, `reportingMonths`. Compares current 12-month rolling sales against same period previous year. Used in main dashboard KPI display.                                                                                                                                             |
| **GET_SUMMARY_PP**                  | GET    | `/myofficesdashboard/api/summary/program-participations?staff={staffId}&office={officeId}`                 | Retrieves detailed program participation data for all Vision Source programs. Returns array of program objects with: `programName`, `programType`, `enrollmentStatus`, `enrollmentDate`, `currentStatus`, `benefitsEarned`, `requirementsCompliance`. Includes: Frame Dream, The Optical Dream (TOD), VSLearning, VSNext, Max Programs (FocusCL, Luxottica). Used to populate program participation cards with drill-down details. |
| **GET_VSLEARNING_YTD_LESSONS**      | GET    | `/myofficesdashboard/api/summary/education-chart-ytd?staff={staffId}&office={officeId}`                    | Retrieves year-to-date VSLearning completion data for chart visualization. Returns array of monthly data points: `month`, `onlineLessonsCompleted`, `inPersonHoursCompleted`, `totalCECredits`, `staffParticipationCount`, `complianceStatus`. Used to render educational engagement trend charts showing online learning activity.                                                                                                |
| **GET_VSLEARNING_IN_PERSON**        | GET    | `/myofficesdashboard/api/summary/education-in-person-chart-ytd?staff={staffId}&office={officeId}`          | Retrieves year-to-date in-person training attendance data. Returns array of monthly data: `month`, `regionalMeetingsAttended`, `nationalConferenceAttended`, `lunchAndLearns`, `totalInPersonHours`, `participatingStaff`. Used to visualize in-person educational engagement separate from online learning.                                                                                                                       |

#### Account History & Financial Management Endpoints

- **Repository**: visionsource-webapi
- **Source**: My Offices Dashboard API
  (`/myofficesdashboard/api/account-history`)
- **Database**: VSSqlServerCRM
- **DB Tables**: vPendingSalesAndUnreported, CTE, Company
- **DB Procedures**: p_GetPaymentHistoryUnreportedSales,
  spGetRebateToRoyaltyInfo

| Endpoint                               | Method | URL Pattern                                                                                   | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                     |
| -------------------------------------- | ------ | --------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GET_REBATES_TO_ROYALTIES**           | GET    | `/myofficesdashboard/api/account-history/rebate-to-royalty?office={officeId}&staff={staffId}` | Retrieves current rebate-to-royalty automatic application setting status. Returns: `isEnabled` (boolean), `effectiveDate`, `appliedRebateTotal`, `newMemberPromiseParticipant` (boolean), `canModifySetting` (boolean based on ownership role). This feature allows practice owners to automatically apply future rebate earnings toward royalty fee obligations. Setting is locked ON for New Member Promise participants. |
| **GET_BALANCE_DUE**                    | GET    | `/myofficesdashboard/api/account-history/dashboard?office={officeId}`                         | Retrieves comprehensive account balance dashboard data. Returns: `currentBalanceDue`, `dueDate`, `lastPaymentDate`, `lastPaymentAmount`, `accountStatus` (current/overdue/paid), `paymentHistory[]`, `upcomingCharges[]`, `accountAge`, `lateFeeAmount`. Provides complete financial overview for practice's Vision Source account.                                                                                         |
| **GET_SALES_PAYMENTS_HISTORY**         | GET    | `/myofficesdashboard/api/account-history/sales?office={officeId}&type=posted&year=-1`         | Retrieves complete sales and payment history across all years. The `year=-1` parameter indicates "all years". Returns array of transaction records: `transactionDate`, `transactionType` (sale/payment/adjustment), `amount`, `balance`, `reportingPeriod`, `referenceNumber`, `status`. Used to populate complete account history table with pagination.                                                                   |
| **GET_SALES_PAYMENTS_HISTORY_BY_YEAR** | GET    | `/myofficesdashboard/api/account-history/sales?office={officeId}&type=posted&year={year}`     | Retrieves sales and payment history filtered by specific year. Returns array of transactions for selected year with same structure as GET_SALES_PAYMENTS_HISTORY. Used when user filters account history by year using year dropdown selector.                                                                                                                                                                              |
| **GET_SALE_DETAILS**                   | GET    | `/myofficesdashboard/api/account-history/sales/{saleId}`                                      | Retrieves detailed information for specific sale transaction. Returns: `saleId`, `officeId`, `reportingMonth`, `reportingYear`, `grossSales`, `pairsSold`, `reportedBy` (staff info), `reportedDate`, `calculatedRoyalty`, `paymentStatus`, `paymentDate`, `paymentAmount`, `notes`, `modificationHistory[]`. Used in sale detail modal/view for editing or reviewing specific sale records.                                |

#### Staff Management & Personnel Endpoints

- **Repository**: visionsource-webapi
- **Source**: Staff Management API (`/staffmanagement/api/personnel`)
- **Database**: CRM
- **DB Tables**: vStaffOffices, Staff
- **DB Procedures**: spGetOfficesByStaff, spGetStaffById

| Endpoint                                  | Method | URL Pattern                                                | Purpose                                                                                                                                                                                                                                                                                                                                                                                  |
| ----------------------------------------- | ------ | ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GET_PERSON_IN_OFFICE**                  | GET    | `/staffmanagement/api/personnel/offices/{officeId}/staffs` | Retrieves all staff members associated with specific office. Returns array of staff records: `staffId`, `firstName`, `lastName`, `email`, `phone`, `roles[]`, `hireDate`, `status` (active/inactive), `dnnUserId`, `isOwner`, `permissions`. Used to populate office personnel roster in Manage Personnel tab.                                                                           |
| **GET_ROLES**                             | GET    | `/staffmanagement/api/personnel/vsroles`                   | Retrieves complete list of available Vision Source roles for assignment. Returns array of role objects: `roleId`, `roleName`, `roleDescription`, `permissionLevel`, `isOwnerRole`, `requiresLicense`. Roles include: Practice Owner, Office Manager, Optician, Optometrist, Billing Staff, etc. Used in role selection dropdown when adding/editing staff members.                       |
| **GET_OE_TRACKER_STATES**                 | GET    | `{REST_ENDPOINT}/states`                                   | Retrieves list of US states for address validation and OE Tracker integration. Returns array: `stateCode`, `stateName`, `oeTrackerEnabled`. Used in staff address forms and OE Tracker state licensure validation. External API endpoint separate from main staff management service.                                                                                                    |
| **PUT_EXISTING_PERSON**                   | PUT    | `/staffmanagement/api/personnel/staff`                     | Updates existing staff member information. Payload: `{staffId, firstName, lastName, email, phone, address, roles[], officeAssignments[], status, notes}`. Validates: email uniqueness, role permissions, ownership requirements. Returns: `staffId`, `updateConfirmation`, `changedFields[]`, `effectiveDate`. Used when editing staff member details in personnel management interface. |
| **GET_ALL_OFFICES_INFORMATION_BY_PERSON** | GET    | `/staffmanagement/api/personnel/staffs/{staffId}/offices`  | Retrieves all office associations for specific staff member. Returns array of office assignments: `officeId`, `officeName`, `roles[]`, `startDate`, `endDate`, `isPrimaryOffice`, `accessLevel`. Used to display multi-office staff member's complete office network and manage cross-office assignments.                                                                                |

#### Office Information Management Endpoints

- **Repository**: visionsource-webapi
- **Source**: My Offices Dashboard API (`/myofficesdashboard/api/offices`)
- **Database**: VSSqlServerCRM
- **DB Tables**: vSummaryCompany, vOfficeStaff, vCompany
- **Authentication**: spGetOfficeDetailInfoAndHours,
  spUpdateOfficeContactAndHours

| Endpoint                         | Method | URL Pattern                                                        | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| -------------------------------- | ------ | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **GET_STATES**                   | GET    | `/myofficesdashboard/api/offices/states`                           | Retrieves list of US states for office address validation. Returns array: `stateCode`, `stateName`, `stateAbbreviation`. Used in office address form dropdowns and address validation. Simple reference data endpoint.                                                                                                                                                                                                                           |
| **GET_OFFICE_INFORMATION_HOURS** | GET    | `/myofficesdashboard/api/offices/info-and-hours?office={officeId}` | Retrieves complete office information and operating hours. Returns: `officeId`, `officeName`, `address`, `phone`, `fax`, `email`, `website`, `socialMedia` (Facebook, Twitter), `operatingHours[]` (7 days), `officeSize`, `examLanes`, `specialNotes`. Used to populate office information edit form with current values.                                                                                                                       |
| **PUT_OFFICE_INFORMATION_HOURS** | PUT    | `/myofficesdashboard/api/offices/info-and-hours`                   | Updates office information and operating hours. Payload: `{officeId, officeName, address, phone, email, website, socialMedia, operatingHours[], officeSize, examLanes, specialNotes}`. Validates: phone format, email format, URL formats, office size numeric range, exam lanes (1-99). Returns: `officeId`, `updatedFields[]`, `validationWarnings[]`, `confirmationMessage`. Changes are immediately visible on public-facing office locator. |

#### VSNext Program Management Endpoints

- **Repository**: visionsource-webapi
- **Sources**: REST API & Data Driven Survey API
- **Database**: VisionSource
- **DB Tables**: DDSurvey_User_Response_Instance,
  DDSurvey_Question_User_Response, DDSurvey, SurveyQuestions
- **DB Procedures**: spDDSurvey_GetQuestionsResponsesHistory,
  spDDSurvey_GetQuestionsResponses

| Endpoint               | Method | URL Pattern                                                                 | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ---------------------- | ------ | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **GET_VSNEXT_CREDITS** | GET    | `{REST_ENDPOINT}/staffs/{staffId}/programs/VS Next Royalty Credits/offices` | Retrieves VSNext program participation and credit information. Returns: `offices[]` array with `officeId`, `officeName`, `programStatus`, `creditsEarned`, `creditsApplied`, `remainingCredits`, `expirationDate`, `currentYearEligibility`. VSNext provides royalty credits for new members meeting participation requirements. Used to display credit balances and program status.                                                       |
| **GET_VSNEXT_SURVEY**  | GET    | `{DD_SURVEY_API}/{userId}/surveys/{surveyId}/{officeId}/questions`          | Retrieves VSNext program survey questions for data collection. Returns: `surveyId`, `surveyTitle`, `questions[]` array with: `questionId`, `questionText`, `questionType`, `options[]`, `required`, `validationRules`, `previousResponse`. Survey collects program participation metrics and member feedback. Used to render dynamic survey form.                                                                                          |
| **POST_VSNEXT_FORM**   | POST   | `{DD_SURVEY_API}/user-response/submit/true`                                 | Submits VSNext program survey responses. Payload: `{userId, surveyId, officeId, responses[]}` where responses contains: `questionId`, `answer`, `timestamp`. The `true` parameter indicates final submission (not draft). Validates: all required questions answered, response formats correct. Returns: `submissionId`, `submissionDate`, `programEligibilityUpdated`, `confirmationMessage`. Triggers program eligibility recalculation. |

### VSMyOfficesDashboard Application Interaction Patterns

#### Module Initialization & Office Selection Flow

When a user accesses the My Offices Dashboard, the system executes the following
initialization sequence:

```plaintext
Page Load (Main.ascx)
    └─→ Document Ready Event
        └─→ COMMON = common() (Initialize global configuration)
        ├─→ Parse URL path parameters:
        │   ├─→ Extract office ID from URL (/My-Offices-New/{officeId})
        │   ├─→ Extract tab parameter (/tab/{tabName})
        │   ├─→ Extract additional context (card, expand, year, etc.)
        │   └─→ Store in COMMON.QUERY_PARAMS
        ├─→ Evaluate user permissions:
        │   ├─→ Check LOGGED_USER.IS_SUPERUSER
        │   ├─→ Check LOGGED_USER.IS_DNN_ADMINISTRATOR
        │   ├─→ Check LOGGED_USER.IS_CORPORATE
        │   └─→ Determine initial staffId for office query
        ├─→ loadOfficeSelector()
        │   └─→ GET_ALL_OFFICES
        │       └─→ AJAX call to /myofficesdashboard/api/search/offices
        │           └─→ Success Response
        │               ├─→ If COMMON.OFFICEID exists (URL parameter):
        │               │   ├─→ Validate user has access to specified office
        │               │   ├─→ If valid: preSelectOffice(COMMON.OFFICEID)
        │               │   └─→ If invalid: show access denied message
        │               ├─→ Else if single office returned:
        │               │   └─→ autoSelectOffice(offices[0].officeId)
        │               ├─→ Else (multiple offices):
        │               │   ├─→ Populate office dropdown selector
        │               │   ├─→ Enable doctor search (if applicable)
        │               │   └─→ Show "Select an office to continue" prompt
        │               └─→ If no offices returned:
        │                   └─→ Show "No accessible offices" message
        └─→ initializeDashboard(selectedOfficeId)
            ├─→ Store selectedOffice in session state
            ├─→ updateURLWithOfficeId(officeId)
            ├─→ loadTabNavigation()
            │   ├─→ Check user permissions per tab:
            │   │   ├─→ Summary (all users)
            │   │   ├─→ Account History (owners, financial reporting)
            │   │   ├─→ Manage Personnel (owners, personnel managers)
            │   │   ├─→ Business of Optometry (BOO subscribers)
            │   │   ├─→ Office Information (owners, office managers)
            │   │   ├─→ Communication (all users)
            │   │   └─→ VSNext (eligible participants)
            │   ├─→ Show/hide tabs based on permissions
            │   └─→ Set active tab from COMMON.QUERY_PARAMS.PATH_TAB
            ├─→ loadActiveTab()
            │   └─→ Trigger tab-specific initialization function
            └─→ hideLoadingSpinner()
```

#### Doctor/Office Search & Switching Flow

Multi-office administrators can search for and switch between different doctors'
offices:

```plaintext
User Types in Doctor Search Box
    └─→ keyup Event (debounced 300ms)
        └─→ doctorSearch(searchTerm)
            ├─→ If searchTerm.length >= 3:
            │   └─→ SEARCH_DOCTOR
            │       └─→ GET /myofficesdashboard/api/search/owners
            │           └─→ Query parameter: ?staff={current user}&query={term}
            │           └─→ Success Response
            │               ├─→ Render autocomplete dropdown
            │               ├─→ Show matching doctors:
            │               │   ├─→ Display: name, email, office count
            │               │   └─→ Sort by: name ascending
            │               └─→ Attach click handlers to results
            └─→ Else if searchTerm.length < 3:
                └─→ Hide autocomplete dropdown

User Clicks Doctor in Search Results
    └─→ selectDoctor(selectedStaffId)
        ├─→ Update session state with selectedStaffId
        ├─→ showLoadingSpinner("Loading offices...")
        ├─→ GET_ALL_OFFICES_BY_SELECTED_DOCTOR
        │   └─→ GET /myofficesdashboard/api/search/offices
        │       └─→ Query parameter: ?staff={selectedStaffId}
        │       └─→ Success Response
        │           ├─→ Clear current office selector
        │           ├─→ Populate with selected doctor's offices
        │           ├─→ If single office:
        │           │   └─→ autoSelectOffice(offices[0].officeId)
        │           └─→ Else:
        │               ├─→ Enable office dropdown
        │               └─→ Prompt user to select office
        └─→ updateBreadcrumb(selectedDoctor.name)
        └─→ hideLoadingSpinner()

User Changes Office in Dropdown
    └─→ officeChange Event
        ├─→ Get selected officeId from dropdown
        ├─→ Validate office selection
        ├─→ updateURLWithOfficeId(officeId)
        ├─→ clearAllTabData()
        ├─→ initializeDashboard(officeId)
        └─→ Reload active tab with new office context
```

#### Summary Tab Initialization & Data Loading Flow

When the Summary tab becomes active, it loads comprehensive performance metrics:

```plaintext
Summary Tab Activated
    └─→ initSummaryTab(officeId)
        ├─→ showLoadingSpinner("Loading dashboard...")
        ├─→ Parallel API calls (Promise.all):
        │   ├─→ GET_PP_PARTICIPATION_PERCENTAGE
        │   ├─→ GET_PP_GROWTH
        │   ├─→ GET_PP_REBATES
        │   ├─→ GET_PP_MEETING_ATTENDANCE
        │   ├─→ GET_SUMMARY_PP
        │   ├─→ GET_FD_SALES
        │   ├─→ GET_TOD_INFO
        │   ├─→ GET_TOD_SALES
        │   ├─→ GET_GEOSPATIAL_REPORT
        │   ├─→ GET_VSLEARNING_YTD_LESSONS
        │   ├─→ GET_VSLEARNING_IN_PERSON
        │   ├─→ GET_12MG_REPORTED_SALES
        │   ├─→ GET_12MG_SALES_COMPARISON
        │   ├─→ GET_VENDOR_REBATES_YEARS
        │   ├─→ GET_VENDOR_REBATES_CHART_DATA
        │   ├─→ GET_MEETING_ATTENDANCE_PER_MONTH
        │   └─→ GET_MEETING_ATTENDANCE_GROUPED_BY_DOCTOR
        └─→ Promise Resolution
            ├─→ renderKPICards():
            │   ├─→ Program Participation Card
            │   │   ├─→ Display: participation percentage
            │   │   ├─→ Display: active programs count
            │   │   └─→ Display: trend indicator (↑/↓/→)
            │   ├─→ 12-Month Growth Card
            │   │   ├─→ Display: YoY growth percentage
            │   │   ├─→ Display: current vs. prior year comparison
            │   │   └─→ Render mini trend chart
            │   ├─→ Vendor Rebates Card
            │   │   ├─→ Display: total rebates earned
            │   │   ├─→ Display: YoY comparison
            │   │   └─→ Display: active vendor count
            │   └─→ Meeting Attendance Card
            │       ├─→ Display: attendance percentage
            │       ├─→ Display: meetings attended / offered
            │       └─→ Display: last meeting attended date
            ├─→ renderProgramParticipationSection():
            │   ├─→ Frame Dream Card:
            │   │   ├─→ Check enrollment status
            │   │   ├─→ If enrolled:
            │   │   │   ├─→ Display fashion week sales data
            │   │   │   ├─→ Render sales trend chart
            │   │   │   ├─→ Show "View Power BI Report" button
            │   │   │   └─→ Calculate achievement level
            │   │   └─→ Else: Show enrollment CTA
            │   ├─→ The Optical Dream Card:
            │   │   ├─→ Display program tier
            │   │   ├─→ Show YTD purchase compliance
            │   │   ├─→ Render vendor breakdown chart
            │   │   └─→ Display renewal date
            │   ├─→ VSLearning Card:
            │   │   ├─→ Render YTD lessons chart
            │   │   ├─→ Display: online lessons completed
            │   │   ├─→ Display: in-person hours completed
            │   │   ├─→ Show CE credits earned
            │   │   └─→ Show "View Power BI Report" button
            │   ├─→ Max Programs Cards (FocusCL, Luxottica):
            │   │   ├─→ Check program enrollment
            │   │   ├─→ Display enrollment status
            │   │   └─→ Show "View Power BI Report" buttons
            │   └─→ Geospatial Analysis Card:
            │       ├─→ Check report availability
            │       ├─→ If available:
            │       │   ├─→ Display report generated date
            │       │   └─→ Show "Download Report" button
            │       └─→ Else: Show "Not Available" message
            ├─→ render12MonthGrowthSection():
            │   ├─→ Render reported sales chart
            │   │   ├─→ Chart type: Line chart (12 months)
            │   │   ├─→ X-axis: Months
            │   │   ├─→ Y-axis: Gross sales ($)
            │   │   └─→ Show: pairs sold as secondary metric
            │   ├─→ Render year-over-year comparison
            │   │   ├─→ Chart type: Grouped bar chart
            │   │   ├─→ Compare: current year vs. prior year
            │   │   └─→ Calculate: growth percentage per month
            │   └─→ Display summary statistics:
            │       ├─→ Total reported sales
            │       ├─→ Total pairs sold
            │       ├─→ Average sale price
            │       └─→ Growth percentage
            ├─→ renderVendorRebatesSection():
            │   ├─→ Populate year selector dropdown
            │   ├─→ Render year-over-year trend chart
            │   │   ├─→ Chart type: Stacked area chart
            │   │   ├─→ Show: quarterly rebates by vendor
            │   │   └─→ Enable: vendor filter toggle
            │   ├─→ Display rebate summary table:
            │   │   ├─→ Columns: Vendor, Q1, Q2, Q3, Q4, Total
            │   │   ├─→ Sort options: Vendor name, Total amount
            │   │   └─→ Enable row click to filter chart
            │   └─→ Attach year change handler:
            │       └─→ GET_VENDOR_REBATES_BY_YEAR(selectedYear)
            └─→ renderMeetingAttendanceSection():
                ├─→ Render 24-month attendance chart
                │   ├─→ Chart type: Line chart with markers
                │   ├─→ X-axis: Months (rolling 24 months)
                │   ├─→ Y-axis: Attendance percentage
                │   └─→ Show: meetings attended vs. offered
                ├─→ If multiple doctors in office:
                │   ├─→ Render per-doctor attendance table
                │   ├─→ Columns: Doctor Name, Attended, %, Last Meeting
                │   └─→ Enable: sort by any column
                └─→ Display upcoming meetings section:
                    ├─→ List: next 3 scheduled meetings
                    └─→ Show: registration links (if applicable)

User Clicks "View Power BI Report" Button
    └─→ openPowerBIReport(reportType)
        ├─→ Determine report configuration:
        │   ├─→ If VSLearning: Use VS_LEARNING_REPORT config
        │   ├─→ If Frame Dream: Use PP_FRAME_DREAM_REPORT config
        │   ├─→ If Max/FocusCL: Use PP_MAX_REPORT config
        │   └─→ If Luxottica: Use PP_LUXOTTICA_REPORT config
        ├─→ Generate embed URL with parameters:
        │   ├─→ Include: officeId, staffId, date range
        │   └─→ Include: report filters based on context
        ├─→ Open Power BI embed in modal or new window
        ├─→ Load Power BI JavaScript SDK
        └─→ initializePowerBIEmbed()
            └─→ (Follow Power BI embed flow - see below)
```

#### Power BI Report Embed Flow - Summary Tab

When Power BI reports are embedded from the Summary tab:

```plaintext
openPowerBIReport(reportConfig)
    └─→ showLoadingOverlay("Loading report...")
    └─→ Construct embed request:
        ├─→ appId = COMMON.BI_REPORTS.APPID
        ├─→ groupId = COMMON.BI_REPORTS.GROUPID
        ├─→ reportKey = reportConfig.REPORT_KEY
        └─→ datasetId = reportConfig.DATASET_ID
    └─→ GET Power BI Embed Token
        └─→ POST BI_REPORTS.SERVICE_PATH/embed/report/
            {appId}/{groupId}/{reportKey}/{datasetId}
            └─→ Success Response:
                ├─→ Extract: accessToken, embedUrl, reportId, expiration
                ├─→ Create embed container in modal/window
                ├─→ Configure Power BI embed settings:
                │   ├─→ type: 'report'
                │   ├─→ tokenType: models.TokenType.Embed
                │   ├─→ accessToken: {received token}
                │   ├─→ embedUrl: {received URL}
                │   ├─→ id: {reportId}
                │   ├─→ settings:
                │   │   ├─→ filterPaneEnabled: true
                │   │   ├─→ navContentPaneEnabled: true
                │   │   ├─→ background: models.BackgroundType.Transparent
                │   │   └─→ layoutType: models.LayoutType.Custom
                │   └─→ filters: [
                │       ├─→ Office filter: {officeId}
                │       ├─→ Date range filter: {contextual}
                │       └─→ Additional contextual filters
                │     ]
                ├─→ Embed report: powerbi.embed(container, config)
                ├─→ Attach event listeners:
                │   ├─→ "loaded" event:
                │   │   ├─→ hideLoadingOverlay()
                │   │   ├─→ Show report container
                │   │   └─→ Log: "Report loaded successfully"
                │   ├─→ "rendered" event:
                │   │   └─→ Log: "Report rendered"
                │   ├─→ "error" event:
                │   │   ├─→ Log error details
                │   │   ├─→ showErrorMessage(error)
                │   │   └─→ Offer: retry or close options
                │   ├─→ "dataSelected" event:
                │   │   └─→ Log: selected data for debugging
                │   └─→ "pageChanged" event:
                │       └─→ Update: current page context
                └─→ setTokenExpirationListener()
                    └─→ Calculate refresh time (expiration - 5 minutes)
                    └─→ setTimeout(() => {
                        refreshPowerBIToken()
                        └─→ GET new embed token (same parameters)
                        └─→ report.setAccessToken(newToken)
                        └─→ setTokenExpirationListener() (recursive)
                      }, refreshTime)

User Closes Power BI Report
    └─→ closeReport Event
        ├─→ report.fullscreen.exit()
        ├─→ powerbi.reset(container)
        ├─→ Clear embed container
        ├─→ Close modal/window
        └─→ Return focus to summary tab
```

## VSReportCenter Module

### VSReportCenter Overview

The VSReportCenter module is a comprehensive Business Intelligence (BI)
reporting platform built on the DotNetNuke (DNN) platform that enables Vision
Source members to access, manage, and interact with a centralized library of
business reports. The module provides a unified interface for both Power BI
interactive dashboards and traditional BI reports (SQL Server Reporting
Services), featuring advanced search capabilities, personalized favorites
management, and flexible viewing modes (card/list views) with preview
functionality. The system integrates with Microsoft Power BI for embedded
interactive reports with automatic token refresh management, ensuring secure and
uninterrupted access to business intelligence data.

### VSReportCenter Location

```plaintext
insight-dnn
|__VisionSourceDNN
   |__DesktopModules
      |__VisionSource
          |__VSReportCenter
              |__App
                 |__Content
                    |__main.css
                 |__Img
                    |__404-RC.png
                    |__ReportCenter_BT.jpg
                 |__Scripts
                    |__main.js
              |__README.md
              |__Main.ascx (Main Module View)
              |__DetailView.html (Power BI Report Detail View)
```

### VSReportCenter API Endpoints Architecture

The module consumes data from two distinct API services:

```plaintext
┌─────────────────────────────────────────────────────────────┐
│                    API ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. REPORT CENTER API (Primary) - REST API                  │
│     Base: /reportcenter/api                                 │
│     Purpose: Report catalog management & user favorites     │
│                                                             │
│  2. POWER BI SERVICE API (Embedded Reports) - WCF Service   │
│     Base: /powerbi/service.svc                              │
│     Purpose: Power BI embed token generation & config       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Report Catalog Management Endpoints

- **Repository**: visionsource-webapi
- **Source**: Report Center API (`/reportcenter/api`)
- **Database**: Visionsource
- **DB Tables**: ReportCenter_Report, ReportCenter_Report_User_Favorites
- **DB Procedures**: sp_ReportCenter_GetActiveReportsForUser,
  sp_ReportCenter_AddFavoriteReportForUser,
  sp_ReportCenter_RemoveFavoriteReportForUser

| Endpoint           | Method | URL Pattern                  | Purpose                                                                                                                                                                                                                                                                                                                             |
| ------------------ | ------ | ---------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GETREPORTS**     | GET    | `/reportcenter/api/{userId}` | Retrieves complete catalog of available reports for the specified user. Returns array of report objects containing: `id`, `name`, `description`, `type` (Power BI/BI Report), `reportKey`, `datasetID`, `reportPath`, `imagePath`, `tags`, and `isUserFavorite` boolean. Response is filtered based on user's role and permissions. |
| **SAVEFAVORITE**   | POST   | `/reportcenter/api/Favorite` | Adds a report to the user's favorites list. Payload includes `ReportId` and `UserId`. Creates new record in user favorites table. Returns success/failure status. This action is idempotent - adding an existing favorite has no effect.                                                                                            |
| **REMOVEFAVORITE** | DELETE | `/reportcenter/api/Favorite` | Removes a report from the user's favorites list. Payload includes `ReportId` and `UserId`. Deletes record from user favorites table. Returns success/failure status. This action is idempotent - removing a non-existent favorite has no effect.                                                                                    |

#### Power BI Embed Service Endpoints

- **Repository**: visionsource-webapi
- **Source**: Power BI Service (`/powerbi/service.svc`)
- **Service Type**: WCF Service (Windows Communication Foundation)
- **Authentication**: Azure Active Directory (Username/Password Credential Flow)

> **📝 Architecture Note**: This WCF service acts as an authentication proxy
> between VSReportCenter and Microsoft's Power BI REST API. It handles OAuth
> token acquisition using Azure AD credentials and generates embed tokens for
> secure report access. The service uses the Microsoft Power BI .NET SDK
> (`Microsoft.PowerBI.Api.V2`) to interact with Power BI workspaces and reports.

| Endpoint          | Method | URL Pattern                                                                   | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ----------------- | ------ | ----------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GETEMBEDTOKEN** | POST   | `/powerbi/service.svc/embed/report/{appId}/{groupId}/{reportKey}/{datasetId}` | Authenticates with Azure AD using service account credentials, then generates a time-limited embed token with "View" permissions for the specified Power BI report. The service uses `UserPasswordCredential` flow with credentials stored in web.config (`PowerBI.User` and `PowerBI.Password`). Parameters: `appId` (Power BI app registration ID: `09f5c047-4d81-4776-b10c-ca064fe34e98`), `groupId` (Power BI workspace ID: `3a4a66f0-efc2-45a9-8c9e-27b065bfb42b`), `reportKey` (unique report identifier from Power BI workspace), `datasetId` (Power BI dataset identifier for token generation). Returns `EmbedReportToken` object containing: `AccessToken` (JWT bearer token), `EmbedUrl` (iframe URL for embedding), `ReportID` (Power BI report GUID), and `Expiration` (Unix timestamp in milliseconds, default 1 hour from generation). Token must be refreshed before expiration to maintain uninterrupted access. |

### VSReportCenter API Interaction Patterns

#### Initialization Flow - Main Report Center

When a user lands on the Report Center main page, the system executes the
following sequence:

```plaintext
Page Load (Main.ascx)
    └─→ Document Ready Event
        └─→ showSpinner(true)
        └─→ initElem() (Initialize UI elements & event handlers)
        └─→ getInitialReports()
            └─→ GETREPORTS (Load all available reports for user)
                └─→ Success Response
                    ├─→ Filter reports with isUserFavorite = true
                    ├─→ If favorites exist:
                    │   ├─→ Set "Show Favorites" toggle to ON
                    │   └─→ renderReports(favoriteReports)
                    └─→ Else:
                        └─→ renderReports(allReports)
                            ├─→ Generate HTML from templates
                            ├─→ Populate card view container
                            ├─→ Populate list view container
                            ├─→ Update search results count
                            └─→ showSpinner(false)
```

#### Search & Filter Flow

Real-time search and filtering operations:

```plaintext
User Types in Search Box
    └─→ keyup Event (Enter key ignored)
        └─→ searchReport(elem)
            ├─→ Get search term (lowercase)
            ├─→ Get current sort options (Name/Type/Favorites)
            ├─→ showSpinner(true)
            ├─→ Filter allReports array:
            │   ├─→ Match against: name, description, tags, type
            │   └─→ Store results in sortedList
            ├─→ Apply "Show Favorites" filter (if active)
            ├─→ Apply active sort options
            ├─→ renderReports(sortedList)
            └─→ Update search results count
```

#### Favorite Toggle Flow

User marking/unmarking reports as favorites:

```plaintext
User Clicks Favorite Heart Icon
    └─→ toggleFavorite(reportId)
        ├─→ Detect current state (solid/regular heart)
        ├─→ Toggle UI icon state immediately:
        │   ├─→ fa-regular fa-heart gray → fa-solid fa-heart primary
        │   └─→ fa-solid fa-heart primary → fa-regular fa-heart gray
        ├─→ Update allReports array (isUserFavorite property)
        ├─→ Update sortedList array (isUserFavorite property)
        └─→ saveFavorite(reportId, action)
            ├─→ If adding: POST /reportcenter/api/Favorite
            └─→ If removing: DELETE /reportcenter/api/Favorite
                └─→ Payload: { ReportId, UserId }
```

#### Report Opening Flow

When user opens a report (handles both Power BI and traditional reports):

```plaintext
User Clicks "Open Report"
    └─→ openReport(reportId)
        ├─→ Find report in allReports array
        ├─→ Determine report type:
        │   ├─→ If "Power BI":
        │   │   ├─→ Set powerBiObject.reportKey
        │   │   ├─→ Set powerBiObject.datasetId
        │   │   └─→ Target URL: "/Leaders/Member-Support-Center/Report-Center/Report"
        │   └─→ If "BI Report":
        │       └─→ Target URL: report.reportPath (direct link)
        └─→ Open in new window:
            ├─→ Safari browser detection:
            │   ├─→ If Safari: window.open() then set location
            │   │   (Workaround for Safari popup blocker)
            └─→ Else: window.open(urlToOpen)
```

#### Power BI Embed Flow - Detail View

When a Power BI report opens in DetailView.html:

```plaintext
DetailView.html Page Load
    └─→ Document Ready
        ├─→ Check if window.opener exists
        │   ├─→ If NO window.opener:
        │   │   └─→ showNoDashboard()
        │   │       ├─→ Hide #ReportContainer
        │   │       └─→ Show 404 error message with llama image
        │   └─→ If window.opener EXISTS:
        │       └─→ Get powerBiObject from parent window:
        │           ├─→ reportKey = window.opener.powerBiObject.reportKey
        │           ├─→ datasetId = window.opener.powerBiObject.datasetId
        │           └─→ initDashboard()
        │               └─→ GETEMBEDTOKEN
        │                   └─→ POST /powerbi/service.svc/embed/report/
        │                       {appId}/{groupId}/{reportKey}/{datasetId}
        │                       └─→ Success Response:
        │                           ├─→ Extract: AccessToken, EmbedUrl, ReportID, Expiration
        │                           ├─→ Configure Power BI embed settings:
        │                           │   ├─→ type: 'report'
        │                           │   ├─→ tokenType: models.TokenType.Embed
        │                           │   ├─→ pageView: 'fitToWidth'
        │                           │   ├─→ viewMode: models.ViewMode.View
        │                           │   └─→ settings: {filterPaneEnabled: false,
        │                           │                  navContentPaneEnabled: true}
        │                           ├─→ Embed report: powerbi.embed(container, config)
        │                           ├─→ Attach event listeners:
        │                           │   ├─→ "loaded" event: Remove loading shade
        │                           │   └─→ "error" event: Log error to console
        │                           └─→ setTokenExpirationListener()
        │                               └─→ Calculate timeout (expiration - 2 minutes)
        │                               └─→ Schedule automatic token refresh:
        │                                   └─→ updateToken() before expiration
        │                                       └─→ GETEMBEDTOKEN (refresh)
        │                                       └─→ report.setAccessToken(newToken)
        │                                       └─→ setTokenExpirationListener() (recursive)
```

---

## VSPayment Module

### VSPayment Overview

The VSPayment module is a comprehensive online payment processing system built
on the DotNetNuke (DNN) platform that enables Vision Source and SmileSource
organizations to accept secure electronic payments for multiple business
purposes. The module provides a flexible, configurable payment interface that
supports four distinct payment scenarios: charitable donations to Vision Source
Foundation, charitable donations to SmileSource Foundation, royalty payments for
member offices, and Marketing Co-Op program payments. Built with PCI compliance
in mind, the system integrates with CyberSource payment gateway using
tokenization technology (Flex Microform) to securely process credit card and
electronic check (ACH) transactions without storing sensitive payment data on
Vision Source servers.

### VSPayment Location

```plaintext
dnn-modules
|__modules
   |__VSPayment
      |__VSPayment.DNN
          |__App
             |__Content (CSS stylesheets)
             |__Pages (HTML templates)
             |__Scripts (JavaScript payment logic)
             |__Types (C# data models)
             |__Tooltipster (UI tooltip library)
          |__Components
             |__FeatureController.cs
          |__Providers
             |__DataProviders
          |__View.ascx (Main module view)
          |__Settings.ascx (Module configuration)
          |__MarketingCoOpPaymentCenter.ascx (Co-Op payment view)
          |__MarketingCoOpThankYou.ascx (Co-Op thank you page)
          |__VSFoundation.htm (Foundation donation template)
          |__SSFoundation.htm (SmileSource donation template)
          |__VSPayment.DNN.dnn (DNN manifest)
```

### VSPayment Module Configuration System

The module uses DNN's **TabModuleSettings** system to enable multiple payment
scenarios from a single module installation. Administrators configure each
module instance via the Settings interface.

#### Settings Configuration Flow

```plaintext
Administrator Workflow:
┌─────────────────────────────────────────────────────────────┐
│ 1. Admin places VSPayment.DNN module on a page              │
│ 2. Admin clicks module settings (gear icon)                 │
│ 3. Settings.ascx loads with two dropdown controls:          │
│    ┌──────────────────────────────────────────────────-┐    │
│    │ Control Type:                                     │    │
│    │   [ VSFoundation ▼ ]                              │    │
│    │   Options: VSFoundation, SSFoundation, VSPayment  │    │
│    └──────────────────────────────────────────────────-┘    │
│    ┌──────────────────────────────────────────────────--┐   │
│    │ View Type: (Dynamically populates based on Control)|   │
│    │   [ Donate ▼ ]                                     │   │
│    │   Options vary by Control Type                     │   │
│    └──────────────────────────────────────────────────--┘   │
│ 4. Admin saves settings                                     │
│ 5. View.ascx.cs reads settings to determine behavior        │
└─────────────────────────────────────────────────────────────┘
```

#### Settings Configuration Matrix

| Control Type        | Available View Types                    | Purpose                                                             | HTML Template                   |
| ------------------- | --------------------------------------- | ------------------------------------------------------------------- | ------------------------------- |
| **VSFoundation**    | • Donate<br>• Thank You                 | Vision Source Foundation charitable donation processing             | VSFoundation.htm                |
| **SSFoundation**    | • Donate<br>• Thank You                 | SmileSource Foundation charitable donation processing               | SSFoundation.htm                |
| **VSPayment**       | • Payment Center<br>• Payment Thank You | Member office royalty payment processing with saved payment methods | PaymentCenter.html              |
| **Marketing Co-Op** | • (Configured differently)              | Marketing Co-Op program payment processing                          | MarketingCoOpPaymentCenter.html |

#### Settings Implementation (Settings.ascx.cs)

```csharp
protected void ddlControl_SelectedIndexChanged(object sender, EventArgs e)
{
    this.ddlView.Items.Clear();

    if (this.ddlControl.SelectedValue == "VSFoundation" ||
        this.ddlControl.SelectedValue == "SSFoundation")
    {
        this.ddlView.Items.Add("Donate");
        this.ddlView.Items.Add("Thank You");
    }
    else // VSPayment
    {
        this.ddlView.Items.Add("Payment Center");
        this.ddlView.Items.Add("Payment Thank You");
    }
}

public override void UpdateSettings()
{
    var modules = new ModuleController();

    // Save as TabModuleSettings (page-specific configuration)
    modules.UpdateTabModuleSetting(TabModuleId, "Control", this.ddlControl.SelectedValue);
    modules.UpdateTabModuleSetting(TabModuleId, "ViewType", this.ddlView.SelectedValue);
}
```

#### Runtime Configuration Reading (View.ascx.cs)

```csharp
public bool IsDonatePage
{
    get
    {
        if (Settings.Contains("ViewType"))
        {
            string viewType = Settings["ViewType"].ToString();
            return viewType == "Donate";
        }
        return false;
    }
}

public bool IsPaymentCenterPage
{
    get
    {
        if (Settings.Contains("ViewType"))
        {
            string viewType = Settings["ViewType"].ToString();
            return viewType == "Payment Center";
        }
        return false;
    }
}
```

**Configuration Benefits:**

- **Single Codebase**: One module handles four different payment scenarios
- **Flexible Deployment**: Same module can be configured differently on
  different pages
- **Maintenance Efficiency**: Updates to payment logic apply across all
  configurations
- **URL Routing**: Different pages (e.g., `/Donate`, `/Payment-Center`) use same
  module with different settings

### VSPayment API Endpoints Architecture

The module integrates with two distinct payment API systems, each serving
different payment scenarios:

```plaintext
┌─────────────────────────────────────────────────────────────┐
│                    API ARCHITECTURE                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. PAYMENT SERVICE API (Primary) - WCF Service                |
│     Base: /Payment/Service.svc/[URI]                        │
│     Purpose: Process all payment transactions               │
│              via CyberSource payment gateway                │
│                                                             │
│  2. MEMBER DASHBOARD SERVICE API (Supporting) - WCF Service │
│     Base: /memberdashboard/service.svc                      │
│     Purpose: Retrieve office information, balances,         │
│              saved payment methods, pending transactions    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Payment Transaction Endpoints

- **Repository**: dnn-modules
- **Source**: Payment Service (`/Payment/Service.svc/[URI]`)
- **Payment Gateway**: CyberSource (Visa)
- **Security**: TLS 1.2, Tokenization via Flex Microform
- **Database**: VSSqlServerCRM
- **DB Tables**: OnlineTransactions, Company, vwFiscalPeriodStatus, Sales,
  RoyaltyPayment, ScheduledRoyaltyPayment, PropertiesHistory
- **DB Procedures**: spSaveTransaction, spAddPaymentRecord,
  spSaveScheduledPayment, spAddPaymentRecord, spAddPropertiesHistory

> **🔒 PCI Compliance Note**: The module uses CyberSource Flex Microform, a
> JavaScript library that creates secure iframes for credit card input fields.
> Card numbers never touch Vision Source servers - they go directly to
> CyberSource, which returns a temporary token. Only this token (valid for 15
> minutes) is sent to the payment API, ensuring PCI DSS compliance without the
> need for Vision Source to maintain PCI certification for card data storage.

| Endpoint                   | Method | URL Pattern                                    | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Request Model            | Response Model          |
| -------------------------- | ------ | ---------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------ | ----------------------- |
| **GENERATE_DONATE_KEY**    | GET    | `/Donate/generate-Key/{key}`                   | Generates CyberSource Flex Microform JSON Web Key (JWK) for donation pages. Called on page load before payment form renders. Uses RsaOaep256 encryption type. Returns JWK object containing public key components (e, n, kty, kid) used by Flex Microform JavaScript to encrypt card data client-side before transmission to CyberSource. Key scoped to `cybs.foundation.merchantID` configuration. TargetOrigin set from `appSettings["targetOrigin"]` for CORS validation.                                                                                                                                                                                                                                                                    | None                     | `string` (JWK JSON)     |
| **DONATE_TOKEN**           | POST   | `/Donate/token/{key}`                          | Processes one-time charitable donations using payment token from Flex Microform. Accepts payment token (15-minute validity), donation amount, donor information (name, email, address), and organization ID ("VisionSource" or "SmileSource"). Normalizes Discovery card type code (042→004). Creates immediate transaction via CyberSource SOAP API using `DonationPaymentHandler`. Records transaction in VSBackOffice database. Triggers SendGrid email receipt using organization-specific template. Returns receipt object with transaction ID, authorization code, masked account number (last 4 digits), transaction date, and confirmation reference ID.                                                                                | `DonationRequest`        | `DonationResponse`      |
| **DONATE_PAYMENT**         | POST   | `/Donate/Payment/{key}`                        | Legacy endpoint for direct credit card processing (bypasses tokenization). Accepts full payment credentials including card number. **Security Risk**: Deprecated in favor of token-based flow. Sets empty string for null `orgId`. Uses `DonationPaymentHandler` to process through CyberSource. Sends same email receipt as token endpoint. Kept for backward compatibility with older donation pages.                                                                                                                                                                                                                                                                                                                                         | `DonationRequest`        | `DonationResponse`      |
| **GENERATE_ROYALTY_KEY**   | GET    | `/Payment/generate-Key/{key}`                  | Generates CyberSource Flex Microform JWK for Payment Center pages. Identical to donate key generation but scoped to `cybs.royalty.merchantID` configuration. Returns JWK with RsaOaep256 encryption for royalty payment context. Used by Payment Center and Co-Op payment forms.                                                                                                                                                                                                                                                                                                                                                                                                                                                                | None                     | `string` (JWK JSON)     |
| **ROYALTY_PAYMENT**        | POST   | `/Payment/Royalty/{key}`                       | Processes member office royalty payments using temporary payment token from Flex Microform. Accepts office ID, organization ID, payment amount, CyberSource token, billing information (name, address, city, state, zip), payment type, and optional scheduled payment date. If `ScheduledDate` provided, creates future-dated payment record without immediate charge (status: "Scheduled"). For immediate payments, processes transaction through CyberSource and records in `VSBackOffice.dbo.Royalty_Pending` table with transaction details (authorization code, request ID, merchant reference number). No email sent (relies on Member Dashboard notification).                                                                          | `RoyaltyRequest`         | `RoyaltyResponse`       |
| **ROYALTY_PAYMENT_METHOD** | POST   | `/Payment/Method/Royalty/{key}`                | Processes royalty payment using saved payment method identified by subscription ID. Accepts subscription ID (10-13 for royalty methods), office ID, payment amount, and optional scheduled date. Retrieves encrypted payment details from `VSBackOffice.dbo.Subscription_PaymentMethods` table (credit card or ACH routing/account numbers). Decrypts credentials using secure key from `appSettings["CryptographyKey"]`. Submits to CyberSource with merchant-defined data fields for reconciliation (office ID, organization ID, payment type). Supports both immediate processing and scheduled payments (creates pending record).                                                                                                           | `PaymentMethodRequest`   | `RoyaltyResponse`       |
| **SAVE_PAYMENT_METHOD**    | POST   | `/Payment/Method/{key}`                        | Saves new payment method for future use. Accepts payment type ("MASTERCARD", "C" for checking, "S" for savings), office ID, cardholder/account holder information, billing address, and encrypted payment credentials. Determines next available method ID from range [10, 11, 12, 13] by checking existing methods. Stores encrypted data in `Subscription_PaymentMethods` table with subscription ID, office ID, organization ID, payment type, cardholder name, last 4 digits (masked), expiration date (cards only), and encrypted full credentials. Maximum 4 saved methods per office enforced. Returns success/failure status with assigned method ID. Uses default `RoyaltyPaymentHandler` for database operations.                     | `RoyaltyRequest`         | `PaymentMethodResponse` |
| **GET_PAYMENT_METHOD**     | GET    | `/Payment/Method/{subscriptionId}/{key}`       | Retrieves details of saved payment method without exposing sensitive data. Queries `Subscription_PaymentMethods` by subscription ID. Returns masked account information (last 4 digits only), payment type ("MASTERCARD", "Checking", "Savings"), cardholder/account holder name, billing address (street, city, state, zip), expiration date (credit cards only), and method ID. Does not decrypt or return full card numbers or routing numbers. Used to populate confirmation screens ("Pay with VISA ending in 1234") and saved method selection interfaces in Payment Center.                                                                                                                                                              | None                     | `PaymentMethodInfo`     |
| **SCHEDULE_PAYMENT**       | POST   | `/Payment/Schedule/{key}`                      | Creates future-dated payment using saved payment method. Accepts subscription ID, scheduled date (must be within current billing period - validated server-side), payment amount, office ID, organization ID, and notification email. Validates scheduled date falls within allowed billing window (typically current month + grace period). Creates pending payment record with status "Scheduled" in `Royalty_Pending` table. Background job (Windows Service or SQL Agent job) processes scheduled payments on due date by calling `ROYALTY_PAYMENT_METHOD`. Email confirmation sent to user immediately upon scheduling. Scheduled payment appears in Member Dashboard "Pending Transactions" view with cancel option (if before due date). | `SchedulePaymentRequest` | `PaymentMethodResponse` |
| **COOP_PAYMENT**           | POST   | `/Co-Op/payment/{key}`                         | Processes Marketing Co-Op program payments using payment token from Flex Microform. Dedicated endpoint for Co-Op fee payments (separate accounting from royalty payments). Accepts office ID, Co-Op program identifier, payment amount, CyberSource token, and billing information. Uses `CoOpPaymentHandler` to route transaction to Co-Op financial system. Records transaction in Co-Op-specific database tables (separate from royalty tables) with program tracking metadata (Co-Op enrollment year, program type, fee category). Returns receipt object for Co-Op payment confirmation page (MarketingCoOpThankYou.ascx). Uses `cybs.marketingCo-op.merchantID` CyberSource account.                                                      | `RoyaltyRequest`         | `RoyaltyResponse`       |
| **COOP_PAYMENT_METHOD**    | POST   | `/Co-Op/payment/By-method/{key}`               | Processes Co-Op payment using saved payment method. Similar flow to `ROYALTY_PAYMENT_METHOD` but uses `CoOpPaymentHandler` to route transaction to Co-Op accounting system. Accepts subscription ID (36-39 for Co-Op methods), office ID, amount. Retrieves saved payment credentials from separate Co-Op payment methods storage. Processes through CyberSource using Co-Op merchant account. Records in Co-Op financial tables (`VSBackOffice.dbo.CoOp_Payments`). Maintains accounting separation from royalty payments for reconciliation purposes.                                                                                                                                                                                         | `PaymentMethodRequest`   | `RoyaltyResponse`       |
| **GET_COOP_METHOD**        | GET    | `/Co-op/payment/Method/{subscriptionId}/{key}` | Retrieves saved payment method details specific to Co-Op payments. Returns identical data structure as `GET_PAYMENT_METHOD` (masked account, payment type, cardholder, address, expiration) but filtered to Co-Op payment methods. Co-Op payment methods use different subscription ID range (36-39) from royalty methods (10-13) to maintain separation in database. Uses `CoOpPaymentHandler` to query Co-Op-specific payment method tables.                                                                                                                                                                                                                                                                                                  | None                     | `PaymentMethodInfo`     |
| **SAVE_COOP_METHOD**       | POST   | `/Co-Op/payment/method/{key}`                  | Saves payment method specifically for Co-Op payments. Separate storage from royalty payment methods to maintain accounting separation between royalty and Co-Op financial systems. Accepts same payment data structure as `SAVE_PAYMENT_METHOD` but assigns Co-Op-specific subscription IDs from range [36, 37, 38, 39]. Maximum 4 saved Co-Op payment methods per office enforced. Stores in Co-Op payment methods table with Co-Op program association. Uses `CoOpPaymentHandler` for database operations. Returns success/failure with assigned Co-Op method ID.                                                                                                                                                                             | `RoyaltyRequest`         | `PaymentMethodResponse` |

#### Member Dashboard Service Endpoints

- **Repository**: dnn-modules
- **Source**: Member Dashboard Service (`/memberdashboard/service.svc`)
- **Database**: VSSqlServerCRM, VisionSource
- **DB Tables**: vSummaryCompany, PropertiesHistory, TempBalanceDue,
  vPendingPayments, ScheduledRoyaltyPayment
- **DB Procedures**: spGetOfficeDetailInfo, spDeletePaymentMethod,
  spGetBalanceDue, p_GetPaymentHistoryPendingPayments

| Endpoint                     | Method | URL Pattern                                  | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| ---------------------------- | ------ | -------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **GET_OFFICE_INFO**          | GET    | `/{orgId}/offices/{officeId}`                | Retrieves basic office information for display in payment interface header. Returns office name, address, primary contact information, and active status. Used to populate "Paying for: [Office Name]" header in Payment Center. Validates office exists and belongs to specified organization (Vision Source vs SmileSource).                                                                                                                              |
| **GET_PAYMENT_METHODS**      | GET    | `/{orgId}/offices/{officeId}/paymentmethods` | Retrieves list of all saved payment methods for the specified office. Returns array of payment method objects containing: subscription ID, masked account number (last 4 digits), payment type (MASTERCARD/Checking/Savings), cardholder name, expiration date, and method ID. Used to populate "Saved Payment Methods" section in Payment Center. Response filtered to show only active, non-expired methods.                                              |
| **GET_COOP_METHODS**         | GET    | `/offices/{officeId}/CoOpPaymentMethods`     | Retrieves saved payment methods specific to Marketing Co-Op payments. Separate from royalty payment methods to maintain accounting separation. Returns same data structure as GET_PAYMENT_METHODS but filtered to Co-Op subscription IDs (36-39).                                                                                                                                                                                                           |
| **DELETE_PAYMENT_METHOD**    | DELETE | `/payment/methods/{methodId}`                | Soft-deletes a saved payment method by setting inactive flag. Accepts method ID from saved methods list. Validates user has permission to delete method (must be associated with office). Method remains in database for audit trail but no longer appears in saved methods list. Cannot delete method if scheduled payments reference it - user must cancel scheduled payments first.                                                                      |
| **GET_BALANCE**              | GET    | `/{orgId}/offices/{officeId}/balance`        | Calculates total outstanding balance due for office. Aggregates unreported sales, previous period balances, adjustments, credits, and pending payments. Returns single numeric value representing amount owed. Used for "Pay in Full" option - fills payment amount with calculated balance. Calculation includes late fees if applicable based on due dates.                                                                                               |
| **GET_COOP_BALANCE**         | GET    | `/offices/{officeId}/CoOpBalance`            | Calculates Marketing Co-Op program balance for office. Separate from royalty balance calculation. Returns amount owed for Co-Op program fees based on Co-Op enrollment and program participation. Used in Co-Op Payment Center to display balance due.                                                                                                                                                                                                      |
| **GET_PENDING_TRANSACTIONS** | GET    | `/{orgId}/offices/{officeId}/pending`        | Retrieves detailed breakdown of pending transactions affecting balance. Returns arrays of: unreported sales (by year/month with royalty amounts), adjustments (manual corrections by staff), credits (promotional credits or refunds), and pending payments (scheduled or in-process transactions). Used to populate tooltip showing "what makes up my balance" with expandable detail sections. Includes oldest due date for late fee calculation display. |

### VSPayment API Interaction Patterns

#### Initialization Flow - Donation Page

When a user lands on a Foundation donation page:

```plaintext
Page Load (View.ascx - Donate Configuration)
    └─→ Page_Load executes (IsPostBack = false)
        └─→ CheckAccessPermissions() (Skipped for donation pages)
        └─→ Determine Control Type from Settings
            └─→ If "VSFoundation" or "SSFoundation":
                ├─→ GENERATE_KEY (GET /Donate/generate-Key)
                │   └─→ Returns JWK for Flex Microform initialization
                └─→ Load HTML template (VSFoundation.htm or SSFoundation.htm)
                    └─→ JavaScript drawPaymentUI() executes
                        ├─→ Initialize Flex Microform with JWK
                        ├─→ Create secure iframe for card number input
                        ├─→ Attach form validation handlers
                        └─→ Display donation form
```

#### Payment Submission Flow - Donation

User completing a donation form:

```plaintext
User Fills Form & Clicks "Donate"
    └─→ Client-side validation executes (validate() function)
        ├─→ Verify amount > 0
        ├─→ Verify required fields (name, email, address)
        └─→ Validation passes
            └─→ Call Flex Microform createToken()
                ├─→ Card data sent directly to CyberSource (never hits VS servers)
                └─→ CyberSource returns transient token (15min validity)
                    └─→ makeDonation(transientToken) executes
                        ├─→ Save payment data to cookie (for error recovery)
                        ├─→ Show loading overlay
                        └─→ __doPostBack('SubmitPayment', paymentData)
                            └─→ SERVER SIDE (View.ascx.cs)
                                └─→ Page_Load (IsPostBack = true)
                                    └─→ Request["__EVENTTARGET"] == "SubmitPayment"
                                        └─→ MakePOSTRequest()
                                            ├─→ POST /Donate/token
                                            └─→ ResponseHandler callback
                                                └─→ Parse receipt JSON
                                                └─→ Save receipt to Session
                                                └─→ Redirect to Thank You page
```

#### Initialization Flow - Payment Center

When a user lands on the Payment Center page:

```plaintext
Page Load (View.ascx - Payment Center Configuration)
    └─→ Page_Load executes (IsPostBack = false)
        └─→ CheckAccessPermissions()
            ├─→ Verify user has role (Members/Financial Reporting/Admin)
            ├─→ Extract staffId from user profile
            ├─→ Extract officeId from query string (?oid=123)
            ├─→ GET_OFFICE_INFO (Verify office association)
            └─→ Access granted OR redirect to NoAccess.html
        └─→ GENERATE_KEY (GET /Payment/generate-Key)
        └─→ Load HTML template (PaymentCenter.html)
            └─→ JavaScript openPaymentCenter() executes
                └─→ loadAllData() - Parallel API calls:
                    ├─→ GET_OFFICE_INFO (Office details)
                    ├─→ GET_PAYMENT_METHODS (Saved methods)
                    ├─→ GET_PENDING_TRANSACTIONS (Balance breakdown)
                    └─→ GET_BALANCE (Total amount due)
                        └─→ All responses received
                            ├─→ Display office name header
                            ├─→ Display total balance due
                            ├─→ Populate saved methods list (radio buttons)
                            ├─→ Show pending sales tooltip
                            ├─→ Initialize date picker (if no scheduled payment exists)
                            ├─→ Initialize Flex Microform
                            └─→ Hide loading spinner
```

#### Payment Submission Flow - Payment Center with Saved Method

User paying with a saved payment method:

```plaintext
User Selects Saved Method & Clicks "Pay Now"
    └─→ setPaymentValues(methodData) executes
        ├─→ Build payment payload:
        │   ├─→ OfficeId: from query string
        │   ├─→ SubscriptionId: from selected radio button
        │   ├─→ Amount: from balance or custom amount
        │   └─→ PaymentType: "MASTERCARD", "C" (checking), or "S" (savings)
        └─→ verifyAmount() validates amount
            └─→ showConfirm() displays confirmation modal
                └─→ User clicks "Confirm Payment"
                    └─→ Save payment data to cookie (error recovery)
                    └─→ __doPostBack('payWithMethod', paymentData)
                        └─→ SERVER SIDE (View.ascx.cs)
                            └─→ Page_Load (IsPostBack = true)
                                └─→ Request["__EVENTTARGET"] == "payWithMethod"
                                    └─→ MakePOSTRequest()
                                        ├─→ POST /payment/method/royalty
                                        │   └─→ API retrieves saved method
                                        │   └─→ API decrypts payment credentials
                                        │   └─→ API processes through CyberSource
                                        └─→ ResponseHandler callback
                                            ├─→ Parse receipt JSON
                                            ├─→ Save receipt to Session
                                            └─→ Redirect to Thank You page
```

#### Payment Submission Flow - Payment Center with New Card

User paying with a new credit card (not saving method):

```plaintext
User Clicks "Add New Payment Method"
    └─→ showNewMethodFormulary() displays form
        └─→ User fills form & clicks "Pay Now"
            └─→ Client-side validation
                └─→ Call Flex Microform createToken()
                    └─→ CyberSource returns token
                        └─→ setPaymentValues(null) builds payload:
                            ├─→ Token from Flex Microform
                            ├─→ Amount, office ID, billing info
                            └─→ SaveMethod: false (not saving for future)
                        └─→ __doPostBack('submitPayment', paymentData)
                            └─→ SERVER SIDE
                                └─→ POST /payment/royalty
                                    └─→ ResponseHandler
                                        └─→ Redirect to Thank You page
```

#### Save Payment Method Flow

User opting to save a payment method for future use:

```plaintext
User Clicks "Save Payment Method"
    └─→ showNewMethodFormulary() displays form
        └─→ User fills form & checks "Save for future payments"
            └─→ User clicks "Save Method"
                └─→ Client-side validation
                    └─→ Call Flex Microform createToken()
                        └─→ setPaymentValues(null) builds payload:
                            ├─→ AllocatedMethodIds: [10, 11, 12] (existing methods)
                            └─→ Payment credentials
                        └─→ GetNewPaymentMethod() calculates next available ID
                            └─→ Possible IDs: [10, 11, 12, 13]
                            └─→ Subtract existing: [10, 11, 12]
                            └─→ Next available: 13
                        └─→ __doPostBack('saveMethod', paymentData)
                            └─→ SERVER SIDE
                                └─→ POST /payment/method
                                    └─→ SaveMethodHandler
                                        ├─→ Display success message
                                        ├─→ Reload page to refresh saved methods
                                        └─→ New method appears in list
```

#### Schedule Future Payment Flow

User scheduling a payment for a future date:

```plaintext
User Selects "Schedule Payment" & Picks Date
    └─→ Date picker restricted to current billing period
        └─→ User selects saved method & scheduled date
            └─→ setPaymentValues(methodData) with scheduleDate
                ├─→ ScheduledDate: "10/15/2025"
                ├─→ SubscriptionId: saved method ID
                └─→ Amount: payment amount
            └─→ __doPostBack('saveSchedulePayment', paymentData)
                └─→ SERVER SIDE
                    └─→ If Email missing in payload:
                        ├─→ GET /payment/method/{subscriptionId}
                        └─→ Extract email from saved method
                    └─→ POST /payment/schedule
                        └─→ ScheduleResponseHandler
                            ├─→ Parse receipt
                            ├─→ Build redirect URL to Member Dashboard
                            │   └─→ /tab/account/office/{officeId}/expand/pending
                            └─→ Redirect to show scheduled payment in pending list
```

#### Marketing Co-Op Payment Flow

User accessing Co-Op Payment Center via popup:

```plaintext
User Opens Co-Op Payment Modal (MarketingCoOpPaymentCenter.ascx)
    └─→ Page loads in DNN modal popup (iFrame)
        └─→ Extract officeId from query string (?oid=123)
        └─→ JavaScript parallel calls:
            ├─→ GET_OFFICE_INFO (Office name)
            ├─→ GET_COOP_METHODS (Saved Co-Op payment methods)
            └─→ GET_COOP_BALANCE (Co-Op balance due)
        └─→ Display Co-Op payment interface
            └─→ User selects method & clicks "Pay Now"
                └─→ If using saved method:
                    └─→ __doPostBack('existedMethod', paymentData)
                        └─→ POST /Co-Op/payment/By-method/
                └─→ If using new method:
                    └─→ __doPostBack('redirectThankYou', paymentData)
                        └─→ POST /Co-Op/payment
                └─→ ResponseHandler
                    └─→ Redirect to MarketingCoOpThankYou.ascx
```

#### Error Handling & Recovery Flow

When a payment fails or error occurs:

```plaintext
Payment Error Scenario
    └─→ SERVER SIDE
        ├─→ ProcessPaymentRequestException()
        │   ├─→ Log error to DNN Event Log
        │   ├─→ Send Slack notification (critical errors)
        │   ├─→ Set FailedMessage property
        │   └─→ Redirect back to payment page
        └─→ CLIENT SIDE (Page reloads)
            └─→ managePostBackError() detects failure
                ├─→ Read payment data from cookie
                ├─→ fillFormAfterFail(paymentData)
                │   └─→ Re-populate form fields (except sensitive card data)
                ├─→ Display error message banner
                └─→ User can correct and resubmit
```

## Additional Notes

### Content Delivery Network (CDN): Technical Brief

A **Content Delivery Network (CDN)** is a **globally distributed network of
servers** that caches static content (JavaScript, CSS, images) and delivers it
from the **geographically closest server** to the user.

#### How It Works in VSReportCenter

```plaintext
User in California requests api-service.js
    └─→ CDN Edge Server (Los Angeles) responds in 5ms

User in New York requests same file
    └─→ CDN Edge Server (New York) responds in 5ms

WITHOUT CDN: Both users hit Origin Server in Texas (40-45ms latency)
WITH CDN: Both users hit nearby edge servers (5ms latency - 90% faster)
```

**Implementation:**

```html
<script src='<%= ConfigurationManager.AppSettings["cdnUrl"] %>/api-service/api-service.js?cdv=1043.0223.2023'></script>
```

- `?cdv=1043.0223.2023` = Cache-busting version parameter
- Change version → Instant deployment of new JavaScript

#### Core Benefits

| Benefit         | Impact on VSReportCenter                                     |
| --------------- | ------------------------------------------------------------ |
| **Performance** | 90% faster page loads - critical for Power BI report loading |
| **Reliability** | 99.9% uptime - api-service.js always available               |
| **Cost**        | 95% less bandwidth on origin servers                         |
| **Deployment**  | Zero-downtime JavaScript updates via version parameter       |

### Power BI Embedded: Technical Brief

**Power BI Embedded** allows organizations to display Microsoft Power BI reports
**inside their own website** without redirecting users to PowerBI.com. Users
interact with full Power BI functionality (charts, filters, drill-downs) while
remaining on VisionSource.com.

#### Visual Concept

```plaintext
┌─────────────────────────────────────────┐
│  VisionSource.com/Report-Center         │
│  ┌───────────────────────────────────┐  │
│  │ DetailView.html                   │  │
│  │  ┌─────────────────────────────┐  │  │
│  │  │ Power BI Report (iframe)    │  │  │
│  │  │ • Lives on Microsoft cloud  │  │  │
│  │  │ • Displays in Vision Source │  │  │
│  │  │ • Full interactivity        │  │  │
│  │  └─────────────────────────────┘  │  │
│  └───────────────────────────────────┘  │
│  User never leaves VisionSource.com!    │
└─────────────────────────────────────────┘
```

#### Why Use Embedding vs. Alternatives?

| Approach              | Result                                                              | Decision              |
| --------------------- | ------------------------------------------------------------------- | --------------------- |
| **Power BI Embedded** | User stays on our site, full BI features, Microsoft handles updates | ✅ **CHOSEN**         |
| Link to PowerBI.com   | User leaves site, loses navigation context                          | ❌ Poor UX            |
| Build custom BI       | 6-12 months dev time, expensive to maintain                         | ❌ Not cost-effective |

#### Security: Time-Limited Tokens

**Problem:** Can't give users permanent Power BI passwords (security risk)

**Solution:** Generate temporary "embed tokens" (like concert tickets)

- Valid for **1 specific report only**
- Expires in **1 hour**
- Auto-refreshed in background (seamless UX)
- If stolen, attacker only has 1-hour access to 1 report

#### The Embedding Process

```plaintext
1. User clicks "Open Report"
   └─→ Opens DetailView.html in new window

2. DetailView requests embed token
   └─→ POST /powerbi/service.svc/embed/report/...

3. WCF Service authenticates with Azure AD
   └─→ Microsoft returns: AccessToken, EmbedUrl, ReportID, Expiration

4. JavaScript embeds report
   └─→ Creates <iframe> with token
   └─→ Loads Power BI report from Microsoft

5. Token Management (Background)
   └─→ System refreshes token 2 minutes before expiry
   └─→ User never interrupted
```

### ASP.NET ViewState & Postback Lifecycle

ASP.NET Web Forms uses a sophisticated page lifecycle to maintain state across
postbacks:

```plaintext
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET PAGE LIFECYCLE                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Client Browser                    Server (IIS/DNN)         │
│  ───────────────                   ──────────────           │
│                                                             │
│  1. User loads page                                         │
│     └─→ GET /Donate  ──────────────→  Page_Init             │
│                                       Page_Load             │
│                                       (IsPostBack = false)  │
│                                       Render HTML           │
│     ←────────────────────────────  Response with:           │
│                                       • HTML markup         │
│                                       • __VIEWSTATE         │
│                                       • __EVENTTARGET       │
│                                       • __EVENTARGUMENT     │
│                                                             │
│  2. User fills form & clicks button                         │
│     └─→ __doPostBack() executes                             │
│         ├─→ Sets __EVENTTARGET                              │
│         ├─→ Sets __EVENTARGUMENT                            │
│         └─→ Submits form                                    │
│     POST /Donate  ─────────────────→  Page_Init             │
│     (with __VIEWSTATE,                Page_Load             │
│      __EVENTTARGET,                   (IsPostBack = true)   │
│      __EVENTARGUMENT)                 • Read form data      │
│                                       • Call API            │
│                                       • Redirect            │
│     ←────────────────────────────  Response 302             │
│                                       Location: /Thank-You  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**ViewState Mechanics:**

```html
<!-- Generated by ASP.NET on every page render -->
<input
  type="hidden"
  name="__VIEWSTATE"
  value="dGVzdEVuY29kZWRTdGF0ZURhdGE..."
/>
<input type="hidden" name="__EVENTTARGET" value="SubmitPayment" />
<input
  type="hidden"
  name="__EVENTARGUMENT"
  value='{"amount":"100.00","firstName":"John"}'
/>
```

**Why This Matters for VSPayment:**

- `__EVENTTARGET` acts as a server-side action router (like REST endpoint paths)
- `__EVENTARGUMENT` carries JSON payload (like POST body)
- `__VIEWSTATE` preserves server-side control state (not used heavily in this
  module)
- Single `.ascx` file handles multiple actions via postback routing

### CyberSource Payment Gateway Integration

**CyberSource** is Visa's payment processing platform. The integration works as
follows:

```plaintext
┌─────────────────────────────────────────────────────────────┐
│            CYBERSOURCE PAYMENT FLOW                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  STEP 1: Generate Encryption Key (Page Load)                │
│  ────────────────────────────────────────                   │
│  Vision Source Server                                       │
│    └─→ Calls CyberSource API: "Generate JWK"                │
│        └─→ Returns JSON Web Key (public key cryptography)   │
│            └─→ Passed to Flex Microform JavaScript          │
│                                                             │
│  STEP 2: Secure Card Entry (User Input)                     │
│  ────────────────────────────────────────                   │
│  User Browser                                               │
│    └─→ Flex Microform creates secure iframe                 │
│        └─→ User types card number inside iframe             │
│            └─→ Card data encrypted with JWK                 │
│                └─→ Data stays in iframe sandbox             │
│                    (Vision Source JavaScript can't access)  │
│                                                             │
│  STEP 3: Tokenization (Form Submission)                     │
│  ────────────────────────────────────                       │
│  User clicks "Pay"                                          │
│    └─→ JavaScript calls createToken()                       │
│        └─→ Encrypted card data sent to CyberSource          │
│            └─→ CyberSource validates card                   │
│                └─→ Returns token: {                         │
│                    token: "7234987ASDFJ234",                │
│                    maskedPan: "************1234",           │
│                    cardType: "001" (Visa)                   │
│                }                                            │
│                                                             │
│  STEP 4: Payment Processing (Server)                        │
│  ────────────────────────────────────                       │
│  Vision Source Payment API receives:                        │
│    └─→ Token (not card number!)                             │
│    └─→ Amount, billing info                                 │
│        └─→ API calls CyberSource SOAP service:              │
│            └─→ "Process payment using token 7234987..."     │
│                └─→ CyberSource:                             │
│                    • Exchanges token for card data          │
│                    • Contacts card issuer bank              │
│                    • Authorizes transaction                 │
│                    • Returns authorization code             │
│                └─→ API returns receipt to DNN module        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Security Advantages:**

1. **Separation of Concerns**: Vision Source handles payment orchestration,
   CyberSource handles sensitive card data
2. **Token Expiration**: Tokens expire in 15 minutes (prevents replay attacks)
3. **Iframe Sandboxing**: Browser security model prevents JavaScript from
   reading card data
4. **PCI Scope Reduction**: Vision Source servers never store/process card
   numbers

### Understanding PCI DSS Compliance in Payment Systems

**What is PCI DSS?**

The Payment Card Industry Data Security Standard (PCI DSS) is a comprehensive
security framework established by major credit card companies (Visa, Mastercard,
American Express, Discover) to protect cardholder data throughout the
transaction lifecycle. Organizations that store, process, or transmit credit
card information must comply with these standards to prevent data breaches and
fraud.

**Compliance Scope and Merchant Levels**

The requirements vary based on transaction volume:

- **Level 1 merchants** (processing over 6 million transactions annually) face
  the strictest requirements, including annual on-site security audits by
  qualified assessors
- **Levels 2-4** (smaller transaction volumes) typically complete
  self-assessment questionnaires and quarterly vulnerability scans

**Core Security Requirements**

PCI DSS encompasses 12 fundamental requirements organized into six control
objectives:

1. Build and maintain secure networks through firewall configuration
2. Protect stored cardholder data with encryption and truncation
3. Maintain vulnerability management programs including anti-malware protection
4. Implement strong access control measures with unique IDs for all users
5. Regularly monitor and test security systems through logging and penetration
   testing
6. Maintain comprehensive information security policies

**Compliance Challenges for Organizations**

Meeting PCI DSS requirements presents significant operational and financial
burdens. Organizations must implement extensive security controls including
network segmentation, encryption at rest and in transit, regular security
audits, employee training programs, and incident response procedures. The cost
of maintaining compliance can be substantial, particularly for smaller
organizations.

**VSPayment's Compliance Strategy**

The VSPayment module addresses PCI compliance through **scope reduction** rather
than full certification. By leveraging CyberSource's Flex Microform tokenization
technology, Vision Source removes credit card data from its processing
environment entirely. This architectural decision means sensitive cardholder
data never enters Vision Source's systems, dramatically reducing the scope of
PCI compliance requirements and eliminating the need for expensive annual
audits.

---

### CyberSource Flex Microform: Tokenization Technology

**The Problem with Traditional Payment Processing**

In conventional payment systems, credit card data flows through the merchant's
servers during transaction processing. This creates significant security risks
and compliance burdens, as the merchant must secure, encrypt, and audit every
system that touches payment data. A breach anywhere in this chain exposes
customer financial information.

**How Tokenization Solves the Problem**

Tokenization replaces sensitive payment data with non-sensitive equivalents
called tokens. These tokens serve as references to the actual payment
information stored securely by the payment processor, but contain no exploitable
data themselves. If a token is intercepted, it's useless to attackers because it
cannot be reverse-engineered to obtain the original card number.

**Flex Microform Architecture**

CyberSource's Flex Microform implements tokenization through a sophisticated
client-side architecture:

1. **Secure Iframe Isolation**: When a payment page loads, Flex Microform
   creates an iframe for the credit card number field. This iframe is hosted by
   CyberSource's servers (cybersource.com domain), not by Vision Source. The
   Same-Origin Policy in web browsers prevents Vision Source's JavaScript from
   accessing the iframe's contents, creating an isolation boundary that protects
   cardholder data from potential XSS (Cross-Site Scripting) attacks or
   malicious scripts.

2. **Cryptographic Key Exchange**: Before accepting payment data, the system
   performs a key exchange. Vision Source's server requests a JSON Web Key (JWK)
   from CyberSource containing an RSA public key with 256-bit OAEP padding. This
   key is valid for the user's session and enables client-side encryption. The
   JWK includes cryptographic parameters (exponent, modulus, key type, key ID)
   that Flex Microform uses to encrypt card data before transmission.

3. **Client-Side Tokenization**: When the user enters their card number and
   clicks submit, Flex Microform's JavaScript intercepts the submission. The
   card data is encrypted using the session key and sent directly from the
   user's browser to CyberSource's servers via HTTPS. CyberSource validates the
   card data, performs initial fraud checks, and generates a transient token—a
   random identifier valid for 15 minutes. This token is returned to Vision
   Source's page via callback.

4. **Token-Based Processing**: Vision Source's systems only ever receive the
   token, never the actual card number. When the payment form submits to Vision
   Source's API, it includes this token along with transaction metadata (amount,
   billing address, customer information). The API forwards the token to
   CyberSource, which decrypts it internally, retrieves the associated card
   data, and processes the transaction with the card networks. The response
   returns an authorization code and masked account number (last 4 digits only).

**Security Benefits**

This architecture provides multiple layers of protection:

- **Data Isolation**: Credit card numbers physically never traverse Vision
  Source's network infrastructure or servers
- **Attack Surface Reduction**: Attackers compromising Vision Source's systems
  find no payment data to steal
- **XSS Protection**: The iframe boundary prevents malicious JavaScript from
  accessing card input fields
- **Token Expiration**: The 15-minute token validity limits replay attack
  windows
- **PCI Scope Reduction**: Since Vision Source never handles card data, most PCI
  DSS requirements become inapplicable

**Operational Considerations**

Flex Microform introduces certain operational requirements:

- Users must have JavaScript enabled and use modern browsers supporting secure
  iframes
- Network latency affects user experience during tokenization requests
- Token expiration requires users to complete payments within 15 minutes
- Failed tokenization (due to network issues) requires restarting the payment
  flow

**Saved Payment Methods: A Different Approach**

The VSPayment module also supports saved payment methods for recurring payments.
These stored credentials present a different security model: Vision Source does
store encrypted payment data in its database, bringing these specific systems
under PCI scope. The encryption uses industry-standard algorithms with keys
stored separately from the data. Saved methods display only masked information
(last 4 digits) in user interfaces. This hybrid approach balances convenience
for recurring payments with security for one-time transactions, allowing Vision
Source to maintain limited PCI scope rather than full merchant certification
requirements.

**The Business Impact**

By implementing Flex Microform, Vision Source achieves PCI compliance without
the substantial costs of full merchant certification—estimated to save hundreds
of thousands of dollars annually in audit fees, security infrastructure, and
compliance personnel. This allows the organization to focus resources on member
services rather than payment security infrastructure, while still providing
secure payment processing for donations, royalties, and Co-Op fees.

### DotNetNuke Microfrontend Architecture: JavaScript-First Module Development

**DotNetNuke's Natural Alignment with Microfrontends**

DotNetNuke's modular architecture inherently supports microfrontend
patterns—each module operates as an independent, self-contained application
within the DNN container. The VSMemberDashboard module exemplifies this
architecture by minimizing server-side code-behind logic and implementing nearly
all business functionality in JavaScript, creating a lightweight, maintainable
system that separates concerns between server configuration and client-side
application logic.

**Current VSMemberDashboard Architecture**

The module implements a hybrid approach where `View.ascx.cs` serves exclusively
as a configuration bootstrap:

```plaintext
VSMemberDashboard/
├── View.ascx                    # Server-side configuration injection only
├── View.ascx.cs                 # Minimal code-behind (authentication, settings)
├── App/
│   ├── Scripts/
│   │   ├── dashboard.js         # Core application logic
│   │   ├── personnel.js         # Personnel module
│   │   ├── contact.js           # Contact management
│   │   ├── boo.js               # Business of Optometry
│   │   └── documents.js         # Document management
│   ├── Pages/
│   │   ├── summary.html         # Independent view templates
│   │   ├── account.html
│   │   ├── personnel.html
│   │   └── [additional views]
│   └── Content/
│       └── dashboard.css        # Isolated styling
└── VisionSource.MemberDashboard.WCF/  # Separate REST API layer
```

**The Code-Behind Minimization Strategy**

The `View.ascx.cs` file contains virtually no business logic:

```csharp
protected void Page_Load(object sender, EventArgs e)
{
    try
    {
        // Empty - configuration only
    }
    catch (Exception exc)
    {
        Exceptions.ProcessModuleLoadException(this, exc);
    }
}
```

Instead, the `.ascx` file injects server-side configuration into JavaScript:

- User roles and permissions (`IsInRole()` checks)
- API endpoint URLs from `web.config`
- Module settings from DNN `Settings` dictionary
- User profile data (Staff ID, Doctor ID, Office ID)

**Benefits of JavaScript-First Architecture**

| Aspect                | Traditional Code-Behind                         | JavaScript-First Microfrontend             |
| --------------------- | ----------------------------------------------- | ------------------------------------------ |
| **Deployment**        | Compile DLL → Restart AppPool → Full deployment | Update JS file → Cache invalidation → Live |
| **Development Speed** | C# compilation required                         | Edit → Save → Browser refresh              |
| **Hot Reload**        | Not available                                   | Native support (Webpack HMR, Vite)         |
| **Debugging**         | Visual Studio required                          | Chrome DevTools                            |
| **Technology Stack**  | .NET Framework locked                           | Modern frameworks (React, Vue, TypeScript) |
| **Team Separation**   | Full-stack developers only                      | Frontend/Backend independent teams         |
| **Portability**       | ASP.NET only                                    | Platform-agnostic (can migrate to SPA)     |

**Use Code-Behind For:**

- Injecting secure configuration (API keys, connection strings)
- Server-side role validation before page render
- Reading DNN user context (`UserInfo`, `PortalId`, `ModuleId`)
- Server-side redirects based on permissions

**Use JavaScript For:**

- All UI rendering and interactivity
- Form validation and submission
- API calls and data fetching
- State management
- Client-side routing
- Business logic presentation layer
- Real-time updates and notifications

**Performance Advantages**

- **Client-side rendering**: Eliminates ASP.NET Page Lifecycle overhead
- **No ViewState**: Reduces payload size by removing hidden form data
- **No PostBacks**: Replaces full page reloads with AJAX API calls
- **CDN caching**: Static JavaScript/CSS files cached at edge locations
- **Lazy loading**: Load modules on-demand rather than upfront
