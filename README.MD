Requires JWT_SECRET from vault  
Vault secret should be: mountPoint = "secret", path = "auth"  

User-service also requires mongoDB variables from vault: mountPoint "secret", path = "mongo"  
Needs: MONGO_CONNECTION_STRING && MONGO_DATABASE_NAME  

Also requires VAULT_URL env variable