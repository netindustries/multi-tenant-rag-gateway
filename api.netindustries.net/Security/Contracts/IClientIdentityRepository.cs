using System;
using System.Threading.Tasks;

namespace NII.Security.Contracts
{
    public interface IClientIdentityRepository
    {
        Task<Tuple<string, string, bool>> VerifyIdentityByHashAsync(string apiKeyHash);
        Task RegisterIdentityAsync(string clientName, string emailAddress, string apiKeyHash);
    }
}
