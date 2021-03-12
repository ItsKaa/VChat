using System;
using System.Linq;
using System.Net;
using VChat.Extensions;

namespace VChat.Helpers
{
    public static class GithubHelper
    {
        public const string GithubApiBaseUri = "https://api.github.com";

        /// <summary>
        /// Get the latest github release tag name from a repository.
        /// </summary>
        /// <param name="author">The owner of the repository</param>
        /// <param name="repositoryName">The repository name</param>
        /// <param name="isPrerelease">Returns if the found release is a pre-release</param>
        /// <param name="includePrerelease">Whether or not prereleases should be returned</param>
        /// <returns>The tag name of the release</returns>
        public static string GetLatestGithubRelease(string author, string repositoryName, out bool isPrerelease, bool includePrerelease = false)
        {
            isPrerelease = false;
            try
            {
                var webClient = new WebClient();
                webClient.Headers.Add("User-Agent: VChat-version-checker");
                var jsonString = webClient.DownloadString($"{GithubApiBaseUri}/repos/{author}/{repositoryName}/releases");

                // Bit hacky but this way I won't have to include a json library.
                while (true)
                {
                    int pos = 0;

                    // Get the version name, aka the tag.
                    var tagName = jsonString.Between("\"tag_name\":\"", "\"", out pos, pos);
                    if (string.IsNullOrEmpty(tagName))
                    {
                        return null;
                    }

                    // See if this is a prerelease.
                    var isPrereleaseString = jsonString.Between("\"prerelease\":", ",", out pos, pos);
                    bool.TryParse(isPrereleaseString, out isPrerelease);
                    if (includePrerelease || !isPrerelease)
                    {
                        return tagName;
                    }

                    jsonString = new string(jsonString.Skip(pos).ToArray());
                }
            }
            catch (Exception ex)
            {
                VChatPlugin.LogWarning($"Unable to find the github release for {author}/{repositoryName}: {ex}");
            }

            return null;
        }

        public static string GetLatestGithubRelease(string author, string repositoryName, bool includePrerelease = false)
            => GetLatestGithubRelease(author, repositoryName, out bool _, includePrerelease);
    }
}
