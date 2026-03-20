using System.CommandLine;

public static class WorklogCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var durationArg = new Argument<string>("duration") { Description = "Time spent in YouTrack duration format (e.g. \"1h 30m\", \"45m\", \"2h\", \"1d\"). Work is recorded at the current date and time." };
        var descriptionOpt = new Option<string?>("--description", "-d") { Description = "Work description" };

        var cmd = new Command("worklog", "Log time spent on an issue");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(durationArg);
        cmd.Options.Add(descriptionOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var duration = parseResult.GetValue(durationArg)!;
            var description = parseResult.GetValue(descriptionOpt);
            await new YouTrackClient(Config.LoadOrThrow()).LogWorkAsync(issueId, duration, description);
            Console.WriteLine($"Logged {duration} on {issueId}.");
        }));

        return cmd;
    }
}
