namespace SthinkGetDataFromTaxCodeApi.Models;

public class ClientInfoResponse
{
    public string Status { get; set; } = default!;
    public string Message { get; set; } = default!;
    public ClientInfomation? ClientInformation { get; set; } 

}

public class ClientInfomation
{
    public string TaxCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Address { get; set; } = default!;
}

public class ResponeStatus
{
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Error = "Error";
}
    