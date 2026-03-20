using System.CommandLine;

public static class AttachCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var fileArg = new Argument<string>("file") { Description = "Absolute or relative path to the file to upload as an attachment" };

        var cmd = new Command("attach", "Upload a file attachment to an issue");
        cmd.Arguments.Add(issueArg);
        cmd.Arguments.Add(fileArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var filePath = parseResult.GetValue(fileArg)!;
            await new YouTrackClient(Config.LoadOrThrow()).AttachFileAsync(issueId, filePath);
            Console.WriteLine($"Attached {Path.GetFileName(filePath)} to {issueId}.");
        }));

        return cmd;
    }
}
