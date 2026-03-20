using System.Net.Http.Json;
using System.Text.Json;

public class YouTrackClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public YouTrackClient(Config config)
    {
        _baseUrl = config.BaseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
    }

    public async Task<List<Issue>> SearchAsync(string query, int top = 20)
    {
        var url = $"{_baseUrl}/api/issues?query={Uri.EscapeDataString(query)}&fields=id,idReadable,summary,customFields(name,value(name,login,fullName))&$top={top}";
        return await GetAsync<List<Issue>>(url) ?? [];
    }

    public async Task<MeUser> GetMeAsync()
        => (await GetAsync<MeUser>($"{_baseUrl}/api/users/me?fields=login,fullName,email"))!;

    public async Task<List<YtProject>> GetProjectsAsync()
    {
        var all = new List<YtProject>();
        int skip = 0;
        const int pageSize = 100;
        while (true)
        {
            var page = await GetAsync<List<YtProject>>($"{_baseUrl}/api/admin/projects?fields=id,name,shortName&$top={pageSize}&$skip={skip}") ?? [];
            all.AddRange(page);
            if (page.Count < pageSize) break;
            skip += pageSize;
        }
        return all;
    }

    public async Task<Issue> CreateIssueAsync(string projectShortName, string summary, string? description)
    {
        // The API requires the internal project ID, not the short name
        var projects = await GetProjectsAsync();
        var project = projects.FirstOrDefault(p => p.ShortName.Equals(projectShortName, StringComparison.OrdinalIgnoreCase))
            ?? throw new YouTrackException($"Project '{projectShortName}' not found. Run 'yt projects' to see available projects.");

        var body = new { project = new { id = project.Id }, summary, description };
        var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/api/issues?fields=id,idReadable,summary", body, JsonOptions);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<Issue>(JsonOptions))!;
    }

    public async Task UpdateIssueAsync(string issueId, string? summary, string? description)
    {
        var body = new Dictionary<string, object?>();
        if (summary is not null) body["summary"] = summary;
        if (description is not null) body["description"] = description;
        var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/api/issues/{issueId}", body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task UpdateFixVersionsAsync(string issueId, string[] versions)
    {
        // Create any missing versions in the bundle first
        if (versions.Length > 0)
        {
            var issueProject = await GetAsync<IssueProjectRef>($"{_baseUrl}/api/issues/{issueId}?fields=project(id,shortName)");
            var projectId = issueProject?.Project?.Id
                ?? throw new YouTrackException("Could not determine project for issue.");

            var customFields = await GetAsync<List<ProjectCustomField>>(
                $"{_baseUrl}/api/admin/projects/{projectId}/customFields?fields=id,field(name),bundle(id)") ?? [];
            var bundleId = customFields
                .FirstOrDefault(f => f.Field?.Name?.Equals("Fix versions", StringComparison.OrdinalIgnoreCase) == true)
                ?.Bundle?.Id
                ?? throw new YouTrackException("Could not find Fix versions field for this project.");

            var existing = await GetAsync<List<YtVersion>>(
                $"{_baseUrl}/api/admin/customFieldSettings/bundles/version/{bundleId}/values?fields=id,name") ?? [];
            var existingNames = existing.Select(v => v.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var v in versions.Where(v => !existingNames.Contains(v)))
            {
                var createResp = await _http.PostAsJsonAsync(
                    $"{_baseUrl}/api/admin/customFieldSettings/bundles/version/{bundleId}/values",
                    new { name = v }, JsonOptions);
                await EnsureSuccessAsync(createResp);
            }
        }

        // Clear all fix versions — REST accepts an empty array for multi-value fields
        var clearBody = new Dictionary<string, object?>
        {
            ["customFields"] = new object[]
            {
                new Dictionary<string, object?> { ["$type"] = "MultiVersionIssueCustomField", ["name"] = "Fix versions", ["value"] = Array.Empty<object>() }
            }
        };
        var clearResp = await _http.PostAsJsonAsync($"{_baseUrl}/api/issues/{issueId}", clearBody, JsonOptions);
        await EnsureSuccessAsync(clearResp);

        // Set new versions via command API (YouTrack REST rejects VersionValue bodies)
        if (versions.Length > 0)
            await ApplyCommandAsync(issueId, "Fix versions " + string.Join(" ", versions));
    }

    public async Task MoveIssueAsync(string issueId, string projectShortName)
    {
        var projects = await GetProjectsAsync();
        var project = projects.FirstOrDefault(p => p.ShortName.Equals(projectShortName, StringComparison.OrdinalIgnoreCase))
            ?? throw new YouTrackException($"Project '{projectShortName}' not found. Run 'yt projects' to see available projects.");
        var body = new Dictionary<string, object?> { ["project"] = new Dictionary<string, object?> { ["id"] = project.Id } };
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/issues/{issueId}", body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task AddCommentAsync(string issueId, string text)
    {
        var body = new { text };
        var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/api/issues/{issueId}/comments", body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task<IssueDetail> GetIssueAsync(string issueId)
    {
        var fields = "id,idReadable,summary,description,reporter(fullName,login),customFields(name,value(name,login,fullName)),tags(name)";
        return (await GetAsync<IssueDetail>($"{_baseUrl}/api/issues/{issueId}?fields={fields}"))!;
    }

    public async Task<List<IssueComment>> GetCommentsAsync(string issueId)
        => await GetAsync<List<IssueComment>>($"{_baseUrl}/api/issues/{issueId}/comments?fields=id,text,author(login,fullName),created") ?? [];

    public async Task<List<IssueAttachment>> GetAttachmentsAsync(string issueId)
        => await GetAsync<List<IssueAttachment>>($"{_baseUrl}/api/issues/{issueId}/attachments?fields=id,name,mimeType,url") ?? [];

    public async Task<byte[]> DownloadAsync(string url)
    {
        var fullUrl = url.StartsWith("http") ? url : $"{_baseUrl}{url}";
        return await _http.GetByteArrayAsync(fullUrl);
    }

    public async Task LogWorkAsync(string issueId, string duration, string? description)
    {
        var body = new Dictionary<string, object?> { ["duration"] = new { presentation = duration } };
        if (description is not null) body["text"] = description;
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/issues/{issueId}/timeTracking/workItems", body, JsonOptions);
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new YouTrackException("Time tracking is not enabled for this project. Enable it in YouTrack under Project Settings → Time Tracking.");
        await EnsureSuccessAsync(response);
    }

    public async Task EditCommentAsync(string issueId, string commentId, string text)
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/issues/{issueId}/comments/{commentId}", new { text }, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task AttachFileAsync(string issueId, string filePath)
    {
        if (!File.Exists(filePath))
            throw new YouTrackException($"File not found: {filePath}");
        using var form = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(filePath);
        var fileName = Path.GetFileName(filePath);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(filePath));
        form.Add(content, "file", fileName);
        var response = await _http.PostAsync($"{_baseUrl}/api/issues/{issueId}/attachments", form);
        await EnsureSuccessAsync(response);
    }

    private static string GetMimeType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"            => "image/png",
        ".gif"            => "image/gif",
        ".pdf"            => "application/pdf",
        ".txt" or ".log"  => "text/plain",
        ".md"             => "text/markdown",
        ".zip"            => "application/zip",
        ".json"           => "application/json",
        ".xml"            => "application/xml",
        _                 => "application/octet-stream"
    };

    public async Task ApplyCommandAsync(string issueIdReadable, string command)
    {
        var body = new { query = command, issues = new[] { new { idReadable = issueIdReadable } } };
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/commands", body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task<(int StatusCode, string Body)> RawAsync(string method, string path, string? body)
    {
        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : $"{_baseUrl}{path}";
        var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url);
        if (body is not null)
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(content);
            content = System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch { }
        return ((int)response.StatusCode, content);
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        string? detail = null;
        try
        {
            var err = JsonSerializer.Deserialize<JsonElement>(body);
            detail = err.TryGetProperty("error_description", out var d) ? d.GetString() :
                     err.TryGetProperty("message", out var m) ? m.GetString() : null;
        }
        catch { }
        throw new YouTrackException(detail ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
    }
}

public record Issue(string Id, string IdReadable, string Summary, List<CustomField>? CustomFields)
{
    public string? State => CustomFields?
        .FirstOrDefault(f => f.Name.Equals("State", StringComparison.OrdinalIgnoreCase))
        ?.DisplayValue;
}

public record IssueDetail(
    string Id,
    string IdReadable,
    string Summary,
    string? Description,
    IssuePerson? Reporter,
    List<CustomField> CustomFields,
    List<IssueTag>? Tags);

public record IssuePerson(string Login, string? FullName);

// Value is JsonElement? because YouTrack custom field values are polymorphic:
// objects (enum, user, version...), strings, numbers, arrays, or null.
public record IssueComment(string Id, string Text, IssuePerson? Author, long Created)
{
    public DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds(Created).ToLocalTime();
}

public record IssueAttachment(string Id, string Name, string MimeType, string Url);

public record IssueTag(string Name);

public record CustomField(string Name, JsonElement? Value)
{
    public string? DisplayValue => Value is not { } v ? null : v.ValueKind switch
    {
        JsonValueKind.Null   => null,
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Number => v.GetRawText(),
        JsonValueKind.Object =>
            v.TryGetProperty("name", out var name)     ? name.GetString() :
            v.TryGetProperty("fullName", out var fn)   ? fn.GetString() :
            v.TryGetProperty("login", out var login)   ? login.GetString() : null,
        JsonValueKind.Array  => string.Join(", ", v.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Object
                ? (e.TryGetProperty("name", out var n) ? n.GetString() :
                   e.TryGetProperty("fullName", out var fn) ? fn.GetString() : null)
                : e.GetString())
            .Where(s => s is not null)),
        _ => null
    };
}

public record MeUser(string Login, string? FullName, string? Email);

public record YtProject(string Id, string Name, string ShortName);

public record YtVersion(string Id, string Name);

// Lightweight project ref used when only id is needed (avoids requiring Name field)
public record YtProjectRef(string Id, string ShortName);

public record IssueProjectRef(YtProjectRef? Project);

public record BundleRef(string? Id);

public record FieldRef(string? Name);

public record ProjectCustomField(string? Id, FieldRef? Field, BundleRef? Bundle);

public class YouTrackException(string message) : Exception(message);
