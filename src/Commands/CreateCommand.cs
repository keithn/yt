using System.CommandLine;

public static class CreateCommand
{
    public static Command Build()
    {
        var projectArg = new Argument<string?>("project") { Description = "Project short name (e.g. PROJ)", Arity = ArgumentArity.ZeroOrOne };
        var summaryArg = new Argument<string?>("summary") { Description = "Issue summary", Arity = ArgumentArity.ZeroOrOne };
        var descriptionOpt = new Option<string?>("--description") { Description = "Issue description" };
        var typeOpt = new Option<string?>("--type") { Description = "Issue type (e.g. Bug, Feature)" };

        var cmd = new Command("create", "Create a new issue");
        cmd.Arguments.Add(projectArg);
        cmd.Arguments.Add(summaryArg);
        cmd.Options.Add(descriptionOpt);
        cmd.Options.Add(typeOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var project = parseResult.GetValue(projectArg) ?? Prompt("Project short name (run 'yt projects' to list)");
            var summary = parseResult.GetValue(summaryArg) ?? Prompt("Summary");
            var type = parseResult.GetValue(typeOpt) ?? PromptOptionalSingle("Type (e.g. Bug, Feature, leave blank to skip)");
            var description = parseResult.GetValue(descriptionOpt) ?? PromptOptional("Description (optional)");

            var client = new YouTrackClient(Config.LoadOrThrow());
            var issue = await client.CreateIssueAsync(project, summary, description);

            if (!string.IsNullOrEmpty(type))
                await client.ApplyCommandAsync(issue.IdReadable, $"type {type}");

            Console.WriteLine($"Created {issue.IdReadable}: {issue.Summary}");
        }));

        return cmd;
    }

    private static string Prompt(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static string? PromptOptionalSingle(string label)
    {
        Console.Write($"{label}: ");
        var value = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? PromptOptional(string label)
    {
        Console.WriteLine($"{label} (type '.' on its own line to finish):");
        var lines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != ".")
        {
            if (line is null) break;
            lines.Add(line);
        }
        return lines.Count > 0 ? string.Join("\n", lines) : null;
    }
}
