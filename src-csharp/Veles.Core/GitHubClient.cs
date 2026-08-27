using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Veles.Core
{
    public sealed class GitHubClient
    {
        private readonly string _owner;
        private readonly string _repo;
        private readonly string _token;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public GitHubClient(string owner, string repo, string token = null)
        {
            _owner = owner; _repo = repo; _token = token;
        }

        public async Task<ReleaseInfo> GetLatestReleaseAsync(CancellationToken cancellationToken)
        {
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases/latest", _owner, _repo);
            var raw = await SendAsync(HttpMethod.Get, url, null, "application/vnd.github+json", cancellationToken).ConfigureAwait(false);
            var obj = _json.DeserializeObject(raw) as System.Collections.Generic.Dictionary<string, object>;
            return ToRelease(obj);
        }

        public async Task<System.Collections.Generic.List<ReleaseInfo>> GetReleasesAsync(CancellationToken cancellationToken)
        {
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases?per_page=100", _owner, _repo);
            var raw = await SendAsync(HttpMethod.Get, url, null, "application/vnd.github+json", cancellationToken).ConfigureAwait(false);
            var values = _json.DeserializeObject(raw) as object[];
            var releases = new System.Collections.Generic.List<ReleaseInfo>();
            if (values != null) foreach (var value in values)
            {
                var obj = value as System.Collections.Generic.Dictionary<string, object>;
                if (obj != null) releases.Add(ToRelease(obj));
            }
            return releases;
        }

        public async Task<ReleaseInfo> UpdateReleaseAsync(string releaseId, string name, string body, CancellationToken cancellationToken)
        {
            EnsureToken();
            var payload = _json.Serialize(new { name = name, body = body, draft = false, prerelease = false });
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases/{2}", _owner, _repo, Uri.EscapeDataString(releaseId));
            var raw = await SendAsync(new HttpMethod("PATCH"), url, payload, "application/json", cancellationToken).ConfigureAwait(false);
            return ToRelease(_json.DeserializeObject(raw) as System.Collections.Generic.Dictionary<string, object>);
        }

        public async Task DeleteReleaseAsync(string releaseId, CancellationToken cancellationToken)
        {
            EnsureToken();
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases/{2}", _owner, _repo, Uri.EscapeDataString(releaseId));
            await SendAsync(HttpMethod.Delete, url, null, "application/vnd.github+json", cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteTagAsync(string tag, CancellationToken cancellationToken)
        {
            EnsureToken();
            var url = string.Format("https://api.github.com/repos/{0}/{1}/git/refs/tags/{2}", _owner, _repo, Uri.EscapeDataString(tag));
            await SendAsync(HttpMethod.Delete, url, null, "application/vnd.github+json", cancellationToken).ConfigureAwait(false);
        }

        public async Task<ReleaseInfo> CreateReleaseAsync(string tag, string name, string body, CancellationToken cancellationToken)
        {
            EnsureToken();
            var payload = _json.Serialize(new { tag_name = tag, name = name, body = body, draft = false, prerelease = false });
            var url = string.Format("https://api.github.com/repos/{0}/{1}/releases", _owner, _repo);
            var raw = await SendAsync(HttpMethod.Post, url, payload, "application/json", cancellationToken).ConfigureAwait(false);
            return ToRelease(_json.DeserializeObject(raw) as System.Collections.Generic.Dictionary<string, object>);
        }

        public async Task UploadAssetAsync(string uploadUrl, string filePath, string assetName, IProgress<int> progress, CancellationToken cancellationToken)
        {
            EnsureToken();
            var cleanUrl = uploadUrl.Replace("{?name,label}", string.Empty);
            var separator = cleanUrl.Contains("?") ? "&" : "?";
            var url = cleanUrl + separator + "name=" + Uri.EscapeDataString(assetName);
            var fileInfo = new FileInfo(filePath);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.UserAgent = "Veles-Build-Publisher/1.0";
            request.Accept = "application/vnd.github+json";
            request.ContentType = "application/octet-stream";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + _token;
            request.ContentLength = fileInfo.Length;
            using (var input = fileInfo.OpenRead())
            using (var stream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                var buffer = new byte[1024 * 1024]; long uploaded = 0; int count;
                while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);
                    uploaded += count;
                    if (progress != null) progress.Report((int)(uploaded * 100L / Math.Max(1L, fileInfo.Length)));
                }
            }
            using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
            using (var reader = new StreamReader(response.GetResponseStream())) { await reader.ReadToEndAsync().ConfigureAwait(false); }
        }

        private async Task<string> SendAsync(HttpMethod method, string url, string body, string contentType, CancellationToken cancellationToken)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method.Method;
            request.UserAgent = "Veles-Launcher/1.0";
            request.Accept = "application/vnd.github+json";
            if (!string.IsNullOrEmpty(_token)) request.Headers[HttpRequestHeader.Authorization] = "Bearer " + _token;
            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                request.ContentType = contentType; request.ContentLength = bytes.Length;
                using (var stream = await request.GetRequestStreamAsync().ConfigureAwait(false)) await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(response.GetResponseStream())) return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (WebException error)
            {
                var response = error.Response as HttpWebResponse;
                string details = error.Message;
                if (response != null) using (var reader = new StreamReader(response.GetResponseStream())) details = await reader.ReadToEndAsync().ConfigureAwait(false);
                throw new InvalidOperationException("GitHub API: " + details, error);
            }
        }

        private void EnsureToken() { if (string.IsNullOrWhiteSpace(_token)) throw new InvalidOperationException("Для публикации требуется GitHub Personal Access Token."); }

        private static ReleaseInfo ToRelease(System.Collections.Generic.Dictionary<string, object> obj)
        {
            if (obj == null) throw new InvalidOperationException("GitHub вернул пустой релиз.");
            var body = StringValue(obj, "body");
            var release = new ReleaseInfo { Id = StringValue(obj, "id"), TagName = StringValue(obj, "tag_name"), Name = StringValue(obj, "name"), Body = body, HtmlUrl = StringValue(obj, "html_url"), UploadUrl = StringValue(obj, "upload_url"), IsActive = ParseMarker(body, "VELES_ACTIVE", true), Priority = ParseIntMarker(body, "VELES_PRIORITY", 9999), Assets = new System.Collections.Generic.List<ReleaseAsset>() };
            var assets = obj.ContainsKey("assets") ? obj["assets"] as object[] : null;
            if (assets != null) foreach (var value in assets)
            {
                var asset = value as System.Collections.Generic.Dictionary<string, object>;
                if (asset != null) release.Assets.Add(new ReleaseAsset { Name = StringValue(asset, "name"), BrowserDownloadUrl = StringValue(asset, "browser_download_url"), UploadUrl = StringValue(asset, "url") });
            }
            var uploadUrl = StringValue(obj, "upload_url");
            foreach (var asset in release.Assets) if (string.IsNullOrEmpty(asset.UploadUrl)) asset.UploadUrl = uploadUrl;
            return release;
        }
        private static string StringValue(System.Collections.Generic.Dictionary<string, object> value, string key) { object result; return value != null && value.TryGetValue(key, out result) && result != null ? Convert.ToString(result) : string.Empty; }
        private static bool ParseMarker(string body, string key, bool defaultValue) { var marker = key + "="; using (var reader = new StringReader(body ?? string.Empty)) { string line; while ((line = reader.ReadLine()) != null) { line = line.Trim(); if (line.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) { var value = line.Substring(marker.Length).Trim(); return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase); } } } return defaultValue; }
        private static int ParseIntMarker(string body, string key, int defaultValue) { var marker = key + "="; using (var reader = new StringReader(body ?? string.Empty)) { string line; while ((line = reader.ReadLine()) != null) { line = line.Trim(); if (line.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) { int value; return int.TryParse(line.Substring(marker.Length).Trim(), out value) ? value : defaultValue; } } } return defaultValue; }
    }
}
