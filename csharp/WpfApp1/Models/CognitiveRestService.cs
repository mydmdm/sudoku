using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfApp1.Models
{
    public class RestRequestInfo
    {
        public RestRequestInfo(RestSharp.Method method, string root)
        {
            Method = method;
            Root = root;
        }
        public RestSharp.Method Method { get; set; }
        public string Root { get; set; }
    }
    class CognitiveRestService
    {
        public CognitiveRestService(string key, string endpoint)
        {
            SubscriptionKey = key;
            SubscriptionEndpoint = endpoint;
            MaxRetry = 10;
        }

        public string SubscriptionKey { get; set; }
        public string SubscriptionEndpoint { get; set; }
        public int MaxRetry { get; set; }

        public RestRequestInfo RequestInfo;
        public Dictionary<string, string> Headers;
        public Dictionary<string, string> Parameters;
        public RestSharp.RestRequest Request;

        public RestSharp.RestRequest CreateRequest(RestRequestInfo req, Dictionary<string,string> headers, Dictionary<string,string> parameters)
        {
            Request = new RestSharp.RestRequest(req.Root, req.Method);
            if (headers != null)
            {
                foreach (var head in headers)
                {
                    Request.AddHeader(head.Key, head.Value);
                }
            }
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    Request.AddParameter(param.Key, param.Value);
                }
            }
            return Request;
        }

        public RestSharp.RestRequest CreateRequest()
        {
            return CreateRequest(RequestInfo, Headers, Parameters);
        }

        public void AddData(object data)
        {
            if (Headers["Content-Type"] == "application/octet-stream")
            {
            }
            else if (Headers["Content-Type"] == "application/json")
            {

            }
        }

        public RestSharp.IRestResponse SendRequest(int maxRetry = 10)
        {
            var client = new RestSharp.RestClient();
            RestSharp.IRestResponse response = client.Execute(Request);
            return response;
        }

        public void RecognizeTextDefault()
        {
            RequestInfo = new RestRequestInfo(RestSharp.Method.POST, String.Format("{0}/{1}?", SubscriptionEndpoint, "recognizeText"));
            Parameters = new Dictionary<string, string>{
                { "handwriting", "true" },
            };
            Headers = new Dictionary<string, string>{
                { "Ocp-Apim-Subscription-Key", SubscriptionKey },
                { "Content-Type", "application/octet-stream" },
            };
        }
    }
}
