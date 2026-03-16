using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageMagickUI
{
    /// <summary>
    /// Boîte de dialogue modale demandée quand le fichier de destination existe déjà.
    /// Résultat : Cancel | Overwrite | Rename  (+  ApplyToAll pour les batchs).
    /// </summary>
    public class OverwriteDialog : Form
    {
        public enum Choice { Cancel, Overwrite, Rename }

        public Choice Result     { get; private set; } = Choice.Cancel;
        public string FinalPath  { get; private set; }
        public bool   ApplyToAll { get; private set; } = false;

        private readonly TextBox _txtName;

        private static readonly Color BG     = Color.FromArgb(245, 245, 248);
        private static readonly Color FG     = Color.FromArgb( 30,  30,  30);

        public OverwriteDialog(string existingPath)
        {
            FinalPath = existingPath;

            Text            = "Fichier existant";
            Size            = new Size(540, 240);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = BG;
            Font            = new Font("Segoe UI", 9f);

            // Message
            var lbl = new Label
            {
                Text      = $"\u26a0  Le fichier de destination existe déjà :\n{existingPath}",
                Location  = new Point(12, 12),
                Size      = new Size(508, 40),
                ForeColor = FG,
            };

            // Champ nouveau nom
            var lblName = new Label { Text = "Nouveau nom :", Location = new Point(12, 62), AutoSize = true, ForeColor = FG };
            _txtName = new TextBox
            {
                Text        = AutoRename(existingPath),
                Location    = new Point(118, 59),
                Width       = 402,
                BorderStyle = BorderStyle.FixedSingle,
            };

            // Checkbox "appliquer à tous" (visible en contexte batch)
            var chkAll = new CheckBox
            {
                Text      = "Appliquer à tous les fichiers suivants",
                Location  = new Point(12, 95),
                AutoSize  = true,
                ForeColor = FG,
            };

            // Boutons
            var btnCancel    = MakeBtn("\u274c Annuler",  Color.FromArgb(160,  60,  60));
            var btnOverwrite = MakeBtn("\u267b \u00c9craser",  Color.FromArgb(180, 100,  20));
            var btnRename    = MakeBtn("\u270f Renommer", Color.FromArgb( 25, 118, 210));

            btnCancel.Location    = new Point( 12, 170);
            btnOverwrite.Location = new Point(178, 170);
            btnRename.Location    = new Point(354, 170);

            btnCancel.Click += (_, _) =>
            {
                ApplyToAll = chkAll.Checked;
                Result     = Choice.Cancel;
                Close();
            };
            btnOverwrite.Click += (_, _) =>
            {
                ApplyToAll = chkAll.Checked;
                Result     = Choice.Overwrite;
                FinalPath  = existingPath;
                Close();
            };
            btnRename.Click += (_, _) =>
            {
                var name = _txtName.Text.Trim();
                if (string.IsNullOrEmpty(name)) { MessageBox.Show("Entrez un nom de fichier."); return; }
                var dir   = Path.GetDirectoryName(existingPath) ?? "";
                FinalPath = Path.Combine(dir, name);
                ApplyToAll = chkAll.Checked;
                Result    = Choice.Rename;
                Close();
            };

            Controls.AddRange(new Control[] { lbl, lblName, _txtName, chkAll, btnCancel, btnOverwrite, btnRename });
        }

        private static Button MakeBtn(string label, Color bg)
        {
            var b = new Button { Text = label, Width = 155, Height = 28, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        /// <summary>Génère un nom unique en ajoutant _1, _2… avant l'extension.</summary>
        public static string AutoRename(string path)
        {
            var dir  = Path.GetDirectoryName(path) ?? "";
            var stem = Path.GetFileNameWithoutExtension(path);
            var ext  = Path.GetExtension(path);
            int i    = 1;
            string candidate;
            do { candidate = $"{stem}_{i++}{ext}"; }
            while (File.Exists(Path.Combine(dir, candidate)));
            return candidate;
        }
    }
}
