using Microsoft.AspNetCore.Authorization;
using SthinkGetDataFromTaxCodeApi.Authentication;
using SthinkGetDataFromTaxCodeApi.Models;
using SthinkGetDataFromTaxCodeApi.Services;

namespace SthinkGetDataFromTaxCodeApi.Apis;

public static class ApplicationApi
{
    public static IEndpointRouteBuilder MapApplicationApi(this IEndpointRouteBuilder endpoints)
    {
        var vApi = endpoints.NewVersionedApi("TaxCode");
        var v1 = vApi.MapGroup("/api/v{version:apiVersion}").HasApiVersion(1, 0);

        v1.MapGet("/session-infomation", [Authorize] (ISessionInfoServices sessionInfoServices) => sessionInfoServices.GetSessionInfoAsync());
        v1.MapPost("/client-infomation", [Authorize] (IClientInfoServices clientInfoServices, ClientInfoRequest clientInfoRequest) => 
        clientInfoServices.GetClientInfoAsync(clientInfoRequest));

        v1.MapPost("/auth/token", (IApplicationAuthenticationServices applicationAuthenticationSevices) => 
        TypedResults.Ok(new { Token = applicationAuthenticationSevices.GenerateToken() })).AllowAnonymous();

        return endpoints;
    }
}
