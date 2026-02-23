using HtmlAgilityPack;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Playwright;
using SthinkGetDataFromTaxCodeApi.Models;
using System.Net.Http.Headers;
using System.Text;

namespace SthinkGetDataFromTaxCodeApi.Services;

public class ClientInfoServices(ILogger<ClientInfoServices> logger, IConfiguration configuration) : IClientInfoServices
{
    public async Task<Results<Ok<ClientInfoResponse>, BadRequest<string>>> GetClientInfoAsync(ClientInfoRequest clientInfoRequest)
    {
        if (string.IsNullOrEmpty(clientInfoRequest.TaxCode) || clientInfoRequest.TaxCode.Length < 10)
        {
            return TypedResults.BadRequest("Request is invalid");
        }

         var clientInfo = await GetTraCuuNNTClientInfoFromTaxCodeAsync(clientInfoRequest);

        // Should prioritize TraCuuNNT but TraCuuNNT can be overload, if failed or overload then try to get from thongtincongty.vn
        if (clientInfo.Status != ResponeStatus.Success)
        {
            string messsage = clientInfo.Message;
            clientInfo = await GetThongTinCongTyDotVnClientInfoFromTaxCodeAsync(clientInfoRequest.TaxCode);

            // Keep failed message from TraCuuNNT
            if (clientInfo.Status != ResponeStatus.Success && !string.IsNullOrEmpty(clientInfoRequest.CaptchaCode))
                clientInfo.Message = messsage;
        }

        return TypedResults.Ok(clientInfo);
    }

    private async Task<ClientInfoResponse> GetTraCuuNNTClientInfoFromTaxCodeAsync(ClientInfoRequest clientInfoRequest)
    {
        try
        {
            if (string.IsNullOrEmpty(clientInfoRequest.CaptchaCode) || string.IsNullOrEmpty(clientInfoRequest.Cookies))
                return new ClientInfoResponse
                {
                    Status = ResponeStatus.Failed,
                    Message = "Cookies or CaptchaCode is empty",
                    ClientInformation = null
                };

            var host = configuration.GetValue<string>("TraCuuNNT:Host");
            var baseUrl = configuration.GetValue<string>("TraCuuNNT:BaseUrl");
            var url = configuration.GetValue<string>("TraCuuNNT:Url");

            using var handler = new HttpClientHandler
            {
                UseCookies = false
            };

            using var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Cookie", clientInfoRequest.Cookies);

            // add default headers like browser (optional)
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/140.0.0.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,vi;q=0.8");
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br, zstd");
            client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
            client.DefaultRequestHeaders.Add("Host", host);
            client.DefaultRequestHeaders.Add("Origin", baseUrl);
            client.DefaultRequestHeaders.Add("Referer", url);
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Chromium\";v=\"140\", \"Not=A?Brand\";v=\"24\", \"Microsoft Edge\";v=\"140\"");
            client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

            var formData = new Dictionary<string, string>
            {
                { "mst", clientInfoRequest.TaxCode },
                { "captcha", clientInfoRequest.CaptchaCode },
                { "cm", "cm" }
            };
            var content = new FormUrlEncodedContent(formData);

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse responseBody to extract client information
            var doc = new HtmlDocument();
            doc.LoadHtml(responseBody);

            var rows = doc.DocumentNode.SelectNodes("//table[@class='ta_border']//tr[td]");
            if(rows == null)
            {
                logger.LogWarning("TraCuuNNT: Captcha does not correct");
                return new ClientInfoResponse
                {
                    Status = ResponeStatus.Failed,
                    Message = "Mã xác thực không đúng hoặc hết hạn",
                    ClientInformation = null
                };
            }
            else if (rows[0].SelectNodes("td").Count == 1)
            {
                logger.LogWarning("TraCuuNNT: Client information not found");
                return new ClientInfoResponse
                {
                    Status = ResponeStatus.Failed,
                    Message = "Không tìm thấy thông tin mã số thuế",
                    ClientInformation = null
                };
            }
            else
            {
                logger.LogInformation("TraCuuNNT: Get client information successfully");
                return new ClientInfoResponse
                {
                    Status = ResponeStatus.Success,
                    Message = "",
                    ClientInformation = new ClientInfomation
                    {
                        TaxCode = clientInfoRequest.TaxCode,
                        FullName = rows[0].SelectNodes("td")[2].InnerText.Trim(),
                        Address = rows[0].SelectNodes("td")[3].InnerText.Trim()
                    }
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"TraCuuNNT: Failed to get client information: {ex.Message}");
            return new ClientInfoResponse
            {
                Status = ResponeStatus.Error,
                Message = "Unexpected error",
                ClientInformation = null
            };
        }
    }

    private async Task<ClientInfoResponse> GetThongTinCongTyDotVnClientInfoFromTaxCodeAsync(string taxCode)
    {
        try
        {
            // use Playwright to simulate a real browser to advoid bot detection
            // this page parameter is in url format: https://thongtincongty.vn/ma-so-thue/{taxCode}-anyletter?taxCode={taxCode}
            // * just need to replace {taxCode} with the actual tax code, the rest of the url(anyletter) can be anything but must be atleast 1 character after "-"
            StringBuilder url = new StringBuilder();
            url.Append(configuration.GetValue<string>("ThongTinCongTyDotVn:BaseUrl"));
            url.Append(taxCode);
            url.Append("-anyletter?taxCode=");
            url.Append(taxCode);

            var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.GotoAsync(url.ToString());

            var nameLocator = page.Locator(configuration.GetValue<string>("ThongTinCongTyDotVn:nameLocator")!);
            await nameLocator.WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = 10000
            });
            var name = await nameLocator.InnerTextAsync();

            // There are two "address" locator, we need to get the first one
            var addressLocator = page.Locator(configuration.GetValue<string>("ThongTinCongTyDotVn:addressLocator")!).First;
            await addressLocator.WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = 1000 
            });
            var address = await addressLocator.InnerTextAsync();

            logger.LogInformation("thongtincongty.vn: Get client information successfully");
            return new ClientInfoResponse
            {
                Status = ResponeStatus.Success,
                Message = "",
                ClientInformation = new ClientInfomation
                {
                    TaxCode = taxCode,
                    FullName = name,
                    Address = address
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to get client information from thongtincongty.vn: {ex.Message}");
            return new ClientInfoResponse
            {
                Status = ResponeStatus.Error,
                Message = "Unexpected error",
                ClientInformation = null
            };
        }
    }
}
