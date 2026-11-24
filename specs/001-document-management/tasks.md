# Tasks: Document Management

**Input**: Design documents from `/specs/001-document-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: The implementation plan and constitution require focused authorization, lifecycle, validation, and integration coverage.

## Existing MVP Baseline (Preserved)

The repository already contains partial document upload MVP implementation and tests. These existing capabilities and task IDs remain in scope and must not be removed or regenerated:

- Existing `Document`, `DocumentShare`, `DocumentService`, `FileStorageService`, `Documents.razor`, and MVP test conventions
- Existing integer `DocumentId`, text category, local outside-`wwwroot` storage, project authorization, and direct-ID checks
- Existing setup and foundational task IDs T001-T062 remain the execution baseline. Preserve any completed baseline items and their status; do not mark new requirements complete until their tests pass.

The following six approved clarification decisions require implementation beyond the existing partial MVP:

- Project-authorized direct sharing only
- Provider-neutral malware scanning with fail-closed behavior
- Independent per-file success/failure in mixed batch uploads
- Project/task association consistency
- Permanent confirmed deletion with bounded audit retention
- Project-upload notifications and direct-share notifications

Additional specification requirements still pending beyond the partial MVP (these are not new clarification decisions):

- Team Lead same-Department access and administrator access to all documents
- Shared with Me, direct task-detail upload, date-range filtering, and per-file upload progress
- A functioning local malware scanner (the current MVP has none; test fakes do not satisfy the requirement)
- `GetUrlAsync` contract coverage and at least 100 negative authorization cases
- P3 administrator audit/reporting story

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm and complete the existing project structure before feature work

- [ ] T001 Verify the .NET 8 solution, EF Core SQLite configuration, and test project configuration in ContosoDashboard/ContosoDashboard.csproj and ContosoDashboard.Tests/ContosoDashboard.Tests.csproj
- [ ] T002 Verify the existing document domain service, local storage implementation, document page, and test conventions in ContosoDashboard/Services/DocumentService.cs, ContosoDashboard/Services/FileStorageService.cs, ContosoDashboard/Pages/Documents.razor, and ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T003 Verify the local upload root resolves to AppContext.BaseDirectory/AppData/uploads and remains outside wwwroot in ContosoDashboard/Services/FileStorageService.cs
- [ ] T004 Add approved document category constants for Project Documents, Team Resources, Personal Files, Reports, Presentations, and Other in ContosoDashboard/Models/Document.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Complete shared persistence and authorization infrastructure that blocks every user story

**CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Update ContosoDashboard/Models/Document.cs to preserve the integer DocumentId key and text Category while enforcing required Title, required Category, original FileName, MIME FileType (max length 255), unique generated FilePath (max length 500), FileSizeBytes, uploader, optional ProjectId/TaskId, Tags (max length 500), timestamps, and no soft-delete recovery field
- [ ] T006 Replace the user-only share shape in ContosoDashboard/Models/DocumentShare.cs with exactly one recipient: nullable UserId or nullable TeamRole, SharedByUserId, SharedDate, and optional Reason (max length 500), with project-authorized recipient validation
- [ ] T007 [P] Add ContosoDashboard/Models/DocumentActivity.cs with DocumentActivityId, nullable historical DocumentId without a Document foreign key, ActingUserId, Action (Upload, Download, Update, Replace, Share, or Delete), UTC OccurredAt, and optional Details limited to 500 characters
- [ ] T008 Update relationship mappings and create the required unique active-share and DocumentActivity query indexes for Document, DocumentShare, and DocumentActivity in ContosoDashboard/Data/ApplicationDbContext.cs; this is the only index-creation task
- [ ] T009 [P] Add compensating-safe file operations for path validation, GUID storage names, replacement, download, and deletion in ContosoDashboard/Services/FileStorageService.cs
- [ ] T010 Centralize server-side document authorization for owner, project manager, project member, same-Department Team Lead, administrator, and eligible explicit-share access in ContosoDashboard/Services/DocumentService.cs; Team Lead status must come from authenticated role and Department claims, administrator access covers all documents, and neither role may bypass the project-authorized direct-share rule
- [ ] T011 Add category, title, description, tags, path, size, project/task, and approved extension/MIME-pair validation rules with clear exceptions in ContosoDashboard/Services/DocumentService.cs
- [ ] T012 [P] Register or update application service dependencies, including document reporting and notification support, in ContosoDashboard/Program.cs
- [ ] T013 [P] Add EF in-memory and local-file test fixtures plus seeded owner, project-manager, project-member, same-Department Team Lead, different-Department Team Lead, administrator, and unauthorized users in ContosoDashboard.Tests/DocumentServiceMvpTests.cs

**Checkpoint**: Foundation ready; shared models, storage, authorization, and test infrastructure are available

---

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1) MVP

**Goal**: Authenticated project or personal users can validate, upload, categorize, and retain documents with an audit trail

**Independent Test**: Log in, upload a valid PDF or Office/text/image file with title/category/optional project metadata, and verify it appears in an authorized personal or project document list without orphaned records

### Tests for User Story 1

- [ ] T014 [P] [US1] Add an integration test for valid project and personal uploads, required category/title metadata, generated paths, and Upload audit activity in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T015 [P] [US1] Add validation tests for zero-byte, missing-extension, unsupported-extension/MIME, over-25-MB files, and category rejection in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T016 [P] [US1] Add failure tests proving storage failure leaves no metadata row and database-save failure removes the newly written file or records a cleanup-retry failure with no success result or metadata/audit row in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T063 [P] [US1] Add mixed-batch tests proving valid files upload independently and invalid files return individual failures without metadata or orphan files in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T064 [P] [US1] Add fail-closed malware-scan tests for unavailable, failed, errored, and indeterminate outcomes using test doubles only to verify rejection behavior in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T017 [P] [US1] Add upload authorization tests for project members, same-Department Team Leads, administrators, different-Department Team Leads, and denied users, including same-project task restrictions, in ContosoDashboard.Tests/DocumentServiceMvpTests.cs

### Implementation for User Story 1

- [ ] T018 [P] [US1] Complete server-side upload validation and project/task membership checks, using an optional TaskId and rejecting task/project mismatches, in ContosoDashboard/Services/DocumentService.cs
- [ ] T019 [P] [US1] Implement GUID-based unique storage writes and compensating cleanup after metadata/audit transaction failure, including cleanup-failure logging/retry information, in ContosoDashboard/Services/FileStorageService.cs
- [ ] T065 [US1] Define and register a provider-neutral IMalwareScanService backed by a functioning local scanner that actually scans file content and reports Clean, ThreatFound, Unavailable, Error, or Indeterminate in ContosoDashboard/Services/ and ContosoDashboard/Program.cs. A no-op implementation, fabricated Clean result, or test fake MUST NOT satisfy this task; keep scanner completion pending until the real local implementation is verified.
- [ ] T066 [US1] Process mixed batch uploads independently and return one success or rejection result per file in ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Pages/Documents.razor
- [ ] T067 [US1] Invoke malware scanning before file storage and fail closed, storing no file bytes or document/audit metadata for any non-Clean result in ContosoDashboard/Services/DocumentService.cs
- [ ] T020 [US1] Persist upload metadata, including optional ProjectId/TaskId, UploadDate/UpdatedDate and DocumentActivity Upload, in ContosoDashboard/Services/DocumentService.cs
- [ ] T021 [P] [US1] Extend ContosoDashboard/Pages/Documents.razor with required title/category, optional description/project/task/tags, approved categories, per-file progress and success/failure feedback, and duplicate-submit prevention; preserve the three-click upload target
- [ ] T022 [US1] Update document list refresh logic in ContosoDashboard/Pages/Documents.razor to show only owned, project-authorized, same-Department Team Lead, or administrator-accessible documents
- [ ] T023 [US1] Enforce task-to-project consistency before persisting uploaded task documents in ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: US1 passes only after validation, fail-closed behavior, per-file outcomes/progress, and a functioning local scanner are implemented and verified. Until then, successful-upload capability remains pending.

---

## Phase 4: User Story 2 - Find and retrieve documents quickly (Priority: P1)

**Goal**: Authorized users can search, browse, filter, preview, and download documents under 2 seconds for typical 500-document lists

**Independent Test**: Search or filter by title/tag/uploader/category/project/task, then preview/download only matching authorized records and reject a direct unauthorized DocumentId

### Tests for User Story 2

- [ ] T024 [P] [US2] Add direct-ID access tests for owner, project member, project manager, explicit share, and unrelated unauthorized users in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T025 [P] [US2] Add search/filter/sort tests covering title, description, tags, uploader name, category, project, task, and inclusive date-range boundaries, with unauthorized rows excluded in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T026 [P] [US2] Add download/preview tests proving successful authorized streams and blocked unknown or unauthorized DocumentIds in ContosoDashboard.Tests/DocumentServiceMvpTests.cs

### Implementation for User Story 2

- [ ] T027 [P] [US2] Extend IDocumentService with authorized search, bounded page/sort, direct lookup, preview/download, and no-existence-disclosure behavior in ContosoDashboard/Services/DocumentService.cs
- [ ] T028 [US2] Implement authorization-first EF queries for owner/project/Team Lead/administrator/share access and document search/filter/sort, including date-range filtering, in ContosoDashboard/Services/DocumentService.cs
- [ ] T029 [P] [US2] Add preview/download stream operations that validate the resolved file remains below the storage root and record Download activity in ContosoDashboard/Services/FileStorageService.cs and ContosoDashboard/Services/DocumentService.cs
- [ ] T030 [P] [US2] Add no public file parameter or public-file route; expose document retrieval only through document-ID authorized service/page handlers in ContosoDashboard/Pages/Documents.razor
- [ ] T031 [US2] Add search, category, project, date-range, task, uploader, and sort controls to ContosoDashboard/Pages/Documents.razor
- [ ] T032 [P] [US2] Add authorized document-list/preview/download actions to ContosoDashboard/Pages/ProjectDetails.razor
- [ ] T033 [US2] Add preview support for PDF and image types and authorized download actions for Office/text files in ContosoDashboard/Pages/Documents.razor

**Checkpoint**: US2 is independently functional for authorized retrieval and search

---

## Phase 5: User Story 3 - Share and manage document permissions (Priority: P2)

**Goal**: Owners and project managers can share only with project-authorized users or teams, update or replace files, delete safely, and audit every action

**Independent Test**: An owner/project manager shares with an eligible project member/team, the recipient gets a notification and access, while out-of-project/direct personal shares are rejected

### Tests for User Story 3

- [ ] T034 [P] [US3] Add share tests for owner, project manager, same-Department Team Lead, administrator, eligible user/TeamRole, unknown recipient, user outside project, team with no project recipients, and personal documents in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T035 [P] [US3] Add notification and audit tests for Share, Update, Replace, and Delete events in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T036 [P] [US3] Add update/replace/delete authorization tests for owner, project manager, same-Department Team Lead, different-Department Team Lead, administrator, and unauthorized member in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T037 [P] [US3] Add replacement and deletion cleanup tests proving old files are removed or unreachable and database state remains compensating-safe in ContosoDashboard.Tests/DocumentServiceMvpTests.cs

### Implementation for User Story 3

- [ ] T038 [P] [US3] Implement exactly-one-recipient ShareDocumentAsync for authorized owner, project manager, same-Department Team Lead, or administrator; validate each recipient user or TeamRole against project membership/manager in ContosoDashboard/Services/DocumentService.cs
- [ ] T039 [P] [US3] Resolve project TeamRole shares to matching project members and send one in-app notification per eligible recipient; send project-upload notifications to project members except the uploader through ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Services/NotificationService.cs
- [ ] T040 [P] [US3] Implement UpdateDocumentAsync metadata rules and destination project/task authorization in ContosoDashboard/Services/DocumentService.cs
- [ ] T041 [P] [US3] Implement ReplaceDocumentAsync to write before metadata update, record Replace, and remove the old file safely in ContosoDashboard/Services/DocumentService.cs
- [ ] T042 [US3] Implement owner/project-manager/same-Department-Team-Lead/administrator DeleteDocumentAsync with permanent document/file removal after confirmation, bounded audit activity, and direct retrieval prevention in ContosoDashboard/Services/DocumentService.cs
- [ ] T043 [P] [US3] Add share recipient, Shared with Me navigation, edit metadata, replace file, and role-appropriate delete confirmation controls to ContosoDashboard/Pages/Documents.razor
- [ ] T044 [US3] Hide share/update/delete controls for unauthorized users and reflect server rejection safely in ContosoDashboard/Pages/Documents.razor

**Checkpoint**: US3 is independently functional for sharing and lifecycle management

---

## Phase 6: User Story 4 - Dashboard and task integration (Priority: P2)

**Goal**: Recent document activity and task/project associations are visible only within each user's authorization scope

**Independent Test**: Open a task/project/dashboard and see authorized recent and associated documents, counts, and no inaccessible records

### Tests for User Story 4

- [ ] T045 [P] [US4] Add dashboard and task integration tests proving the widget shows the current user's five most recent uploads, counts only currently authorized documents, direct task-detail uploads work for project members/managers, and task associations remain project-scoped in ContosoDashboard.Tests/DocumentServiceMvpTests.cs

### Implementation for User Story 4

- [ ] T046 [US4] Add the direct upload flow to the task detail page in ContosoDashboard/Pages/Tasks.razor and pass the task's project-validated TaskId through ContosoDashboard/Services/DocumentService.cs
- [ ] T047 [P] [US4] Add task and recent-document query methods to ContosoDashboard/Services/DocumentService.cs and document summary/count methods to ContosoDashboard/Services/DashboardService.cs
- [ ] T048 [P] [US4] Add recent-document summary and document-count fields to ContosoDashboard/Pages/Index.razor
- [ ] T049 [P] [US4] Add authorized document attachment and viewing controls to task context in ContosoDashboard/Pages/Tasks.razor
- [ ] T050 [P] [US4] Add task/document association display to project context in ContosoDashboard/Pages/ProjectDetails.razor
**Checkpoint**: US4 is independently functional for authorized dashboard, project, and task integration

---

## Phase 7: User Story 5 - Audit and administrator reporting (Priority: P3)

**Goal**: Administrators can review bounded document activity and aggregate active uploader, document type/category, and access-pattern reports.

**Independent Test**: An administrator retrieves audit/report data for a bounded time window, while non-administrators are denied.

- [ ] T068 [P] [US5] Add audit tests for successful Upload, Download, Update, Replace, Share, and Delete actions and verify bounded document, user, action, and UTC fields in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T069 [P] [US5] Add administrator reporting tests for active uploaders, MIME/category counts, and access patterns while denying non-administrators in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T070 [P] [US5] Add an administrator-only reporting service and read-only projection for active uploaders, MIME/category counts, and access patterns in ContosoDashboard/Services/DocumentReportingService.cs
- [ ] T071 [P] [US5] Add a clearly marked administrator audit/reporting view in ContosoDashboard/Pages/Documents.razor or ContosoDashboard/Pages/DocumentReports.razor
- [ ] T072 [P] [US5] Ensure deletion retains only bounded audit records and never exposes deleted file content or full document metadata in ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: US5 is independently functional for audit and administrator reporting

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Complete security, quality, documentation, and performance validation

- [ ] T051 [P] Add component/service tests proving per-file progress changes through upload, success, and failure states without hiding independent batch outcomes in ContosoDashboard.Tests/
- [ ] T052 [P] Implement per-file upload progress rendering and state reset in ContosoDashboard/Pages/Documents.razor; progress must remain visible through each file's completion or rejection
- [ ] T053 [P] Add focused service and integration coverage for every upload/download/search/share/update/replace/delete audit action in ContosoDashboard.Tests/
- [ ] T054 [P] Review every DocumentService mutation/read for centralized authorization and direct-ID disclosure prevention in ContosoDashboard/Services/DocumentService.cs
- [ ] T055 [P] Harden IFileStorageService path resolution and cleanup against traversal, missing files, and generated-path collisions in ContosoDashboard/Services/FileStorageService.cs
- [ ] T056 [P] Add EF model tests verifying the active-share uniqueness and DocumentActivity OccurredAt/DocumentId/Action index mappings created by T008 in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T057 Update ContosoDashboard/Pages/Documents.razor and project/task/dashboard views for consistent clear errors and upload/list performance feedback
- [ ] T058 [P] Update specs/001-document-management/quickstart.md with executable final validation steps for all five stories
- [ ] T059 Run `dotnet test ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` and document failures or unavailable checks in the final feature report
- [ ] T060 Run dotnet build for ContosoDashboard/ContosoDashboard.csproj and resolve compile failures
- [ ] T061 Measure average authorized-document retrieval under 30 seconds, list/search for 500 records within 2 seconds, preview within 3 seconds, and upload behavior for files up to 25 MB within 30 seconds on a typical network
- [ ] T062 Review changes against .specify/memory/constitution.md and confirm training-only, offline, layered, scanner, authorization, 100-negative-case, and quality gates remain documented
- [ ] T073 [P] [US3] Add Shared with Me tests for eligible recipients, unrelated users, revoked/out-of-project authorization, and no document-existence disclosure in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T074 [US3] Implement an authorization-filtered Shared with Me query and view in ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Pages/Documents.razor
- [ ] T075 [P] Add service-level authorization tests for Team Lead same-Department access and administrator all-document access across list/search/direct-ID/download/preview operations in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T076 [P] Implement and test IFileStorageService.GetUrlAsync(string filePath, TimeSpan expiration) in ContosoDashboard/Services/FileStorageService.cs; any returned location must route through server-side authorization and must not expose a public file path or bypass the project-authorized share rule
- [ ] T077 [P] Add a table-driven authorization suite with at least 100 negative cases across owner, project member, project manager, Team Lead, administrator, explicit-share, direct-ID, preview, download, update, replace, share, and delete scenarios in ContosoDashboard.Tests/DocumentServiceMvpTests.cs
- [ ] T078 Obtain and document an approved audit-event retention duration in specs/001-document-management/research.md and specs/001-document-management/data-model.md before implementing any time-based purge; until approved, preserve bounded event fields and do not invent an expiration window
- [ ] T079 Run and document the representative usability check for SC-006 and SC-008: at least five users, at least 80% complete upload/retrieval without assistance, and at least 80% complete upload in no more than three clicks in specs/001-document-management/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
- **Polish (Phase 8)**: Depends on all desired user stories and P3 reporting being complete

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2; no dependency on other user stories
- **User Story 2 (P1)**: Starts after Phase 2; uses US1 document records but remains independently testable
- **User Story 3 (P2)**: Starts after Phase 2; extends US1/US2 lifecycle behavior but is independently testable
- **User Story 4 (P2)**: Starts after Phase 2; reads authorized US1/US2 documents and task/project context
- **User Story 5 (P3)**: Starts after the foundational audit entity and P1-P4 document actions establish persisted audit events
- Upload acceptance is blocked until T065 verifies a functioning local scanner; T064 test doubles prove fail-closed handling only.
- FR-024 is covered by T073-T074; FR-025 by T045-T046; date-range filtering by T025, T028, and T031; per-file progress by T021, T051, and T052.
- T008 creates the indexes and T056 tests their mappings; neither task duplicates the other's work.

### Within Each User Story

- Tests (where included) should be written and confirmed failing before implementation
- Models and persistence precede service behavior
- Centralized authorization precedes UI actions
- Services precede page integration
- Story implementation is complete before moving to the next priority

---

## Parallel Opportunities

- Phase 1 tasks that inspect independent project files can run in parallel
- Phase 2 model, storage, test-fixture, and registration tasks marked [P] can run in parallel
- Once Phase 2 completes, US1, US2, US3, and US4 can be staffed independently
- Tests marked [P] within a story can run concurrently
- Separate model/service/page files marked [P] can be changed concurrently when dependencies are already complete

---

## Parallel Example: User Story 1

```text
T014, T015, T016, and T017: write independent upload validation and authorization tests
T018 and T019: complete service validation and storage implementation in separate files
T021 and T022: update upload and list UI behavior in Documents.razor
```

## Parallel Example: User Story 2

```text
T024, T025, and T026: write independent direct-ID, search, and retrieval tests
T027 and T029: extend service contracts and storage download behavior
T031, T032, and T033: update documents, project, preview, and download UI surfaces
```

## Parallel Example: User Story 3

```text
T034, T035, T036, and T037: run independent sharing and lifecycle test additions
T038, T039, T040, and T041: implement separate share/update/replace policies
T043 and T044: update share and lifecycle controls in the document page
```


---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Stop and independently validate valid upload, rejection cases, project visibility, and audit behavior
5. Do not deploy/demo successful upload as complete until a functioning local scanner is verified. If it is unavailable, demonstrate only the fail-closed rejection path and keep US1 pending.

### Incremental Delivery

1. Complete Setup and Foundational
2. Add User Story 1 and validate it independently
3. Add User Story 2 and validate retrieval/search
4. Add User Story 3 and validate sharing/lifecycle
5. Add User Story 4 and validate dashboard/task integration
6. Add P3 User Story 5 and validate audit/reporting integration

### Parallel Team Strategy

1. Team completes Setup and Foundational together
2. After foundational completion:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
   - Developer D: User Story 4
   - Developer E: P3 User Story 5 after audit actions establish persisted data
3. Merge only after service authorization and database/storage boundaries remain consistent

---

## Notes

- [P] tasks = different files or independent work with no dependency on incomplete tasks
- [US1], [US2], [US3], and [US4] labels map tasks to the specification user-story phases
- Setup and foundational tasks do not receive story labels
- Every task includes an exact repository file path or directory
- All tasks follow the required checkbox, sequential ID, optional [P], and story-label format
