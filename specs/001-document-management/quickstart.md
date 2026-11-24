# Quickstart: Document Management Validation

## Goal

Verify the new feature works in the existing ContosoDashboard application without introducing a separate deployment host or cloud dependency.

## Prerequisites

- .NET 8 SDK installed
- SQLite connection configured in `ContosoDashboard/appsettings.json`
- App run through `dotnet run --project ContosoDashboard`
- Test packages restored from the solution
- A functioning local malware scanner configured through `IMalwareScanService` before any successful-upload acceptance run. The current MVP does not provide one; until implemented and verified, valid uploads must remain blocked and scanner acceptance remains pending.

## Validation Steps

1. From the repository root, run `dotnet test ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` and confirm the existing and document tests pass.
2. Run `dotnet run --project ContosoDashboard/ContosoDashboard.csproj`.
3. Open the local HTTPS URL and log in as a seeded user.
4. Confirm the configured scanner performs an actual local scan. A no-op hook, test fake, or fabricated Clean result does not permit the successful-upload checks below.
5. Navigate to `/documents`.
6. Upload a valid PDF, Office, text, or image file with a required title and one of the six categories. Confirm each file has visible progress and that each batch item reports its own result.
7. Verify oversize, zero-byte, missing-extension, unsupported-type, and missing-metadata submissions are rejected without a document record or orphan file.
8. Simulate a database-save failure after a successful file write and verify the generated file is deleted (or recorded for cleanup retry), no Document/audit row remains, and no success is reported.
9. Associate an upload with task 1 or 2 only when project 1 is selected; reject task/project mismatches and personal-task associations. Upload another document directly from the task detail page and verify it appears in both task and project context.
10. Associate a valid project document with project 1 and confirm users 2, 3, and 4 can retrieve it but an unrelated user cannot.
11. Verify a Team Lead can view/manage same-Department uploader documents and is denied for a different Department. Verify an administrator can view/manage all documents; exercise both through direct IDs and UI/service calls.
12. Request a project document by a known DocumentId while unauthorized; the service must return no document and no download stream.
13. As an owner or project manager, share with a project-authorized user or matching project role. Confirm an in-app notification and the recipient's Shared with Me entry.
14. Reject a direct share to a user or team not already authorized for the document's project. Team Lead/admin privileges must not bypass this rule.
15. Search and filter by title, description, tags, uploader, category, project, task, and inclusive date range. Confirm unauthorized records are absent.
16. Preview a PDF/image and download an authorized Office/text file. Verify `GetUrlAsync` can only produce a protected server-handler location and does not expose a public file path or bypass authorization.
17. Edit metadata, replace the file, and delete the document; confirm audit activity and that old file data is removed or unreachable.
18. Open the dashboard and confirm the widget shows the current user's five most recent uploads and the count includes only documents currently authorized for that user.
19. Submit a batch containing at least one valid and one invalid file; confirm valid files persist independently and invalid files receive individual rejection results.
20. Configure the scanner as unavailable or failed and confirm no file bytes, document metadata, or success result are created.
21. Confirm a real scanner's Clean result permits only approved file types, and ThreatFound, Error, or Indeterminate results reject the file. Test doubles may cover rejection branches only.
22. Confirm a project upload notification reaches project members except the uploader, and a direct share notification reaches each eligible recipient.
23. Confirm permanent deletion removes the file and Document row while bounded audit events remain accessible only to authorized reporting; do not assume an unapproved automatic retention period.
24. Run the table-driven negative authorization suite and verify at least 100 unauthorized cases pass across read and mutation operations.
25. Open P3 reporting as an administrator and verify uploader, category/MIME, and access-pattern summaries; repeat as a non-administrator and confirm denial.

## Success Signals

- File upload succeeds while preserving metadata.
- Validation prevents oversized or invalid file types.
- Unauthorized access attempts fail even when a direct document ID is known.
- UI and service layers remain aligned with existing role permissions.
- Direct share recipients are project-authorized.
- Team Lead access is Department-scoped and administrator access is server-authorized for every operation.
- Shared with Me and task-detail uploads show only authorized project documents.
- Per-file progress, date-range filtering, and task-detail upload are covered by UI/service tests.
- Database metadata and local file cleanup remain consistent after success and failure.
- Mixed batches report valid and invalid files independently.
- A functioning local scanner is verified; no-op/test hooks do not qualify. Unavailable, failed, threat, or indeterminate outcomes always block upload.
- At least 100 negative authorization cases pass, including direct IDs and all document operations.
- P3 audit/reporting access is administrator-only.

## Pending Release Gates

- The current MVP has no functioning malware scanner. Successful-upload validation remains blocked until a real local scanner is implemented and verified; do not treat test-only `Clean` results as completion.
- An audit-retention duration has not been specified by the stakeholder. Do not implement time-based deletion until an authorized retention policy is documented.
