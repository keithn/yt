using System.CommandLine;

var rootCommand = new RootCommand("yt - YouTrack CLI tool");

rootCommand.Subcommands.Add(AuthCommand.Build());
rootCommand.Subcommands.Add(LogoutCommand.Build());
rootCommand.Subcommands.Add(MeCommand.Build());
rootCommand.Subcommands.Add(SearchCommand.Build());
rootCommand.Subcommands.Add(ViewCommand.Build());
rootCommand.Subcommands.Add(OpenCommand.Build());
rootCommand.Subcommands.Add(CreateCommand.Build());
rootCommand.Subcommands.Add(CommentCommand.Build());
rootCommand.Subcommands.Add(AssignCommand.Build());
rootCommand.Subcommands.Add(ApplyCommand.Build());
rootCommand.Subcommands.Add(ProjectsCommand.Build());

return await rootCommand.Parse(args).InvokeAsync();
