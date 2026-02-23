namespace SthinkGetDataFromTaxCodeApi.Models;

public class ClientInfoRequest
{
    public string TaxCode { get; set; } = default!;
    public string Cookies { get; set; } = string.Empty;
    public string CaptchaCode { get; set; } = string.Empty;
}
