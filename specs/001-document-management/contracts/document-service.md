# Document Service Contract

This feature is integrated into Blazor Server rather than exposing a separate REST API. These application-service contracts define the boundary between Pages and domain behavior.

## UploadDocumentAsync

**Input**
- `requestingUserId: int`
- `title: string`
- `category: string`
- `description: string?`
- `projectId: int?`
- `taskId: int?`
- `tags: string?`
- `fileName: string`
- `contentType: string`
- `fileStream: Stream`
- `fileSizeBytes: long`
- `scanService: IMalwareScanService` is resolved by the application and is not caller-supplied

**Preconditions**
- User is authenticated.
- Title and category are present.
- Category is one of the six approved values.
- File size is greater than zero and at most 25 MB.
- Extension/MIME matches one of the approved pairs: PDF, Word (.doc/.docx), Excel (.xls/.xlsx), PowerPoint (.ppt/.pptx), text (.txt), or image (.png/.jpg/.jpeg/.gif). Missing, mismatched, or unsupported MIME information is rejected.
- If project-scoped, user is the project manager or a project member.
- If task-scoped, task belongs to the project and the user is authorized for that project.
- `taskId` is optional and personal-document task associations are rejected.
- The file is submitted to the provider-neutral local malware scan hook before storage.
- Batch uploads are processed independently and return one result for each file.

**Result**
- Returns `Document` with a generated integer ID and GUID-based storage path.
- Records an `Upload` DocumentActivity event.

**Failure**
- Throws a validation exception for invalid metadata/type/size.
- Throws `UnauthorizedAccessException` for project/task access denial.
- Leaves neither a document metadata row nor orphan file after failure.
- Database persistence and `Upload` audit creation happen in one database transaction.
- If the database save fails after storage succeeds, delete the generated file. Return no success and retain no document/audit row.
- If compensating deletion fails, surface the operation failure and record the path plus failure for cleanup retry; do not report success.
- If the scan is unavailable, failed, threat-detected, errored, or indeterminate, reject the file before storage and return no file/document success.
- Valid files in a mixed batch remain successful even when other files fail validation or scanning.

## IMalwareScanService

**Input**
- `fileName: string`
- `contentType: string`
- `fileStream: Stream`

**Result**
- Returns a provider-neutral result containing `Clean`, `ThreatFound`, `Unavailable`, `Error`, or `Indeterminate`, scanner name, UTC timestamp, and bounded details.

**Precondition**
- The scan hook is available and the input stream is positioned for reading.

**Failure**
- Any outcome other than `Clean` is fail-closed and blocks storage.
- An unavailable or errored hook is not considered an implementation failure that may still allow upload.

## SearchDocumentsAsync

**Input**
- `requestingUserId: int`
- optional title/description/tag text
- optional uploader ID/name
- optional category
- optional project ID
- optional task ID
- sort field and bounded page

**Result**
- Authorized documents only: owner, project member/manager, or explicit user share.
- Direct team shares expand to the authorized project members.

## GetDocumentByIdAsync

**Input**
- `documentId: int`
- `requestingUserId: int`

**Result**
- Returns `Document` only when the centralized authorization policy permits access.
- Unknown or unauthorized IDs return no document (no existence disclosure).

## DownloadDocumentAsync / PreviewDocumentAsync

**Input**
- `documentId: int`
- `requestingUserId: int`

**Preconditions**
- Authorized through `GetDocumentByIdAsync`.
- Stored file exists and resolves below the local storage root.

**Result**
- Returns a stream and response metadata.
- Records `Download` activity once per successful download. Preview does not require a separate audit action.

## UpdateDocumentAsync

**Input**
- document ID, requesting user ID, title, description, category, tags, and optional project/task association.

**Precondition**
- Requester is the document owner or project manager for the document's project.
- If project/task association changes, requester is authorized for the destination project/task.

**Result**
- Updates metadata and `UpdatedDate`.
- Records an `Update` DocumentActivity event.

## ReplaceDocumentAsync

**Input**
- document ID, requesting user ID, file name, content type, stream, and size.

**Precondition**
- Same authorization as metadata update.
- File passes the upload validation rules.

**Result**
- Writes the replacement under a GUID-based path.
- Updates document metadata and records `Replace`.
- Removes the old file after successful metadata update; compensating cleanup is required on failures.

## ShareDocumentAsync

**Input**
- `documentId: int`
- `requestingUserId: int`
- exactly one of `recipientUserId: int?` or `recipientTeamRole: string?`
- optional reason

**Precondition**
- Requester is owner or project manager.
- The document is project-associated.
- A user recipient is the project manager or a current project member.
- A team recipient resolves to at least one project member or project-manager role with the requested role.

**Result**
- Creates a share record and an in-app notification for each recipient.
- Records a `Share` DocumentActivity event.

**Upload notification rule**
- After a project document upload succeeds, create one in-app notification for each eligible project member, excluding the uploader.

**Failure**
- Rejects personal-document, missing-project, unknown-user, or out-of-project team/user shares.

## DeleteDocumentAsync

**Input**
- `documentId: int`
- `requestingUserId: int`

**Precondition**
- Requester is owner or project manager.

**Result**
- Permanently removes the document record and physical file, records a bounded `Delete` audit event, and prevents direct retrieval.

**Failure**
- Unknown or unauthorized document is false/no-op and is not exposed as existence information.

## GetDocumentReportAsync

**Input**
- `requestingUserId: int`
- `windowStart: DateTime`
- `windowEnd: DateTime`

**Preconditions**
- Requesting user has the existing application administrator role.
- Reporting window is bounded and end is not earlier than start.

**Result**
- Returns a read-only `DocumentReportSummary` projection containing:
  - active uploader user ID, display name, and upload count;
  - category and MIME-type document counts;
  - authorized document access/download counts and distinct acting-user counts derived from `DocumentActivity`.

**Failure**
- Non-administrator requests are rejected as unauthorized.
- The projection is derived, not persisted, and does not introduce a public or unaudited report table.
