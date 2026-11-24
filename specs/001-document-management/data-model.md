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

`Document` has no persisted `IsDeleted` field. Confirmed deletion permanently removes the row and file; soft-delete and recovery state are out of scope.

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
| DocumentId | int? | Nullable retained document identity after permanent document-row removal |
| ActingUserId | int | FK to User |
| Action | string | `Upload`, `Download`, `Update`, `Replace`, `Share`, or `Delete` |
| OccurredAt | DateTime | UTC action timestamp |
| Details | string? | Optional metadata, share target, or deletion context; max 500 characters and must exclude file contents and sensitive metadata |

`DocumentId` is a nullable historical scalar for deletion events, not a foreign key to `Document`; deleting the document must not cascade-delete its accountability event. Events retain only document identity, acting user, action, UTC timestamp, and the minimal bounded context required for accountability. No audit-retention duration is defined in the stakeholder brief; do not introduce an automatic purge until a retention policy is approved.

### MalwareScanResult

Read-only service contract value; not necessarily persisted as a source-of-truth entity.

| Field | Type | Notes |
|---|---|---|
| Outcome | enum/text | `Clean`, `ThreatFound`, `Unavailable`, `Error`, or `Indeterminate` |
| ScannerName | string | Provider-neutral configured scanner name |
| OccurredAt | DateTime | UTC scan timestamp |
| Details | string? | Optional bounded operational error/threat category without file content |

An upload may proceed only when a functioning configured local scanner actually scanned the file and returned `Clean`. A no-op hook, test fake, or fabricated `Clean` result does not satisfy the requirement. `ThreatFound`, `Unavailable`, `Error`, and `Indeterminate` must reject the file and leave no file or document metadata behind. The current MVP has no functioning scanner and remains pending this requirement.

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
- `Document` to `DocumentActivity`: operational audit history while the document exists; Delete activity may retain a nullable historical `DocumentId` after the document row is removed
- `User` to `DocumentActivity`: one-to-many, actor history
- `ProjectMember.Role` to `DocumentShare.TeamRole`: team resolution only; no standalone team entity

## Authorization Scope

- Owner, project-member, project-manager, Team Lead, and administrator checks are enforced server-side for every read and mutation.
- Team Lead access is determined from the authenticated Team Lead role and matching Department claim between the Team Lead and document uploader. A Department claim does not itself grant project membership or task-association rights.
- Administrators may view, preview, download, update, replace, and delete any document. Mutations are audited.
- Neither Team Lead nor administrator status relaxes the direct-share rule: recipients of direct project-document shares must already be authorized for that project.
- Shared with Me is a query/view over explicit shares and returns only documents the current recipient remains authorized to access; it is not a separate source of truth.

## Validation Rules

- `Title` is required.
- `Category` must be one of the approved values.
- `DocumentId` must be integer keyed, not GUID-based.
- `FileType` must support long MIME strings for Office documents.
- `FilePath` must be generated before database insert and should be unique.
- Access is allowed only when the server-side policy grants owner, project, Team Lead Department, administrator, or eligible explicit-share access.
- `TaskId`, when present, must reference a task whose `ProjectId` equals the document's `ProjectId`. Project members and project managers may create this association only when authorized for the project.
- A mixed-validity batch uses independent document records; each file is validated and has an individual result.
- A functioning local scanner must scan the file and return `Clean` before file storage. A no-op or test-only hook is not a successful scan. Unavailable or failed scans are rejection failures, not successful uploads.
- Permanent deletion keeps only bounded `DocumentActivity`; historical `DocumentId` must not retain a relationship to a deleted Document row.
- The same approved extension/MIME pair table used at upload and replacement is used for validation.
- Database-save failure after file write requires compensating file deletion and must not expose an upload success.
