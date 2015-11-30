using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Smithers.Client
{
    public abstract class BaseClient
    {
        [DataContract]
        private class ErrorResponse
        {
            [DataMember(Name = "detail")]
            public string Detail { get; set; }
        }

        string _apiBase;
        string _apiKey;
        string _apiSecret;

        public BaseClient(string apiKey, string apiSecret, string apiBase)
        {
            _apiKey = apiKey;
            _apiBase = apiBase;
            _apiSecret = apiSecret;
        }

        protected Uri BuildUri(string path, bool withApiKey = false)
        {
            string uri = _apiBase + path;
            if (withApiKey) uri += "?api_key=" + _apiKey;

            return new Uri(uri, UriKind.Absolute);
        }

        public static string GetErrorMsgFromWebException(WebException e)
        {
            if (e.Response == null) return null;

            Stream responseStream = e.Response.GetResponseStream();

            if (!responseStream.CanRead) return null;

            if (e.Response.ContentType == "application/json")
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ErrorResponse));
                ErrorResponse errorResponse = (ErrorResponse)serializer.ReadObject(responseStream);
                return errorResponse.Detail;
            }
            else
            {
                return e.Message;
            }
        }
    }
}
