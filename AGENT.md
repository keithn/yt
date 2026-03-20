# yt — Agent Instructions

`yt` is a CLI tool for interacting with YouTrack. Use it to search, view, create, and update issues on behalf of the user.

## Authentication

Auth is pre-configured by the user. If any command returns an auth error, ask the user to run:
```
yt auth <base-url> <api-key>
```

## Commands

### Search issues
```
yt search [query] [-n <count>] [--recent]
```
- `query` is optional — defaults to `assignee: me`
- Uses YouTrack query syntax: `assignee: me`, `project: WEB #unresolved`, `state: -Deployed`
- `--recent` sorts by last updated
- `-n` limits results (default 20)

Output columns: `ID  State  Summary`

### View an issue
```
yt view <issue-id> [--comments|-c] [--images|-i]
```
- Shows custom fields, reporter, and description
- `-c` fetches comments with author, timestamp, and comment ID in [brackets]
- Comment IDs shown with `-c` are required by `yt edit-comment`
- Piped output is clean markdown (suitable for `glow`)

### Create an issue
```
yt create <project> <summary> [--description <text>] [--type <type>]
```
- `project` is the short name (e.g. `WEB`) — use `yt projects` to list them
- `--type` sets the issue type (e.g. Bug, Feature, Task)
- Prints the new issue ID on success
- Omit args to use interactive prompts

### Update an issue
```
yt update <issue-id> [--summary <text>] [--description <text>] [--type <type>]
                     [--state <state>] [--fix-versions [v1 v2 ...]]
                     [--move <project>] [--command <command>]
```
- At least one option is required
- `--state` — quote multi-word values: `--state "In Progress"`
- `--fix-versions v1 v2` — replaces all existing fix versions; missing versions are created automatically
- `--fix-versions` with no values — clears all fix versions
- `--move OTHER` — moves issue to another project by short name
- `--command` — raw YouTrack command string for anything not covered above

### Comment on an issue
```
yt comment <issue-id> <text>
```
Supports YouTrack markdown.

### Edit a comment
```
yt edit-comment <issue-id> <comment-id> <text>
```
Get `comment-id` from `yt view <issue-id> -c` (shown in [brackets] after author and timestamp).

### Assign an issue
```
yt assign <issue-id> <login>
```
- `login` is the YouTrack login name (e.g. `john.doe`) — use `yt me` to see your own
- Use `unassigned` to clear the assignee

### Link issues
```
yt link <issue-id> <link-type> <target-id>
yt unlink <issue-id> <link-type> <target-id>
```
- `link-type` must match a link type configured in your YouTrack instance (e.g. `"depends on"`, `"relates to"`, `"duplicates"`, `"is subtask of"`)
- Quote multi-word link types

### Tags
```
yt tag <issue-id> <tag>
yt untag <issue-id> <tag>
```

### Log work
```
yt worklog <issue-id> <duration> [--description <text>]
```
- Duration format: `1h 30m`, `45m`, `2h`, `1d`
- Work is recorded at the current date and time

### Attach a file
```
yt attach <issue-id> <file>
```
- `file` is an absolute or relative path to the file to upload

### Apply a raw YouTrack command
```
yt command <issue-id> "<command>"
```
Commands use YouTrack's command language. Examples:
- `yt command WEB-1 "state In Progress"`
- `yt command WEB-1 "assignee john.doe priority Major"`
- `yt command WEB-1 "fix version 6.128.0"`

Multiple fields can be set in a single command string.

### Raw API call
```
yt api <method> <path> [--body|-b <json>]
```
- Makes an authenticated request directly against the YouTrack REST API using the stored credentials
- `path` is relative to the base URL (e.g. `/api/issues/PROJ-123?fields=id,summary`) or a full URL
- Response is always pretty-printed JSON written to stdout; HTTP status is written to stderr
- Exits with code 1 on non-2xx responses
- Use this for any endpoint not covered by other `yt` commands
- Examples:
  - `yt api GET "/api/issues/PROJ-123?fields=id,summary,customFields(name,value(name))"`
  - `yt api GET "/api/admin/projects?fields=id,shortName&$top=10"`
  - `yt api POST "/api/commands" --body '{"query":"priority Critical","issues":[{"idReadable":"PROJ-123"}]}'`
  - `yt api DELETE "/api/issues/PROJ-123/comments/79-12345"`
- **Windows git bash**: paths starting with `/` are converted by MSYS. Prefix the command with `MSYS_NO_PATHCONV=1` or use PowerShell

### Open in browser
```
yt open <issue-id>
```

### List projects
```
yt projects
```
Output columns: `ShortName  Name`

### Current user
```
yt me
```

## Common workflows

**Find what to work on:**
```
yt search "assignee: me state: -Resolved state: -Deployed" --recent
```

**Triage — view and update state:**
```
yt view WEB-123 -c
yt update WEB-123 --state "In Progress"
```

**Create, assign, and link:**
```
yt create WEB "Fix login crash" --description "Reproducible on empty email field" --type Bug
yt assign WEB-456 john.doe
yt link WEB-456 "depends on" WEB-100
```

**Log work and attach evidence:**
```
yt worklog WEB-123 "2h" --description "Debugged and fixed root cause"
yt attach WEB-123 ./logs/trace.log
```

**Update fix versions for a release:**
```
yt update WEB-123 --fix-versions 2.0.0 2.1.0
```

**Move a misrouted ticket:**
```
yt projects
yt update WEB-123 --move BACKEND
```

## Output and errors

- Errors print to stderr with the prefix `Error:` — do not retry blindly, surface the message to the user
- Query errors include the full query string for debugging
- All commands exit 0 on success
