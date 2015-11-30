using Smithers.ClientLib;
using Smithers.Sessions.Archiving;
using SmithersDS4.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Smithers.Client
{
    public class UploadStatusChangeEventArgs : EventArgs
    {
        public DS4Session Session { get; set; }
        public Uploader Uploader { get; set; }
    }

    public class UploadManager
    {
        /// <summary>
        /// FIXME: Should be adaptive actually
        /// </summary>
        int REUPLOAD_INTERVAL_IN_SECONDS = 10;
        int UPLOAD_WORKERS_COUNT = 2;

        Dictionary<DS4Session, Uploader> _uploaders = new Dictionary<DS4Session, Uploader>();

        AsyncWorkQueue<Uploader, UploadResult> _workQueue;
        public UploadManager()
        {
            _workQueue = new AsyncWorkQueue<Uploader, UploadResult>(UPLOAD_WORKERS_COUNT);
            _workQueue.WorkCompleted += Workqueue_UploadWorkCompleted;
            _workQueue.StartWorking();
        }

        public event EventHandler<UploadStatusChangeEventArgs> UploadStatusChanged;
        public event EventHandler<UploadResult> UploadFinished;

        public void StopRetryUpload(DS4Session session)
        {
            Uploader uploader;

            if (AllUploads.TryGetValue(session, out uploader))
            {
                uploader.TryCancel();
            }
        }

        public Uploader StartUpload(string apiKey, string apiSecret, string apiBase, DS4Session session)
        {
            Uploader uploader;

            if(!_uploaders.TryGetValue(session, out uploader))
            {
                uploader = new Uploader(apiKey, apiSecret, apiBase, session);
                uploader.Completing += (sender, e) =>
                {
                    if (e.Success)
                    {
                        //session.Metadata.BodyhubPersonId = e.BodyhubPersonId;
                        //_store.Save();
                    }
                };

                _uploaders[session] = uploader;
            }

            if (!File.Exists(session.CompressedScanFile))
            {
                // Something went wrong probably during Compression and file is deleted
                throw new InvalidOperationException("Compressed Scan file is missing!");

            }

            session.Uploader = uploader;

            if(uploader.Waiting)
            {
                throw new InvalidOperationException("Uploader is already enqueued for upload");
            }
            else if (uploader.InProgress)
            {
                throw new InvalidOperationException("Upload is already in progress");
            }
            else if (uploader.ReTrying)
            {
                AutoReTryer autoReTryer = uploader.ReTryer;

                if (autoReTryer == null)
                {
                    throw new InvalidOperationException("Uploader in ReTrying state should have a ReTryer registered");
                }

                autoReTryer.StartNow();
            }
            else
            {
                EnqueueUploadWork(uploader);
            }
            return uploader;
        }

        /// <summary>
        /// Processing upload result and re-upload using a linear back-off function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Workqueue_UploadWorkCompleted(object sender, WorkCompletedEventArgs<Uploader,UploadResult> e)
        {
            Uploader uploader = e.WorkItem;
            UploadResult result = e.Result;
            
            if (result.Exception != null && !result.Canceled)
            {
                //Re-upload
                AutoReTryer autoReTryer = uploader.ReTryer;

                if (autoReTryer == null)
                {
                    autoReTryer = new AutoReTryer(
                        () => EnqueueUploadWork(uploader),
                        (seq) =>  seq +1,                        
                        REUPLOAD_INTERVAL_IN_SECONDS);
                    uploader.ReTryer = autoReTryer;
                }

                autoReTryer.StartOrContinueCountdown();
            }
        }

        public async Task ArchiveAndStartUpload(DS4Session session, string apiBase, string apiKey, string apiSecret)
        {
            // Archive

            if (!File.Exists(session.CompressedScanFile))
            {
                Archiver archiver = new Archiver();
                session.Compressing = true;
                ArchiveResult result = await archiver.PerformArchive(session.SessionPath, session.CompressedScanFile);

                if (!result.Success)
                {
                    return;
                }
            }

            session.Compressing = false;

            _upload(session, apiBase, apiKey, apiSecret);
            
        }

        private void _upload(DS4Session session, string apiBase, string apiKey, string apiSecret)
        {
            Uploader uploader = this.StartUpload(apiKey, apiSecret, apiBase, session);

            uploader.Completed += UploadFinished;
            
        }

        private void EnqueueUploadWork(Uploader uploader)
        {
            _workQueue.EnqueueWork(uploader, (_uploader) => _uploader.PerformUpload());
            uploader.Waiting = true;
        }

        public Dictionary<DS4Session, Uploader> AllUploads { get { return new Dictionary<DS4Session, Uploader>(_uploaders); } }

        public int UploadInProgressCount { get { return _uploaders.Count(pair => pair.Value.InProgress); } }

    }
}
