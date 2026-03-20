using System.CommandLine;

public static class UntagCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var tagArg = new Argument<string>("tag") { Description = "Tag name to remove" };

        var cmd = new Command("untag", "Remove a tag from an issue");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(tagArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var tag = parseResult.GetValue(tagArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).ApplyCommandAsync(issueId, $"remove tag {tag}");
            Console.WriteLine($"Removed tag \"{tag}\" from {issueId}.");
        }));

        return cmd;
    }
}
