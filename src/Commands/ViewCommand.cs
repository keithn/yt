using System.CommandLine;

public static class ViewCommand
{
    public static Command Build()
    {
        var issueArg = new Argument<string>("issue-id") { Description = "Issue ID (e.g. PROJ-123)" };
        var commentsOpt = new Option<bool>("--comments", "-c") { Description = "Include comments" };
        var imagesOpt = new Option<bool>("--images", "-i") { Description = "Display image attachments inline" };

        var cmd = new Command("view", "Show full details of an issue: summary, custom fields, description, and optionally comments. When piped, outputs clean markdown suitable for further processing.");
        cmd.Arguments.Add(issueArg);
        cmd.Options.Add(commentsOpt);
        cmd.Options.Add(imagesOpt);
        cmd.SetAction(async (parseResult, ct) => await Cmd.RunAsync(async () =>
        {
            var issueId = parseResult.GetValue(issueArg)!;
            var showImages = parseResult.GetValue(imagesOpt) && !Console.IsOutputRedirected;
            var client = new YouTrackClient(Config.LoadOrThrow());

            var issueTask = client.GetIssueAsync(issueId);
            var attachmentsTask = showImages
                ? client.GetAttachmentsAsync(issueId)
                : Task.FromResult(new List<IssueAttachment>());
            var commentsTask = parseResult.GetValue(commentsOpt)
                ? client.GetCommentsAsync(issueId)
                : Task.FromResult(new List<IssueComment>());

            await Task.WhenAll(issueTask, attachmentsTask, commentsTask);

            var issue = issueTask.Result;
            var images = attachmentsTask.Result.Where(a => a.MimeType.StartsWith("image/")).ToList();
            var comments = commentsTask.Result;

            if (Console.IsOutputRedirected)
            {
                RenderMarkdown(issue, comments);
            }
            else
            {
                RenderColorized(issue, comments);
                foreach (var image in images)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  {image.Name}");
                    Console.ResetColor();
                    var bytes = await client.DownloadAsync(image.Url);
                    WriteInlineImage(bytes, image.Name);
                    Console.WriteLine();
                }
            }
        }));

        return cmd;
    }

    private static void RenderMarkdown(IssueDetail issue, List<IssueComment> comments)
    {
        Console.WriteLine($"# {issue.IdReadable}: {issue.Summary}");
        Console.WriteLine();

        var fields = issue.CustomFields.Where(f => f.DisplayValue is not null).ToList();
        if (issue.Reporter is not null)
            fields.Add(new CustomField("Reporter",
                System.Text.Json.JsonSerializer.SerializeToElement(issue.Reporter.FullName ?? issue.Reporter.Login)));

        if (fields.Count > 0)
        {
            Console.WriteLine("| Field | Value |");
            Console.WriteLine("|---|---|");
            foreach (var field in fields)
                Console.WriteLine($"| {field.Name} | {field.DisplayValue} |");
            Console.WriteLine();
        }

        if (issue.Tags is { Count: > 0 })
        {
            Console.WriteLine($"| Tags | {string.Join(", ", issue.Tags.Select(t => t.Name))} |");
            Console.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(issue.Description))
        {
            Console.WriteLine(issue.Description);
            Console.WriteLine();
        }

        if (comments.Count > 0)
        {
            Console.WriteLine("## Comments");
            Console.WriteLine();
            foreach (var comment in comments)
            {
                var author = comment.Author?.FullName ?? comment.Author?.Login ?? "Unknown";
                Console.WriteLine($"**{author}** — {comment.CreatedAt:yyyy-MM-dd HH:mm} <!-- id:{comment.Id} -->");
                Console.WriteLine();
                Console.WriteLine(comment.Text);
                Console.WriteLine();
                Console.WriteLine("---");
                Console.WriteLine();
            }
        }
    }

    private static void RenderColorized(IssueDetail issue, List<IssueComment> comments)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(issue.IdReadable);
        Console.ResetColor();
        Console.Write(": ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(issue.Summary);
        Console.ResetColor();
        Console.WriteLine();

        foreach (var field in issue.CustomFields.Where(f => f.DisplayValue is not null))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {field.Name,-15} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(field.DisplayValue);
            Console.ResetColor();
        }

        if (issue.Reporter is not null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {"Reporter",-15} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(issue.Reporter.FullName ?? issue.Reporter.Login);
            Console.ResetColor();
        }

        if (issue.Tags is { Count: > 0 })
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {"Tags",-15} ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(string.Join(", ", issue.Tags.Select(t => t.Name)));
            Console.ResetColor();
        }

        if (!string.IsNullOrWhiteSpace(issue.Description))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(issue.Description);
            Console.ResetColor();
        }

        if (comments.Count > 0)
        {
            Console.WriteLine();
            foreach (var comment in comments)
            {
                var author = comment.Author?.FullName ?? comment.Author?.Login ?? "Unknown";
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(author);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  {comment.CreatedAt:yyyy-MM-dd HH:mm}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  [{comment.Id}]");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(comment.Text);
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }

    private static void WriteInlineImage(byte[] data, string name)
    {
        var b64Data = Convert.ToBase64String(data);
        var b64Name = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(name));
        // iTerm2 inline image protocol, supported by WezTerm, iTerm2, and others
        Console.Write($"\x1b]1337;File=name={b64Name};size={data.Length};inline=1:{b64Data}\x07");
    }
}
