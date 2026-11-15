using System.Drawing.Printing;
using System.Text;

namespace ParquetPro;

public static class Export
{
    static Encoding W1252()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, new EncoderReplacementFallback("?"), new DecoderReplacementFallback("?"));
    }

    static List<string> Wrap(string text, int width)
    {
        var res = new List<string>();
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var line = raw.Replace("\t", "    ").Replace("✓", "-").Replace("•", "-").Replace("→", "->").Replace("└─", "+-");
            if (line.Length == 0) { res.Add(""); continue; }
            while (line.Length > width)
            {
                int cut = line.LastIndexOf(' ', width);
                if (cut <= 0) cut = width;
                res.Add(line[..cut]);
                line = line[cut..].TrimStart();
            }
            res.Add(line);
        }
        return res;
    }

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    /// Génère un PDF texte (A4) avec la mention de simulation sur chaque page.
    public static void Pdf(string path, string title, string body)
    {
        var enc = W1252();
        var lines = Wrap(body, 95);
        const int perPage = 58;
        var pages = new List<List<string>>();
        for (int i = 0; i < Math.Max(1, lines.Count); i += perPage) pages.Add(lines.Skip(i).Take(perPage).ToList());
        var objs = new List<byte[]>();
        int nPages = pages.Count;
        // 1 catalog, 2 pages, 3 font regular, 4 font bold, puis (page, contenu) * n
        var kids = string.Join(" ", Enumerable.Range(0, nPages).Select(i => $"{5 + i * 2} 0 R"));
        objs.Add(enc.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objs.Add(enc.GetBytes($"<< /Type /Pages /Kids [{kids}] /Count {nPages} >>"));
        objs.Add(enc.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objs.Add(enc.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
        for (int p = 0; p < nPages; p++)
        {
            var sb = new StringBuilder();
            sb.Append("q 0.88 g BT /F2 34 Tf 0.7071 0.7071 -0.7071 0.7071 110 160 Tm (" + Esc(U.Mention) + ") Tj ET Q\n");
            sb.Append("0 0.16 0.33 rg 0 792 595 50 re f\n");
            sb.Append("BT /F2 14 Tf 1 1 1 rg 40 812 Td (PARQUET PRO) Tj ET\n");
            sb.Append("BT /F1 9 Tf 1 1 1 rg 160 812 Td (" + Esc(title.Length > 80 ? title[..80] : title) + ") Tj ET\n");
            sb.Append("BT /F2 9 Tf 0.75 0 0 rg 40 775 Td (" + Esc(U.Mention) + ") Tj ET\n");
            sb.Append("BT /F1 9.5 Tf 0 0 0 rg 13 TL 40 752 Td\n");
            foreach (var l in pages[p]) sb.Append("(" + Esc(l) + ") Tj T*\n");
            sb.Append("ET\n");
            sb.Append("BT /F1 8 Tf 0.4 0.4 0.4 rg 40 30 Td (" + Esc($"{U.Mention} — Généré le {DateTime.Now:dd/MM/yyyy HH:mm} — Page {p + 1}/{nPages}") + ") Tj ET\n");
            var content = enc.GetBytes(sb.ToString());
            objs.Add(enc.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {6 + p * 2} 0 R >>"));
            var stream = new List<byte>();
            stream.AddRange(enc.GetBytes($"<< /Length {content.Length} >>\nstream\n"));
            stream.AddRange(content);
            stream.AddRange(enc.GetBytes("\nendstream"));
            objs.Add(stream.ToArray());
        }
        using var fs = new FileStream(path, FileMode.Create);
        var offsets = new List<long>();
        void W(string s) { var b = enc.GetBytes(s); fs.Write(b, 0, b.Length); }
        W("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        for (int i = 0; i < objs.Count; i++)
        {
            offsets.Add(fs.Position);
            W($"{i + 1} 0 obj\n"); fs.Write(objs[i], 0, objs[i].Length); W("\nendobj\n");
        }
        long xref = fs.Position;
        W($"xref\n0 {objs.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) W($"{o:0000000000} 00000 n \n");
        W($"trailer\n<< /Size {objs.Count + 1} /Root 1 0 R /Info << /Title ({Esc(title)}) /Producer (PARQUET PRO) /Subject ({Esc(U.Mention)}) >> >>\nstartxref\n{xref}\n%%EOF");
    }

    public static string SafeName(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
        return s.Length > 80 ? s[..80] : s;
    }

    public static void PdfOpen(string title, string body)
    {
        using var sfd = new SaveFileDialog { Filter = "Document PDF (*.pdf)|*.pdf", FileName = SafeName(title) + ".pdf", InitialDirectory = Db.ExportDir };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        Pdf(sfd.FileName, title, body);
        Biz.Admin("Export PDF", sfd.FileName);
        if (Ui.Confirm("Export PDF réalisé :\n" + sfd.FileName + "\n\nOuvrir le document ?"))
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
    }

    public static void Print(string title, string body)
    {
        var lines = Wrap(body, 100);
        int idx = 0, page = 0;
        var pd = new PrintDocument { DocumentName = title };
        pd.BeginPrint += (s, e) => { idx = 0; page = 0; };
        pd.PrintPage += (s, e) =>
        {
            page++;
            var g = e.Graphics; var m = e.MarginBounds;
            using (var wf = new Font("Segoe UI", 30, FontStyle.Bold))
            using (var wb = new SolidBrush(Color.FromArgb(40, 120, 120, 120)))
            {
                var st = g.Save();
                g.TranslateTransform(e.PageBounds.Width / 2f, e.PageBounds.Height / 2f);
                g.RotateTransform(-45);
                var sz = g.MeasureString(U.Mention, wf);
                g.DrawString(U.Mention, wf, wb, -sz.Width / 2, -sz.Height / 2);
                g.Restore(st);
            }
            using var fT = new Font("Segoe UI", 12, FontStyle.Bold);
            using var fM = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var fB = new Font("Consolas", 9f);
            float y = m.Top;
            g.DrawString("PARQUET PRO — " + title, fT, Brushes.Navy, m.Left, y); y += 24;
            g.DrawString(U.Mention, fM, Brushes.DarkRed, m.Left, y); y += 22;
            float lh = fB.GetHeight(g);
            while (idx < lines.Count && y + lh < m.Bottom - 20)
            {
                g.DrawString(lines[idx++], fB, Brushes.Black, m.Left, y); y += lh;
            }
            g.DrawString($"{U.Mention} — page {page}", fM, Brushes.Gray, m.Left, m.Bottom - 10);
            e.HasMorePages = idx < lines.Count;
        };
        using var dlg = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700, Text = "Aperçu avant impression — " + title };
        dlg.ShowDialog();
    }

    public static void Csv(System.Data.DataTable dt, string name)
    {
        using var sfd = new SaveFileDialog { Filter = "Fichier CSV (*.csv)|*.csv", FileName = SafeName(name) + ".csv", InitialDirectory = Db.ExportDir };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        var sb = new StringBuilder();
        sb.AppendLine("# " + U.Mention);
        sb.AppendLine(string.Join(";", dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName)));
        foreach (System.Data.DataRow r in dt.Rows) sb.AppendLine(string.Join(";", r.ItemArray.Select(v => "\"" + U.S(v).Replace("\"", "\"\"").Replace("\n", " ") + "\"")));
        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
        Ui.Info("Export réalisé : " + sfd.FileName);
    }
}
