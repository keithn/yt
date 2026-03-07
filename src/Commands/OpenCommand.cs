using System.CommandLine;
using System.Diagnostics;

public static class OpenCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };

        var cmd = new Command("open", "Open an issue in the browser");
        cmd.Arguments.Add(issueArg);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var config = Config.LoadOrThrow();
            var url = $"{config.BaseUrl.TrimEnd('/')}/issue/{parseResult.GetValue(issueArg)}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            await Task.CompletedTask;
        }));

        return cmd;
    }
}
