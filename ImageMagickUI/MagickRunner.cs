using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageMagickUI
{
    public static class MagickRunner
    {
        public const string Exe = "magick";

        public static event Action<string>? OnLog;

        // ---- état busy
        private static int _busy = 0;
        public  static bool IsBusy => _busy != 0;
        public  static event Action<bool>? BusyChanged;

        private static void Log(string msg) => OnLog?.Invoke(msg);

        public static Task RunAsync(IEnumerable<string> args)
        {
            var safeArgs = SanitizeArgs(args);
            var joined   = string.Join(" ", WrapArgs(safeArgs));
            Log("\u25b6 " + Exe + " " + joined);

            return Task.Run(() =>
            {
                if (Interlocked.Exchange(ref _busy, 1) == 1)
                {
                    Log("\u26a0 Un traitement est déjà en cours, veuillez patienter.");
                    return;
                }
                BusyChanged?.Invoke(true);
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
                finally
                {
                    Interlocked.Exchange(ref _busy, 0);
                    BusyChanged?.Invoke(false);
                }
            });
        }

        // Remplace les virgules décimales par des points dans les tokens numériques.
        // Surcharges string uniquement (compatibilité net48 : pas de .Contains(char) ni .Replace(char,char)).
        private static IEnumerable<string> SanitizeArgs(IEnumerable<string> args)
        {
            foreach (var a in args)
            {
                var s = a;
                // Ne pas toucher aux chemins (contiennent \ ou /) ni aux flags ImageMagick (-option)
                bool isPath = s.IndexOf("\\") >= 0 || s.IndexOf("/") >= 0;
                if (!isPath)
                {
                    bool isFlag = s.StartsWith("-");
                    bool startsNumeric = s.Length > 0 && (char.IsDigit(s[0]) || s[0] == '+');
                    bool flagNumericVal = isFlag && s.Length > 1 && (char.IsDigit(s[1]) || s[1] == '+');
                    if (!isFlag || startsNumeric || flagNumericVal)
                        s = s.Replace(",", ".");
                }
                yield return s;
            }
        }

        // IndexOf(string) disponible en net48 (contrairement à IndexOf(char) via surcharge BCL)
        private static IEnumerable<string> WrapArgs(IEnumerable<string> args)
        {
            foreach (var a in args)
                yield return a.IndexOf(" ") >= 0 ? $"\"{a}\"" : a;
        }
    }
}
