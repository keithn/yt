using System.CommandLine;

public static class AuthCommand
{
    public static Command Build()
    {
        var baseUrlArg = new Argument<string>("base-url") { Description = "YouTrack base URL (e.g. https://youtrack.example.com)" };
        var apiKeyArg = new Argument<string>("api-key") { Description = "YouTrack permanent token" };

        var cmd = new Command("auth", "Authenticate with YouTrack");
        cmd.Arguments.Add(baseUrlArg);
        cmd.Arguments.Add(apiKeyArg);
        cmd.SetAction(parseResult =>
        {
            Config.Save(new Config
            {
                BaseUrl = parseResult.GetValue(baseUrlArg)!,
                ApiKey = parseResult.GetValue(apiKeyArg)!
            });
            Console.WriteLine("Authentication saved.");
        });

        return cmd;
    }
}
