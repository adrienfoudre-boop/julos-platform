using System.Text;

namespace ParquetPro;

/// Générateur PDF mis en page : couverture, sommaire paginé, en-têtes, pieds de page, filigrane de simulation.
public class PdfDoc
{
    record Ln(string Text, string Font, float Size, string Color, float Indent, float Before);
    readonly List<List<Ln>> pages = new();
    readonly List<(string title, int level, int page)> toc = new();
    List<Ln> cur;
    float y;
    const float Top = 770, Bottom = 70, Left = 50, Width = 495;
    public string Ref = "", Title = "";
    public bool Toc = true;
    readonly List<string[]> cover = new();
    bool hasCover;

    public void Cover(string surtitre, string titre, string sous, params string[] lignes) { hasCover = true; cover.Add(new[] { surtitre, titre, sous }); cover.Add(lignes); }

    void NewPage() { cur = new List<Ln>(); pages.Add(cur); y = Top; }

    public void Section(string title, bool newPage = true)
    {
        if (newPage || cur == null) NewPage();
        toc.Add((title, 1, pages.Count));
        Add(title.ToUpper(U.Fr), "F2", 15, "0 0.18 0.40", 0, 8);
        Add(new string('_', 70), "F1", 8, "0.6 0.65 0.75", 0, 0);
    }
    public void Sub(string title)
    {
        if (cur == null) NewPage();
        if (y < Bottom + 60) NewPage();
        toc.Add((title, 2, pages.Count));
        Add(title, "F2", 11.5f, "0 0.27 0.55", 0, 10);
    }
    public void Para(string text, float size = 9.8f, string font = "F1", float indent = 0, string color = "0.1 0.1 0.12")
    {
        if (cur == null) NewPage();
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var line = Clean(raw);
            int max = (int)((Width - indent) / (size * (font == "F3" ? 0.6f : 0.5f)));
            if (line.Length == 0) { Add("", font, size, color, indent, 0); continue; }
            while (line.Length > max)
            {
                int cut = line.LastIndexOf(' ', max); if (cut <= 4) cut = max;
                Add(line[..cut], font, size, color, indent, 0);
                line = "  " + line[cut..].TrimStart();
            }
            Add(line, font, size, color, indent, 0);
        }
    }
    public void KV(string k, string v) => Para($"{k} : {v}", 9.8f, "F1", 10);
    public void Space(float h = 8) { if (cur == null) NewPage(); y -= h; }

    void Add(string t, string f, float s, string col, float ind, float before)
    {
        float lh = s * 1.38f;
        if (y - lh - before < Bottom) NewPage(); else y -= before;
        y -= lh;
        cur.Add(new Ln(t, f, s, col, ind, y));
    }

    static string Clean(string s) => s.Replace("\t", "    ").Replace("✓", "-").Replace("✔", "-").Replace("✖", "x").Replace("•", "-").Replace("→", "->").Replace("└─", "+-").Replace("─", "-").Replace("≥", ">=").Replace("’", "'");
    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    public void Save(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(1252, new EncoderReplacementFallback("?"), new DecoderReplacementFallback("?"));
        // pages de sommaire
        int tocPerPage = 40;
        int tocPages = toc.Count == 0 || !Toc ? 0 : (toc.Count + tocPerPage - 1) / tocPerPage;
        int offset = (hasCover ? 1 : 0) + tocPages;
        int total = pages.Count + offset;
        var streams = new List<string>();
        if (hasCover)
        {
            var sb = new StringBuilder();
            sb.Append("0 0.16 0.33 rg 0 600 595 242 re f\n");
            sb.Append($"BT /F2 12 Tf 0.75 0.82 0.92 rg 50 790 Td ({Esc(Clean(cover[0][0]))}) Tj ET\n");
            float yy = 740;
            foreach (var part in Wrap(Clean(cover[0][1]), 38)) { sb.Append($"BT /F2 24 Tf 1 1 1 rg 50 {yy:0} Td ({Esc(part)}) Tj ET\n"); yy -= 30; }
            sb.Append($"BT /F1 12 Tf 0.85 0.9 0.97 rg 50 625 Td ({Esc(Clean(cover[0][2]))}) Tj ET\n");
            sb.Append("0.75 0 0 RG 2 w 50 540 495 34 re S\n");
            sb.Append($"BT /F2 13 Tf 0.75 0 0 rg 88 552 Td ({Esc(U.Mention)}) Tj ET\n");
            yy = 500;
            foreach (var l in cover[1]) { sb.Append($"BT /F1 11 Tf 0.1 0.1 0.15 rg 50 {yy:0} Td ({Esc(Clean(l))}) Tj ET\n"); yy -= 18; }
            sb.Append($"BT /F1 9 Tf 0.4 0.4 0.45 rg 50 90 Td ({Esc("Document généré par PARQUET PRO le " + DateTime.Now.ToString("dd/MM/yyyy à HH:mm") + " — environnement de simulation")}) Tj ET\n");
            sb.Append($"BT /F1 9 Tf 0.4 0.4 0.45 rg 50 76 Td ({Esc("Toutes les personnes, faits et décisions sont fictifs. Ce document n'est pas un acte juridique.")}) Tj ET\n");
            streams.Add(sb.ToString());
        }
        for (int tp = 0; tp < tocPages; tp++)
        {
            var sb = new StringBuilder();
            Frame(sb, streams.Count + 1, total);
            if (tp == 0) sb.Append("BT /F2 16 Tf 0 0.18 0.40 rg 50 735 Td (SOMMAIRE) Tj ET\n");
            float yy = 700;
            foreach (var e in toc.Skip(tp * tocPerPage).Take(tocPerPage))
            {
                string t = Clean(e.title); if (t.Length > 75) t = t[..75] + "...";
                float x = e.level == 1 ? 50 : 70;
                string f = e.level == 1 ? "/F2 10.5" : "/F1 9.5";
                sb.Append($"BT {f} Tf 0.1 0.1 0.15 rg {x} {yy:0} Td ({Esc(t)}) Tj ET\n");
                sb.Append($"BT /F1 9.5 Tf 0.1 0.1 0.15 rg 520 {yy:0} Td ({e.page + offset}) Tj ET\n");
                sb.Append($"0.8 0.82 0.86 RG 0.5 w {x + Math.Min(330, t.Length * 5.3f) + 10:0} {yy + 2:0} m 512 {yy + 2:0} l S\n");
                yy -= e.level == 1 ? 17 : 14;
            }
            streams.Add(sb.ToString());
        }
        foreach (var p in pages)
        {
            var sb = new StringBuilder();
            Frame(sb, streams.Count + 1, total);
            foreach (var l in p)
                if (l.Text.Length > 0) sb.Append($"BT /{l.Font} {l.Size.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} Tf {l.Color} rg {Left + l.Indent:0} {l.Before.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} Td ({Esc(l.Text)}) Tj ET\n");
            streams.Add(sb.ToString());
        }
        // assemblage
        var objs = new List<byte[]>();
        int n = streams.Count;
        objs.Add(enc.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objs.Add(enc.GetBytes($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, n).Select(i => $"{6 + i * 2} 0 R"))}] /Count {n} >>"));
        objs.Add(enc.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objs.Add(enc.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
        objs.Add(enc.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"));
        for (int i = 0; i < n; i++)
        {
            var content = enc.GetBytes(streams[i]);
            objs.Add(enc.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >> /Contents {7 + i * 2} 0 R >>"));
            var st = new List<byte>(); st.AddRange(enc.GetBytes($"<< /Length {content.Length} >>\nstream\n")); st.AddRange(content); st.AddRange(enc.GetBytes("\nendstream"));
            objs.Add(st.ToArray());
        }
        using var fs = new FileStream(path, FileMode.Create);
        var offs = new List<long>();
        void W(string s) { var b = enc.GetBytes(s); fs.Write(b, 0, b.Length); }
        W("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        for (int i = 0; i < objs.Count; i++) { offs.Add(fs.Position); W($"{i + 1} 0 obj\n"); fs.Write(objs[i], 0, objs[i].Length); W("\nendobj\n"); }
        long xref = fs.Position;
        W($"xref\n0 {objs.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offs) W($"{o:0000000000} 00000 n \n");
        W($"trailer\n<< /Size {objs.Count + 1} /Root 1 0 R /Info << /Title ({Esc(Clean(Title))}) /Producer (PARQUET PRO) /Subject ({Esc(U.Mention)}) >> >>\nstartxref\n{xref}\n%%EOF");
    }

    void Frame(StringBuilder sb, int page, int total)
    {
        sb.Append($"q 0.9 g BT /F2 30 Tf 0.7071 0.7071 -0.7071 0.7071 120 170 Tm ({Esc(U.Mention)}) Tj ET Q\n");
        sb.Append("0 0.16 0.33 rg 0 812 595 30 re f\n");
        sb.Append($"BT /F2 10 Tf 1 1 1 rg 50 823 Td (PARQUET PRO) Tj ET\n");
        sb.Append($"BT /F1 9 Tf 0.85 0.9 0.97 rg 140 823 Td ({Esc(Clean((Ref + " — " + Title).Length > 85 ? (Ref + " — " + Title)[..85] : Ref + " — " + Title))}) Tj ET\n");
        sb.Append($"BT /F2 8 Tf 0.75 0 0 rg 50 798 Td ({Esc(U.Mention)}) Tj ET\n");
        sb.Append("0.75 0.78 0.84 RG 0.6 w 50 50 m 545 50 l S\n");
        sb.Append($"BT /F1 8 Tf 0.4 0.4 0.45 rg 50 38 Td ({Esc(U.Mention + " — " + Ref)}) Tj ET\n");
        sb.Append($"BT /F1 8 Tf 0.4 0.4 0.45 rg 500 38 Td ({page} / {total}) Tj ET\n");
    }

    static IEnumerable<string> Wrap(string s, int max)
    {
        while (s.Length > max) { int c = s.LastIndexOf(' ', max); if (c <= 0) c = max; yield return s[..c]; s = s[c..].TrimStart(); }
        yield return s;
    }
}

/// Construction du dossier d'enquête imprimable (complet ou sélection de pièces).
public static class DossierPdf
{
    public static readonly string[] Sections = { "Page de présentation", "Sommaire", "Synthèse de l'affaire", "Chronologie", "Fiches des personnes concernées", "Procès-verbaux", "Comptes rendus d'audition", "Rapports des services", "Inventaire des pièces et scellés", "Documents complémentaires", "Historique des décisions" };

    public static void Generer(long dos, string path, HashSet<string> sections, HashSet<long> pieces)
    {
        using var c = new DbCtx();
        var d = c.Rows(@"SELECT numero, intitule, categorie, qualification, situation, priorite, (SELECT nom FROM organismes WHERE id=service_enqueteur_id), date_transmission, faits, lieu_faits, date_faits,
            (SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=referent_id), (SELECT nom FROM services WHERE id=service_id), gravite FROM dossiers WHERE id=@p0", dos)[0];
        var pdf = new PdfDoc { Ref = U.S(d[0]), Title = U.S(d[1]) };
        bool S(string s) => sections.Contains(s);
        if (S("Page de présentation"))
            pdf.Cover("PARQUET DE VALMONT (SIMULÉ) — DOSSIER DE PROCÉDURE", U.S(d[1]), $"Référence {d[0]} — {d[2]}",
                $"Qualification : {d[3]}", $"Situation : {d[4]}", $"Priorité : {d[5]}", $"Service enquêteur : {d[6]}", $"Date de transmission : {U.Aff(d[7])}", $"Référent : {d[11]}", $"Service : {d[12]}",
                $"Pièces : {c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0", dos)}   Personnes : {c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0", dos)}");
        if (S("Synthèse de l'affaire"))
        {
            pdf.Section("Synthèse de l'affaire");
            pdf.KV("Référence", U.S(d[0])); pdf.KV("Intitulé", U.S(d[1])); pdf.KV("Catégorie", U.S(d[2])); pdf.KV("Qualification", U.S(d[3]));
            pdf.KV("Situation", U.S(d[4])); pdf.KV("Gravité (échelle de simulation)", U.S(d[13]) + " / 3"); pdf.KV("Lieu des faits", U.S(d[9])); pdf.KV("Date des faits", U.Aff(d[10]));
            pdf.Sub("Exposé des faits"); pdf.Para(U.S(d[8]));
            pdf.Sub("Personnes concernées");
            foreach (var p in c.Rows("SELECT dp.role, p.prenom||' '||p.nom, p.ref, IFNULL(dp.declaration,'') FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE dp.dossier_id=@p0 ORDER BY dp.role", dos))
                pdf.Para($"- {p[0]} : {p[1]} ({p[2]}){(U.S(p[3]) != "" ? " — " + p[3] : "")}", 9.8f, "F1", 10);
        }
        if (S("Chronologie"))
        {
            pdf.Section("Chronologie");
            foreach (var x in c.Rows("SELECT date, libelle, source FROM dossier_chrono WHERE dossier_id=@p0 UNION ALL SELECT date, 'Décision '||ref||' : '||libelle||' — '||orientation, 'Parquet' FROM decisions WHERE dossier_id=@p0 ORDER BY 1", dos))
                pdf.Para($"{U.Aff(x[0])}   {x[1]}   [{x[2]}]", 9.3f);
        }
        if (S("Fiches des personnes concernées"))
        {
            pdf.Section("Fiches des personnes concernées");
            foreach (var p in c.Rows("SELECT dp.personne_id, dp.role, IFNULL(dp.declaration,'') FROM dossier_personnes dp WHERE dp.dossier_id=@p0", dos))
            {
                long pid = U.L(p[0]);
                var x = c.Rows("SELECT ref, nom, prenom, date_naissance, lieu_naissance, adresse, ville, profession, statut FROM personnes WHERE id=@p0", pid)[0];
                pdf.Sub($"{x[1]} {x[2]} — {p[1]}");
                pdf.KV("Référence", U.S(x[0])); pdf.KV("Né(e) le", $"{U.P(x[3]):dd/MM/yyyy} à {x[4]}"); pdf.KV("Adresse (fictive)", $"{x[5]}, {x[6]}"); pdf.KV("Profession", U.S(x[7])); pdf.KV("Statut (simulation)", U.S(x[8]));
                if (U.S(p[2]) != "") pdf.KV("Position", U.S(p[2]));
                pdf.KV("Mentions au casier simulé", c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", pid).ToString());
                pdf.KV("Autres dossiers", string.Join(", ", c.Rows("SELECT d.numero FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id WHERE dp.personne_id=@p0 AND dp.dossier_id<>@p1", pid, dos).Select(r => U.S(r[0]))) is var o && o != "" ? o : "aucun");
            }
        }
        void Pieces(string section, string where)
        {
            var rows = c.Rows($"SELECT id, cote, titre, date, auteur, contenu, type FROM pieces WHERE dossier_id=@p0 AND {where} ORDER BY id", dos).Where(r => pieces == null || pieces.Contains(U.L(r[0]))).ToList();
            if (rows.Count == 0) return;
            pdf.Section(section);
            bool first = true;
            foreach (var r in rows)
            {
                if (!first) pdf.Space(14);
                first = false;
                pdf.Sub($"{r[1]} — {r[2]}");
                pdf.Para($"Nature : {r[6]}    Date : {U.Aff(r[3])}    Rédacteur : {r[4]}", 8.8f, "F1", 0, "0.35 0.35 0.4");
                pdf.Space(4);
                pdf.Para(U.S(r[5]), 9.5f);
            }
        }
        if (S("Procès-verbaux")) Pieces("Procès-verbaux", "type='Procès-verbal'");
        if (S("Comptes rendus d'audition")) Pieces("Comptes rendus d'audition et d'interrogatoire", "type IN ('Audition','Interrogatoire')");
        if (S("Rapports des services")) Pieces("Rapports des services", "type='Rapport'");
        if (S("Inventaire des pièces et scellés"))
        {
            pdf.Section("Inventaire des pièces et scellés");
            pdf.Sub("Pièces de la procédure");
            foreach (var r in c.Rows("SELECT cote, type, titre, date FROM pieces WHERE dossier_id=@p0 ORDER BY id", dos)) pdf.Para($"{r[0],-5} {U.Aff(r[3])}  {r[1]} — {r[2]}", 9f);
            pdf.Sub("Éléments matériels placés sous scellés");
            var sc = c.Rows("SELECT numero, description, statut, date FROM scelles WHERE dossier_id=@p0", dos);
            if (sc.Count == 0) pdf.Para("Aucun scellé.");
            foreach (var s in sc) pdf.Para($"{s[0]} — {s[1]} ({s[2]}, {U.Aff(s[3])})", 9.3f);
            pdf.Sub("Véhicules");
            var vs = c.Rows("SELECT v.immatriculation, v.marque||' '||v.modele||' '||v.couleur, dv.role FROM dossier_vehicules dv JOIN vehicules v ON v.id=dv.vehicule_id WHERE dv.dossier_id=@p0", dos);
            if (vs.Count == 0) pdf.Para("Aucun véhicule.");
            foreach (var v in vs) pdf.Para($"{v[0]} — {v[1]} ({v[2]})", 9.3f);
        }
        if (S("Documents complémentaires")) Pieces("Documents complémentaires", "type NOT IN ('Procès-verbal','Audition','Interrogatoire','Rapport')");
        if (S("Historique des décisions"))
        {
            pdf.Section("Historique des décisions");
            var ds = c.Rows("SELECT ref, date, libelle, orientation, statut, etat_execution, IFNULL(resultat,''), motivation, source FROM decisions WHERE dossier_id=@p0 ORDER BY date", dos);
            if (ds.Count == 0) pdf.Para("Aucune décision enregistrée.");
            foreach (var x in ds)
            {
                pdf.Sub($"{x[0]} — {x[2]}");
                pdf.KV("Date", U.Aff(x[1])); pdf.KV("Orientation", U.S(x[3])); pdf.KV("Statut", $"{x[4]} / {x[5]}"); pdf.KV("Origine", U.S(x[8]));
                pdf.KV("Motivation", U.S(x[7])); if (U.S(x[6]) != "") pdf.KV("Résultat", U.S(x[6]));
            }
        }
        pdf.Toc = S("Sommaire");
        pdf.Save(path);
    }
}
