using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ImageMagickUI
{
    static class Program
    {
        // Active le DPI awareness "Per Monitor V2" avant la création de la fenêtre
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // DPI scaling : à appeler avant tout appel WinForms
            try { SetProcessDPIAware(); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
