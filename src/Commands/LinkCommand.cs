using System.CommandLine;

public static class LinkCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var linkTypeArg = new Argument<string>("link-type") { Description = "Link type name as configured in your YouTrack instance (e.g. \"depends on\", \"relates to\", \"duplicates\", \"is subtask of\"). Must match exactly. Multi-word types must be quoted." };
        var targetArg = new Argument<string>("target-id") { Description = "Target issue ID (e.g. PROJ-456)" };

        var cmd = new Command("link", "Link two issues together");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(linkTypeArg);
        cmd.Arguments.Add(targetArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var linkType = parseResult.GetValue(linkTypeArg)!;
            var targetId = parseResult.GetValue(targetArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).ApplyCommandAsync(issueId, $"{linkType} {targetId}");
            Console.WriteLine($"Linked {issueId} → {linkType} → {targetId}.");
        }));

        return cmd;
    }
}
