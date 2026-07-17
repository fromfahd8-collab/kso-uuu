using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KSO.Modules
{
    public static class SpeedTest
    {
        private static readonly HttpClient _client = new HttpClient() { Timeout = TimeSpan.FromSeconds(20) };
        // ملف 10MB من سيرفر مايكروسوفت للاختبار
        private const string TestFileUrl = "https://speedtest.tele2.net/10MB.zip";

        public static async Task<double> MeasureDownloadSpeedAsync()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                using var response = await _client.GetAsync(TestFileUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength?? 10 * 1024 * 1024;
                var bytesRead = 0L;
                var buffer = new byte[81920];

                using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                {
                    bytesRead += read;
                    if (sw.Elapsed > TimeSpan.FromSeconds(10)) break; // نوقف بعد 10 ثواني
                }

                sw.Stop();
                double seconds = sw.Elapsed.TotalSeconds;
                if (seconds < 0.1) seconds = 0.1;

                // نحسب Mbps صح: Bytes * 8 / 1000000 / seconds
                double mbps = (bytesRead * 8.0) / 1000000.0 / seconds;
                return Math.Round(mbps, 1);
            }
            catch
            {
                // لو فشل نرجع 20mbps عشان يدي 32 خيط افتراضي
                return 20.0;
            }
        }
    }
}