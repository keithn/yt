using System.CommandLine;

public static class CommentCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var textArg = new Argument<string>("text") { Description = "Comment text. Supports YouTrack markdown. Pass as a single quoted argument for multi-line content." };

        var cmd = new Command("comment", "Add a comment to an issue");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(textArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            await new YouTrackClient(Config.LoadOrThrow()).AddCommentAsync(
                parseResult.GetValue(issueArg)!,
                parseResult.GetValue(textArg)!);
            Console.WriteLine("Comment added.");
        }));

        return cmd;
    }
}
