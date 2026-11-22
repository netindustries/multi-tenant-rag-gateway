using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NII.Security.Configuration;
using NII.Security.Contracts;

namespace NII.Security.DataAccess
{
    public class SqlClientIdentityRepository : IClientIdentityRepository
    {
        private readonly SecuritySettings _settings;

        public SqlClientIdentityRepository(IOptions<SecuritySettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<Tuple<string, string, bool>> VerifyIdentityByHashAsync(string apiKeyHash)
        {
            using (var conn = new SqlConnection(_settings.SecureConn))
            {
                using (var cmd = new SqlCommand("APISecurity.usp_GetIdentityByApiKeyHash", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ApiKeyHash", SqlDbType.NVarChar, 64).Value = apiKeyHash;
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return Tuple.Create(
                                reader["ClientName"].ToString(),
                                reader["EmailAddress"].ToString(),
                                Convert.ToBoolean(reader["IsActive"])
                            );
                        }
                    }
                }
            }
            return null;
        }

        public async Task RegisterIdentityAsync(string clientName, string emailAddress, string apiKeyHash)
        {
            using (var conn = new SqlConnection(_settings.SecureConn))
            {
                using (var cmd = new SqlCommand("APISecurity.usp_InsertClientIdentity", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ClientName", SqlDbType.NVarChar, 100).Value = clientName;
                    cmd.Parameters.Add("@EmailAddress", SqlDbType.NVarChar, 256).Value = emailAddress;
                    cmd.Parameters.Add("@ApiKeyHash", SqlDbType.NVarChar, 64).Value = apiKeyHash;
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
