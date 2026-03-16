using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMagickUI
{
    public class MainForm : Form
    {
        private TextBox  txtInput  = new();
        private TextBox  txtOutput = new();
        private TextBox  txtLog    = new();
        private TabControl tabs    = new();

        private static readonly Color BG      = Color.FromArgb(22,  22,  35);
        private static readonly Color SURFACE = Color.FromArgb(35,  35,  55);
        private static readonly Color ACCENT  = Color.FromArgb(60,  100, 200);
        private static readonly Color GOLD    = Color.FromArgb(255, 215,   0);
        private static readonly Color FG      = Color.FromArgb(220, 220, 235);

        public MainForm()
        {
            Text            = "ImageMagick UI  —  WinForms";
            Size            = new Size(1100, 820);
            MinimumSize     = new Size(900,  650);
            BackColor       = BG;
            ForeColor       = FG;
            Font            = new Font("Segoe UI", 9f);
            StartPosition   = FormStartPosition.CenterScreen;
            MagickRunner.OnLog += AppendLog;
            BuildLayout();
        }

        private void BuildLayout()
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = BG,
                Padding     = new Padding(8),
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent,  100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            mainPanel.Controls.Add(BuildIOBar(),   0, 0);
            mainPanel.Controls.Add(BuildTabs(),    0, 1);
            mainPanel.Controls.Add(BuildConsole(), 0, 2);
            Controls.Add(mainPanel);
        }

        private Panel BuildIOBar()
        {
            var p = StyledPanel();
            p.Dock = DockStyle.Fill;
            var lblIn = Label("\ud83d\udcc2 Source :");
            lblIn.Location = new Point(8, 8);
            txtInput = TextBox(500);
            txtInput.Location = new Point(90, 6);
            var btnIn = Btn("Parcourir", (_, _) =>
            {
                using var d = new OpenFileDialog { Filter = AllFilesFilter() };
                if (d.ShowDialog() == DialogResult.OK) txtInput.Text = d.FileName;
            });
            btnIn.Location = new Point(600, 5);
            var lblOut = Label("\ud83d\udcbe Sortie  :");
            lblOut.Location = new Point(8, 40);
            txtOutput = TextBox(500);
            txtOutput.Location = new Point(90, 38);
            var btnOut = Btn("Parcourir", (_, _) =>
            {
                using var d = new SaveFileDialog { Filter = AllFilesFilter() };
                if (d.ShowDialog() == DialogResult.OK) txtOutput.Text = d.FileName;
            });
            btnOut.Location = new Point(600, 37);
            var btnInfo = Btn("\u2139 Infos fichier", async (_, _) =>
            {
                var src = txtInput.Text;
                if (!File.Exists(src)) { AppendLog("\u26a0 Fichier source introuvable"); return; }
                await MagickRunner.RunAsync(new[] { "identify", "-verbose", src });
            });
            btnInfo.Location = new Point(8, 68);
            p.Controls.AddRange(new Control[] { lblIn, txtInput, btnIn, lblOut, txtOutput, btnOut, btnInfo });
            return p;
        }

        private TabControl BuildTabs()
        {
            tabs = new TabControl
            {
                Dock      = DockStyle.Fill,
                BackColor = SURFACE,
                ForeColor = FG,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            };
            tabs.TabPages.Add(BuildTabTransform());
            tabs.TabPages.Add(BuildTabEffects());
            tabs.TabPages.Add(BuildTabColors());
            tabs.TabPages.Add(BuildTabPdf());
            tabs.TabPages.Add(BuildTabAnnotate());
            tabs.TabPages.Add(BuildTabBatch());
            return tabs;
        }

        private Panel BuildConsole()
        {
            var p = StyledPanel();
            p.Dock = DockStyle.Fill;
            var lbl = Label("\ud83d\udccb Console");
            lbl.ForeColor = GOLD;
            lbl.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lbl.Location = new Point(6, 4);
            txtLog = new TextBox
            {
                Multiline  = true,
                ReadOnly   = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor  = Color.FromArgb(15, 15, 25),
                ForeColor  = Color.FromArgb(180, 230, 180),
                Font       = new Font("Consolas", 8.5f),
                Location   = new Point(6, 24),
                Size       = new Size(1060, 115),
                Anchor     = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Text       = "Pr\u00eat. S\u00e9lectionnez un fichier source et destination...",
            };
            var btnClear = Btn("\ud83d\uddd1 Effacer", (_, _) => txtLog.Clear());
            btnClear.Location = new Point(6, 144);
            p.Controls.AddRange(new Control[] { lbl, txtLog, btnClear });
            p.Resize += (_, _) => { txtLog.Width = p.Width - 18; };
            return p;
        }

        private TabPage BuildTabTransform()
        {
            var pg = Tab("\ud83d\udd04 Transformations");
            var sc = MakeScrollPanel(pg);
            int y = 8;
            Section(sc, "Redimensionner", ref y);
            var nW = Num(sc, "Largeur :", 1920, 1, 99999, ref y, 140);
            var nH = Num(sc, "Hauteur :", 1080, 1, 99999, ref y, 140);
            var chRatio = Check(sc, "Conserver les proportions", true, ref y);
            BtnAction(sc, "Redimensionner", ref y, async () =>
            {
                var size = chRatio.Checked ? $"{(int)nW.Value}x{(int)nH.Value}>" : $"{(int)nW.Value}x{(int)nH.Value}";
                await Run(txtInput.Text, txtOutput.Text, "-resize", size);
            });
            Section(sc, "Recadrer (Crop)", ref y);
            var cW = Num(sc, "W :", 800, 1, 99999, ref y, 80);
            var cH = Num(sc, "H :", 600, 1, 99999, ref y, 80);
            var cX = Num(sc, "X :", 0, 0, 99999, ref y, 80);
            var cY = Num(sc, "Y :", 0, 0, 99999, ref y, 80);
            BtnAction(sc, "Recadrer", ref y, async () =>
                await Run(txtInput.Text, txtOutput.Text, "-crop", $"{(int)cW.Value}x{(int)cH.Value}+{(int)cX.Value}+{(int)cY.Value}", "+repage"));
            Section(sc, "Rotation", ref y);
            var nAngle = NumDec(sc, "Angle (\u00b0) :", 0, -360, 360, ref y, 120);
            BtnAction(sc, "Appliquer rotation", ref y, async () =>
                await Run(txtInput.Text, txtOutput.Text, "-rotate", nAngle.Value.ToString("0.##")));
            Section(sc, "Miroirs", ref y);
            BtnAction(sc, "Flip (axe horizontal)", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-flip"));
            BtnAction(sc, "Flop (axe vertical)",   ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-flop"));
            BtnAction(sc, "Transpose",              ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-transpose"));
            BtnAction(sc, "Transverse",             ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-transverse"));
            Section(sc, "Rognage automatique", ref y);
            var nFuzz = NumDec(sc, "Fuzz % :", 5, 0, 100, ref y, 100);
            BtnAction(sc, "Rogner (Trim)", ref y, async () =>
                await Run(txtInput.Text, txtOutput.Text, "-fuzz", $"{nFuzz.Value}%", "-trim", "+repage"));
            Section(sc, "Bordure", ref y);
            var nBorder = Num(sc, "\u00c9paisseur :", 10, 0, 500, ref y, 100);
            var txtBorderColor = TxtBox(sc, "Couleur :", "white", ref y, 120);
            BtnAction(sc, "Ajouter bordure", ref y, async () =>
                await Run(txtInput.Text, txtOutput.Text, "-bordercolor", txtBorderColor.Text, "-border", $"{(int)nBorder.Value}x{(int)nBorder.Value}"));
            return pg;
        }

        private TabPage BuildTabEffects()
        {
            var pg = Tab("\u2728 Effets visuels");
            var sc = MakeScrollPanel(pg);
            int y = 8;
            Section(sc, "Flou (Blur)", ref y);
            var bR = NumDec(sc, "Rayon :", 0, 0, 50, ref y, 80);
            var bS = NumDec(sc, "Sigma :", 3, 0, 50, ref y, 80);
            BtnAction(sc, "Blur",          ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-blur", $"{bR.Value}x{bS.Value}"));
            BtnAction(sc, "Gaussian Blur", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-gaussian-blur", $"{bR.Value}x{bS.Value}"));
            BtnAction(sc, "Motion Blur",   ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-motion-blur", $"0x{bS.Value}+45"));
            Section(sc, "Net\u02adet\u00e9 (Sharpen)", ref y);
            var shR = NumDec(sc, "Rayon :", 0, 0, 50, ref y, 80);
            var shS = NumDec(sc, "Sigma :", 1, 0, 50, ref y, 80);
            var shA = NumDec(sc, "Amount :", 0.5m, 0, 10, ref y, 80);
            var shT = NumDec(sc, "Threshold :", 0.1m, 0, 10, ref y, 90);
            BtnAction(sc, "Sharpen",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-sharpen", $"{shR.Value}x{shS.Value}"));
            BtnAction(sc, "Unsharp Mask", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-unsharp", $"{shR.Value}x{shS.Value}+{shA.Value}+{shT.Value}"));
            Section(sc, "Bruit (Noise)", ref y);
            var noiseTypes = new[] { "Uniform","Gaussian","Multiplicative","Impulse","Laplacian","Poisson" };
            var cmbNoise = Combo(sc, "Type :", noiseTypes, "Multiplicative", ref y, 160);
            var nAtt = NumDec(sc, "Att\u00e9nuation :", 0.4m, 0, 5, ref y, 80);
            BtnAction(sc, "Ajouter bruit", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-attenuate", nAtt.Value.ToString("0.##"), "+noise", cmbNoise.Text));
            BtnAction(sc, "Despeckle",     ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-despeckle"));
            var nMedian = NumDec(sc, "Rayon m\u00e9dian :", 1, 0, 20, ref y, 100);
            BtnAction(sc, "M\u00e9dian", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-median", nMedian.Value.ToString("0.##")));
            Section(sc, "Effets artistiques", ref y);
            var nChar = NumDec(sc, "Rayon charcoal :", 2, 0, 20, ref y, 120);
            BtnAction(sc, "Charcoal", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-charcoal", nChar.Value.ToString("0.##")));
            var nOil = NumDec(sc, "Rayon peinture :", 4, 0, 20, ref y, 120);
            BtnAction(sc, "Oil Paint", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-paint", nOil.Value.ToString("0.##")));
            var skR = NumDec(sc, "R sketch :", 0, 0, 50, ref y, 90);
            var skS = NumDec(sc, "S sketch :", 1, 0, 50, ref y, 90);
            var skA = NumDec(sc, "Angle :", 45, 0, 360, ref y, 90);
            BtnAction(sc, "Sketch", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-sketch", $"{skR.Value}x{skS.Value}+{skA.Value}"));
            BtnAction(sc, "Emboss", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-emboss", "0x1"));
            Section(sc, "Distorsions", ref y);
            var nSwirl = NumDec(sc, "Swirl (\u00b0) :", 90, -360, 360, ref y, 100);
            BtnAction(sc, "Swirl",   ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-swirl",   nSwirl.Value.ToString("0.##")));
            var nImplode = NumDec(sc, "Implode :", 0.5m, -5, 5, ref y, 80);
            BtnAction(sc, "Implode", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-implode", nImplode.Value.ToString("0.##")));
            var nSpread = NumDec(sc, "Spread (px) :", 5, 0, 100, ref y, 100);
            BtnAction(sc, "Spread",  ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-spread",  nSpread.Value.ToString("0.##")));
            var wAmp = NumDec(sc, "Amplitude vague :", 10, 0, 200, ref y, 130);
            var wLen = NumDec(sc, "Longueur vague :", 100, 1, 1000, ref y, 130);
            BtnAction(sc, "Wave",    ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-wave", $"{wAmp.Value}x{wLen.Value}"));
            var nPx = NumDec(sc, "Pixelate % :", 10, 1, 50, ref y, 100);
            BtnAction(sc, "Pixelate", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-scale", $"{nPx.Value}%", "-scale", "100%"));
            Section(sc, "\ud83d\udda8 Effet Imprim\u00e9 / Scann\u00e9", ref y);
            AddNote(sc, "Simule l'impression puis le scan d'un document.", ref y);
            var nDpi   = Num(sc, "DPI :", 150, 72, 600, ref y, 80);
            var nRot   = NumDec(sc, "Rotation :", 0.3m, -5, 5, ref y, 80);
            var nAtt1  = NumDec(sc, "Bruit 1 :", 0.4m, 0, 2, ref y, 80);
            var nAtt2  = NumDec(sc, "Bruit 2 :", 0.03m, 0, 1, ref y, 80);
            var nQual  = Num(sc, "Qualit\u00e9 JPEG :", 80, 1, 100, ref y, 90);
            var chGray = Check(sc, "Niveaux de gris", false, ref y);
            BtnAction(sc, "\ud83d\udda8 Appliquer effet scan", ref y, async () =>
            {
                var args = new System.Collections.Generic.List<string>
                {
                    "-density", ((int)nDpi.Value).ToString(),
                    txtInput.Text,
                    "-rotate",    nRot.Value.ToString("0.##"),
                    "-attenuate", nAtt1.Value.ToString("0.##"),
                    "+noise",     "Multiplicative",
                    "-attenuate", nAtt2.Value.ToString("0.##"),
                    "+noise",     "Multiplicative",
                    "-sharpen",   "0x1.0",
                };
                if (chGray.Checked) args.AddRange(new[] { "-colorspace", "Gray" });
                args.AddRange(new[] { "-compress", "JPEG", "-quality", ((int)nQual.Value).ToString() });
                args.Add(txtOutput.Text);
                await MagickRunner.RunAsync(args);
            });
            return pg;
        }

        private TabPage BuildTabColors()
        {
            var pg = Tab("\ud83c\udfa8 Couleurs");
            var sc = MakeScrollPanel(pg);
            int y = 8;
            Section(sc, "Espace colorim\u00e9trique", ref y);
            var spaces = new[] { "sRGB","Gray","CMYK","HSL","HSB","Lab","XYZ","YCbCr","YUV","LinearGray" };
            var cmbCs = Combo(sc, "Espace :", spaces, "sRGB", ref y, 160);
            BtnAction(sc, "Convertir",       ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-colorspace", cmbCs.Text));
            BtnAction(sc, "Niveaux de gris", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-colorspace", "Gray"));
            BtnAction(sc, "N\u00e9gatif",         ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-negate"));
            BtnAction(sc, "Normaliser",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-normalize"));
            BtnAction(sc, "\u00c9galiser",        ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-equalize"));
            BtnAction(sc, "Auto-Level",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-auto-level"));
            BtnAction(sc, "Auto-Gamma",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-auto-gamma"));
            Section(sc, "Luminosit\u00e9 / Contraste", ref y);
            var nBright   = NumDec(sc, "Luminosit\u00e9 :", 0, -100, 100, ref y, 100);
            var nContrast = NumDec(sc, "Contraste :",  0, -100, 100, ref y, 100);
            BtnAction(sc, "Appliquer", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-brightness-contrast", $"{nBright.Value}x{nContrast.Value}"));
            Section(sc, "Gamma", ref y);
            var nGamma = NumDec(sc, "Gamma :", 1.0m, 0.1m, 10, ref y, 80);
            BtnAction(sc, "Appliquer gamma", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-gamma", nGamma.Value.ToString("0.##")));
            Section(sc, "Niveaux (Levels)", ref y);
            var nBlack   = NumDec(sc, "Noir % :",  0,    0, 100,  ref y, 80);
            var nWhite   = NumDec(sc, "Blanc % :", 100,  0, 100,  ref y, 80);
            var nLvGamma = NumDec(sc, "Gamma :",   1.0m, 0.1m, 10, ref y, 80);
            BtnAction(sc, "Appliquer niveaux", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-level", $"{nBlack.Value}%,{nWhite.Value}%,{nLvGamma.Value}"));
            Section(sc, "Modulation HSB", ref y);
            var nModB = NumDec(sc, "Luminosit\u00e9 % :", 100, 0, 200, ref y, 120);
            var nModS = NumDec(sc, "Saturation % :", 100, 0, 200, ref y, 120);
            var nModH = NumDec(sc, "Teinte % :", 100, 0, 200, ref y, 100);
            BtnAction(sc, "Moduler", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-modulate", $"{nModB.Value},{nModS.Value},{nModH.Value}"));
            Section(sc, "S\u00e9pia / Teinte / Colorisation", ref y);
            var nSepia = NumDec(sc, "Seuil s\u00e9pia % :", 80, 0, 100, ref y, 120);
            BtnAction(sc, "S\u00e9pia-tone", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-sepia-tone", $"{nSepia.Value}%"));
            var txtTint  = TxtBox(sc, "Couleur tint :", "#FFD700", ref y, 100);
            var nTintOp  = NumDec(sc, "Opacit\u00e9 :", 50, 0, 100, ref y, 80);
            BtnAction(sc, "Tint", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-fill", txtTint.Text, "-tint", nTintOp.Value.ToString("0.##")));
            var txtColColor = TxtBox(sc, "Couleur colorize :", "blue", ref y, 100);
            var nColPct     = NumDec(sc, "% :", 50, 0, 100, ref y, 60);
            BtnAction(sc, "Colorize", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-fill", txtColColor.Text, "-colorize", nColPct.Value.ToString("0.##")));
            Section(sc, "Seuillage / Posterize / Dithering", ref y);
            var nThr  = NumDec(sc, "Seuil % :", 50, 0, 100, ref y, 80);
            BtnAction(sc, "Threshold", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-threshold", $"{nThr.Value}%"));
            var nPost = Num(sc, "Niveaux posterize :", 4, 2, 256, ref y, 100);
            BtnAction(sc, "Posterize", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-posterize", ((int)nPost.Value).ToString()));
            var nDither = Num(sc, "Couleurs dither :", 16, 2, 256, ref y, 100);
            BtnAction(sc, "Dither Riemersma", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-dither", "Riemersma", "-colors", ((int)nDither.Value).ToString()));
            Section(sc, "Extraire canal", ref y);
            var channels = new[] { "Red","Green","Blue","Alpha","Cyan","Magenta","Yellow","Black","All" };
            var cmbCh = Combo(sc, "Canal :", channels, "Red", ref y, 120);
            BtnAction(sc, "S\u00e9parer canal", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-channel", cmbCh.Text, "-separate"));
            return pg;
        }

        private TabPage BuildTabPdf()
        {
            var pg = Tab("\ud83d\udcc4 PDF");
            var sc = MakeScrollPanel(pg);
            int y = 8;
            Section(sc, "PDF \u2192 Images", ref y);
            AddNote(sc, "Sortie exemple : page-%04d.png", ref y);
            var nPdfDpi = Num(sc, "DPI :", 200, 72, 600, ref y, 80);
            BtnAction(sc, "Exporter les pages", ref y, async () =>
                await MagickRunner.RunAsync(new[] { "-density", ((int)nPdfDpi.Value).ToString(), txtInput.Text, txtOutput.Text }));
            Section(sc, "Extraire une plage de pages", ref y);
            AddNote(sc, "Syntaxe : 0-2 (pages 1-3)  ou  0,2,4", ref y);
            var txtPages = TxtBox(sc, "Pages :", "0-2", ref y, 120);
            BtnAction(sc, "Extraire pages", ref y, async () =>
                await MagickRunner.RunAsync(new[] { $"{txtInput.Text}[{txtPages.Text.Trim()}]", txtOutput.Text }));
            Section(sc, "Compresser PDF", ref y);
            var nCmpDpi  = Num(sc, "DPI :",    150, 72, 600, ref y, 80);
            var nCmpQual = Num(sc, "Qualit\u00e9 :", 75, 1, 100, ref y, 80);
            BtnAction(sc, "Compresser PDF", ref y, async () =>
                await MagickRunner.RunAsync(new[] { "-density", ((int)nCmpDpi.Value).ToString(), txtInput.Text, "-compress", "JPEG", "-quality", ((int)nCmpQual.Value).ToString(), txtOutput.Text }));
            Section(sc, "Images \u2192 PDF (dossier batch)", ref y);
            var txtImgDir = TxtBox(sc, "Dossier :", "", ref y, 350);
            AddBrowseFolderBtn(sc, txtImgDir, ref y);
            BtnAction(sc, "Assembler en PDF", ref y, async () =>
            {
                var dir = txtImgDir.Text; var dst = txtOutput.Text;
                if (!Directory.Exists(dir)) { AppendLog("\u26a0 Dossier introuvable"); return; }
                var imgs = Directory.GetFiles(dir, "*.*")
                    .Where(f => new[] { ".png",".jpg",".jpeg",".tif",".tiff",".bmp" }.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f).ToList();
                if (!imgs.Any()) { AppendLog("\u26a0 Aucune image trouv\u00e9e"); return; }
                await MagickRunner.RunAsync(imgs.Concat(new[] { dst }));
            });
            Section(sc, "Montage (grille d'images)", ref y);
            var txtMntDir = TxtBox(sc, "Dossier images :", "", ref y, 350);
            AddBrowseFolderBtn(sc, txtMntDir, ref y);
            var nMntCols = Num(sc, "Colonnes :", 3, 1, 20, ref y, 70);
            var nMntW    = Num(sc, "Larg. tuile :", 200, 50, 2000, ref y, 90);
            var nMntH    = Num(sc, "Haut. tuile :", 200, 50, 2000, ref y, 90);
            BtnAction(sc, "Cr\u00e9er montage", ref y, async () =>
            {
                var dir = txtMntDir.Text; var dst = txtOutput.Text;
                if (!Directory.Exists(dir)) { AppendLog("\u26a0 Dossier introuvable"); return; }
                var imgs = Directory.GetFiles(dir, "*.*")
                    .Where(f => new[] { ".png",".jpg",".jpeg" }.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f).ToArray();
                var args = new System.Collections.Generic.List<string> { "montage" };
                args.AddRange(imgs);
                args.AddRange(new[] { "-tile", $"{(int)nMntCols.Value}x", "-geometry", $"{(int)nMntW.Value}x{(int)nMntH.Value}+4+4", dst });
                await MagickRunner.RunAsync(args);
            });
            return pg;
        }

        private TabPage BuildTabAnnotate()
        {
            var pg = Tab("\u270f\ufe0f Annotations");
            var sc = MakeScrollPanel(pg);
            int y = 8;
            Section(sc, "Ins\u00e9rer du texte", ref y);
            var txtContent   = TxtBox(sc, "Texte :", "Mon texte", ref y, 400);
            var txtFont      = TxtBox(sc, "Police :", "Arial", ref y, 150);
            var nFontSize    = Num(sc, "Taille :", 36, 1, 500, ref y, 80);
            var txtFontColor = TxtBox(sc, "Couleur :", "black", ref y, 100);
            var nTxtX        = Num(sc, "X :", 10, 0, 9999, ref y, 80);
            var nTxtY        = Num(sc, "Y :", 10, 0, 9999, ref y, 80);
            var gravities    = new[] { "NorthWest","North","NorthEast","West","Center","East","SouthWest","South","SouthEast" };
            var cmbGrav      = Combo(sc, "Gravit\u00e9 :", gravities, "NorthWest", ref y, 160);
            BtnAction(sc, "Ins\u00e9rer texte", ref y, async () =>
                await MagickRunner.RunAsync(new[] { txtInput.Text, "-font", txtFont.Text, "-pointsize", ((int)nFontSize.Value).ToString(), "-fill", txtFontColor.Text, "-gravity", cmbGrav.Text, "-annotate", $"+{(int)nTxtX.Value}+{(int)nTxtY.Value}", txtContent.Text, txtOutput.Text }));
            Section(sc, "Filigrane (Watermark)", ref y);
            var txtWm  = TxtBox(sc, "Fichier WM :", "", ref y, 350);
            AddBrowseFileBtn(sc, txtWm, ref y);
            var nWmOp  = NumDec(sc, "Opacit\u00e9 % :", 50, 0, 100, ref y, 80);
            var cmbWmG = Combo(sc, "Position :", gravities, "Center", ref y, 160);
            BtnAction(sc, "Appliquer watermark", ref y, async () =>
                await MagickRunner.RunAsync(new[] { "composite", "-dissolve", nWmOp.Value.ToString("0.##"), "-gravity", cmbWmG.Text, txtWm.Text, txtInput.Text, txtOutput.Text }));
            Section(sc, "Dessiner un rectangle", ref y);
            var rX1 = Num(sc, "X1 :", 10,  0, 9999, ref y, 70);
            var rY1 = Num(sc, "Y1 :", 10,  0, 9999, ref y, 70);
            var rX2 = Num(sc, "X2 :", 200, 0, 9999, ref y, 70);
            var rY2 = Num(sc, "Y2 :", 200, 0, 9999, ref y, 70);
            var txtRectStroke = TxtBox(sc, "Trait :",  "red",  ref y, 80);
            var txtRectFill   = TxtBox(sc, "Fill :",   "none", ref y, 80);
            BtnAction(sc, "Dessiner rectangle", ref y, async () =>
                await MagickRunner.RunAsync(new[] { txtInput.Text, "-fill", txtRectFill.Text, "-stroke", txtRectStroke.Text, "-draw", $"rectangle {(int)rX1.Value},{(int)rY1.Value} {(int)rX2.Value},{(int)rY2.Value}", txtOutput.Text }));
            Section(sc, "Dessiner un cercle", ref y);
            var cCX = Num(sc, "CX :", 100, 0, 9999, ref y, 70);
            var cCY = Num(sc, "CY :", 100, 0, 9999, ref y, 70);
            var cR  = Num(sc, "R :",   50, 1, 9999, ref y, 70);
            var txtCircColor = TxtBox(sc, "Couleur :", "blue", ref y, 80);
            BtnAction(sc, "Dessiner cercle", ref y, async () =>
            {
                int cx = (int)cCX.Value, cy = (int)cCY.Value, cr = (int)cR.Value;
                await MagickRunner.RunAsync(new[] { txtInput.Text, "-fill", "none", "-stroke", txtCircColor.Text, "-draw", $"circle {cx},{cy} {cx + cr},{cy}", txtOutput.Text });
            });
            return pg;
        }

        private TabPage BuildTabBatch()
        {
            var pg = Tab("\ud83d\udce6 Batch & Format");
            var sc = MakeScrollPanel(pg);
            int y = 8;
            Section(sc, "Traitement par lot", ref y);
            var txtBatchIn  = TxtBox(sc, "Dossier source :", "", ref y, 350);
            AddBrowseFolderBtn(sc, txtBatchIn, ref y);
            var txtBatchOut = TxtBox(sc, "Dossier sortie :", "", ref y, 350);
            AddBrowseFolderBtn(sc, txtBatchOut, ref y);
            var batchOps   = new[] { "Grayscale","Resize 50%","Resize 150%","Normalize","Scan Effect","JPEG q85","Auto-level","Auto-gamma" };
            var cmbBatchOp = Combo(sc, "Op\u00e9ration :", batchOps, "Grayscale", ref y, 200);
            BtnAction(sc, "\ud83d\udd04 Lancer le batch", ref y, async () =>
            {
                var inDir = txtBatchIn.Text; var outDir = txtBatchOut.Text;
                if (!Directory.Exists(inDir) || string.IsNullOrWhiteSpace(outDir)) { AppendLog("\u26a0 V\u00e9rifiez les dossiers"); return; }
                Directory.CreateDirectory(outDir);
                var exts  = new[] { ".png",".jpg",".jpeg",".tif",".tiff",".pdf",".bmp",".gif" };
                var files = Directory.GetFiles(inDir).Where(f => exts.Contains(Path.GetExtension(f).ToLower())).ToArray();
                AppendLog($"\ud83d\udd04 Batch '{cmbBatchOp.Text}' \u2014 {files.Length} fichier(s)...");
                foreach (var f in files)
                {
                    var dst = Path.Combine(outDir, Path.GetFileName(f));
                    string[] args = cmbBatchOp.Text switch
                    {
                        "Grayscale"   => new[] { f, "-colorspace", "Gray", dst },
                        "Resize 50%"  => new[] { f, "-resize", "50%",  dst },
                        "Resize 150%" => new[] { f, "-resize", "150%", dst },
                        "Normalize"   => new[] { f, "-normalize", dst },
                        "JPEG q85"    => new[] { f, "-quality", "85", dst },
                        "Auto-level"  => new[] { f, "-auto-level", dst },
                        "Auto-gamma"  => new[] { f, "-auto-gamma", dst },
                        "Scan Effect" => new[] { "-density","150", f, "-rotate","0.3", "-attenuate","0.4","+noise","Multiplicative", "-sharpen","0x1.0","-compress","JPEG","-quality","80", dst },
                        _             => new[] { f, dst }
                    };
                    await MagickRunner.RunAsync(args);
                }
                AppendLog("\u2705 Batch termin\u00e9.");
            });
            Section(sc, "Conversion de format", ref y);
            AddNote(sc, "Changez l'extension dans le chemin de sortie pour changer le format.", ref y);
            var nConvQ = Num(sc, "Qualit\u00e9 (JPEG/WebP) :", 85, 1, 100, ref y, 90);
            BtnAction(sc, "Convertir", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-quality", ((int)nConvQ.Value).ToString()));
            Section(sc, "M\u00e9tadonn\u00e9es", ref y);
            BtnAction(sc, "Supprimer m\u00e9tadonn\u00e9es (-strip)", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-strip"));
            BtnAction(sc, "\u2139 Afficher infos (identify)", ref y, async () =>
            {
                if (!File.Exists(txtInput.Text)) { AppendLog("\u26a0 Fichier introuvable"); return; }
                await MagickRunner.RunAsync(new[] { "identify", "-verbose", txtInput.Text });
            });
            return pg;
        }

        private async Task Run(string src, string dst, params string[] middle)
        {
            if (!File.Exists(src)) { AppendLog("\u26a0 Fichier source introuvable"); return; }
            if (string.IsNullOrWhiteSpace(dst)) { AppendLog("\u26a0 D\u00e9finissez le fichier de sortie"); return; }
            var args = new System.Collections.Generic.List<string> { src };
            args.AddRange(middle);
            args.Add(dst);
            await MagickRunner.RunAsync(args);
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => AppendLog(msg))); return; }
            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private Panel StyledPanel() => new Panel { BackColor = SURFACE, Padding = new Padding(4) };
        private TabPage Tab(string name) => new TabPage(name) { BackColor = BG, ForeColor = FG };

        // Renommé Scroll → MakeScrollPanel pour éviter le conflit avec ScrollableControl.Scroll (CS0108)
        private Panel MakeScrollPanel(TabPage tp)
        {
            var s = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG };
            tp.Controls.Add(s);
            return s;
        }
        private Label Label(string text) => new Label { Text = text, ForeColor = FG, AutoSize = true };
        private TextBox TextBox(int width) => new TextBox { Width = width, BackColor = SURFACE, ForeColor = FG };
        private Button Btn(string label, EventHandler handler, int width = 160)
        {
            var b = new Button { Text = label, Width = width, Height = 28, BackColor = ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += handler;
            return b;
        }
        private void Section(Panel sc, string title, ref int y)
        {
            var l = new Label { Text = "\u2014 " + title + " \u2014", ForeColor = GOLD, Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, Location = new Point(8, y) };
            sc.Controls.Add(l); y += 24;
        }
        private void AddNote(Panel sc, string text, ref int y)
        {
            var l = new Label { Text = text, ForeColor = Color.FromArgb(160, 160, 180), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), Location = new Point(12, y) };
            sc.Controls.Add(l); y += 20;
        }
        private NumericUpDown Num(Panel sc, string label, int def, int min, int max, ref int y, int w = 80)
        {
            var lbl = new Label { Text = label, ForeColor = FG, AutoSize = true, Location = new Point(12, y + 3) };
            var nud = new NumericUpDown { Minimum = min, Maximum = max, Value = def, Width = w, Location = new Point(lbl.PreferredWidth + 20, y), BackColor = SURFACE, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
            sc.Controls.AddRange(new Control[] { lbl, nud }); y += 30;
            return nud;
        }
        private NumericUpDown NumDec(Panel sc, string label, decimal def, decimal min, decimal max, ref int y, int w = 80)
        {
            var lbl = new Label { Text = label, ForeColor = FG, AutoSize = true, Location = new Point(12, y + 3) };
            var nud = new NumericUpDown { Minimum = min, Maximum = max, Value = def, DecimalPlaces = 2, Increment = 0.1m, Width = w, Location = new Point(lbl.PreferredWidth + 20, y), BackColor = SURFACE, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
            sc.Controls.AddRange(new Control[] { lbl, nud }); y += 30;
            return nud;
        }
        private TextBox TxtBox(Panel sc, string label, string def, ref int y, int width = 200)
        {
            var lbl = new Label { Text = label, ForeColor = FG, AutoSize = true, Location = new Point(12, y + 3) };
            var txt = new TextBox { Text = def, Width = width, Location = new Point(lbl.PreferredWidth + 20, y), BackColor = SURFACE, ForeColor = FG };
            sc.Controls.AddRange(new Control[] { lbl, txt }); y += 30;
            return txt;
        }
        private ComboBox Combo(Panel sc, string label, string[] items, string def, ref int y, int width = 160)
        {
            var lbl = new Label { Text = label, ForeColor = FG, AutoSize = true, Location = new Point(12, y + 3) };
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Location = new Point(lbl.PreferredWidth + 20, y), BackColor = SURFACE, ForeColor = FG };
            cmb.Items.AddRange(items); cmb.SelectedItem = def;
            sc.Controls.AddRange(new Control[] { lbl, cmb }); y += 30;
            return cmb;
        }
        private CheckBox Check(Panel sc, string label, bool def, ref int y)
        {
            var chk = new CheckBox { Text = label, Checked = def, ForeColor = FG, Location = new Point(12, y), AutoSize = true };
            sc.Controls.Add(chk); y += 26;
            return chk;
        }
        private void BtnAction(Panel sc, string label, ref int y, Func<Task> action)
        {
            var b = new Button { Text = label, Width = 220, Height = 28, Location = new Point(12, y), BackColor = ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += async (_, _) => await action();
            sc.Controls.Add(b); y += 36;
        }
        private void AddBrowseFileBtn(Panel sc, TextBox target, ref int y)
        {
            var b = new Button { Text = "\ud83d\udcc2 Parcourir...", Width = 130, Height = 26, Location = new Point(12, y), BackColor = Color.FromArgb(50, 70, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => { using var d = new OpenFileDialog { Filter = AllFilesFilter() }; if (d.ShowDialog() == DialogResult.OK) target.Text = d.FileName; };
            sc.Controls.Add(b); y += 30;
        }
        private void AddBrowseFolderBtn(Panel sc, TextBox target, ref int y)
        {
            var b = new Button { Text = "\ud83d\udcc1 Dossier...", Width = 120, Height = 26, Location = new Point(12, y), BackColor = Color.FromArgb(50, 70, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => { using var d = new FolderBrowserDialog(); if (d.ShowDialog() == DialogResult.OK) target.Text = d.SelectedPath; };
            sc.Controls.Add(b); y += 30;
        }
        private static string AllFilesFilter() =>
            "Tous les fichiers|*.*|Images|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp;*.gif;*.webp|PDF|*.pdf";
    }
}
