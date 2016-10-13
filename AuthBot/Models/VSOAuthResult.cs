using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthBot.Models
{

    public class VSOAuthResult
    {
        public VSOAuthResult() { }

        [JsonProperty(PropertyName = "access_token")]
        public String accessToken { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public String tokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public String expiresIn { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public String refreshToken { get; set; }
        public static VSOAuthResult FromVSOAuthenticationResult(String strWebAuthResponse)
        {
            VSOAuthResult token = new VSOAuthResult();
            token = JsonConvert.DeserializeObject<VSOAuthResult>(strWebAuthResponse);

            return token;
        }
    }
    

    


}
