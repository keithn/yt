public static class Cmd
{
    public static async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (YouTrackException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
        catch (TaskCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error: Request timed out. Check your YouTrack URL and network connection.");
            Console.ResetColor();
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }
}
