using System.CommandLine;

public static class UpdateCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var summaryOpt = new Option<string?>("--summary", "-s") { Description = "New summary" };
        var descriptionOpt = new Option<string?>("--description", "-d") { Description = "New description" };
        var typeOpt = new Option<string?>("--type") { Description = "Issue type as configured in the project (e.g. Bug, Feature, Task, Epic)" };
        var stateOpt = new Option<string?>("--state") { Description = "Issue state name as configured in the project workflow (e.g. Open, \"In Progress\", Resolved, Won't fix). Multi-word states must be quoted." };
        var fixVersionsOpt = new Option<string[]>("--fix-versions") { Description = "Fix versions to set, replacing all existing values (e.g. --fix-versions 1.0 2.0). Pass the flag with no values to clear all fix versions. Missing versions are created automatically.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
        var moveOpt = new Option<string?>("--move") { Description = "Move the issue to another project by its short name (e.g. OTHER). Run 'yt projects' to list available project short names." };
        var commandOpt = new Option<string?>("--command", "-c") { Description = "Raw YouTrack command string for any field not covered by other options (e.g. \"priority Critical\", \"assignee john.doe\")" };

        var cmd = new Command("update", "Update an existing issue");
        cmd.Arguments.Add(issueArg);
        cmd.Options.Add(summaryOpt);
        cmd.Options.Add(descriptionOpt);
        cmd.Options.Add(typeOpt);
        cmd.Options.Add(stateOpt);
        cmd.Options.Add(fixVersionsOpt);
        cmd.Options.Add(moveOpt);
        cmd.Options.Add(commandOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var summary = parseResult.GetValue(summaryOpt);
            var description = parseResult.GetValue(descriptionOpt);
            var type = parseResult.GetValue(typeOpt);
            var state = parseResult.GetValue(stateOpt);
            var fixVersionsResult = parseResult.GetResult(fixVersionsOpt);
            var fixVersions = parseResult.GetValue(fixVersionsOpt);
            var move = parseResult.GetValue(moveOpt);
            var command = parseResult.GetValue(commandOpt);

            if (summary is null && description is null && type is null && state is null
                && fixVersionsResult is null && move is null && command is null)
                throw new YouTrackException("Specify at least one option to update.");

            var client = new YouTrackClient(Config.LoadOrThrow());

            if (summary is not null || description is not null)
                await client.UpdateIssueAsync(issueId, summary, description);

            if (type is not null)
                await client.ApplyCommandAsync(issueId, $"type {type}");

            if (state is not null)
                await client.ApplyCommandAsync(issueId, $"state {state}");

            if (fixVersionsResult is not null)
                await client.UpdateFixVersionsAsync(issueId, fixVersions ?? []);

            if (move is not null)
                await client.MoveIssueAsync(issueId, move);

            if (command is not null)
                await client.ApplyCommandAsync(issueId, command);

            Console.WriteLine($"Updated {issueId}.");
        }));

        return cmd;
    }
}
