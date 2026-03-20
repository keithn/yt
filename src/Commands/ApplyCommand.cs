using System.CommandLine;

public static class ApplyCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var commandArg = new Argument<string>("command") { Description = "YouTrack command string. Examples: \"state In Progress\", \"priority Critical\", \"type Bug\", \"fix versions 1.0\", \"assignee john.doe\", \"tag needs-review\". Multiple commands can be space-separated." };

        var cmd = new Command("command", "Apply a raw YouTrack command to an issue. Use this for any field update not covered by other yt commands.");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(commandArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).ApplyCommandAsync(issueId, parseResult.GetValue(commandArg)!);
            Console.WriteLine($"Command applied to {issueId}.");
        }));

        return cmd;
    }
}
