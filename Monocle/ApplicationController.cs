using Monocle.Properties;
using Smithers.Client;
using Smithers.Sessions;
using Smithers.Sessions.Archiving;
using SmithersDS4.Reading;
using SmithersDS4.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monocle
{
    public class ApplicationController
    {
        CaptureController _captureController;
        UploadManager _uploadManager;
        SmithersLogger _logger;

        public ApplicationController(string baseDirectory)
        {

            _logger = new SmithersLogger(baseDirectory);
            _captureController = new CaptureController(baseDirectory, _logger);           
                _uploadManager = new UploadManager();

            //Read credential into settings
            try
            {
                _loadCredentials(baseDirectory);
            }
            catch (InvalidDataException e)
            {
                _logger.info("Bad Credentials format: " + e.Message);
                return;
            }

        }

        private void _loadCredentials(string baseDirectory)
        {
            string credentialFile = Path.Combine(baseDirectory, "credentials.json");
            if (File.Exists(credentialFile))
            {
                Dictionary<string, string> credentials; 
                //Load from file
                try
                {
                    credentials = JSONHelper.Instance.DeserializeObject<Dictionary<string, string>>(credentialFile);
                }
                catch (Exception)
                {
                    throw new InvalidDataException("JSON deserialization ERROR");
                }
                string accessKey, secret;

                if (credentials.TryGetValue("accessKey", out accessKey) && credentials.TryGetValue("secret", out secret))
                {
                    Settings.Default.accessKey = accessKey;
                    Settings.Default.secret = secret;
                    Settings.Default.Save();
                }
            }
            else
            {
                Dictionary<string, string> credentials = new Dictionary<string,string>() { {"accessKey", ""}, {"secret", ""}};
                JSONHelper.Instance.Serialize(credentials, credentialFile);
            }
        }

        public CaptureController CaptureController { get { return _captureController; } }

        public UploadManager UploadManager { get { return _uploadManager; } }

        public async Task CompressAndUploadAndStartNewSession()
        {
            DS4Session oldSession = _captureController.Session;

            await _uploadManager.ArchiveAndStartUpload(oldSession, Settings.Default.apiBase, Settings.Default.accessKey, Settings.Default.secret);
            
        }

        public void TryCancelUpload()
        {
            Uploader uploader;

            if (_uploadManager.AllUploads.TryGetValue(_captureController.Session, out uploader))
            {
                _logger.info("Upload Canceled");
                uploader.TryCancel();
            }
        }
    }
}
