# EMPTRACK role authorization and negative-test matrix

The role permissions below are the intended source-level policy. **No authenticated HTTP tests were executed** in this environment; scenarios are marked Not Run until tested against a running app and controlled database.

## Role-by-role permissions

| Module / action | Admin | HR Head | Head Manager | Manager | HR | Employee |
|---|---|---|---|---|---|---|
| Employee create | Allowed | HR scope | Own department, Employee role only | Denied | Denied | Denied |
| Employee profile edit | Permitted fields; generated code/serial immutable | Denied | Denied | Denied | Own email/phone only | Own email/phone only |
| Employee read | All, including inactive | Authorized department scope | Own department | Own team | Own record | Own record |
| Team create | Denied | HR scope | Own department | Denied | Read-only | Read-only |
| Team edit/delete | Allowed; delete blocked on references | HR scope | Own department | Read-only | Read-only | Read-only |
| Leave requests | View/approve/reject; cannot request personally | Authorized HR scope and approval authority | Own department and approval authority; no self-approval | View team requests; cannot approve/reject | Own requests | Own requests |
| Leave balances | View only; no manual create/edit | Department scope, view only | No manual balance writes | No manual balance writes | Department scope, view only | Own balances only |
| Attendance | View all; no self check-in/out | HR scope and own self-service | Department scope and own self-service | Team scope and own self-service | Own record | Own record |
| Salary payments | Create/edit/view all; delete denied | Own history | Own history | Own history | Own history | Own history |
| Holidays | Create/edit/delete/view | View only | View only | View only | View only | View only |
| Tasks | View only | Scoped read | Manage own department tasks | Manage own team tasks | Scoped read only | Assigned task status only |
| Performance reviews | View all; administrative management | Own, reviewer, or department-scoped read; eligible reviewer management | Own, reviewer, or department-scoped read; eligible reviewer management | Own, reviewer, or team-scoped read; eligible reviewer management | Own, reviewer, or department-scoped read; eligible reviewer management | Own review read only |
| Concerns | View/status/delete; no arbitrary content edit | HR scope/status | Department scope | Team scope | HR scope/status | Own create/view/edit/delete where permitted |
| Announcements | Create/edit/delete; all audiences | Audience-filtered read | Audience-filtered read | Audience-filtered read | Audience-filtered read | Audience-filtered read |
| Notifications | Individual/group send to all active employees; read own | Individual/group send in HR scope; read own | Individual/group send in department; read own | Individual/group send in team; read own | Read own; no create | Read own; no create |

## Required negative tests — Not Run

| # | Scenario | Expected result | Actual result |
|---:|---|---|---|
| 1 | Employee changes employee ID to view another employee | NotFound/Forbid; no data | Not Run — runtime unavailable |
| 2 | Head Manager posts another department or unauthorized role when creating employee | Validation failure; no write | Not Run — runtime unavailable |
| 3 | Non-Admin creates/edits/deletes salary | Forbid; no write | Not Run — runtime unavailable |
| 4 | Non-Admin opens another employee's salary detail | Forbid; no salary data | Not Run — runtime unavailable |
| 5 | Manager attempts leave approval/rejection | Forbid/denied; no balance change | Not Run — runtime unavailable |
| 6 | Head Manager attempts own leave approval | Denied | Not Run — runtime unavailable |
| 7 | Duplicate leave approval request submitted | Only one approval/deduction | Not Run — SQL Server unavailable |
| 8 | User manipulates another employee's attendance | NotFound/Forbid; no write | Not Run — runtime unavailable |
| 9 | Admin attempts attendance check-in | Denied | Not Run — runtime unavailable |
| 10 | Assignee posts task content fields to status endpoint | Only status changes | Not Run — runtime unavailable |
| 11 | User changes concern ID to access another concern | NotFound/Forbid | Not Run — runtime unavailable |
| 12 | Unauthorized user changes concern status or deletes it | Forbid; no write | Not Run — runtime unavailable |
| 13 | User opens announcement for another department | NotFound; no content | Not Run — migration/database required |
| 14 | Notification recipient changes another recipient's read status | NotFound/Forbid; no write | Not Run — runtime unavailable |
| 15 | User accesses another department/team's records | NotFound/Forbid or scoped list | Not Run — runtime unavailable |
| 16 | Deactivated employee reuses an existing session | Session rejected | Not Run — runtime unavailable |
| 17 | User opens account reset-password URL/POST | Forbid | Not Run — runtime unavailable |
| 18 | Ordinary user opens DatabaseTestController | Forbid | Not Run — runtime unavailable |
| 19 | Duplicate holiday date submitted | Validation failure; DB index after migration | Not Run — runtime/database unavailable |
| 20 | Team with employee/history references is deleted | Deletion blocked | Not Run — runtime/database unavailable |
| 21 | Leave accrual runs repeatedly for same period | No duplicate accrual | Not Run — SQL Server unavailable |
| 22 | Historical employee/leave/attendance/review/concern records remain intact and scoped | Records preserved; unauthorized reads denied | Not Run — runtime/database unavailable |

Static source review and delimiter checks passed, but do not mark any of these HTTP/database scenarios as passed until executed with test accounts for all six roles.
