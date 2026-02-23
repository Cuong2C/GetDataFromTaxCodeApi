namespace SthinkGetDataFromTaxCodeApi.Models;

public class SessionInfoResponse
{
    public string Cookies { get; set; } = default!;
    public string? CaptchaImage { get; set; }
}
