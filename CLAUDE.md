# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`yt` is a standalone CLI tool for YouTrack written in C# targeting .NET 10. Source lives under `src/`.

## Commands

```bash
# Build
dotnet build src/yt.csproj

# Run
dotnet run --project src/yt.csproj -- <args>

# Pack as global tool
dotnet pack src/yt.csproj -c Release -o nupkg

# Add a NuGet package (always specify nuget.org source — there is a private Feedz feed in NuGet.Config that requires auth)
dotnet add src/yt.csproj package <PackageName> --source https://api.nuget.org/v3/index.json
```

## Architecture

### Entry point
**`src/Program.cs`** — creates the root command and registers all subcommands. Adding a new command means creating a file in `src/Commands/` and adding one `Subcommands.Add()` line here.

### Key files
- **`src/YouTrackClient.cs`** — all YouTrack REST API calls. Uses a shared `GetAsync<T>` / `EnsureSuccessAsync` pattern that throws `YouTrackException` on API errors. `HttpClient` has a 30s timeout; `TaskCanceledException` is caught in `CommandHelper`.
- **`src/Config.cs`** — saves/loads `BaseUrl` + `ApiKey` to `%APPDATA%/yt/config.json` (Windows) or `~/.config/yt/config.json` (macOS/Linux) via `Environment.SpecialFolder.ApplicationData`.
- **`src/CommandHelper.cs`** — `Cmd.RunAsync` wraps every command action, catching `YouTrackException` and `TaskCanceledException` and printing clean errors to stderr.

### Commands folder
Each file in `src/Commands/` exports a single `static Command Build()` method:

| File | CLI command |
|---|---|
| `AuthCommand.cs` | `yt auth <base-url> <api-key>` |
| `LogoutCommand.cs` | `yt logout` |
| `MeCommand.cs` | `yt me` |
| `SearchCommand.cs` | `yt search [query] [-n] [--recent]` |
| `ViewCommand.cs` | `yt view <id> [-c] [-i]` |
| `OpenCommand.cs` | `yt open <id>` |
| `CreateCommand.cs` | `yt create [project] [summary] [--description]` |
| `CommentCommand.cs` | `yt comment <id> <text>` |
| `AssignCommand.cs` | `yt assign <id> <user>` |
| `ApplyCommand.cs` | `yt command <id> <command>` |
| `ProjectsCommand.cs` | `yt projects` |

### Models (defined at bottom of `YouTrackClient.cs`)
- `Issue` — search result (id, idReadable, summary, customFields)
- `IssueDetail` — full issue with description, reporter, customFields
- `IssueComment` — text, author, created timestamp (Unix ms)
- `IssueAttachment` — name, mimeType, url
- `CustomField` — name + `JsonElement?` value with `DisplayValue` property
- `MeUser`, `YtProject` — for `me` and `projects` commands

### CustomField deserialization
YouTrack custom field values are polymorphic (objects, arrays, strings, numbers). `CustomField.Value` is `JsonElement?` and `DisplayValue` handles all cases via a `ValueKind` switch. Always request `value(name,login,fullName)` in the fields parameter — requesting just `value` returns only `$type` with no data.

### System.CommandLine 3.0 preview API
This version differs from 2.x:

```csharp
var arg = new Argument<string>("name") { Description = "..." };
command.Arguments.Add(arg);

var opt = new Option<bool>("--flag") { Description = "..." };
command.Options.Add(opt);

rootCommand.Subcommands.Add(subCommand);

// Async action (preferred)
command.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
{
    var val = parseResult.GetValue(arg);
}));

// Optional argument
var arg = new Argument<string?>("name") { Arity = ArgumentArity.ZeroOrOne };
```

### Inline image rendering
`ViewCommand` uses the iTerm2 inline image protocol (`\x1b]1337;File=...`) to display image attachments in terminals that support it (WezTerm, iTerm2). Only rendered when `--images` / `-i` is passed and stdout is not redirected.

### Piped output
`ViewCommand` detects `Console.IsOutputRedirected` and switches to clean markdown output (suitable for `glow`) instead of colorized output. Image rendering is skipped when piped.
