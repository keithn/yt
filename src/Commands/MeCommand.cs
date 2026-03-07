using System.CommandLine;

public static class MeCommand
{
    public static Command Build()
    {
        var cmd = new Command("me", "Show the authenticated user");
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var me = await new YouTrackClient(Config.LoadOrThrow()).GetMeAsync();
            Console.WriteLine($"{me.FullName ?? me.Login}");
            if (me.Email is not null)
                Console.WriteLine($"  {me.Email}");
            Console.WriteLine($"  {me.Login}");
        }));

        return cmd;
    }
}
