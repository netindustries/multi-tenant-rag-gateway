
-- Target Database Context: APISupport
USE APISupport;
GO

INSERT INTO APISecurity.ApiIdentities (ClientName, EmailAddress, ApiKeyHash, IsActive)
VALUES (
    'LocalDevAdmin', 
    'ckissinger@netindustries.net', 
    'e8cc7093e8ad11d4bac840e97fe29013cebdb18784a5216c13b6fbb25f31df15', -- Paste your generated hash here
    1
);
GO
