
using Microsoft.AspNetCore.Http.HttpResults;
using SthinkGetDataFromTaxCodeApi.Models;

namespace SthinkGetDataFromTaxCodeApi.Services
{
    public interface IClientInfoServices
    {
        Task<Results<Ok<ClientInfoResponse>, BadRequest<string>>> GetClientInfoAsync(ClientInfoRequest clientInfoRequest);
    }
}
