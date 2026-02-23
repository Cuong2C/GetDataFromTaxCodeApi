using Microsoft.AspNetCore.Http.HttpResults;
using SthinkGetDataFromTaxCodeApi.Models;
using System.Net.Http.Headers;

namespace SthinkGetDataFromTaxCodeApi.Services;

public class TraCuuNNTSessionInfoServices(ILogger<TraCuuNNTSessionInfoServices> logger, IConfiguration configuration) : ISessionInfoServices
{
    public async Task<Results<Ok<SessionInfoResponse>, BadRequest<string>>> GetSessionInfoAsync()
    {
        try
        {
            var host = configuration.GetValue<string>("TraCuuNNT:Host");
            var url = configuration.GetValue<string>("TraCuuNNT:CaptchaUrl");

            using var handler = new HttpClientHandler
            {
                UseCookies = false
            };

            using var client = new HttpClient(handler);

            // add default headers like browser (optional)
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/140.0.0.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,vi;q=0.8");
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br, zstd");
            client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
            client.DefaultRequestHeaders.Add("Host", host);
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Chromium\";v=\"140\", \"Not=A?Brand\";v=\"24\", \"Microsoft Edge\";v=\"140\"");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            if(!response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
            {
                logger.LogError("No Set-Cookie header found in the response.");
                return TypedResults.BadRequest("No Set - Cookie header found in the response");
            }

            var cookies = string.Join("; ", cookieValues);

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var base64 = Convert.ToBase64String(imageBytes);

            logger.LogInformation("Get cookies and captcha success");
            return TypedResults.Ok(
                new SessionInfoResponse
                {
                    Cookies = cookies,
                    CaptchaImage = base64
                }
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error when fetching captcha");
            return TypedResults.BadRequest("Không thể kết nối đến server");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");
            return TypedResults.BadRequest("Đã xảy ra lỗi trong hệ thống");
        }
    }
}
