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
yt view WEB-123 --comments       # include comments  (-c)
yt view WEB-123 --images         # display image attachments inline (-i)
yt view WEB-123 -c | glow        # pipe to glow for markdown rendering
```

### Create an issue

```bash
yt create WEB "Fix login crash"
yt create WEB "Fix login crash" --description "Happens on empty email"
yt create                        # interactive prompts
```

### Comment, assign, and apply commands

```bash
yt comment WEB-123 "Looking into this now"
yt assign WEB-123 john.doe
yt command WEB-123 "state In Progress"
yt command WEB-123 "fix version 6.128.0 priority Major"
```

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
- `yt view WEB-123 --comments` / `-c` — include comments with author and timestamp
- `yt view WEB-123 | glow` — pipe for markdown rendering (use when piping, not interactive)

### Create
- `yt create WEB "Summary"` — create in project by short name
- `yt create WEB "Summary" --description "Detail"` — with description
- `yt create` — interactive prompts for project, summary, description
- Use `yt projects` to find the correct project short name

### Update
- `yt command WEB-123 "state In Progress"` — apply a YouTrack command
- `yt command WEB-123 "assignee john.doe priority Major"` — multiple fields at once
- `yt command WEB-123 "fix version 6.128.0"` — set fix version
- `yt assign WEB-123 john.doe` — shorthand for assignee command
- `yt comment WEB-123 "text"` — add a comment

### Other
- `yt open WEB-123` — open in browser
- `yt projects` — list projects (ShortName, Name)
- `yt me` — show authenticated user

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
