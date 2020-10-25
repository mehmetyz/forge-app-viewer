using System;
using System.Collections.Generic;
using Autodesk.Forge;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ForgeApp.Controllers
{
    
    public class Auth
    {
        private TwoLeggedApi api;
        private Scope[] scopes;
        private string clientID = "";
        private string clientSecret = "";

        public class Token
        {
            public string TokenType { get; private set; }
            public int ExpiresIn { get; private set; }
            public DateTime ExpiresAt { get; set; }
            public string AccessToken { get; private set; }
            public Token(string type, int expiresIn, string accessToken)
            {

                this.TokenType = type;
                this.ExpiresIn = expiresIn;
                this.AccessToken = accessToken;
                this.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            }
        }

        public Auth()
        {
            api = new TwoLeggedApi();
            scopes = new Scope[] {
                Scope.BucketCreate,
                Scope.BucketDelete,
                Scope.BucketRead,
                Scope.DataCreate,
                Scope.DataRead,
                Scope.DataWrite,
                Scope.CodeAll
            };

            this.clientID = GetEnvVariable("FORGE_CLIENT_ID");
            this.clientSecret = GetEnvVariable("FORGE_CLIENT_SECRET");
        }

        /// <summary>
        /// Get access token sync.
        /// </summary>
        /// <returns>Access token</returns>
        public Token GetToken()
        {
            var response = api.Authenticate(clientID, clientSecret,
              grantType: "client_credentials", scopes);

            if (string.IsNullOrEmpty(response.ToString()))
                return null;
            Dictionary<string, dynamic> json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response.ToString());
            Token token = new Token(json["token_type"],
                (int)json["expires_in"],
                json["access_token"]);


            return token;

        }

        /// <summary>
        /// Get access token async.
        /// </summary>
        /// <returns>Access token</returns>

        public async Task<Token> GetTokenAsync()
        {
            var response =  await api.AuthenticateAsync(clientID,clientSecret,
              grantType: "client_credentials", scopes);

            if (string.IsNullOrEmpty(response.ToString()))
                return null;

            Dictionary<string, dynamic> json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response.ToString());
            Token token = new Token(json["token_type"],
                (int)json["expires_in"],
                json["access_token"]);

            return token;

        }


        /// <summary>
        /// Get determined variable.
        /// </summary>
        /// <returns>Variable that was determined.</returns>
        public static string GetEnvVariable(string key)
        {
            return Environment.GetEnvironmentVariable(key).Trim();
        }
    }
}
