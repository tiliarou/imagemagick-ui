using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickUI
{
    public static class MagickRunner
    {
        public const string Exe = "magick";

        public static event Action<string>? OnLog;

        private static void Log(string msg) => OnLog?.Invoke(msg);

        public static Task RunAsync(IEnumerable<string> args)
        {
            var joined = string.Join(" ", WrapArgs(args));
            Log("\u25b6 " + Exe + " " + joined);

            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName               = Exe,
                        Arguments              = joined,
                        UseShellExecute        = false,
                        CreateNoWindow         = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding  = Encoding.UTF8,
                    };

                    using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

                    proc.OutputDataReceived += (_, e) => { if (e.Data != null) Log(e.Data); };
                    proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) Log("\u26a0 " + e.Data); };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    proc.WaitForExit();

                    Log(proc.ExitCode == 0 ? "\u2705 Succ\u00e8s" : $"\u274c Code retour : {proc.ExitCode}");
                }
                catch (Exception ex)
                {
                    Log("\u274c " + ex.Message);
                }
            });
        }

        private static IEnumerable<string> WrapArgs(IEnumerable<string> args)
        {
            foreach (var a in args)
                yield return a.Contains(' ') ? $"\"{ a}\"" : a;
        }
    }
}
