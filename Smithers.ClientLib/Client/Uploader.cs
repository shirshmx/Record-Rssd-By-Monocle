using Smithers.ClientLib;
using Smithers.Sessions;
using SmithersDS4.Sessions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smithers.Client
{
    public class UploadResult
    {
        public bool Success { get; set; }
        public bool Canceled { get; set; }
        public WebException Exception { get; set; }
        public string BodyId { get; set; }

        public List<Uploader.DS4Artifacts> Artifacts {get; set;}

    }

    public class DS4BodyRequest
    {
        public string bodyType { get; set; }
        public string uploadedObjectKey {get; set; }
        public List<Dictionary<string, string>> artifacts { get; set; }

        public DS4BodyRequest(string bodyType, string objectKey, List<Dictionary<string, string>> artifacts)
        {
            this.bodyType = bodyType;
            this.uploadedObjectKey = objectKey;
            this.artifacts = artifacts;
        }
    }

    /// <summary>
    /// Handles the data upload to the cloud.
    /// </summary>
    public class Uploader : BaseClient, INotifyPropertyChanged
    {
        bool _started = false;
        bool _canceled;
        bool _waiting = false;

        WebClient _s3Proxy;
        CancellationTokenSource _cancellationTokenSource;
        
        long _totalBytesToSend = 0;
        long _bytesSent = 0;
        int _percentComplete = 0;

        UploadResult _result;
        AutoReTryer _reTryer;

        public event EventHandler<UploadResult> Completing;
        public event EventHandler<UploadResult> Completed;
        public event EventHandler<UploadResult> Cancelled;
        public event PropertyChangedEventHandler PropertyChanged;
        private DS4Session _session;
        private  string _authHeader;

        [DataContract]
        public class DS4Artifacts
        {
            [DataMember(Name = "artifactsType")]
            public string ArtifactsType { get; set; }

            [DataMember(Name = "id")]
            public string ArtifactsId { get; set; }

        }

        [DataContract]
        private class DS4PostUploadResponse
        {
            [DataMember(Name = "id")]
            public string BodyId { get; set; }

            [DataMember(Name = "artifacts")]
            public List<DS4Artifacts> Artifacts { get; set; }
        }

        public bool Waiting
        {
            get { return _waiting; }
            set 
            { 
                _waiting = value;
                OnPropertyChanged("Waiting");
                OnPropertyChanged("Result"); 
            }
        }

        public bool ReTrying { get { return _reTryer != null ? _reTryer.SecondsUntilNextRetry != null : false; } }

        public bool InProgress { get { return _started && _result == null; } }
        public bool Canceled { get { return _canceled; } }
        public bool Success { get { return _result == null ? false : _result.Success; } }
        public UploadResult Result { get { return _result; } }
        public AutoReTryer ReTryer { get { return _reTryer; } set { _reTryer = value; OnPropertyChanged("ReTryer"); } }
        public string BytesSent { get { return string.Format(new FileSizeFormatProvider(), "{0:fs1}", _bytesSent); } }
        public string TotalBytesToSend { get { return string.Format(new FileSizeFormatProvider(), "{0:fs1}", _totalBytesToSend); } }
        public string ProgressInMB
        {
            get
            {
                return string.Format("{0} OF {1} MB", ((float)_bytesSent / (1024.0F * 1024.0F)).ToString("0.0"),
                             ((float)_totalBytesToSend / (1024.0F * 1024.0F)).ToString("0.0"));
            }
        }
        public int PercentComplete { get { return _percentComplete; } }

        public Uploader(string apiKey, string apiSecret, string apiBase, DS4Session session) : base(apiKey, apiSecret, apiBase) 
        {
            _session = session;
           
            _authHeader =  "SecretPair accessKey=" + apiKey + ",secret=" + apiSecret;
        }

        public async Task<UploadResult> PerformUpload()
        {
            if (this.InProgress)
                throw new InvalidOperationException("Uploader was already started");

            _started = true;
            _result = null;
            this.Waiting = false;
            OnPropertyChanged("InProgress");

            _cancellationTokenSource = new CancellationTokenSource();
             
            UploadResult result;
            try
            {
                result = await Upload(_session, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _canceled = true;
                result =  new UploadResult { Success = false,  Canceled = true };
               
            }

            if (Completing != null) Completing(this, result);

            _result = result;

            if (Completed != null) Completed(this, result);
            
            OnPropertyChanged("InProgress");
            OnPropertyChanged("Success");
            OnPropertyChanged("Result");

            return result;
        }

        private void Progress(object sender, UploadProgressChangedEventArgs e)
        {
            _bytesSent = e.BytesSent;
            _totalBytesToSend = e.TotalBytesToSend;
            _percentComplete = (int)(100F * e.BytesSent / e.TotalBytesToSend);

            OnPropertyChanged("BytesSent");
            OnPropertyChanged("TotalBytesToSend");
            OnPropertyChanged("PercentComplete");
            OnPropertyChanged("ProgressInMB");
        }

        /// <summary>
        /// Uploads the specified file to   the cloud.
        /// </summary>
        /// <param name="file">The full path to the desired .zip file.</param>
        private async Task<UploadResult> Upload(DS4Session session, CancellationToken ct)
        {
            string file = session.CompressedScanFile;
            string contentType = "application/x/7z-compressed";

            WebClient proxy;

            string result = string.Empty;

            // Step1: Get s3 signed URL
            proxy = new WebClient();

            // Gather data
            string fileName = Path.GetFileName(file);
            
            Dictionary<string, string> postDict = new Dictionary<string, string> {
                {"filename", fileName},
                {"file_type", contentType},
            };

            String postData = JSONHelper.Instance.Serialize(postDict);
            // Prepare request
            proxy.Headers["Content-Type"] = "application/json";
            proxy.Headers["Authorization"] = _authHeader;

            // Perform request            
            try
            {
                result = await proxy.UploadStringTaskAsync(this.BuildUri("s3/sign"), "POST", postData);
            }
            catch (WebException ex)
            {
                return new UploadResult { Success = false, Exception = ex };
            }

            ct.ThrowIfCancellationRequested();

            // Step 2: Upload to s3 signed PUT URL
            _s3Proxy = proxy = new WebClient();
            proxy.UploadProgressChanged += new UploadProgressChangedEventHandler(this.Progress);
            proxy.UploadDataCompleted += (s , e) => { if(ct.IsCancellationRequested) _canceled = true; };

            // Gather data
            Dictionary<string, string> response = JSONHelper.Instance.CreateDictionary(result);
            string url = response["signedUrl"];
            string key = response["key"];

            byte[] binaryData = File.ReadAllBytes(file);

            // Prepare headers
            proxy.Headers["Content-Type"] = contentType;

            // Prepare request
            Uri uri = new Uri(url, UriKind.Absolute);
            // Perform request
            try
            {
                byte[] uploadResponse = await proxy.UploadDataTaskAsync(uri, "PUT", binaryData);
            }
            catch (WebException ex)
            {
                
                Console.WriteLine(Uploader.GetErrorMsgFromWebException(ex));
                return new UploadResult { Success = false, Exception = ex, Canceled = _canceled };
            }

            ct.ThrowIfCancellationRequested();

            // Step 3: PostUpload and get returned BodyId
            proxy = new WebClient();

            //Assemble payload
            List<Dictionary<string, string> > artifacts = new List<Dictionary<string, string> >();
            artifacts.Add(new Dictionary<string, string> { 
                {"artifactsType","DS4Measurements"}
            });

            artifacts.Add(new Dictionary<string, string> { 
                {"artifactsType","DS4Alignment"}
            });

            DS4BodyRequest bodyRequest = new DS4BodyRequest("ds4_scan", key, artifacts);

            postData = JSONHelper.Instance.Serialize(bodyRequest);

            // Prepare request
            proxy.Headers["Content-Type"] = "application/json";
            proxy.Headers["Authorization"] = _authHeader;

            // Perform request
            try
            {
                result = await proxy.UploadStringTaskAsync(this.BuildUri("bodies/from_scan"), "POST", postData);
            }
            catch (WebException ex)
            {
                Console.WriteLine(Uploader.GetErrorMsgFromWebException(ex));
                return new UploadResult { Success = false, Exception = ex };
            }

            DS4PostUploadResponse ds4PostUploadResponse;
            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(result)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DS4PostUploadResponse));
                ds4PostUploadResponse = (DS4PostUploadResponse)serializer.ReadObject(ms);
                ms.Close();
            }

            string bodyId = ds4PostUploadResponse.BodyId;

            return new UploadResult { Success = true, BodyId = bodyId, Artifacts = ds4PostUploadResponse.Artifacts };
        }

        public void TryCancel()
        {
            _cancellationTokenSource.Cancel();
            if(_s3Proxy != null)
                _s3Proxy.CancelAsync();

            if (ReTryer != null)
                ReTryer.TryCancel();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
