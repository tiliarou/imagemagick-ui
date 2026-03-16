using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageMagickUI
{
    /// <summary>
    /// Fenêtre flottante pour afficher la sortie de 'magick identify -verbose'.
    /// </summary>
    public class InfoWindow : Form
    {
        private readonly TextBox _txt;

        public InfoWindow(string filePath)
        {
            Text            = $"Infos — {System.IO.Path.GetFileName(filePath)}";
            Size            = new Size(740, 620);
            MinimumSize     = new Size(500, 300);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(245, 245, 248);
            Font            = new Font("Segoe UI", 9f);

            _txt = new TextBox
            {
                Multiline   = true,
                ReadOnly    = true,
                ScrollBars  = ScrollBars.Both,
                WordWrap    = false,
                Dock        = DockStyle.Fill,
                Font        = new Font("Consolas", 9f),
                BackColor   = Color.FromArgb(250, 250, 252),
                ForeColor   = Color.FromArgb(20, 60, 20),
                BorderStyle = BorderStyle.None,
                Text        = "Chargement...",
            };

            var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(235, 235, 240) };

            var btnCopy = new Button
            {
                Text      = "\ud83d\udccb Copier tout",
                Width     = 120, Height = 26,
                Location  = new Point(8, 5),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (_, _) => { if (!string.IsNullOrEmpty(_txt.Text)) Clipboard.SetText(_txt.Text); };

            var btnSave = new Button
            {
                Text      = "\ud83d\udcbe Enregistrer",
                Width     = 120, Height = 26,
                Location  = new Point(136, 5),
                BackColor = Color.FromArgb(70, 130, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (_, _) =>
            {
                using var dlg = new SaveFileDialog
                {
                    Title      = "Enregistrer les infos",
                    Filter     = "Fichier texte|*.txt|Tous les fichiers|*.*",
                    FileName   = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_infos.txt",
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                    System.IO.File.WriteAllText(dlg.FileName, _txt.Text, System.Text.Encoding.UTF8);
            };

            var btnClose = new Button
            {
                Text      = "Fermer",
                Width     = 80, Height = 26,
                Location  = new Point(264, 5),
                BackColor = Color.FromArgb(140, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (_, _) => Close();

            toolbar.Controls.AddRange(new Control[] { btnCopy, btnSave, btnClose });
            Controls.Add(_txt);
            Controls.Add(toolbar);
        }

        /// <summary>Remplace le contenu de la fenêtre (thread-safe).</summary>
        public void SetContent(string text)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetContent(text))); return; }
            _txt.Text = text;
            _txt.SelectionStart = 0;
            _txt.ScrollToCaret();
        }

        /// <summary>Ajoute une ligne (thread-safe, utile pendant le streaming).</summary>
        public void AppendLine(string line)
        {
            if (InvokeRequired) { Invoke(new Action(() => AppendLine(line))); return; }
            _txt.AppendText(line + Environment.NewLine);
        }
    }
}
