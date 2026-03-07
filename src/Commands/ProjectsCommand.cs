using System.CommandLine;

public static class ProjectsCommand
{
    public static Command Build()
    {
        var cmd = new Command("projects", "List available YouTrack projects");
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var projects = await new YouTrackClient(Config.LoadOrThrow()).GetProjectsAsync();
            if (projects.Count == 0)
            {
                Console.WriteLine("No projects found.");
                return;
            }
            foreach (var p in projects.OrderBy(p => p.ShortName))
                Console.WriteLine($"{p.ShortName,-15} {p.Name}");
        }));

        return cmd;
    }
}
