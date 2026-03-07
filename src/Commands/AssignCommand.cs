using System.CommandLine;

public static class AssignCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var userArg = new Argument<string>("user") { Description = "Username to assign to" };

        var cmd = new Command("assign", "Assign an issue to a user");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(userArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var user = parseResult.GetValue(userArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).ApplyCommandAsync(issueId, $"assignee {user}");
            Console.WriteLine($"Assigned {issueId} to {user}.");
        }));

        return cmd;
    }
}
