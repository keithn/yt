using System.CommandLine;

public static class UnlinkCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var linkTypeArg = new Argument<string>("link-type") { Description = "Link type name to remove (e.g. \"depends on\", \"relates to\", \"duplicates\"). Must match the type used when the link was created." };
        var targetArg = new Argument<string>("target-id") { Description = "Target issue ID (e.g. PROJ-456)" };

        var cmd = new Command("unlink", "Remove a link between two issues");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(linkTypeArg);
        cmd.Arguments.Add(targetArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var linkType = parseResult.GetValue(linkTypeArg)!;
            var targetId = parseResult.GetValue(targetArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).ApplyCommandAsync(issueId, $"remove {linkType} {targetId}");
            Console.WriteLine($"Unlinked {issueId} from {targetId}.");
        }));

        return cmd;
    }
}
