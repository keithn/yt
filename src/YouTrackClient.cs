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
        var url = $"{_baseUrl}/api/issues?query={Uri.EscapeDataString(query)}&fields=id,idReadable,summary,customFields(name,value(name))&$top={top}";
        return await GetAsync<List<Issue>>(url) ?? [];
    }

    public async Task<MeUser> GetMeAsync()
        => (await GetAsync<MeUser>($"{_baseUrl}/api/users/me?fields=login,fullName,email"))!;

    public async Task<List<YtProject>> GetProjectsAsync()
        => await GetAsync<List<YtProject>>($"{_baseUrl}/api/admin/projects?fields=id,name,shortName&$top=500") ?? [];

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

    public async Task AddCommentAsync(string issueId, string text)
    {
        var body = new { text };
        var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/api/issues/{issueId}/comments", body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task<IssueDetail> GetIssueAsync(string issueId)
    {
        var fields = "id,idReadable,summary,description,reporter(fullName,login),customFields(name,value(name,login,fullName))";
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

    public async Task ApplyCommandAsync(string issueId, string command)
    {
        var body = new { query = command, issues = new[] { new { id = issueId } } };
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/commands", body, JsonOptions);
        await EnsureSuccessAsync(response);
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
    List<CustomField> CustomFields);

public record IssuePerson(string Login, string? FullName);

// Value is JsonElement? because YouTrack custom field values are polymorphic:
// objects (enum, user, version...), strings, numbers, arrays, or null.
public record IssueComment(string Id, string Text, IssuePerson? Author, long Created)
{
    public DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds(Created).ToLocalTime();
}

public record IssueAttachment(string Id, string Name, string MimeType, string Url);

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

public class YouTrackException(string message) : Exception(message);
