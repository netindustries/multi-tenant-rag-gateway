using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NII.Security.Configuration;
using NII.Security.Contracts;

namespace NII.Security.DataAccess
{
    public class SqlInfrastructureSecretRepository : IInfrastructureSecretRepository
    {
        private readonly SecuritySettings _settings;

        public SqlInfrastructureSecretRepository(IOptions<SecuritySettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<Tuple<byte[], byte[]>> FetchSecretAsync(string secretKey)
        {
            using (var conn = new SqlConnection(_settings.SecureConn))
            {
                using (var cmd = new SqlCommand("APISecurity.usp_GetSystemSecret", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@SecretKey", SqlDbType.NVarChar, 100).Value = secretKey;
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return Tuple.Create((byte[])reader["EncryptedData"], (byte[])reader["IV"]);
                        }
                    }
                }
            }
            throw new KeyNotFoundException($"System secret '{secretKey}' missing.");
        }

        public async Task SaveSecretAsync(string secretKey, byte[] encryptedData, byte[] iv)
        {
            using (var conn = new SqlConnection(_settings.SecureConn))
            {
                using (var cmd = new SqlCommand("APISecurity.usp_UpsertSystemSecret", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@SecretKey", SqlDbType.NVarChar, 100).Value = secretKey;
                    cmd.Parameters.Add("@EncryptedData", SqlDbType.VarBinary, -1).Value = encryptedData;
                    cmd.Parameters.Add("@IV", SqlDbType.VarBinary, 16).Value = iv;
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
