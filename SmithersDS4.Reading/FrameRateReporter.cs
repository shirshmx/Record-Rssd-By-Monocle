using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    public class FpsChangedEventArgs : EventArgs
    {
        public double Fps { get; set; }

        public FpsChangedEventArgs(double fps)
        {
            this.Fps = fps;
        }
    }

    public class FrameRateReporter
    {
        Stopwatch _stopwatch = new Stopwatch();
        double _lastTick = 0.0;

        public double InstantFrameRate { get; set; }

        public FrameRateReporter()
        {
            _stopwatch.Start();
        }

        public event EventHandler<FpsChangedEventArgs> FpsChanged;

        private double IntervalToFps(double milliseconds)
        {
            return 1000.0 / milliseconds;
        }

        protected void Tick()
        {
            double now = _stopwatch.ElapsedMilliseconds;

            this.InstantFrameRate = IntervalToFps(now - _lastTick);

            _lastTick = now;

            if (FpsChanged != null)
            {
                FpsChanged(this, new FpsChangedEventArgs(this.InstantFrameRate));
            }
        }
        
        public void FrameArrived(object sender, FrameArrivedEventArgs ea)
        {
            Tick();
        }
    }
}
