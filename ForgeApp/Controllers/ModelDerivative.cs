using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autodesk.Forge;
using Autodesk.Forge.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForgeApp.Controllers
{
    public class ModelDerivative
    {
        private Auth.Token token;
        private DerivativesApi api;
        public string URN { get; private set; }

        public ModelDerivative(Auth.Token accessToken)
        {
            if(accessToken != null && !string.IsNullOrWhiteSpace(accessToken.AccessToken))
                token = accessToken;
        }
        public event EventHandler Failed;
        public event EventHandler Completed;

        /// <summary>
        /// Translate object by safe urn from object.
        /// </summary>
        /// <param name="safeUrn">Object's, that will be translated, URN</param>
        /// <returns>Nothing...</returns>
        public async Task TranslateObject(string safeUrn)
        {
            
            List<JobPayloadItem> outputs = new List<JobPayloadItem>()
            {
            new JobPayloadItem(
                JobPayloadItem.TypeEnum.Svf,
                new List<JobPayloadItem.ViewsEnum>()
                {
                JobPayloadItem.ViewsEnum._2d,
                JobPayloadItem.ViewsEnum._3d
                })
            };
            JobPayload job;
            job = new JobPayload(new JobPayloadInput(safeUrn), new JobPayloadOutput(outputs));
            api = new DerivativesApi();
            api.Configuration.AccessToken = this.token.AccessToken;
            dynamic jobPosted = await api.TranslateAsync(job);
            Dictionary<string, dynamic> callBackData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jobPosted.ToString());

            this.URN = callBackData["urn"].ToString();
            string status = callBackData["result"];
            while (status == "inprogress" || status == "pending" || status == "created")
            {
                dynamic resp = await api.GetManifestAsyncWithHttpInfo(this.URN);
                JObject jObject = JsonConvert.DeserializeObject<JObject>(resp.Data.ToString());

                status = jObject["status"].ToString();

            }
           
            if (status == "success")
                Completed?.Invoke(null, new EventArgs());

            else if (status == "failed" || status == "timeout")
                Failed?.Invoke(null, new EventArgs());

            this.IsTranslated = status == "success";
        }
        public bool IsTranslated { get; private set; } = false;
       
        /// <summary>
        /// Get thumbnail (base64 code) from translate requester.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetThumbnail()
        {
            if (api == null || string.IsNullOrWhiteSpace(this.URN))
                return null;

            MemoryStream ms = await api.GetThumbnailAsync(this.URN,100,100);

            return Convert.ToBase64String(ms.ToArray());

        }
        /// <summary>
        /// Convert string to base 64 code
        /// </summary>
        /// <param name="plainText">String to encode</param>
        /// <returns>Base 64 code</returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
