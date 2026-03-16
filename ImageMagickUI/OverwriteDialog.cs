using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageMagickUI
{
    /// <summary>
    /// Boîte de dialogue modale demandée quand le fichier de destination existe déjà.
    /// Résultat : Cancel | Overwrite | Rename  (+  ApplyToAll pour les batchs).
    /// La hauteur est calculée dynamiquement selon la longueur du chemin affiché.
    /// </summary>
    public class OverwriteDialog : Form
    {
        public enum Choice { Cancel, Overwrite, Rename }

        public Choice Result     { get; private set; } = Choice.Cancel;
        public string FinalPath  { get; private set; }
        public bool   ApplyToAll { get; private set; } = false;

        private readonly TextBox _txtName;

        private static readonly Color BG = Color.FromArgb(245, 245, 248);
        private static readonly Color FG = Color.FromArgb( 30,  30,  30);

        public OverwriteDialog(string existingPath)
        {
            FinalPath = existingPath;

            Text            = "Fichier existant";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = BG;
            Font            = new Font("Segoe UI", 9f);

            const int PAD   = 12;
            const int W     = 540;
            const int BTN_H = 30;

            // --- Label message (hauteur auto selon longueur du chemin) ---
            var lbl = new Label
            {
                Text      = $"\u26a0  Le fichier de destination existe déjà :\n{existingPath}",
                Location  = new Point(PAD, PAD),
                Width     = W - PAD * 2,
                AutoSize  = false,
                ForeColor = FG,
            };
            // Calculer la hauteur nécessaire pour le texte
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                var sz = g.MeasureString(lbl.Text, Font, lbl.Width);
                lbl.Height = (int)Math.Ceiling(sz.Height) + 6;
            }

            int y = PAD + lbl.Height + 8;

            // --- Champ nouveau nom ---
            var lblName = new Label
            {
                Text      = "Nouveau nom :",
                Location  = new Point(PAD, y + 3),
                AutoSize  = true,
                ForeColor = FG,
            };
            _txtName = new TextBox
            {
                Text        = AutoRename(existingPath),
                Location    = new Point(118, y),
                Width       = W - 118 - PAD,
                BorderStyle = BorderStyle.FixedSingle,
            };
            y += 30;

            // --- Checkbox ---
            var chkAll = new CheckBox
            {
                Text      = "Appliquer à tous les fichiers suivants",
                Location  = new Point(PAD, y),
                AutoSize  = true,
                ForeColor = FG,
            };
            y += 30;

            // --- Boutons ---
            y += 8; // marge avant boutons
            var btnCancel    = MakeBtn("\u274c Annuler",   Color.FromArgb(160,  60,  60));
            var btnOverwrite = MakeBtn("\u267b \u00c9craser",   Color.FromArgb(180, 100,  20));
            var btnRename    = MakeBtn("\u270f Renommer",  Color.FromArgb( 25, 118, 210));

            btnCancel.Location    = new Point(PAD,             y);
            btnOverwrite.Location = new Point(PAD + 166,       y);
            btnRename.Location    = new Point(PAD + 166 + 166, y);

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
                FinalPath  = Path.Combine(dir, name);
                ApplyToAll = chkAll.Checked;
                Result     = Choice.Rename;
                Close();
            };

            // Hauteur finale = position boutons + hauteur bouton + marge basse + chrome fenêtre
            int clientH = y + BTN_H + PAD;
            ClientSize  = new Size(W, clientH);

            Controls.AddRange(new Control[] { lbl, lblName, _txtName, chkAll, btnCancel, btnOverwrite, btnRename });
        }

        private static Button MakeBtn(string label, Color bg)
        {
            var b = new Button { Text = label, Width = 155, Height = 30, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
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
