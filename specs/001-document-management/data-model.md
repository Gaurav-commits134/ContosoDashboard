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

Represents explicit access granted to another user.

| Field | Type | Notes |
|---|---|---|
| DocumentShareId | int | Primary key |
| DocumentId | int | FK to Document |
| UserId | int | Recipient user |
| SharedByUserId | int | User who granted access |
| SharedDate | DateTime | Audit timestamp |
| Reason | string? | Optional share note |

### Notification

Already exists in the app and will be used to notify users when they receive a document share or relevant project update.

## Relationships

- `User` to `Document`: one-to-many, uploaded by user
- `Project` to `Document`: one-to-many, optional project association
- `TaskItem` to `Document`: one-to-many, optional task association
- `Document` to `DocumentShare`: one-to-many, explicit access grants
- `User` to `DocumentShare`: one-to-many, recipient and actor relationships

## Validation Rules

- `Title` is required.
- `Category` must be one of the approved values.
- `DocumentId` must be integer keyed, not GUID-based.
- `FileType` must support long MIME strings for Office documents.
- `FilePath` must be generated before database insert and should be unique.
- Access is allowed only when the document is owned by the user, explicitly shared, or associated with a project they can view.
