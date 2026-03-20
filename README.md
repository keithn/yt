# yt

A standalone CLI tool for [YouTrack](https://www.jetbrains.com/youtrack/) written in C# / .NET 10.

## Installation

Download the latest binary for your platform from the [Releases](../../releases/latest) page:

| Platform | File |
|---|---|
| Windows | `yt-win-x64.zip` |
| Linux | `yt-linux-x64` |

**Linux** — make the binary executable and move it onto your PATH:

```bash
chmod +x yt-linux-x64
mv yt-linux-x64 /usr/local/bin/yt
```

**Windows** — extract `yt.exe` from the zip and place it in a folder that is on your PATH.

## Setup

Get a permanent token from YouTrack: **Profile → Account Security → Tokens → New token**.

```bash
yt auth https://youtrack.example.com perm:your-token-here
```

Credentials are saved to `%APPDATA%\yt\config.json` (Windows) or `~/.config/yt/config.json` (macOS/Linux).

## Commands

### Search

```bash
yt search                                      # your open issues (default)
yt search "assignee: me state: -Deployed"
yt search "assignee: me" --recent              # sort by recently updated
yt search "project: WEB #unresolved" -n 50    # up to 50 results
```

Uses [YouTrack search syntax](https://www.jetbrains.com/help/youtrack/server/search-and-filter-issues.html) directly.

### View an issue

```bash
yt view WEB-123
yt view WEB-123 --comments       # include comments with IDs  (-c)
yt view WEB-123 --images         # display image attachments inline (-i)
yt view WEB-123 -c | glow        # pipe to glow for markdown rendering
```

### Create an issue

```bash
yt create WEB "Fix login crash"
yt create WEB "Fix login crash" --description "Happens on empty email" --type Bug
yt create                        # interactive prompts
```

### Update an issue

```bash
yt update WEB-123 --summary "New title"
yt update WEB-123 --state "In Progress"
yt update WEB-123 --type Bug
yt update WEB-123 --fix-versions 1.0 2.0   # replaces all fix versions (created if missing)
yt update WEB-123 --fix-versions            # clears all fix versions
yt update WEB-123 --move OTHER              # move to another project
yt update WEB-123 --command "priority Critical"   # raw YouTrack command
```

### Comment and edit comments

```bash
yt comment WEB-123 "Looking into this now"
yt view WEB-123 -c                          # comment IDs shown in [brackets]
yt edit-comment WEB-123 <comment-id> "Updated text"
```

### Assign

```bash
yt assign WEB-123 john.doe      # use login name (see: yt me)
yt assign WEB-123 unassigned    # clear assignee
```

### Link issues

```bash
yt link WEB-123 "depends on" WEB-456
yt link WEB-123 "duplicates" WEB-789
yt unlink WEB-123 "depends on" WEB-456
```

Link type names must match your YouTrack instance configuration exactly.

### Tags

```bash
yt tag WEB-123 needs-review
yt untag WEB-123 needs-review
```

### Log work

```bash
yt worklog WEB-123 "1h 30m"
yt worklog WEB-123 "45m" --description "Investigated root cause"
```

### Attach a file

```bash
yt attach WEB-123 ./screenshot.png
yt attach WEB-123 ./logs/error.log
```

### Apply a raw YouTrack command

```bash
yt command WEB-123 "state In Progress"
yt command WEB-123 "fix version 6.128.0 priority Major"
```

Multiple fields can be combined in a single command string.

### Other

```bash
yt open WEB-123      # open issue in browser
yt me                # show authenticated user
yt projects          # list available projects
yt logout            # remove stored credentials
```

## Using with AI agents

`yt` is designed to work well with AI coding agents (Claude Code, Cursor, Copilot, etc.). The [`AGENT.md`](./AGENT.md) file in this repo contains instructions written for agents — covering every command, output format, and common workflows.

### Claude Code

Add to your project's `CLAUDE.md` or your global `~/.claude/CLAUDE.md`:

```markdown
## YouTrack

Use the `yt` CLI to interact with YouTrack.

### Search
- `yt search` — my open issues (default)
- `yt search "assignee: me state: -Resolved"` — filter by state
- `yt search "project: WEB #unresolved" --recent -n 50` — sort by updated, up to 50 results
- Query uses YouTrack search syntax; negate values with `-` e.g. `state: -Deployed`
- Output columns: ID, State, Summary

### View
- `yt view WEB-123` — fields, custom fields, description
- `yt view WEB-123 -c` — include comments; comment IDs shown in [brackets] for use with edit-comment
- `yt view WEB-123 | glow` — pipe for markdown rendering

### Create
- `yt create WEB "Summary"` — create in project by short name
- `yt create WEB "Summary" --description "Detail" --type Bug`
- Use `yt projects` to list valid project short names

### Update
- `yt update WEB-123 --summary "New title"` — update summary
- `yt update WEB-123 --state "In Progress"` — update state (quote multi-word values)
- `yt update WEB-123 --type Bug` — update type
- `yt update WEB-123 --fix-versions 1.0 2.0` — set fix versions (replaces existing, creates if missing)
- `yt update WEB-123 --fix-versions` — clear all fix versions
- `yt update WEB-123 --move OTHER` — move to another project
- `yt update WEB-123 --command "priority Critical"` — raw YouTrack command

### Comments
- `yt comment WEB-123 "text"` — add a comment
- `yt edit-comment WEB-123 <comment-id> "new text"` — edit; get IDs from `yt view WEB-123 -c`

### Links and tags
- `yt link WEB-123 "depends on" WEB-456` — link issues (type must match YouTrack config)
- `yt unlink WEB-123 "depends on" WEB-456` — remove link
- `yt tag WEB-123 needs-review` / `yt untag WEB-123 needs-review`

### Work and attachments
- `yt worklog WEB-123 "1h 30m" --description "..."` — log time
- `yt attach WEB-123 ./file.png` — upload attachment

### Other
- `yt assign WEB-123 john.doe` — assign by login; `yt me` shows your login
- `yt open WEB-123` — open in browser
- `yt projects` — list projects (ShortName, Name)
- `yt command WEB-123 "..."` — apply raw YouTrack command string

### Errors
- Errors print to stderr prefixed with `Error:` — surface to the user, do not retry blindly
- Query errors include the full query string
```

### Cursor / Copilot / other agents

Add the contents of [`AGENT.md`](./AGENT.md) to your `.cursorrules`, system prompt, or agent context file.

## Building from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/your-org/yt
cd yt
dotnet run --project src/yt.csproj -- --help
```
