using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static async Task<int> Execute(string command, string workingDirectory, string[] arguments)
        {
            Log.Debug("Executing {Command} @ {WorkingDirectory} with args {Arguments}", command, workingDirectory, arguments);

            ProcessStartInfo procInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true
            };

            foreach (string arg in arguments)
                procInfo.ArgumentList.Add(arg);

            Stopwatch sw = Stopwatch.StartNew();

            Process proc = Process.Start(procInfo);
            Log.Debug("Started process id {Pid}", proc.Id);

            await proc.WaitForExitAsync();

            sw.Stop();

            Log.Debug("Pid {Pid} exited with exit code {ExitCode}, ran for {Runtime}", proc.Id, proc.ExitCode, sw.Elapsed);

            return proc.ExitCode;
        }
    }
}