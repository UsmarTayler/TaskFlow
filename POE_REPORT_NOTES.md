# POE Part 1 — Report Notes (paste into your Word doc)

1) Reason for Design Choice (MVC)
- MVC selected per brief; server-rendered Razor views keep the UI simple and testable.
- Non-functional requirement satisfied by using dummy provider and JS alerts (no POST, no persistence).

2) Database Structure (in words)
- Entities: Lecturer, Claim, SupportingDocument, ClaimStatus.
- Relationships: Lecturer 1..* Claims; Claim 0..* SupportingDocuments.
- Key fields: Claim(ClaimId, LecturerName, Month, HoursWorked, HourlyRate, Description, Status, SubmissionDate).
- Part 2 will implement SQL persistence using EF Core.

3) GUI Layout (reasoning)
- Navigation mirrors workflow and roles:
  * Submit Claim (form)
  * Claim Status (table with optional search)
  * Admin Review (pending list + simulated actions)

4) Assumptions / Constraints
- Assumptions: one claim per month; digital documents; admins verify manually.
- Constraints: UI-only; buttons do not modify data; no authentication; Bootstrap layout.

5) UML Diagram
- Include your draw.io UML and a short mapping to the entities above.

6) Project Plan
- Table/Gantt covering: setup MVC, create models, seed dummy provider, build views, documentation, commits.

7) Repo Link
- Add your GitHub URL after pushing this project.

8) References
- .NET docs, Bootstrap docs, any other resources used.

9) AI Declaration
- You used AI to scaffold non-functional UI and draft documentation; you reviewed and tested everything yourself.
