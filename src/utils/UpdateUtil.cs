using System.Net.Http;
using System.Text.Json;

namespace LIveSubs.utils
{
    public static class UpdateUtil
    {
       public const string GitHubRepoUrl = "https://github.com/Diva143V/LiveSubs";
public const string GitHubReleasesUrl = "https://github.com/Diva143V/LiveSubs/releases";
public const string GitHubLatestReleaseApi = "https://api.github.com/repos/Diva143V/LiveSubs/releases/latest";
        public static async Task<string> GetLatestVersion()
        {
            string apiUrl = GitHubLatestReleaseApi;

            using var client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LiveCaptions-Translator");
            var response = await client.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(response);
            var latestVersionRaw = doc.RootElement.GetProperty("tag_name").GetString();
            var latestVersion = string.IsNullOrEmpty(latestVersionRaw)
                ? String.Empty
                : RegexPatterns.VersionNumber().Replace(latestVersionRaw, "");
            return latestVersion;
        }


    }
}
