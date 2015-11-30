using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Smithers.ClientLib
{
    /// <summary>
    /// Exponential back-off AutoReTryer.
    /// </summary>
    public class AutoReTryer : INotifyPropertyChanged, IDisposable
    {

        /// <summary>
        /// All timing in seconds
        /// </summary>
        private Timer _countDownTimer;
        private int _countDownCounter;
        private int _retryCount;

        private Func<int, int> _nextStepLengthFunc;

        /// <summary>
        /// True if retryer is started. For UI binding use
        /// </summary>
        public bool Started { get { return _countDownTimer.Enabled; } }

        /// <summary>
        /// Initial retry interval in Seconds. 
        /// </summary>
        public int InitRetryInterval { get; set; }

        /// <summary>
        /// Updated Retry interval in current generation of the retryer.
        /// </summary>
        public int CurrentRetryInterval { get; set; }

        /// <summary>
        /// Helper property for remaining time of next retry
        /// </summary>
        public int? SecondsUntilNextRetry { get; set; }

        /// <summary>
        /// Callback function to retry on
        /// </summary>
        public Action FuncToReTry { get; set; }

        public string RetryText 
        {
            get
            {
                return String.Format("Retry in {0}s ...", 
                    this.SecondsUntilNextRetry.GetValueOrDefault(this.InitRetryInterval));
            } 
        }

        /// <summary>
        /// Accept a function callback for retry, function to compute step interval for retry and 
        /// a initial retry interval as parameters
        /// </summary>
        /// <param name="funcToReTry"></param>
        /// <param name="nextStepLengthFunc"></param>
        /// <param name="initRetryInterval"></param>
        public AutoReTryer(Action funcToReTry, Func<int, int> nextStepLengthFunc, int initRetryInterval)
        {
            _nextStepLengthFunc = nextStepLengthFunc;

            this.InitRetryInterval = this.CurrentRetryInterval = initRetryInterval;
            this.SecondsUntilNextRetry = null;

            _countDownTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
            _countDownTimer.Elapsed += CountDownTimer_Elapsed;

            this.FuncToReTry = funcToReTry;
        }

        /// <summary>
        /// Compute retry interval based on a provided generator function
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private int ComputeRetryInterval(int sequence)
        {
            return _nextStepLengthFunc(sequence) * this.InitRetryInterval;
        }

        /// <summary>
        /// Set new countdown interval and start countdown timer
        /// </summary>
        public void StartOrContinueCountdown()
        {
            _countDownCounter = 0;
            this.CurrentRetryInterval = ComputeRetryInterval(_retryCount);
            
            _countDownTimer.Start();
            OnPropertyChanged("Started");
        }

        /// <summary>
        /// Cancel any previous pending countdown and start immediately
        /// </summary>
        public void StartNow()
        {
            this.TryCancel();
            this.FuncToReTry();
        }

        /// <summary>
        /// Cancel and reset the current retryer. 
        /// </summary>
        public void TryCancel()
        {
            if (_countDownTimer.Enabled)
            {
                _countDownTimer.Stop();
                _countDownCounter = 0;

                this._retryCount = 0;
                this.CurrentRetryInterval = InitRetryInterval;
                this.SecondsUntilNextRetry = null;

                OnPropertyChanged("RetryText");
            }

            OnPropertyChanged("Started");
        }
        void CountDownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_countDownTimer.Enabled)
                return;

            if (_countDownCounter == this.CurrentRetryInterval)
            {
                _countDownTimer.Stop();
                _countDownCounter = 0;
                _retryCount++;

                this.SecondsUntilNextRetry = null;
                this.FuncToReTry();

                OnPropertyChanged("Started");
                return;
            }
            _countDownCounter++;

            this.SecondsUntilNextRetry = this.CurrentRetryInterval - _countDownCounter;
            OnPropertyChanged("RetryText");
        }

        public void Dispose()
        {
            _countDownTimer.Stop();
            _countDownTimer.Dispose();

            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;
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
