using System.CommandLine;

public static class ApiCommand
{
    public static Command Build()
    {
        var methodArg = new Argument<string>("method") { Description = "HTTP method: GET, POST, PUT, DELETE, PATCH" };
        var pathArg = new Argument<string>("path") { Description = "API path relative to the YouTrack base URL (e.g. /api/issues/PROJ-123?fields=id,summary,customFields(name,value(name))). Full URLs are also accepted." };
        var bodyOpt = new Option<string?>("--body", "-b") { Description = "JSON request body (required for POST/PUT/PATCH)" };

        var cmd = new Command("api", "Make a raw authenticated API call against the configured YouTrack instance. Response is pretty-printed JSON. Use this to access any YouTrack REST endpoint not covered by other commands. Note: on Windows git bash, paths starting with / are converted by MSYS — prefix with MSYS_NO_PATHCONV=1 or use PowerShell.");
        cmd.Arguments.Add(methodArg);
        cmd.Arguments.Add(pathArg);
        cmd.Options.Add(bodyOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var method = parseResult.GetValue(methodArg)!;
            var path = parseResult.GetValue(pathArg)!;
            var body = parseResult.GetValue(bodyOpt);

            var (statusCode, response) = await new YouTrackClient(Config.LoadOrThrow()).RawAsync(method, path, body);

            var isSuccess = statusCode >= 200 && statusCode < 300;
            Console.ForegroundColor = isSuccess ? ConsoleColor.DarkGray : ConsoleColor.Red;
            Console.Error.WriteLine($"HTTP {statusCode}");
            Console.ResetColor();

            Console.WriteLine(response);

            if (!isSuccess)
                Environment.Exit(1);
        }));

        return cmd;
    }
}
