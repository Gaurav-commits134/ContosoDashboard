# Tasks: Document Management

**Input**: Design documents from `/specs/001-document-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Phase 1: Shared Foundation

- [x] T001 Add the document models and relationships in ContosoDashboard/Models and ContosoDashboard/Data/ApplicationDbContext.cs
- [x] T002 Define the storage abstraction and local implementation in ContosoDashboard/Services/
- [x] T003 Register document services and storage providers in ContosoDashboard/Program.cs
- [x] T004 Add a secure upload directory and initialize required file-system configuration

## Phase 2: Core Service Behavior

- [x] T005 Implement the document service with upload validation, authorization checks, and metadata persistence
- [x] T006 Implement project-based visibility and user access filtering in the service layer
- [x] T007 Implement document search, sorting, and filtering logic for lists and project views
- [x] T008 Implement share, download, replace-file, and delete flows with audit-friendly behavior

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1)

**Goal**: Users can upload and see their documents in the dashboard.

### Implementation

- [x] T009 Create a document upload UI and form in ContosoDashboard/Pages/
- [x] T010 Add client-side and server-side validation for file type, file size, and required metadata
- [x] T011 Persist uploaded file metadata plus generated unique file paths
- [x] T012 Add the document list page with sorting and category/project filtering
- [x] T013 Verify uploads appear correctly for owned and project-associated documents

## Phase 4: User Story 2 - Find and retrieve documents quickly (Priority: P1)

**Goal**: Users can search, preview, and download approved documents.

### Implementation

- [ ] T014 Add search and filter controls to the document views and project detail pages
- [ ] T015 Implement preview support for browser-viewable files (PDF and images)
- [ ] T016 Implement download authorization checks and secure file retrieval
- [ ] T017 Confirm that unauthorized attempts are blocked even when a document ID is known

## Phase 5: User Story 3 - Share and manage document permissions (Priority: P2)

**Goal**: Users can share and manage document access with notifications.

### Implementation

- [ ] T018 Add sharing controls and recipient selection to document management flows
- [ ] T019 Trigger in-app notifications when documents are shared or added to project contexts
- [ ] T020 Allow owners or authorized managers to edit metadata and replace document files
- [ ] T021 Allow owners or authorized managers to delete documents with confirmation

## Phase 6: User Story 4 - Dashboard and task integration (Priority: P2)

**Goal**: Documents are discoverable in task and dashboard contexts.

### Implementation

- [ ] T022 Add a recent documents summary widget to the dashboard home page
- [ ] T023 Update dashboard summary cards to include document counts
- [ ] T024 Add task-level document attachment and viewing support
- [ ] T025 Verify project and task document lists remain synchronized with authorization rules

## Phase 7: Quality and Security Review

- [ ] T026 Add focused tests for upload validation, access control, and document listing behavior
- [ ] T027 Check all service-layer authorization and IDOR protections
- [ ] T028 Review file cleanup, storage security, and audit tracking requirements
- [ ] T029 Validate performance assumptions against the project’s local usage profile

## Dependencies & Execution Order

- Shared foundation tasks must complete before user-story work begins.
- User Story 1 establishes the MVP upload and listing flow.
- User Story 2 builds retrieval and security confidence.
- User Story 3 adds sharing and management capability.
- User Story 4 completes workflow integration and dashboard visibility.
- Final quality tasks close the feature and validate the offline-ready implementation.
