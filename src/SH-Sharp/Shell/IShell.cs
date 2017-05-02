using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace SH_Sharp.Shell
{
    public interface IShell : IDisposable
    {
        StreamWriter Input { get; }
        StreamReader Output { get; }
        StreamReader Error { get; }

        Task StartAsync();

        Task<ExecutionResult> ExecuteAsync(string commands);
    }

    public class ExecutionResult {
        public string Result { get; set; }
        public int ResultCode { get; set; }
        public string CurrentWorkingDirectory { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public Dictionary<string, object> Variables { get; set; }
    }
}