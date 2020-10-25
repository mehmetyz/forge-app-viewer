using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Autodesk.Forge;

namespace ForgeApp.Controllers
{
    public delegate void RestErrorHandler(string errorMessage);
    public delegate void UploadEventHandler(bool finished,OSSRequest.OSSFile file);
    public delegate void ProgressiveUploadEventHandler(bool finished,OSSRequest.OSSFile file,int progress);
    
    public class OSSRequest
    {
        private RestClient client;
        private RestRequest request;

        public Auth.Token token;

        public OSSRequest(Auth.Token token)
        {
            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exception("OSS Configuration Error.");
            }

            this.token = token;
            client = new RestClient();
        }

        public event RestErrorHandler ErrorOccurred;
        public async Task<bool> CreateBucket(string key)
        {
            client.BaseUrl = new Uri("https://developer.api.autodesk.com/oss/v2/buckets");
            request = new RestRequest(Method.POST);

            request.AddHeader("Content-Type", "application/json").
                AddHeader("Authorization", $"{this.token.TokenType} {this.token.AccessToken}").
                AddJsonBody(new
                {
                    bucketKey = key,
                    access = "full",
                    policyKey = "persistent"
                });
            IRestResponse response = await client.ExecuteTaskAsync(request);

            request = null;
            if(response.StatusCode != HttpStatusCode.OK)
            {
                string error = "";
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        error = "The request could not be understood by the server due to malformed syntax or missing request headers. The client SHOULD NOT repeat the request without modifications. The response body may give an indication of what is wrong with the request.";
                        break;
                    case HttpStatusCode.Unauthorized:
                        error = "The supplied Authorization header was not valid or the supplied token scope was not acceptable. Verify Authentication and try again.";
                        break;
                    case HttpStatusCode.Forbidden:
                        error = "The Authorization was successfully validated but permission is not granted. Don’t try again unless you solve permissions first.";
                        break;
                    case HttpStatusCode.Conflict:
                        error = "The specified bucket key already exists.";
                        break;
                    case HttpStatusCode.InternalServerError:
                        error = "Internal failure while processing the request, reason depends on error.";
                        break;
                    default:
                        error = "Unknown error occurred.";
                        break;
                }
                ErrorOccurred?.Invoke(error);
                return false;
            }
            return true;
        }

        public async Task<List<Bucket>> GetAllBuckets()
        {
            List<Bucket> buckets = new List<Bucket>();

            client.BaseUrl = new Uri("https://developer.api.autodesk.com/oss/v2/buckets");
            request = new RestRequest(Method.GET);

            request.AddHeader("Authorization", $"Bearer {this.token.AccessToken}").
                AddQueryParameter("region", "US").
                AddQueryParameter("limit", "100");
            IRestResponse response = await client.ExecuteTaskAsync(request);

            request = null;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                string error = "";
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        error = "The request could not be understood by the server due to malformed syntax or missing request headers. The client SHOULD NOT repeat the request without modifications. The response body may give an indication of what is wrong with the request.The request could not be understood by the server due to malformed syntax or missing request headers. The client SHOULD NOT repeat the request without modifications. The response body may give an indication of what is wrong with the request.";
                        break;
                    case HttpStatusCode.Unauthorized:
                        error = "The supplied authorization header was not valid or the supplied token scope was not acceptable. Verify authentication and try again.";
                        break;
                    case HttpStatusCode.Forbidden:
                        error = "Caller is not authorized to call this endpoint.";
                        break;
                    case HttpStatusCode.NotFound:
                        error = "Resource not found.";
                        break;
                    case HttpStatusCode.InternalServerError:
                        error = "Internal failure while processing the request, reason depends on error.";
                        break;
                    default:
                        error = "Unknown error occurred.";
                        break;
                }
                
                ErrorOccurred?.Invoke(error);
            }
            JObject items = JObject.Parse(response.Content);

            Dictionary<string, dynamic> data;

            foreach (dynamic key in items["items"])
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(key.ToString());
                Bucket bucket = await this.GetBucket(data["bucketKey"]);
                buckets.Add(bucket);
                data = null; 
            }
            return buckets;
        }

        public async Task<Bucket> GetBucket(string bucketKey)
        {
            client.BaseUrl = new Uri($"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/details");
            request = new RestRequest(Method.GET);

            request.AddHeader("Authorization", $"Bearer {this.token.AccessToken}").
                AddHeader("Content-Type", "application/json");

            IRestResponse response = await client.ExecuteTaskAsync(request);
            request = null;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string error = "";
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        error = "The request could not be understood by the server due to malformed syntax or missing request headers. The client SHOULD NOT repeat the request without modifications. The response body may give an indication of what is wrong with the request.";
                        break;
                    case HttpStatusCode.Unauthorized:
                        error = "The supplied authorization header was not valid or the supplied token scope was not acceptable. Verify authentication and try again.The supplied Authorization header was not valid or the supplied token scope was not acceptable. Verify Authentication and try again.";
                        break;
                    case HttpStatusCode.Forbidden:
                        error = "The Authorization was successfully validated but permission is not granted. Don’t try again unless you solve permissions first.";
                        break;
                    case HttpStatusCode.NotFound:
                        error = "The specified bucket does not exist.";
                        break;
                    case HttpStatusCode.InternalServerError:
                        error = "Internal failure while processing the request, reason depends on error.";
                        break;
                    default:
                        error = "Unknown error occurred.";
                        break;
                }
                
                ErrorOccurred?.Invoke(error);
            }

            Dictionary<string, dynamic> data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response.Content);

            Bucket bucket = new Bucket(data["bucketKey"],
                data["bucketOwner"],
                (long)data["createdDate"],
                data["policyKey"]);

            return bucket;
        }

        public async Task<bool> DeleteBucket(string bucketKey)
        {
            client.BaseUrl = new Uri("https://developer.api.autodesk.com/oss/v2/buckets/"+bucketKey);
            request = new RestRequest(Method.DELETE);

            request.AddHeader("Authorization", $"Bearer {this.token.AccessToken}");

            IRestResponse response = await client.ExecuteTaskAsync(request);
            request = null;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string error = "";
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        error = "The request could not be understood by the server due to malformed syntax or missing request headers. The client SHOULD NOT repeat the request without modifications. The response body may give an indication of what is wrong with the request.";
                        break;
                    case HttpStatusCode.Unauthorized:
                        error = "The supplied Authorization header was not valid or the supplied token scope was not acceptable. Verify authentication and try again.";
                        break;
                    case HttpStatusCode.Forbidden:
                        error = "The Authorization was successfully validated but permission is not granted. Don’t try again unless you solve permissions first.";
                        break;
                    case HttpStatusCode.NotFound:
                        error = "The specified bucket does not exist.";
                        break;
                    case HttpStatusCode.Conflict:
                        error = "The bucket is currently marked for deletionThe bucket is currently marked for deletion";
                        break;
                    case HttpStatusCode.InternalServerError:
                        error = "Internal failure while processing the request, reason depends on error.";
                        break;
                    default:
                        error = "Unknown error occurred.";
                        break;
                }
                
                ErrorOccurred?.Invoke(error);

                return false;
            }

            return true;
        }

        public class Bucket
        {
            public string BucketKey { get; private set; }
            public string Owner { get; private set; }
            public DateTime CreatedDate { get; private set; } = new DateTime(0);
            public string PolicyKey { get; private set; }
            public Bucket(string bucketKey,string owner,long createdDate,string policyKey)
            {
                this.BucketKey = bucketKey;
                this.Owner = owner;
                this.CreatedDate += TimeSpan.FromMilliseconds(createdDate);
                this.PolicyKey = policyKey;
            }

            public override string ToString()
            {
                return string.Format("Bucket Key: {0}\n" +
                    "Owner: {1}\n" +
                    "CreateDate: {2}\n" +
                    "Policy Key: {3}\n",
                    this.BucketKey,
                    this.Owner,
                    this.CreatedDate.ToShortDateString(),
                    this.PolicyKey);
            }
        }

        ///


        ///OBJECTS
        public event UploadEventHandler Upload;
        public async Task UploadObject(string bucketKey, OSSFile file)
        {

            string objectKey = file.Name;
            MemoryStream data = new MemoryStream(File.ReadAllBytes(file.Path));
            client.BaseUrl = new Uri($"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}");
            request = new RestRequest(Method.PUT);

            request.AddHeader("Authorization", $"{this.token.TokenType} {this.token.AccessToken}").
                AddHeader("Content-Length", data.Length.ToString()).
                AddParameter("application/octet-stream", data.ToArray(),ParameterType.RequestBody);

            Upload?.Invoke(false,file);
            data.Close();
            IRestResponse response = await client.ExecuteTaskAsync(request);
            while (response.StatusCode != HttpStatusCode.OK)
            {
                Upload?.Invoke(false,file);
            }
            Upload?.Invoke(true,file);
        }

        public event ProgressiveUploadEventHandler ProgressUpload;
        public async Task UploadChunkObject(string bucketKey,OSSFile file)
        {

            FileStream data = File.OpenRead(file.Path);
            var length = data.Length;
            string objectKey = file.Name;

            if (await this.GetBucketObject(bucketKey, objectKey) != null)
                await this.DeleteObject(bucketKey, objectKey);

            string sessionId = "SI" + Path.GetFileNameWithoutExtension(objectKey) + length;
            
            ObjectsApi api = new ObjectsApi();

            api.Configuration.AccessToken = this.token.AccessToken;

            int chunkSize = 5 * 1024 * 1024;

            long chunkCount = (long) Math.Round(0.5 + ((double) length / (double) chunkSize));

            long dataWrited = 0;

            for (int i = 0; i < chunkCount; i++)
            {
                long startByte = i * chunkSize; 

                long next = (i + 1) * chunkSize; 

                if (next > length)
                    next = length - 1;
                else
                    next--;

                long byteLength = next - startByte + 1;

                string bytesRange = string.Format("bytes {0}-{1}/{2}", startByte, next, length); 
                byte[] buffer = new byte[byteLength];

                data.Read(buffer, 0, (int)byteLength);
                MemoryStream ms = new MemoryStream(buffer);
                ms.Write(buffer, 0, (int)byteLength);
                ms.Position = 0;

                var response = await api.UploadChunkAsyncWithHttpInfo(bucketKey, objectKey, (int)byteLength, bytesRange, sessionId,ms);

                dataWrited += byteLength;

                double progress = (double)(100 * decimal.Ceiling(dataWrited) / decimal.Ceiling(length)); 

                if(response.StatusCode== 202)
                {
                    ProgressUpload?.Invoke(false, file,Convert.ToInt32(progress));
                }
                else if(response.StatusCode == 200)
                {
                    Upload?.Invoke(true, file);
                    break;
                }
                else
                {
                    ErrorOccurred("Error occurred while uploading...\nCode: "+response.StatusCode);
                    break;
                }

            }
            data.Close();
        }

        public async Task<HttpStatusCode> GetStatusOfChunkFiles(string bucketKey,string objectKey,string sessionId)
        {
            client.BaseUrl = new Uri($"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/status/{sessionId}");
            request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", $"{this.token.TokenType} {this.token.AccessToken}");
            IRestResponse response = await client.ExecuteTaskAsync(request);

            return response.StatusCode;
        }
        public async Task<List<BucketObject>> GetAllObjects(string bucketKey)
        {
            List<BucketObject> objects = new List<BucketObject>();

            client.BaseUrl = new Uri($"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects");
            request = new RestRequest(Method.GET);

            request.AddHeader("Authorization", $"{this.token.TokenType} {this.token.AccessToken}").
                AddHeader("Content-Type", "application/json");

            IRestResponse response = await client.ExecuteTaskAsync(request);

            if(response.StatusCode != HttpStatusCode.OK)
            {
                string error = "";
                switch (response.StatusCode)
                {
                  
                    case HttpStatusCode.BadRequest:
                        error = "The request could not be understood by the server due to malformed syntax or missing request headers. The client SHOULD NOT repeat the request without modifications. The response body may give an indication of what is wrong with the request.";
                        break;
                    case HttpStatusCode.Unauthorized:
                        error = "The supplied Authorization header was not valid or the supplied token scope was not acceptable. Verify Authentication and try again.";
                        break;
                    case HttpStatusCode.Forbidden:
                        error = "The Authorization was successfully validated but permission is not granted. Don’t try again unless you solve permissions first.";
                        break;
                    case HttpStatusCode.NotFound:
                        error = "The bucket does not exist.";
                        break;
                    case HttpStatusCode.InternalServerError:
                        error = "Internal failure while processing the request, reason depends on error.";
                        break;
                    default:
                        error = "Unknown error occurred.";
                        break;
                }
                ErrorOccurred?.Invoke(error);
                return null;
            }
            JObject items = JObject.Parse(response.Content);
            Dictionary<string, dynamic> data;
            foreach (dynamic key in items["items"])
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(key.ToString());

                BucketObject bucketObject = await this.GetBucketObject(data["bucketKey"],data["objectKey"]);
                objects.Add(bucketObject);
                data = null;
            }

            return objects;
        }
        public async Task<BucketObject> GetBucketObject(string bucketKey, string objectKey)
        {
            BucketObject obj = null;
            client.BaseUrl = new Uri(
                $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/details");
            request = new RestRequest(Method.GET);

            request.AddHeader("Authorization", $"{this.token.TokenType} {this.token.AccessToken}").
                AddHeader("Content-Type", "application/json");

            IRestResponse response = await client.ExecuteTaskAsync(request);
            Dictionary<string, dynamic> data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response.Content);
            if (data.Count != 0)
            {
                try
                {
                    obj = new BucketObject(data["bucketKey"],
                    data["objectId"],
                    data["objectKey"],
                    (long)data["size"],
                    data["location"]);

                }
                catch
                {
                    obj = null;
                }

            }
            return obj;
        }

        public async Task<bool> DeleteObject(string bucketKey,string objectKey)
        {
            client.BaseUrl = new Uri(
                $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}");
            request = new RestRequest(Method.DELETE);

            request.AddHeader("Authorization", $"{this.token.TokenType} {this.token.AccessToken}").
                AddHeader("Content-Type", "application/json");

            IRestResponse response = await client.ExecuteTaskAsync(request);
            if(response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            return true;
        }

        public class OSSFile
        {
            public string Path { get; private set; }
            public string Name { get; private set; }
            public long Size { get; private set; }
            public OSSFile(string source)
            {
                var info = new FileInfo(source);
                this.Path = source;
                this.Name = info.Name;
                this.Size = info.Length;
            }
        }
        public class BucketObject
        {
            public string BucketKey { get; private set; }
            public string ObjectID { get; private set; }
            public string ObjectKey { get; private set; }
            public long Size { get; private set; } 
            public string ContentUrl { get; private set; }
            public string Image { get;  set; }
            public string URN { get; set; }


            public BucketObject(string bucketKey,string objectId,string objectKey,long size,string contentUrl)
            {
                this.BucketKey = bucketKey;
                this.ObjectID = objectId;
                this.ObjectKey = objectKey;
                this.Size = size;
                this.ContentUrl = contentUrl;
            }

            public override string ToString()
            {
                return string.Format(
                    "Bucket Key: {0}\n" +
                    "Object ID: {1}\n" +
                    "Object Key: {2}\n" +
                    "Object Size: {3}\n" +
                    "Object URL: {4}\n",
                    this.BucketKey,
                    this.ObjectID,
                    this.ObjectKey,
                    this.Size,
                    this.ContentUrl);
            }
        }

        ///
    }
}
