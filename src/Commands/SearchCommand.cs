using System.CommandLine;

public static class SearchCommand
{
    public static Command Build()
    {
        var queryArg = new Argument<string?>("query")
        {
            Description = "YouTrack query language expression (default: assignee: me). Examples: 'project: PROJ state: Open', 'assignee: john.doe priority: Critical', '#PROJ-123'. Supports full YouTrack query syntax.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var recentOpt = new Option<bool>("--recent") { Description = "Sort results by most recently updated first" };
        var topOpt = new Option<int>("--top", "-n") { Description = "Maximum number of results to return (default: 20)", DefaultValueFactory = _ => 20 };

        var cmd = new Command("search", "Search for issues using YouTrack query language. Output columns: ID, State, Summary.");
        cmd.Arguments.Add(queryArg);
        cmd.Options.Add(recentOpt);
        cmd.Options.Add(topOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var query = parseResult.GetValue(queryArg) ?? "assignee: me";
            if (parseResult.GetValue(recentOpt))
                query += " sort by: {updated} desc";

            List<Issue> issues;
            try
            {
                issues = await new YouTrackClient(Config.LoadOrThrow()).SearchAsync(query, parseResult.GetValue(topOpt));
            }
            catch (YouTrackException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"YouTrack query error: {ex.Message}");
                Console.ResetColor();
                Console.Error.WriteLine($"  Query: {query}");
                return;
            }

            if (issues.Count == 0)
            {
                Console.WriteLine("No issues found.");
                return;
            }
            foreach (var issue in issues)
                Console.WriteLine($"{issue.IdReadable,-15} {issue.State ?? "",-18} {issue.Summary}");
        }));

        return cmd;
    }
}
