using System;
using System.Threading.Tasks;

namespace NII.Security.Contracts
{
    public interface IInfrastructureSecretRepository
    {
        Task<Tuple<byte[], byte[]>> FetchSecretAsync(string secretKey);
        Task SaveSecretAsync(string secretKey, byte[] encryptedData, byte[] iv);
    }
}
