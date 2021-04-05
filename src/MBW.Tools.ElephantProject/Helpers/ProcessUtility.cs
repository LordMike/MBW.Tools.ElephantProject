using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace MBW.Tools.ElephantProject.Helpers
{
    static class ProcessUtility
    {
        public static int GetArgumentsLimit()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return 30000;

            // Some arbitrary limit
            return 10000;
        }

        public static IEnumerable<string[]> CreateArguments(string[] initialArguments, IEnumerable<string> additionalArguments)
        {
            List<string> arguments = new List<string>();

            void ReInit()
            {
                arguments.Clear();
                arguments.AddRange(initialArguments);
            }

            ReInit();

            int maxLength = GetArgumentsLimit();

            foreach (string item in additionalArguments)
            {
                if (arguments.Sum(s => s.Length) > maxLength)
                {
                    yield return arguments.ToArray();

                    ReInit();
                }

                arguments.Add(item);
            }

            yield return arguments.ToArray();
        }

        public static async Task<(int exitCode, IList<string> stdOut, IList<string> stdErr)> Execute(string command, string workingDirectory, string[] arguments)
        {
            Log.Debug("Executing {Command} @ {WorkingDirectory} with args {Arguments}", command, workingDirectory, arguments);

            Process proc = new Process();

            proc.StartInfo.FileName = "dotnet";
            proc.StartInfo.WorkingDirectory = workingDirectory;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.EnableRaisingEvents = true;

            foreach (string arg in arguments)
                proc.StartInfo.ArgumentList.Add(arg);

            ManualResetEvent evntOut = new ManualResetEvent(false);
            ManualResetEvent evntErr = new ManualResetEvent(false);

            List<string> stdOut = new List<string>();
            List<string> stdErr = new List<string>();

            proc.OutputDataReceived += (_, args) =>
            {
                if (args.Data == null)
                    evntOut.Set();
                else
                    stdOut.Add(args.Data);
            };
            proc.ErrorDataReceived += (_, args) =>
            {
                if (args.Data == null)
                    evntErr.Set();
                else
                    stdErr.Add(args.Data);
            };

            Stopwatch sw = Stopwatch.StartNew();

            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            Log.Debug("Started process id {Pid}", proc.Id);

            await proc.WaitForExitAsync();

            evntOut.WaitOne();
            evntErr.WaitOne();
            sw.Stop();

            Log.Debug("Pid {Pid} exited with exit code {ExitCode}, ran for {Runtime}", proc.Id, proc.ExitCode, sw.Elapsed);

            return (proc.ExitCode, stdOut, stdErr);
        }
    }
}