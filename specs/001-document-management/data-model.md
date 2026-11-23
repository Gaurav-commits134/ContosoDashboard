# Data Model: Document Management

## Core Entities

### Document

Represents a stored file with metadata and its relationship to a project, task, and uploader.

| Field | Type | Notes |
|---|---|---|
| DocumentId | int | Primary key, consistent with existing app identity keys |
| Title | string | Required; human-readable label |
| Description | string? | Optional summary |
| Category | string | Required; uses values such as Project Documents, Personal Files, Reports |
| FileName | string | Original file name, stored for display only |
| FilePath | string | Server-side relative path; unique GUID-based name |
| FileType | string | MIME type, max length 255 |
| FileSizeBytes | long | Used for display and validation |
| UploadedByUserId | int | FK to User |
| ProjectId | int? | Optional project association |
| TaskId | int? | Optional task association |
| UploadDate | DateTime | Captured when saved |
| UpdatedDate | DateTime | Audited metadata updates |
| Tags | string? | Searchable tag list |
| IsDeleted | bool | Soft delete support for audit and recovery |

### DocumentShare

Represents explicit access granted to a user or a project team/role.

| Field | Type | Notes |
|---|---|---|
| DocumentShareId | int | Primary key |
| DocumentId | int | FK to Document |
| UserId | int? | Recipient user; required for a user share |
| TeamRole | string? | Project membership role label for a team share |
| SharedByUserId | int | User who granted access |
| SharedDate | DateTime | Audit timestamp |
| Reason | string? | Optional share note |

Validation: exactly one of `UserId` or `TeamRole` must be set. For a user share, the recipient must be the project manager or a member of the document's project. For a team share, the document must have a project and at least one matching project member or the project manager must match the selected team role. Personal-document shares are not implemented in the MVP.

### DocumentActivity

Represents an auditable document action.

| Field | Type | Notes |
|---|---|---|
| DocumentActivityId | int | Primary key |
| DocumentId | int | FK to Document; may be retained after soft deletion |
| ActingUserId | int | FK to User |
| Action | string | `Upload`, `Download`, `Update`, `Replace`, `Share`, or `Delete` |
| OccurredAt | DateTime | UTC action timestamp |
| Details | string? | Optional bounded metadata or share target |

### Notification

Already exists in the app and will be used to notify users when they receive a document share or relevant project update.

### DocumentReportSummary

Read-only reporting projection; not persisted as a source-of-truth entity.

| Field | Type | Notes |
|---|---|---|
| ReportWindowStart | DateTime | UTC reporting period start |
| ReportWindowEnd | DateTime | UTC reporting period end |
| ActiveUploaderSummaries | collection | User ID, display name, upload count |
| CategorySummaries | collection | Category, document count |
| MimeTypeSummaries | collection | MIME type, document count |
| AccessPatternSummaries | collection | Document ID or category, download/access count, distinct acting-user count |
| IsAdministratorOnly | bool | The projection must only be returned to an administrator |

The reporting service derives this projection from `Document`, `DocumentShare`, `ProjectMember`, `User`, and `DocumentActivity`. Counts include only records permitted by the report authorization policy.

## Relationships

- `User` to `Document`: one-to-many, uploaded by user
- `Project` to `Document`: one-to-many, optional project association
- `TaskItem` to `Document`: one-to-many, optional task association
- `Document` to `DocumentShare`: one-to-many, explicit access grants
- `User` to `DocumentShare`: one-to-many, recipient and actor relationships
- `Document` to `DocumentActivity`: one-to-many, audit history
- `User` to `DocumentActivity`: one-to-many, actor history
- `ProjectMember.Role` to `DocumentShare.TeamRole`: team resolution only; no standalone team entity

## Validation Rules

- `Title` is required.
- `Category` must be one of the approved values.
- `DocumentId` must be integer keyed, not GUID-based.
- `FileType` must support long MIME strings for Office documents.
- `FilePath` must be generated before database insert and should be unique.
- Access is allowed only when the document is owned by the user, explicitly shared, or associated with a project they can view.
- `TaskId`, when present, must reference a task whose `ProjectId` equals the document's `ProjectId`.
- The same approved extension/MIME pair table used at upload and replacement is used for validation.
- Database-save failure after file write requires compensating file deletion and must not expose an upload success.
