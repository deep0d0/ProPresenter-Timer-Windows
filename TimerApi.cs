using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace ProPresenterTimer;

internal static class TimerApi
{
    private static readonly HttpClient SharedClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static readonly JsonSerializerOptions JsonRelaxed = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<string> FetchCountdownAsync(string ip, string mode, int timeoutMs, CancellationToken ct)
    {
        ip = ip.Trim();
        if (string.IsNullOrEmpty(ip))
            return "00:00:00";
        return string.Equals(mode, "Resolume", StringComparison.OrdinalIgnoreCase)
            ? await FetchResolumeAsync(ip, timeoutMs, ct).ConfigureAwait(false)
            : await FetchProPresenterVideoAsync(ip, timeoutMs, ct).ConfigureAwait(false);
    }

    public static async Task<string> FetchProPresenterVideoAsync(string baseUrl, int timeoutMs, CancellationToken ct)
    {
        baseUrl = baseUrl.Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return "00:00:00";
        var url = baseUrl.TrimEnd('/') + "/v1/timer/video_countdown";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            var resp = await SharedClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var raw = (await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false)).Trim().Trim('"');
            return string.IsNullOrEmpty(raw) ? "00:00:00" : raw;
        }
        catch
        {
            return "00:00:00";
        }
    }

    public static async Task<string> FetchProPresenterAudioAsync(string baseUrl, int timeoutMs, CancellationToken ct)
    {
        baseUrl = baseUrl.Trim();
        if (string.IsNullOrEmpty(baseUrl))
            return "00:00:00";
        var totalUrl = baseUrl.TrimEnd('/') + "/v1/transport/audio/current";
        var currentUrl = baseUrl.TrimEnd('/') + "/v1/transport/audio/time";

        double totalSeconds = 0;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            using var totalReq = new HttpRequestMessage(HttpMethod.Get, totalUrl);
            var totalResp = await SharedClient.SendAsync(totalReq, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            totalResp.EnsureSuccessStatusCode();
            await using var stream = await totalResp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("duration", out var dur))
            {
                if (dur.ValueKind == JsonValueKind.Number)
                    totalSeconds = dur.GetDouble();
                else if (dur.ValueKind == JsonValueKind.String && double.TryParse(dur.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    totalSeconds = d;
            }
        }
        catch
        {
            return "00:00:00";
        }

        if (totalSeconds <= 0)
            return "00:00:00";

        double currentSeconds = 0;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            using var curReq = new HttpRequestMessage(HttpMethod.Get, currentUrl);
            var curResp = await SharedClient.SendAsync(curReq, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            curResp.EnsureSuccessStatusCode();
            var raw = (await curResp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false)).Trim().Trim('"');
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out currentSeconds))
                currentSeconds = 0;
        }
        catch
        {
            return "00:00:00";
        }

        return FormatSeconds(totalSeconds - currentSeconds);
    }

    public static async Task<string> FetchResolumeAsync(string baseUrl, int timeoutMs, CancellationToken ct)
    {
        var url = baseUrl.TrimEnd('/') + "/api/v1/composition/clips/selected";
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            var resp = await SharedClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var clip = await JsonSerializer.DeserializeAsync<ClipResponse>(stream, JsonRelaxed, cts.Token).ConfigureAwait(false);
            var pos = clip?.Transport?.Position;
            if (pos?.Max is { } maxMs && pos.Value is { } currentMs)
                return FormatSeconds((maxMs - currentMs) / 1000.0);
        }
        catch
        {
            /* fall through */
        }

        return "00:00:00";
    }

    public static string FormatSeconds(double totalSeconds)
    {
        if (totalSeconds < 0)
            return "00:00:00";
        var s = (int)totalSeconds;
        var h = s / 3600;
        var m = (s % 3600) / 60;
        var sec = s % 60;
        return string.Create(CultureInfo.InvariantCulture, $"{h:00}:{m:00}:{sec:00}");
    }

    public static double SecondsFromTimeString(string timeString)
    {
        var parts = timeString.Split(':');
        if (parts.Length != 3)
            return 0;
        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)) return 0;
        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) return 0;
        if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) return 0;
        return h * 3600 + m * 60 + s;
    }
}

internal enum TimerUrgency
{
    Normal,
    Warning,
    Critical,
}

internal static class TimerUrgencyHelper
{
    public static TimerUrgency FromTimeString(string timeString)
    {
        if (timeString == "00:00:00")
            return TimerUrgency.Normal;
        var total = TimerApi.SecondsFromTimeString(timeString);
        if (total <= 10) return TimerUrgency.Critical;
        if (total <= 30) return TimerUrgency.Warning;
        return TimerUrgency.Normal;
    }
}
