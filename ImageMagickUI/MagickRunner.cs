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
            // Forcer point décimal : on remplace toute virgule par un point dans les args numériques
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

        // Remplace les virgules décimales par des points dans les tokens numériques
        // (protection contre les paramètres régionaux Windows)
        private static IEnumerable<string> SanitizeArgs(IEnumerable<string> args)
        {
            foreach (var a in args)
            {
                // Token numérique pur (ex: "0,30" "1920x1080" "-0,5") : on remplace les virgules
                // On ne touche pas aux chemins de fichiers ou flags ImageMagick
                var s = a;
                if (!s.StartsWith("-") && !System.IO.Path.IsPathRooted(s) && !s.Contains('\\') && !s.Contains('/'))
                    s = s.Replace(',', '.');
                // Pour les valeurs numériques passées en valeur simple (ex: "0,30")
                // On applique aussi si ça commence par un chiffre ou signe
                else if ((s.Length > 0 && (char.IsDigit(s[0]) || s[0] == '-' || s[0] == '+'))
                         && !s.Contains('\\') && !s.Contains('/'))
                    s = s.Replace(',', '.');
                yield return s;
            }
        }

        private static IEnumerable<string> WrapArgs(IEnumerable<string> args)
        {
            foreach (var a in args)
                yield return a.IndexOf(' ') >= 0 ? $"\"{a}\"" : a;
        }
    }
}
