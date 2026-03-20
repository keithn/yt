using System.CommandLine;

public static class EditCommentCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var commentArg = new Argument<string>("comment-id") { Description = "Comment ID (shown in brackets when running: yt view <id> -c)" };
        var textArg = new Argument<string>("text") { Description = "New comment text" };

        var cmd = new Command("edit-comment", "Edit an existing comment");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(commentArg);
        cmd.Arguments.Add(textArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var commentId = parseResult.GetValue(commentArg)!;
            var text = parseResult.GetValue(textArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).EditCommentAsync(issueId, commentId, text);
            Console.WriteLine($"Comment updated on {issueId}.");
        }));

        return cmd;
    }
}
