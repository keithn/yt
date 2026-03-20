using System.CommandLine;

var rootCommand = new RootCommand("yt - YouTrack CLI tool");

rootCommand.Subcommands.Add(AuthCommand.Build());
rootCommand.Subcommands.Add(LogoutCommand.Build());
rootCommand.Subcommands.Add(MeCommand.Build());
rootCommand.Subcommands.Add(SearchCommand.Build());
rootCommand.Subcommands.Add(ViewCommand.Build());
rootCommand.Subcommands.Add(OpenCommand.Build());
rootCommand.Subcommands.Add(CreateCommand.Build());
rootCommand.Subcommands.Add(UpdateCommand.Build());
rootCommand.Subcommands.Add(CommentCommand.Build());
rootCommand.Subcommands.Add(EditCommentCommand.Build());
rootCommand.Subcommands.Add(AssignCommand.Build());
rootCommand.Subcommands.Add(ApplyCommand.Build());
rootCommand.Subcommands.Add(LinkCommand.Build());
rootCommand.Subcommands.Add(UnlinkCommand.Build());
rootCommand.Subcommands.Add(TagCommand.Build());
rootCommand.Subcommands.Add(UntagCommand.Build());
rootCommand.Subcommands.Add(WorklogCommand.Build());
rootCommand.Subcommands.Add(AttachCommand.Build());
rootCommand.Subcommands.Add(ProjectsCommand.Build());
rootCommand.Subcommands.Add(ApiCommand.Build());

return await rootCommand.Parse(args).InvokeAsync();
