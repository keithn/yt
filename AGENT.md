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
yt view <issue-id> [--comments|-c]
```
- Shows fields, custom fields, reporter, and description
- `--comments` / `-c` also fetches comments with author and timestamp
- Pipe to `glow` for rendered markdown: `yt view WEB-123 -c | glow`

### Create an issue
```
yt create <project> <summary> [--description <text>]
```
- `project` is the short name (e.g. `WEB`, `PROJ`) — use `yt projects` to list them
- Prints the new issue ID on success

### Comment on an issue
```
yt comment <issue-id> <text>
```

### Assign an issue
```
yt assign <issue-id> <username>
```

### Apply a YouTrack command
```
yt command <issue-id> "<command>"
```
Commands use YouTrack's command language. Examples:
- `yt command WEB-1 "state In Progress"`
- `yt command WEB-1 "assignee john.doe priority Major"`
- `yt command WEB-1 "fix version 6.128.0"`

Multiple fields can be set in a single command string.

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
yt command WEB-123 "state In Progress"
```

**Create and assign in one go:**
```
yt create WEB "Fix login crash" --description "Reproducible on empty email field"
yt assign WEB-456 john.doe
```

## Output and errors

- Errors print to stderr with the prefix `Error:` — do not retry blindly, surface the message to the user
- Query errors include the full query string for debugging
- All commands exit 0 on success
