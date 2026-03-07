using System.CommandLine;

public static class CreateCommand
{
    public static Command Build()
    {
        var projectArg = new Argument<string?>("project") { Description = "Project short name (e.g. PROJ)", Arity = ArgumentArity.ZeroOrOne };
        var summaryArg = new Argument<string?>("summary") { Description = "Issue summary", Arity = ArgumentArity.ZeroOrOne };
        var descriptionOpt = new Option<string?>("--description") { Description = "Issue description" };

        var cmd = new Command("create", "Create a new issue");
        cmd.Arguments.Add(projectArg);
        cmd.Arguments.Add(summaryArg);
        cmd.Options.Add(descriptionOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var project = parseResult.GetValue(projectArg) ?? Prompt("Project");
            var summary = parseResult.GetValue(summaryArg) ?? Prompt("Summary");
            var description = parseResult.GetValue(descriptionOpt) ?? PromptOptional("Description (optional)");

            var issue = await new YouTrackClient(Config.LoadOrThrow()).CreateIssueAsync(project, summary, description);
            Console.WriteLine($"Created {issue.IdReadable}: {issue.Summary}");
        }));

        return cmd;
    }

    private static string Prompt(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static string? PromptOptional(string label)
    {
        Console.Write($"{label}: ");
        var value = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
