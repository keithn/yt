using System.CommandLine;

public static class LogoutCommand
{
    public static Command Build()
    {
        var cmd = new Command("logout", "Remove stored authentication credentials");
        cmd.SetAction((parseResult) =>
        {
            Config.Remove();
            Console.WriteLine("Credentials removed.");
        });

        return cmd;
    }
}
