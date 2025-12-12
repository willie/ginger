using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ginger.Services;

/// <summary>
/// Service for checking GitHub releases for updates.
/// </summary>
public class UpdateService
{
    private const string GitHubReleasesUrl = "https://api.github.com/repos/DominaeDev/ginger/releases/latest";
    private static readonly HttpClient _httpClient = new();

    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Ginger-Update-Checker");
    }

    public record UpdateInfo(bool UpdateAvailable, string CurrentVersion, string LatestVersion, string ReleaseUrl, string ReleaseNotes);

    /// <summary>
    /// Check for updates from GitHub releases.
    /// </summary>
    public static async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            var response = await _httpClient.GetStringAsync(GitHubReleasesUrl);
            var json = JObject.Parse(response);

            var tagName = json["tag_name"]?.ToString() ?? "";
            var latestVersion = tagName.TrimStart('v', 'V');
            var htmlUrl = json["html_url"]?.ToString() ?? "";
            var body = json["body"]?.ToString() ?? "";

            var updateAvailable = IsNewerVersion(currentVersion, latestVersion);

            return new UpdateInfo(updateAvailable, currentVersion, latestVersion, htmlUrl, body);
        }
        catch (Exception ex)
        {
            // Return error info
            return new UpdateInfo(false, currentVersion, "", "", $"Error checking for updates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the current application version.
    /// </summary>
    public static string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    /// <summary>
    /// Compare versions to determine if latest is newer than current.
    /// </summary>
    private static bool IsNewerVersion(string current, string latest)
    {
        if (string.IsNullOrEmpty(latest))
            return false;

        try
        {
            // Parse version strings
            var currentParts = current.Split('.', '-');
            var latestParts = latest.Split('.', '-');

            for (int i = 0; i < Math.Min(3, Math.Max(currentParts.Length, latestParts.Length)); i++)
            {
                int currentNum = i < currentParts.Length && int.TryParse(currentParts[i], out var cn) ? cn : 0;
                int latestNum = i < latestParts.Length && int.TryParse(latestParts[i], out var ln) ? ln : 0;

                if (latestNum > currentNum)
                    return true;
                if (latestNum < currentNum)
                    return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
