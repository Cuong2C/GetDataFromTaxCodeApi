using Microsoft.AspNetCore.Http.HttpResults;
using SthinkGetDataFromTaxCodeApi.Models;

namespace SthinkGetDataFromTaxCodeApi.Services
{
    public interface ISessionInfoServices
    {
        Task<Results<Ok<SessionInfoResponse>, BadRequest<string>>> GetSessionInfoAsync();
    }
}
