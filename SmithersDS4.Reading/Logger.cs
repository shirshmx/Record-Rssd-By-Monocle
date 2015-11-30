using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    public class SmithersLogger
    {
        private string _logFile;
        public SmithersLogger(string baseDirectory)
        {
            _logFile = Path.Combine(baseDirectory, "log.txt");

            if (!File.Exists(_logFile))
            {
                string directory = Path.GetDirectoryName(_logFile);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Create(_logFile).Dispose();
            }

            using (StreamWriter writer = File.AppendText(_logFile))
            {
                writer.WriteLine(DateTime.Now + ": Monocle App starting...");
            }
        }

        public void info(string msg)
        {
            try
            {
                using (StreamWriter writer = File.AppendText(_logFile))
                {
                    writer.WriteLine(DateTime.Now + ": " + msg);
                }
            }
            catch { }
        }

    }
}
