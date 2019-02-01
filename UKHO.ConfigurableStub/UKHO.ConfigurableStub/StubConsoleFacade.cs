using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace UKHO.ConfigurableStub
{
    public class PqcStubConsoleFacade : IDisposable
    {
        private readonly Process autProcess;
        private readonly IList<string> lines = new List<string>();

        public PqcStubConsoleFacade(Process autProcess)
        {
            this.autProcess = autProcess;
            autProcess.OutputDataReceived += OutputDataReceived;
            autProcess.BeginOutputReadLine(); //Read data from the console as it's available.
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Console.WriteLine($"PQC Stub Console: \t{e.Data}");
                lock (lines)
                    lines.Add(e.Data);
            }
        }

        public IEnumerable<string> Lines
        {
            get
            {
                lock (lines)
                    return new ReadOnlyCollection<string>(lines);
            }
        }

        public void Dispose()
        {
            autProcess.OutputDataReceived -= OutputDataReceived;
        }
    }
}