using System.CommandLine;

public static class TagCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var tagArg = new Argument<string>("tag") { Description = "Tag name" };

        var cmd = new Command("tag", "Add a tag to an issue");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(tagArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var tag = parseResult.GetValue(tagArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).ApplyCommandAsync(issueId, $"tag {tag}");
            Console.WriteLine($"Tagged {issueId} with \"{tag}\".");
        }));

        return cmd;
    }
}
