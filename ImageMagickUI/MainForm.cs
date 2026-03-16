using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMagickUI
{
    public class MainForm : Form
    {
        private TextBox    txtInput  = new();
        private TextBox    txtOutput = new();
        private TextBox    txtLog    = new();
        private TabControl tabs      = new();

        private readonly List<Button> _allButtons = new();

        private static readonly Color BG     = Color.FromArgb(245, 245, 248);
        private static readonly Color SURF   = Color.White;
        private static readonly Color ACCENT = Color.FromArgb(25,  118, 210);
        private static readonly Color GOLD   = Color.FromArgb(130,  80,   0);
        private static readonly Color FG     = Color.FromArgb( 30,  30,  30);
        private static readonly Color BORDER = Color.FromArgb(200, 200, 210);
        private static readonly Color LOG_BG = Color.FromArgb(250, 250, 252);
        private static readonly Color LOG_FG = Color.FromArgb( 20,  90,  20);
        private static readonly Color SCAN_ACCENT = Color.FromArgb(80, 50, 20);

        private const int LBL_W  = 155;
        private const int CTL_X  = 168;
        private const int ROW_H  =  30;
        private const int BTN_W  = 240;
        private const int BTN_H  =  28;
        private const int INDENT =  12;

        private static string I(decimal v) => v.ToString(CultureInfo.InvariantCulture);
        private static string I(double  v) => v.ToString(CultureInfo.InvariantCulture);
        private static string I(int     v) => v.ToString(CultureInfo.InvariantCulture);

        // null = toujours demander, true = toujours écraser, false = toujours renommer
        private bool? _batchOverwriteAll = null;

        public MainForm()
        {
            Text          = "ImageMagick UI  —  WinForms";
            Size          = new Size(1100, 820);
            MinimumSize   = new Size(900,  650);
            BackColor     = BG;
            ForeColor     = FG;
            Font          = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.CenterScreen;
            MagickRunner.OnLog       += AppendLog;
            MagickRunner.BusyChanged += SetBusy;
            BuildLayout();
        }

        private void SetBusy(bool busy)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetBusy(busy))); return; }
            foreach (var b in _allButtons) b.Enabled = !busy;
        }

        // ----------------------------------------------------------------
        // Résolution destination avec dialogue si fichier existant.
        // ----------------------------------------------------------------
        private string? ResolveDestination(string proposed, ref bool? batchApplyAll)
        {
            if (!File.Exists(proposed)) return proposed;

            if (batchApplyAll == true)  return proposed;
            if (batchApplyAll == false)
            {
                var dir  = Path.GetDirectoryName(proposed) ?? "";
                var auto = OverwriteDialog.AutoRename(proposed);
                return Path.Combine(dir, auto);
            }

            OverwriteDialog.Choice choice = OverwriteDialog.Choice.Cancel;
            string finalPath = proposed;
            bool applyAll = false;
            bool applyAllOverwrite = false;

            void ShowDialog()
            {
                using var dlg = new OverwriteDialog(proposed);
                dlg.ShowDialog(this);
                choice           = dlg.Result;
                finalPath        = dlg.FinalPath;
                applyAll         = dlg.ApplyToAll;
                applyAllOverwrite = (choice == OverwriteDialog.Choice.Overwrite);
            }

            if (InvokeRequired) Invoke(new Action(ShowDialog));
            else ShowDialog();

            if (applyAll)
                batchApplyAll = applyAllOverwrite ? (bool?)true : false;

            return choice == OverwriteDialog.Choice.Cancel ? null : finalPath;
        }

        private string? ResolveDestination(string proposed)
        {
            bool? dummy = null;
            return ResolveDestination(proposed, ref dummy);
        }

        private void BuildLayout()
        {
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = BG, Padding = new Padding(8),
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            main.RowStyles.Add(new RowStyle(SizeType.Percent,  100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            main.Controls.Add(BuildIOBar(),   0, 0);
            main.Controls.Add(BuildTabs(),    0, 1);
            main.Controls.Add(BuildConsole(), 0, 2);
            Controls.Add(main);
        }

        private Panel BuildIOBar()
        {
            var p = StyledPanel(); p.Dock = DockStyle.Fill;
            AddLbl(p, "\ud83d\udcc2 Source :",  8);
            txtInput  = AddTxt(p, 490, 95, 8);
            AddBtn(p, "Parcourir", 595, 6, 105, (_, _) => BrowseOpen(txtInput));
            AddLbl(p, "\ud83d\udcbe Sortie  :", 42);
            txtOutput = AddTxt(p, 490, 95, 42);
            AddBtn(p, "Parcourir", 595, 40, 105, (_, _) => BrowseSave(txtOutput));
            AddBtn(p, "\u2139 Infos fichier", 8, 74, 140, async (_, _) => await ShowFileInfoAsync(txtInput.Text));
            return p;
        }

        // ----------------------------------------------------------------
        // Infos fichier : fenêtre séparée, ne pollue pas la console
        // ----------------------------------------------------------------
        private async Task ShowFileInfoAsync(string path)
        {
            if (!File.Exists(path)) { AppendLog("\u26a0 Fichier source introuvable"); return; }

            var win = new InfoWindow(path);
            win.Show(this);

            var sb = new System.Text.StringBuilder();
            await MagickRunner.RunCollectAsync(
                new[] { "identify", "-verbose", path },
                line => sb.AppendLine(line));

            win.SetContent(sb.ToString());
        }

        private TabControl BuildTabs()
        {
            tabs = new TabControl
            {
                Dock = DockStyle.Fill, BackColor = SURF, ForeColor = FG,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            };
            tabs.TabPages.Add(BuildTabTransform());
            tabs.TabPages.Add(BuildTabEffects());
            tabs.TabPages.Add(BuildTabColors());
            tabs.TabPages.Add(BuildTabPdf());
            tabs.TabPages.Add(BuildTabAnnotate());
            tabs.TabPages.Add(BuildTabScan());
            tabs.TabPages.Add(BuildTabBatch());
            return tabs;
        }

        private Panel BuildConsole()
        {
            var p = StyledPanel(); p.Dock = DockStyle.Fill;
            var hdr = new Label { Text = "\ud83d\udccb Console", ForeColor = GOLD, Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, Location = new Point(6, 4) };
            txtLog = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                BackColor = LOG_BG, ForeColor = LOG_FG, Font = new Font("Consolas", 8.5f),
                Location  = new Point(6, 24), Size = new Size(1060, 110),
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Pr\u00eat. S\u00e9lectionnez un fichier source et destination...",
            };
            var clr = MakeBtn("\ud83d\uddd1 Effacer", 100);
            clr.Location = new Point(6, 140); clr.Click += (_, _) => txtLog.Clear();
            p.Controls.AddRange(new Control[] { hdr, txtLog, clr });
            p.Resize += (_, _) => { txtLog.Width = p.Width - 18; };
            return p;
        }

        // ---------------------------------------------------------------- onglets
        private TabPage BuildTabTransform()
        {
            var pg = MakeTab("\ud83d\udd04 Transformations"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "Redimensionner", ref y);
            var nW = Row(sc, "Largeur :", 1920, 1, 99999, ref y, 140);
            var nH = Row(sc, "Hauteur :", 1080, 1, 99999, ref y, 140);
            var chRatio = RowChk(sc, "Conserver les proportions", true, ref y);
            BtnRow(sc, "Redimensionner", ref y, async () =>
            {
                var sz  = chRatio.Checked ? $"{I((int)nW.Value)}x{I((int)nH.Value)}>" : $"{I((int)nW.Value)}x{I((int)nH.Value)}";
                await Run(txtInput.Text, txtOutput.Text, "-resize", sz);
            });
            Sec(sc, "Recadrer (Crop)", ref y);
            var cW = Row(sc, "W :", 800, 1, 99999, ref y); var cH = Row(sc, "H :", 600, 1, 99999, ref y);
            var cX = Row(sc, "X :", 0,   0, 99999, ref y); var cY = Row(sc, "Y :", 0,   0, 99999, ref y);
            BtnRow(sc, "Recadrer", ref y, async () =>
                await Run(txtInput.Text, txtOutput.Text, "-crop", $"{I((int)cW.Value)}x{I((int)cH.Value)}+{I((int)cX.Value)}+{I((int)cY.Value)}", "+repage"));
            Sec(sc, "Rotation", ref y);
            var ang = RowD(sc, "Angle (\u00b0) :", 0, -360, 360, ref y, 120);
            BtnRow(sc, "Appliquer rotation", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-rotate", I(ang.Value)));
            Sec(sc, "Miroirs", ref y);
            BtnRow(sc, "Flip (axe horizontal)", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-flip"));
            BtnRow(sc, "Flop (axe vertical)",   ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-flop"));
            BtnRow(sc, "Transpose",              ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-transpose"));
            BtnRow(sc, "Transverse",             ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-transverse"));
            Sec(sc, "Rognage automatique", ref y);
            var fz = RowD(sc, "Fuzz % :", 5, 0, 100, ref y);
            BtnRow(sc, "Rogner (Trim)", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-fuzz", $"{I(fz.Value)}%", "-trim", "+repage"));
            Sec(sc, "Bordure", ref y);
            var nb = Row(sc, "\u00c9paisseur :", 10, 0, 500, ref y, 100);
            var bc = RowTxt(sc, "Couleur :", "white", ref y, 120);
            BtnRow(sc, "Ajouter bordure", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-bordercolor", bc.Text, "-border", $"{I((int)nb.Value)}x{I((int)nb.Value)}"));
            return pg;
        }

        private TabPage BuildTabEffects()
        {
            var pg = MakeTab("\u2728 Effets visuels"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "Flou (Blur)", ref y);
            var bR = RowD(sc, "Rayon :", 0, 0, 50, ref y); var bS = RowD(sc, "Sigma :", 3, 0, 50, ref y);
            BtnRow(sc, "Blur",          ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-blur",          $"{I(bR.Value)}x{I(bS.Value)}"));
            BtnRow(sc, "Gaussian Blur", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-gaussian-blur", $"{I(bR.Value)}x{I(bS.Value)}"));
            BtnRow(sc, "Motion Blur",   ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-motion-blur",   $"0x{I(bS.Value)}+45"));
            Sec(sc, "Net\u02adet\u00e9 (Sharpen)", ref y);
            var shR = RowD(sc, "Rayon :",     0,    0, 50, ref y); var shS = RowD(sc, "Sigma :",     1, 0, 50, ref y);
            var shA = RowD(sc, "Amount :",    0.5m, 0, 10, ref y); var shT = RowD(sc, "Threshold :", 0.1m, 0, 10, ref y);
            BtnRow(sc, "Sharpen",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-sharpen", $"{I(shR.Value)}x{I(shS.Value)}"));
            BtnRow(sc, "Unsharp Mask", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-unsharp", $"{I(shR.Value)}x{I(shS.Value)}+{I(shA.Value)}+{I(shT.Value)}"));
            Sec(sc, "Bruit (Noise)", ref y);
            var noiseT = new[] { "Uniform","Gaussian","Multiplicative","Impulse","Laplacian","Poisson" };
            var cNoise = RowCmb(sc, "Type :", noiseT, "Multiplicative", ref y, 160);
            var nAtt   = RowD(sc, "Att\u00e9nuation :", 0.4m, 0, 5, ref y);
            BtnRow(sc, "Ajouter bruit", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-attenuate", I(nAtt.Value), "+noise", cNoise.Text));
            BtnRow(sc, "Despeckle",     ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-despeckle"));
            var nMed = RowD(sc, "Rayon m\u00e9dian :", 1, 0, 20, ref y);
            BtnRow(sc, "M\u00e9dian", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-median", I(nMed.Value)));
            Sec(sc, "Effets artistiques", ref y);
            var nCh = RowD(sc, "Rayon charcoal :", 2, 0, 20, ref y);
            BtnRow(sc, "Charcoal",  ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-charcoal", I(nCh.Value)));
            var nOil = RowD(sc, "Rayon peinture :", 4, 0, 20, ref y);
            BtnRow(sc, "Oil Paint", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-paint", I(nOil.Value)));
            var skR = RowD(sc, "R sketch :", 0, 0, 50, ref y); var skS = RowD(sc, "S sketch :", 1, 0, 50, ref y); var skA = RowD(sc, "Angle :", 45, 0, 360, ref y);
            BtnRow(sc, "Sketch", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-sketch", $"{I(skR.Value)}x{I(skS.Value)}+{I(skA.Value)}"));
            BtnRow(sc, "Emboss", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-emboss", "0x1"));
            Sec(sc, "Distorsions", ref y);
            var nSw = RowD(sc, "Swirl (\u00b0) :",    90,   -360, 360, ref y);
            BtnRow(sc, "Swirl",    ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-swirl",   I(nSw.Value)));
            var nIm = RowD(sc, "Implode :",         0.5m, -5,   5,   ref y);
            BtnRow(sc, "Implode",  ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-implode", I(nIm.Value)));
            var nSp = RowD(sc, "Spread (px) :",     5,    0,    100, ref y);
            BtnRow(sc, "Spread",   ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-spread",  I(nSp.Value)));
            var wA = RowD(sc, "Amplitude vague :",  10,   0,    200, ref y);
            var wL = RowD(sc, "Longueur vague :",   100,  1,    1000, ref y);
            BtnRow(sc, "Wave",     ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-wave",    $"{I(wA.Value)}x{I(wL.Value)}"));
            var nPx = RowD(sc, "Pixelate % :",      10,   1,    50,  ref y);
            BtnRow(sc, "Pixelate", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-scale",   $"{I(nPx.Value)}%", "-scale", "100%"));
            return pg;
        }

        private TabPage BuildTabColors()
        {
            var pg = MakeTab("\ud83c\udfa8 Couleurs"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "Espace colorim\u00e9trique", ref y);
            var spaces = new[] { "sRGB","Gray","CMYK","HSL","HSB","Lab","XYZ","YCbCr","YUV","LinearGray" };
            var cCs = RowCmb(sc, "Espace :", spaces, "sRGB", ref y, 160);
            BtnRow(sc, "Convertir",       ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-colorspace", cCs.Text));
            BtnRow(sc, "Niveaux de gris", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-colorspace", "Gray"));
            BtnRow(sc, "N\u00e9gatif",         ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-negate"));
            BtnRow(sc, "Normaliser",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-normalize"));
            BtnRow(sc, "\u00c9galiser",        ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-equalize"));
            BtnRow(sc, "Auto-Level",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-auto-level"));
            BtnRow(sc, "Auto-Gamma",      ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-auto-gamma"));
            Sec(sc, "Luminosit\u00e9 / Contraste", ref y);
            var nBr = RowD(sc, "Luminosit\u00e9 :", 0, -100, 100, ref y); var nCo = RowD(sc, "Contraste :", 0, -100, 100, ref y);
            BtnRow(sc, "Appliquer", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-brightness-contrast", $"{I(nBr.Value)}x{I(nCo.Value)}"));
            Sec(sc, "Gamma", ref y);
            var nGa = RowD(sc, "Gamma :", 1.0m, 0.1m, 10, ref y);
            BtnRow(sc, "Appliquer gamma", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-gamma", I(nGa.Value)));
            Sec(sc, "Niveaux (Levels)", ref y);
            var lvBk = RowD(sc, "Noir % :",  0,    0, 100,  ref y);
            var lvWh = RowD(sc, "Blanc % :", 100,  0, 100,  ref y);
            var lvGa = RowD(sc, "Gamma :",   1.0m, 0.1m, 10, ref y);
            BtnRow(sc, "Appliquer niveaux", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-level", $"{I(lvBk.Value)}%,{I(lvWh.Value)}%,{I(lvGa.Value)}"));
            Sec(sc, "Modulation HSB", ref y);
            var mB = RowD(sc, "Luminosit\u00e9 % :", 100, 0, 200, ref y);
            var mS = RowD(sc, "Saturation % :", 100, 0, 200, ref y);
            var mH = RowD(sc, "Teinte % :",     100, 0, 200, ref y);
            BtnRow(sc, "Moduler", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-modulate", $"{I(mB.Value)},{I(mS.Value)},{I(mH.Value)}"));
            Sec(sc, "S\u00e9pia / Teinte / Colorisation", ref y);
            var nSep = RowD(sc, "Seuil s\u00e9pia % :", 80, 0, 100, ref y);
            BtnRow(sc, "S\u00e9pia-tone", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-sepia-tone", $"{I(nSep.Value)}%"));
            var tTnt = RowTxt(sc, "Couleur tint :",    "#FFD700", ref y, 100);
            var nTOp = RowD(sc, "Opacit\u00e9 :",          50, 0, 100, ref y);
            BtnRow(sc, "Tint", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-fill", tTnt.Text, "-tint", I(nTOp.Value)));
            var tCol = RowTxt(sc, "Couleur colorize :", "blue", ref y, 100);
            var nCPc = RowD(sc, "% :",                  50, 0, 100, ref y);
            BtnRow(sc, "Colorize", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-fill", tCol.Text, "-colorize", I(nCPc.Value)));
            Sec(sc, "Seuillage / Posterize / Dithering", ref y);
            var nThr = RowD(sc, "Seuil % :",         50, 0, 100, ref y);
            BtnRow(sc, "Threshold", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-threshold", $"{I(nThr.Value)}%"));
            var nPos = Row(sc, "Niveaux posterize :", 4, 2, 256, ref y);
            BtnRow(sc, "Posterize", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-posterize", I((int)nPos.Value)));
            var nDit = Row(sc, "Couleurs dither :",  16, 2, 256, ref y);
            BtnRow(sc, "Dither Riemersma", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-dither", "Riemersma", "-colors", I((int)nDit.Value)));
            Sec(sc, "Extraire canal", ref y);
            var chs = new[] { "Red","Green","Blue","Alpha","Cyan","Magenta","Yellow","Black","All" };
            var cCh = RowCmb(sc, "Canal :", chs, "Red", ref y, 120);
            BtnRow(sc, "S\u00e9parer canal", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-channel", cCh.Text, "-separate"));
            return pg;
        }

        private TabPage BuildTabPdf()
        {
            var pg = MakeTab("\ud83d\udcc4 PDF"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "PDF \u2192 Images", ref y);
            Note(sc, "Sortie exemple : page-%04d.png", ref y);
            var pDpi = Row(sc, "DPI :", 200, 72, 600, ref y);
            BtnRow(sc, "Exporter les pages", ref y, async () =>
                await MagickRunner.RunAsync(new[] { "-density", I((int)pDpi.Value), txtInput.Text, txtOutput.Text }));
            Sec(sc, "Extraire une plage de pages", ref y);
            Note(sc, "Syntaxe : 0-2 (pages 1-3)  ou  0,2,4", ref y);
            var tPg = RowTxt(sc, "Pages :", "0-2", ref y, 120);
            BtnRow(sc, "Extraire pages", ref y, async () =>
                await MagickRunner.RunAsync(new[] { $"{txtInput.Text}[{tPg.Text.Trim()}]", txtOutput.Text }));
            Sec(sc, "Compresser PDF", ref y);
            var cDpi = Row(sc, "DPI :",    150, 72,  600, ref y);
            var cQl  = Row(sc, "Qualit\u00e9 :", 75,  1,  100, ref y);
            BtnRow(sc, "Compresser PDF", ref y, async () =>
                await MagickRunner.RunAsync(new[] { "-density", I((int)cDpi.Value), txtInput.Text, "-compress", "JPEG", "-quality", I((int)cQl.Value), txtOutput.Text }));
            Sec(sc, "Images \u2192 PDF (dossier batch)", ref y);
            var tDir = RowTxt(sc, "Dossier :", "", ref y, 350);
            BrowseFolderBtn(sc, tDir, ref y);
            BtnRow(sc, "Assembler en PDF", ref y, async () =>
            {
                if (!Directory.Exists(tDir.Text)) { AppendLog("\u26a0 Dossier introuvable"); return; }
                var imgs = Directory.GetFiles(tDir.Text, "*.*")
                    .Where(f => new[] { ".png",".jpg",".jpeg",".tif",".tiff",".bmp" }.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f).ToList();
                if (!imgs.Any()) { AppendLog("\u26a0 Aucune image trouv\u00e9e"); return; }
                await MagickRunner.RunAsync(imgs.Concat(new[] { txtOutput.Text }));
            });
            Sec(sc, "Montage (grille d'images)", ref y);
            var mDir = RowTxt(sc, "Dossier images :", "", ref y, 350);
            BrowseFolderBtn(sc, mDir, ref y);
            var mCol = Row(sc, "Colonnes :",    3,   1, 20,   ref y);
            var mW   = Row(sc, "Larg. tuile :", 200, 50, 2000, ref y);
            var mH2  = Row(sc, "Haut. tuile :", 200, 50, 2000, ref y);
            BtnRow(sc, "Cr\u00e9er montage", ref y, async () =>
            {
                if (!Directory.Exists(mDir.Text)) { AppendLog("\u26a0 Dossier introuvable"); return; }
                var imgs = Directory.GetFiles(mDir.Text, "*.*")
                    .Where(f => new[] { ".png",".jpg",".jpeg" }.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f).ToArray();
                var a = new List<string> { "montage" };
                a.AddRange(imgs);
                a.AddRange(new[] { "-tile", $"{I((int)mCol.Value)}x", "-geometry", $"{I((int)mW.Value)}x{I((int)mH2.Value)}+4+4", txtOutput.Text });
                await MagickRunner.RunAsync(a);
            });
            return pg;
        }

        private TabPage BuildTabAnnotate()
        {
            var pg = MakeTab("\u270f\ufe0f Annotations"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "Ins\u00e9rer du texte", ref y);
            var aTxt  = RowTxt(sc, "Texte :",   "Mon texte", ref y, 400);
            var aFnt  = RowTxt(sc, "Police :",  "Arial",     ref y, 150);
            var aSize = Row(sc,    "Taille :",   36, 1, 500, ref y);
            var aClr  = RowTxt(sc, "Couleur :", "black",     ref y, 100);
            var aX    = Row(sc, "X :", 10, 0, 9999, ref y); var aY = Row(sc, "Y :", 10, 0, 9999, ref y);
            var gravs = new[] { "NorthWest","North","NorthEast","West","Center","East","SouthWest","South","SouthEast" };
            var aGrv  = RowCmb(sc, "Gravit\u00e9 :", gravs, "NorthWest", ref y, 160);
            BtnRow(sc, "Ins\u00e9rer texte", ref y, async () =>
                await MagickRunner.RunAsync(new[] { txtInput.Text, "-font", aFnt.Text, "-pointsize", I((int)aSize.Value), "-fill", aClr.Text, "-gravity", aGrv.Text, "-annotate", $"+{I((int)aX.Value)}+{I((int)aY.Value)}", aTxt.Text, txtOutput.Text }));
            Sec(sc, "Filigrane (Watermark)", ref y);
            var wFile = RowTxt(sc, "Fichier WM :", "", ref y, 350);
            BrowseFileBtn(sc, wFile, ref y);
            var wOp  = RowD(sc, "Opacit\u00e9 % :", 50, 0, 100, ref y);
            var wGrv = RowCmb(sc, "Position :", gravs, "Center", ref y, 160);
            BtnRow(sc, "Appliquer watermark", ref y, async () =>
                await MagickRunner.RunAsync(new[] { "composite", "-dissolve", I(wOp.Value), "-gravity", wGrv.Text, wFile.Text, txtInput.Text, txtOutput.Text }));
            Sec(sc, "Dessiner un rectangle", ref y);
            var rX1 = Row(sc, "X1 :", 10,  0, 9999, ref y); var rY1 = Row(sc, "Y1 :", 10,  0, 9999, ref y);
            var rX2 = Row(sc, "X2 :", 200, 0, 9999, ref y); var rY2 = Row(sc, "Y2 :", 200, 0, 9999, ref y);
            var rStr = RowTxt(sc, "Trait :", "red",  ref y, 80);
            var rFll = RowTxt(sc, "Fill :",  "none", ref y, 80);
            BtnRow(sc, "Dessiner rectangle", ref y, async () =>
                await MagickRunner.RunAsync(new[] { txtInput.Text, "-fill", rFll.Text, "-stroke", rStr.Text, "-draw", $"rectangle {I((int)rX1.Value)},{I((int)rY1.Value)} {I((int)rX2.Value)},{I((int)rY2.Value)}", txtOutput.Text }));
            Sec(sc, "Dessiner un cercle", ref y);
            var cCX = Row(sc, "CX :", 100, 0, 9999, ref y); var cCY = Row(sc, "CY :", 100, 0, 9999, ref y);
            var cRR = Row(sc, "R :",   50, 1, 9999, ref y);
            var cCl = RowTxt(sc, "Couleur :", "blue", ref y, 80);
            BtnRow(sc, "Dessiner cercle", ref y, async () =>
            {
                int cx = (int)cCX.Value, cy = (int)cCY.Value, cr = (int)cRR.Value;
                await MagickRunner.RunAsync(new[] { txtInput.Text, "-fill", "none", "-stroke", cCl.Text, "-draw", $"circle {I(cx)},{I(cy)} {I(cx + cr)},{I(cy)}", txtOutput.Text });
            });
            return pg;
        }

        // ---------------------------------------------------------------- ONGLET SCAN
        private TabPage BuildTabScan()
        {
            var pg = MakeTab("\ud83d\udda8 Scan / Impression"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "Présets rapides", ref y);
            Note(sc, "Charge les paramètres ci-dessous, puis cliquez Appliquer.", ref y);

            NumericUpDown? sDpi = null, sQual = null;
            NumericUpDown? sRot1 = null, sRot2 = null;
            NumericUpDown? sAt1 = null, sAt2 = null, sAt3 = null;
            NumericUpDown? sShrpR = null, sShrpS = null;
            NumericUpDown? sBrightness = null, sContrast = null;
            NumericUpDown? sGamma = null;
            NumericUpDown? sResample = null;
            ComboBox? sSampling = null;
            CheckBox? sGray = null, sSepia = null, sNorm = null, sDeskew = null, sStrip = null;
            ComboBox? sNoiseType = null;
            CheckBox? sDepth8 = null;

            void ApplyPreset(int dpi, decimal rot1, decimal rot2, decimal at1, decimal at2, decimal at3,
                             decimal shrpR, decimal shrpS, int qual, bool gray, bool sep,
                             decimal bright, decimal contrast, decimal gamma, int resample)
            {
                sDpi!.Value        = dpi;
                sRot1!.Value       = rot1;
                sRot2!.Value       = rot2;
                sAt1!.Value        = Math.Min(at1,  sAt1.Maximum);
                sAt2!.Value        = Math.Min(at2,  sAt2.Maximum);
                sAt3!.Value        = Math.Min(at3,  sAt3.Maximum);
                sShrpR!.Value      = shrpR;
                sShrpS!.Value      = Math.Min(shrpS, sShrpS.Maximum);
                sQual!.Value       = qual;
                sGray!.Checked     = gray;
                sSepia!.Checked    = sep;
                sBrightness!.Value = bright;
                sContrast!.Value   = contrast;
                sGamma!.Value      = gamma;
                sResample!.Value   = resample;
            }

            var presetPanel = new FlowLayoutPanel
            {
                Location      = new Point(INDENT, y),
                Size          = new Size(700, 36),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = BG,
            };
            Button PresetBtn(string label)
            {
                var b = new Button { Text = label, Width = 160, Height = 30, BackColor = SCAN_ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 6, 0) };
                b.FlatAppearance.BorderSize = 0;
                _allButtons.Add(b);
                return b;
            }
            var btnLeger  = PresetBtn("\ud83d\uddd2 Léger");
            var btnMoyen  = PresetBtn("\ud83d\uddd2 Moyen");
            var btnAppuye = PresetBtn("\ud83d\uddd2 Appuyé");
            var btnFax    = PresetBtn("\ud83d\udcf1 Fax / N&B");
            presetPanel.Controls.AddRange(new Control[] { btnLeger, btnMoyen, btnAppuye, btnFax });
            sc.Controls.Add(presetPanel); y += 44;

            Sec(sc, "Résolution d'impression", ref y);
            Note(sc, "DPI simulé avant le scan. Plus bas = plus dégradé.", ref y);
            sDpi = Row(sc, "DPI :", 150, 72, 600, ref y, 90);
            Sec(sc, "Rotation / Désalignement", ref y);
            Note(sc, "Simule un document légèrement de travers sur le scanner.", ref y);
            sRot1 = RowD(sc, "Rotation base (\u00b0) :",        0.3m, -5,  5, ref y, 90);
            sRot2 = RowD(sc, "Variation aléatoire (\u00b0) :", 0.0m,  0,  3, ref y, 90);
            Sec(sc, "Bruit (grain scanner)", ref y);
            Note(sc, "Couches successives de bruit multiplicatif.", ref y);
            var noiseTypes = new[] { "Multiplicative", "Gaussian", "Uniform", "Laplacian" };
            sNoiseType = RowCmb(sc, "Type de bruit :", noiseTypes, "Multiplicative", ref y, 160);
            sAt1 = RowD(sc, "Atténuation 1 :", 0.40m, 0, 2, ref y, 90);
            sAt2 = RowD(sc, "Atténuation 2 :", 0.03m, 0, 2, ref y, 90);
            sAt3 = RowD(sc, "Atténuation 3 :", 0.00m, 0, 2, ref y, 90);
            Note(sc, "Atténuation 3 = 0 désactive la 3ème couche.", ref y);
            Sec(sc, "Nettété (Unsharp Mask)", ref y);
            Note(sc, "Simule la sur-nettété des pilotes de scanner.", ref y);
            sShrpR = RowD(sc, "Rayon :", 0.0m, 0,    10,  ref y, 90);
            sShrpS = RowD(sc, "Sigma :", 1.0m, 0.1m, 10,  ref y, 90);
            Sec(sc, "Luminosité / Contraste / Gamma", ref y);
            sBrightness = RowD(sc, "Luminosité :", 0,    -100, 100, ref y, 90);
            sContrast   = RowD(sc, "Contraste :",  0,    -100, 100, ref y, 90);
            sGamma      = RowD(sc, "Gamma :",      1.0m,  0.1m, 5,  ref y, 90);
            Sec(sc, "Réduction de taille", ref y);
            Note(sc, "Options complémentaires pour alléger le fichier sans dégrader la qualité visuelle.", ref y);
            sResample = Row(sc, "Resample DPI :", 0, 0, 600, ref y, 90);
            Note(sc, "0 = désactivé. Rééchantillonne la résolution méta.", ref y);
            var samplingOptions = new[] { "Aucun", "4:2:0 (chroma -20%)", "4:1:1 (chroma -30%)" };
            sSampling = RowCmb(sc, "Chroma subsampling :", samplingOptions, "Aucun", ref y, 220);
            Note(sc, "4:2:0 est invisible sur texte/docs.", ref y);
            sDepth8 = RowChk(sc, "Forcer profondeur 8 bits (-depth 8)", true, ref y);
            Note(sc, "Supprime les canaux 16 bits superflus.", ref y);
            Sec(sc, "Options de sortie", ref y);
            sGray   = RowChk(sc, "Niveaux de gris",             false, ref y);
            sSepia  = RowChk(sc, "Ton sépia (vieux document)",  false, ref y);
            sNorm   = RowChk(sc, "Normaliser après scan",       false, ref y);
            sDeskew = RowChk(sc, "Deskew auto (-deskew 40%)",   false, ref y);
            sStrip  = RowChk(sc, "Supprimer métadonnées",      true,  ref y);
            sQual   = Row(sc, "Qualité JPEG :", 80, 1, 100, ref y, 90);

            btnLeger.Click  += (_, _) => ApplyPreset(200, 0.1m, 0.1m, 0.15m, 0.01m, 0,     0, 0.5m,  90, false, false,  5,  5, 1.0m,  72);
            btnMoyen.Click  += (_, _) => ApplyPreset(150, 0.3m, 0.2m, 0.40m, 0.03m, 0,     0, 1.0m,  80, false, false,  0,  0, 1.0m,  72);
            btnAppuye.Click += (_, _) => ApplyPreset(100, 0.5m, 0.4m, 0.70m, 0.10m, 0.05m, 0, 1.5m,  65, false, false, -5, 10, 0.9m,  72);
            btnFax.Click    += (_, _) => ApplyPreset(100, 0.4m, 0.3m, 0.60m, 0.08m, 0.02m, 0, 2.0m,  60, true,  false,  0, 15, 0.85m, 72);

            y += 6;
            BtnRow(sc, "\ud83d\udda8 Appliquer sur le fichier source", ref y, async () =>
            {
                var dst = ResolveDestination(txtOutput.Text);
                if (dst == null) { AppendLog("\u23f9 Opération annulée."); return; }
                await ApplyScanEffect(
                    txtInput.Text, dst,
                    sDpi!, sRot1!, sRot2!, sAt1!, sAt2!, sAt3!, sNoiseType!,
                    sShrpR!, sShrpS!, sBrightness!, sContrast!, sGamma!,
                    sQual!, sGray!, sSepia!, sNorm!, sDeskew!, sStrip!,
                    sResample!, sSampling!, sDepth8!);
            });

            Sec(sc, "Traitement par lot (dossier)", ref y);
            Note(sc, "Applique les mêmes réglages ci-dessus à toutes les images d'un dossier.", ref y);
            var bIn  = RowTxt(sc, "Dossier source :", "", ref y, 350); BrowseFolderBtn(sc, bIn,  ref y);
            var bOut = RowTxt(sc, "Dossier sortie :", "", ref y, 350); BrowseFolderBtn(sc, bOut, ref y);
            var bExt = RowTxt(sc, "Extension sortie :", ".jpg", ref y, 60);
            Note(sc, "Ex: .jpg .png .tif  —  laissez vide pour conserver l'extension originale.", ref y);

            BtnRow(sc, "\ud83d\udd04 Lancer le batch scan", ref y, async () =>
            {
                if (!Directory.Exists(bIn.Text) || string.IsNullOrWhiteSpace(bOut.Text)) { AppendLog("\u26a0 Vérifiez les dossiers"); return; }
                Directory.CreateDirectory(bOut.Text);
                var exts  = new[] { ".png",".jpg",".jpeg",".tif",".tiff",".bmp",".gif",".pdf" };
                var files = Directory.GetFiles(bIn.Text).Where(f => exts.Contains(Path.GetExtension(f).ToLower())).OrderBy(f => f).ToArray();
                AppendLog($"\ud83d\udd04 Batch Scan — {files.Length} fichier(s)...");
                _batchOverwriteAll = null;
                foreach (var f in files)
                {
                    var ext = string.IsNullOrWhiteSpace(bExt.Text) ? Path.GetExtension(f) : bExt.Text.Trim();
                    var proposed = Path.Combine(bOut.Text, Path.GetFileNameWithoutExtension(f) + ext);
                    var dst = ResolveDestination(proposed, ref _batchOverwriteAll);
                    if (dst == null) { AppendLog("\u23f9 Batch annulé par l'utilisateur."); break; }
                    await ApplyScanEffect(
                        f, dst,
                        sDpi!, sRot1!, sRot2!, sAt1!, sAt2!, sAt3!, sNoiseType!,
                        sShrpR!, sShrpS!, sBrightness!, sContrast!, sGamma!,
                        sQual!, sGray!, sSepia!, sNorm!, sDeskew!, sStrip!,
                        sResample!, sSampling!, sDepth8!);
                }
                AppendLog("\u2705 Batch Scan terminé.");
            });
            return pg;
        }

        private async Task ApplyScanEffect(
            string src, string dst,
            NumericUpDown sDpi, NumericUpDown sRot1, NumericUpDown sRot2,
            NumericUpDown sAt1, NumericUpDown sAt2, NumericUpDown sAt3,
            ComboBox sNoiseType,
            NumericUpDown sShrpR, NumericUpDown sShrpS,
            NumericUpDown sBrightness, NumericUpDown sContrast,
            NumericUpDown sGamma, NumericUpDown sQual,
            CheckBox sGray, CheckBox sSepia, CheckBox sNorm, CheckBox sDeskew, CheckBox sStrip,
            NumericUpDown sResample, ComboBox sSampling, CheckBox sDepth8)
        {
            if (!File.Exists(src)) { AppendLog($"\u26a0 Fichier introuvable : {src}"); return; }
            if (string.IsNullOrWhiteSpace(dst)) { AppendLog("\u26a0 Définissez le fichier de sortie"); return; }

            var rng     = new Random();
            double var2 = (double)sRot2.Value * (rng.NextDouble() * 2 - 1);
            double rot  = (double)sRot1.Value + var2;

            var a = new List<string> { "-density", I((int)sDpi.Value), src };
            if (rot != 0) a.AddRange(new[] { "-rotate", rot.ToString("0.##", CultureInfo.InvariantCulture) });

            string noise = sNoiseType.Text;
            if (sAt1.Value > 0) a.AddRange(new[] { "-attenuate", I(sAt1.Value), "+noise", noise });
            if (sAt2.Value > 0) a.AddRange(new[] { "-attenuate", I(sAt2.Value), "+noise", noise });
            if (sAt3.Value > 0) a.AddRange(new[] { "-attenuate", I(sAt3.Value), "+noise", noise });
            if (sShrpS.Value > 0) a.AddRange(new[] { "-unsharp", $"{I(sShrpR.Value)}x{I(sShrpS.Value)}+0.5+0.05" });
            if (sBrightness.Value != 0 || sContrast.Value != 0)
                a.AddRange(new[] { "-brightness-contrast", $"{I(sBrightness.Value)}x{I(sContrast.Value)}" });
            if (sGamma.Value != 1.0m) a.AddRange(new[] { "-gamma", I(sGamma.Value) });
            if (sDepth8.Checked)      a.AddRange(new[] { "-depth", "8" });
            if (sResample.Value > 0)  a.AddRange(new[] { "-resample", I((int)sResample.Value) });
            if (sSampling.SelectedIndex == 1) a.AddRange(new[] { "-sampling-factor", "4:2:0" });
            else if (sSampling.SelectedIndex == 2) a.AddRange(new[] { "-sampling-factor", "4:1:1" });
            if (sGray.Checked)   a.AddRange(new[] { "-colorspace", "Gray" });
            if (sSepia.Checked)  a.AddRange(new[] { "-sepia-tone", "80%" });
            if (sNorm.Checked)   a.Add("-normalize");
            if (sDeskew.Checked) a.AddRange(new[] { "-deskew", "40%" });
            if (sStrip.Checked)  a.Add("-strip");
            a.AddRange(new[] { "-compress", "JPEG", "-quality", I((int)sQual.Value) });
            a.Add(dst);
            await MagickRunner.RunAsync(a);
        }

        // ---------------------------------------------------------------- Batch onglet
        private TabPage BuildTabBatch()
        {
            var pg = MakeTab("\ud83d\udce6 Batch & Format"); var sc = MakeSP(pg); int y = 8;
            Sec(sc, "Traitement par lot", ref y);
            var bIn  = RowTxt(sc, "Dossier source :", "", ref y, 350); BrowseFolderBtn(sc, bIn,  ref y);
            var bOut = RowTxt(sc, "Dossier sortie :", "", ref y, 350); BrowseFolderBtn(sc, bOut, ref y);
            var bOps = new[] { "Grayscale","Resize 50%","Resize 150%","Normalize","JPEG q85","Auto-level","Auto-gamma" };
            var bOp  = RowCmb(sc, "Op\u00e9ration :", bOps, "Grayscale", ref y, 200);
            BtnRow(sc, "\ud83d\udd04 Lancer le batch", ref y, async () =>
            {
                if (!Directory.Exists(bIn.Text) || string.IsNullOrWhiteSpace(bOut.Text)) { AppendLog("\u26a0 V\u00e9rifiez les dossiers"); return; }
                Directory.CreateDirectory(bOut.Text);
                var exts  = new[] { ".png",".jpg",".jpeg",".tif",".tiff",".pdf",".bmp",".gif" };
                var files = Directory.GetFiles(bIn.Text).Where(f => exts.Contains(Path.GetExtension(f).ToLower())).ToArray();
                AppendLog($"\ud83d\udd04 Batch '{bOp.Text}' \u2014 {files.Length} fichier(s)...");
                _batchOverwriteAll = null;
                foreach (var f in files)
                {
                    var proposed = Path.Combine(bOut.Text, Path.GetFileName(f));
                    var dst = ResolveDestination(proposed, ref _batchOverwriteAll);
                    if (dst == null) { AppendLog("\u23f9 Batch annulé par l'utilisateur."); break; }
                    string[] args = bOp.Text switch
                    {
                        "Grayscale"   => new[] { f, "-colorspace", "Gray", dst },
                        "Resize 50%"  => new[] { f, "-resize", "50%",  dst },
                        "Resize 150%" => new[] { f, "-resize", "150%", dst },
                        "Normalize"   => new[] { f, "-normalize", dst },
                        "JPEG q85"    => new[] { f, "-quality", "85", dst },
                        "Auto-level"  => new[] { f, "-auto-level", dst },
                        "Auto-gamma"  => new[] { f, "-auto-gamma", dst },
                        _             => new[] { f, dst }
                    };
                    await MagickRunner.RunAsync(args);
                }
                AppendLog("\u2705 Batch termin\u00e9.");
            });
            Sec(sc, "Conversion de format", ref y);
            Note(sc, "Changez l'extension dans le chemin de sortie pour changer le format.", ref y);
            var cQl = Row(sc, "Qualit\u00e9 (JPEG/WebP) :", 85, 1, 100, ref y);
            BtnRow(sc, "Convertir", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-quality", I((int)cQl.Value)));
            Sec(sc, "M\u00e9tadonn\u00e9es", ref y);
            BtnRow(sc, "Supprimer m\u00e9tadonn\u00e9es (-strip)", ref y, async () => await Run(txtInput.Text, txtOutput.Text, "-strip"));
            BtnRow(sc, "\u2139 Afficher infos (identify)", ref y, async () => await ShowFileInfoAsync(txtInput.Text));
            return pg;
        }

        // ---------------------------------------------------------------- Run / Log
        private async Task Run(string src, string dst, params string[] middle)
        {
            if (!File.Exists(src)) { AppendLog("\u26a0 Fichier source introuvable"); return; }
            var resolved = ResolveDestination(dst);
            if (resolved == null) { AppendLog("\u23f9 Opération annulée."); return; }
            var a = new List<string> { src };
            a.AddRange(middle); a.Add(resolved);
            await MagickRunner.RunAsync(a);
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => AppendLog(msg))); return; }
            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        // ---------------------------------------------------------------- UI factories
        private Panel StyledPanel() => new Panel { BackColor = SURF, Padding = new Padding(4), BorderStyle = BorderStyle.FixedSingle };
        private TabPage MakeTab(string name) => new TabPage(name) { BackColor = BG, ForeColor = FG };
        private Panel MakeSP(TabPage tp)
        {
            var s = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BG };
            tp.Controls.Add(s); return s;
        }
        private void AddLbl(Panel p, string text, int y)
        {
            var l = new Label { Text = text, ForeColor = FG, AutoSize = true, Location = new Point(8, y + 4) };
            p.Controls.Add(l);
        }
        private TextBox AddTxt(Panel p, int w, int x, int y)
        {
            var t = new TextBox { Width = w, Location = new Point(x, y), BackColor = SURF, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(t); return t;
        }
        private void AddBtn(Panel p, string label, int x, int y, int w, EventHandler h)
        {
            var b = MakeBtn(label, w); b.Location = new Point(x, y); b.Click += h; p.Controls.Add(b);
        }
        private Button MakeBtn(string label, int w = BTN_W)
        {
            var b = new Button { Text = label, Width = w, Height = BTN_H, BackColor = ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            _allButtons.Add(b);
            return b;
        }
        private void Sec(Panel sc, string title, ref int y)
        {
            y += 4;
            var sep = new Panel { Location = new Point(INDENT, y), Size = new Size(500, 1), BackColor = BORDER };
            var lbl = new Label { Text = title, ForeColor = GOLD, Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, Location = new Point(INDENT, y + 4) };
            sc.Controls.Add(sep); sc.Controls.Add(lbl);
            y += 26;
        }
        private void Note(Panel sc, string text, ref int y)
        {
            var l = new Label { Text = text, ForeColor = Color.FromArgb(100, 100, 120), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), Location = new Point(INDENT + 4, y) };
            sc.Controls.Add(l); y += 20;
        }
        private NumericUpDown Row(Panel sc, string label, int def, int min, int max, ref int y, int w = 90)
        {
            var lbl = new Label { Text = label, ForeColor = FG, Size = new Size(LBL_W, 20), TextAlign = ContentAlignment.MiddleRight, Location = new Point(INDENT, y + 2) };
            var nud = new NumericUpDown { Minimum = min, Maximum = max, Value = def, Width = w, Location = new Point(CTL_X, y), BackColor = SURF, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
            sc.Controls.AddRange(new Control[] { lbl, nud }); y += ROW_H; return nud;
        }
        private NumericUpDown RowD(Panel sc, string label, decimal def, decimal min, decimal max, ref int y, int w = 90)
        {
            var lbl = new Label { Text = label, ForeColor = FG, Size = new Size(LBL_W, 20), TextAlign = ContentAlignment.MiddleRight, Location = new Point(INDENT, y + 2) };
            var nud = new NumericUpDown { Minimum = min, Maximum = max, Value = def, DecimalPlaces = 2, Increment = 0.05m, Width = w, Location = new Point(CTL_X, y), BackColor = SURF, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
            sc.Controls.AddRange(new Control[] { lbl, nud }); y += ROW_H; return nud;
        }
        private TextBox RowTxt(Panel sc, string label, string def, ref int y, int width = 200)
        {
            var lbl = new Label { Text = label, ForeColor = FG, Size = new Size(LBL_W, 20), TextAlign = ContentAlignment.MiddleRight, Location = new Point(INDENT, y + 2) };
            var txt = new TextBox { Text = def, Width = width, Location = new Point(CTL_X, y), BackColor = SURF, ForeColor = FG, BorderStyle = BorderStyle.FixedSingle };
            sc.Controls.AddRange(new Control[] { lbl, txt }); y += ROW_H; return txt;
        }
        private ComboBox RowCmb(Panel sc, string label, string[] items, string def, ref int y, int width = 160)
        {
            var lbl = new Label { Text = label, ForeColor = FG, Size = new Size(LBL_W, 20), TextAlign = ContentAlignment.MiddleRight, Location = new Point(INDENT, y + 2) };
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Location = new Point(CTL_X, y), BackColor = SURF, ForeColor = FG };
            cmb.Items.AddRange(items); cmb.SelectedItem = def;
            sc.Controls.AddRange(new Control[] { lbl, cmb }); y += ROW_H; return cmb;
        }
        private CheckBox RowChk(Panel sc, string label, bool def, ref int y)
        {
            var chk = new CheckBox { Text = label, Checked = def, ForeColor = FG, Location = new Point(CTL_X, y), AutoSize = true };
            sc.Controls.Add(chk); y += ROW_H - 2; return chk;
        }
        private void BtnRow(Panel sc, string label, ref int y, Func<Task> action)
        {
            var b = MakeBtn(label);
            b.Location = new Point(CTL_X, y);
            b.Click += async (_, _) => await action();
            sc.Controls.Add(b); y += BTN_H + 10;
        }
        // MakeSecBtn enregistre aussi dans _allButtons pour être désactivé pendant les traitements
        private Button MakeSecBtn(string label)
        {
            var b = new Button { Text = label, Width = 130, Height = 26, BackColor = Color.FromArgb(70, 130, 180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            _allButtons.Add(b);  // <-- fix: inclus dans SetBusy
            return b;
        }
        private void BrowseFileBtn(Panel sc, TextBox target, ref int y)
        {
            var b = MakeSecBtn("\ud83d\udcc2 Parcourir...");
            b.Location = new Point(CTL_X, y);
            b.Click += (_, _) => { using var d = new OpenFileDialog { Filter = AllFilesFilter() }; if (d.ShowDialog() == DialogResult.OK) target.Text = d.FileName; };
            sc.Controls.Add(b); y += 32;
        }
        private void BrowseFolderBtn(Panel sc, TextBox target, ref int y)
        {
            var b = MakeSecBtn("\ud83d\udcc1 Dossier...");
            b.Location = new Point(CTL_X, y);
            b.Click += (_, _) => { using var d = new FolderBrowserDialog(); if (d.ShowDialog() == DialogResult.OK) target.Text = d.SelectedPath; };
            sc.Controls.Add(b); y += 32;
        }
        private void BrowseOpen(TextBox t) { using var d = new OpenFileDialog { Filter = AllFilesFilter() }; if (d.ShowDialog() == DialogResult.OK) t.Text = d.FileName; }
        private void BrowseSave(TextBox t) { using var d = new SaveFileDialog { Filter = AllFilesFilter() }; if (d.ShowDialog() == DialogResult.OK) t.Text = d.FileName; }
        private static string AllFilesFilter() => "Tous les fichiers|*.*|Images|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp;*.gif;*.webp|PDF|*.pdf";
    }
}
