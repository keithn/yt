using System.CommandLine;

public static class ApplyCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var commandArg = new Argument<string>("command") { Description = "YouTrack command to apply (e.g. \"state In Progress\")" };

        var cmd = new Command("command", "Apply a YouTrack command to an issue");
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
