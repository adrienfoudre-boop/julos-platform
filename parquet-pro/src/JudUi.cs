using System.Data;
using System.Text;

namespace ParquetPro;

public static class Rtf
{
    public static RichTextBox Reader(float size = 10.5f) => new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new Font("Segoe UI", size), DetectUrls = false };
    public static void H(RichTextBox r, string t, float size = 13f, Color? c = null) { r.SelectionStart = r.TextLength; r.SelectionFont = new Font("Segoe UI Semibold", size); r.SelectionColor = c ?? Theme.Navy; r.AppendText(t + "\n"); }
    public static void P(RichTextBox r, string t, bool bold = false, Color? c = null) { r.SelectionStart = r.TextLength; r.SelectionFont = new Font("Segoe UI", r.Font.Size, bold ? FontStyle.Bold : FontStyle.Regular); r.SelectionColor = c ?? Color.FromArgb(30, 30, 35); r.AppendText(t + "\n"); }
    public static void KV(RichTextBox r, string k, string v) { r.SelectionStart = r.TextLength; r.SelectionFont = new Font("Segoe UI", r.Font.Size, FontStyle.Bold); r.SelectionColor = Theme.Anthracite; r.AppendText(k + " : "); r.SelectionFont = new Font("Segoe UI", r.Font.Size); r.SelectionColor = Color.Black; r.AppendText(v + "\n"); }
    public static void Sep(RichTextBox r) => P(r, "", false);
}

// ============================================================ lecteur de pièce
public class PieceViewer : Form
{
    public static void Open(IWin32Window owner, long id, bool modal = false)
    {
        var f = new PieceViewer(id);
        if (modal) { f.ShowDialog(owner); f.Dispose(); } else f.Show(owner);
    }

    public PieceViewer(long id)
    {
        var x = Db.Rows("SELECT p.cote, p.titre, p.type, p.contenu, p.personne_id, p.dossier_id, d.numero FROM pieces p JOIN dossiers d ON d.id=p.dossier_id WHERE p.id=@p0", id)[0];
        Db.Exec("UPDATE pieces SET lue=1 WHERE id=@p0", id);
        KeyPreview = true; KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        Text = $"{x[6]} — {x[0]} — {x[1]}"; Width = 860; Height = 780; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false; BackColor = Theme.Gray;
        string full = Db.Q(c => Jud.EnTete(c, id)) + U.S(x[3]) + "\n\n" + U.Mention;
        var page = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 16, 40, 16), BackColor = Theme.Gray };
        var sheet = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(36, 28, 36, 28) };
        var rt = Rtf.Reader(10.5f);
        var lines = full.Replace("\r", "").Split('\n');
        foreach (var l in lines)
        {
            if (l == U.Mention) Rtf.P(rt, l, true, Theme.Red);
            else if (l.Length > 0 && l == l.ToUpper(U.Fr) && l.Any(char.IsLetter) && l.Length < 90) Rtf.H(rt, l, 12.5f);
            else if (l.StartsWith("Q : ")) Rtf.P(rt, l, true, Theme.Blue);
            else Rtf.P(rt, l);
        }
        rt.SelectionStart = 0;
        sheet.Controls.Add(rt); page.Controls.Add(sheet);
        var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.White, Padding = new Padding(6, 3, 6, 3) };
        bar.Items.Add(Icons.Tool("print", "Imprimer la pièce", () => Export.Print($"{x[6]} {x[0]} {x[1]}", full)));
        bar.Items.Add(Icons.Tool("document", "Exporter en PDF", () => Export.PdfOpen($"{x[6]} {x[0]} {x[1]}", full)));
        if (U.L(x[4]) != 0) bar.Items.Add(Icons.Tool("personne", "Fiche de la personne", () => PersonneWindow.Open(this, U.L(x[4]))));
        bar.Items.Add(Icons.Tool("dossier", "Ouvrir le dossier", () => DossierWindow.Open(U.L(x[5]))));
        Controls.Add(page); Controls.Add(bar);
    }
}

// ============================================================ volet de pièces (grille + aperçu)
public class PiecesPane : SplitContainer
{
    public readonly GridPanel Grid;
    readonly RichTextBox prev = Rtf.Reader(9.5f);
    public PiecesPane(string key, Func<string, int, int, DataTable> loader)
    {
        Dock = DockStyle.Fill; Orientation = Orientation.Vertical;
        Grid = new GridPanel(key) { Loader = loader };
        Grid.SelChanged += () =>
        {
            long id = Grid.SelectedId; prev.Clear();
            if (id == 0) return;
            var t = Db.Str("SELECT contenu FROM pieces WHERE id=@p0", id);
            if (t == null) { t = Db.Str("SELECT contenu FROM documents WHERE id=@p0", -id); }
            Rtf.P(prev, t ?? "");
        };
        Grid.Activated += id => { if (id > 0) PieceViewer.Open(FindForm(), id); else { using var d = new DocumentDialog(-id); d.ShowDialog(); } Grid.Reload(); };
        Grid.Bar.Items.Insert(0, Icons.Tool("view", "Consulter la pièce", () => { long id = Grid.SelectedId; if (id > 0) PieceViewer.Open(FindForm(), id); else if (id < 0) { using var d = new DocumentDialog(-id); d.ShowDialog(); } }));
        Panel1.Controls.Add(Grid);
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        p.Controls.Add(prev);
        Panel2.Controls.Add(p);
        HandleCreated += (s, e) => { try { SplitterDistance = Width * 55 / 100; } catch { } };
    }
}

// ============================================================ fenêtre de consultation d'un dossier
public class DossierWindow : Form
{
    static readonly Dictionary<long, DossierWindow> open = new();
    public static void Open(long id)
    {
        if (id == 0) return;
        if (open.TryGetValue(id, out var w) && !w.IsDisposed) { w.Reload(); w.Activate(); if (w.WindowState == FormWindowState.Minimized) w.WindowState = FormWindowState.Normal; return; }
        w = new DossierWindow(id) { Icon = Program.AppIcon };
        open[id] = w;
        w.FormClosed += (s, e) => open.Remove(id);
        w.Show(MainForm.Instance);
    }
    public static void RefreshIfOpen(long id) { if (open.TryGetValue(id, out var w) && !w.IsDisposed) w.Reload(); }

    readonly long id;
    readonly Panel head = new() { Dock = DockStyle.Top, Height = 96, BackColor = Theme.Navy };
    readonly ListBox sections = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 34, IntegralHeight = false, BackColor = Color.White };
    readonly Panel content = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    static readonly string[] Noms = { "Synthèse de l'affaire", "Présentation des faits", "Chronologie", "Personnes concernées", "Auditions et interrogatoires", "Procès-verbaux", "Rapports d'enquête", "Pièces et documents", "Éléments matériels", "Observations des services", "Communications", "Décisions", "Historique complet" };
    static readonly string[] Ico = { "cabinet", "document", "history", "personne", "phone", "document", "enquete", "attach", "car", "edit", "mail", "decision", "journal" };
    readonly long[] counts = new long[13];

    public DossierWindow(long id)
    {
        this.id = id;
        Width = 1280; Height = 840; StartPosition = FormStartPosition.CenterScreen; Font = Theme.F; BackColor = Theme.Back; MinimumSize = new Size(1000, 640);
        foreach (var n in Noms) sections.Items.Add(n);
        sections.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(new SolidBrush(sel ? Theme.Light : Color.White), e.Bounds);
            if (sel) e.Graphics.FillRectangle(new SolidBrush(Theme.Accent), e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height);
            e.Graphics.DrawImage(Icons.Get(Ico[e.Index], 18, sel ? Theme.Accent : Theme.Anthracite), e.Bounds.X + 12, e.Bounds.Y + 8);
            e.Graphics.DrawString($"{e.Index + 1}. {Noms[e.Index]}", sel ? Theme.FB : Theme.F, new SolidBrush(Theme.Navy), e.Bounds.X + 38, e.Bounds.Y + 9);
            if (counts[e.Index] > 0)
            {
                var t = counts[e.Index].ToString(); var sz = e.Graphics.MeasureString(t, Theme.F);
                var rc = new RectangleF(e.Bounds.Right - sz.Width - 18, e.Bounds.Y + 8, sz.Width + 10, 18);
                e.Graphics.FillRectangle(new SolidBrush(Theme.Gray), rc);
                e.Graphics.DrawString(t, Theme.F, new SolidBrush(Theme.Anthracite), rc.X + 5, rc.Y + 1);
            }
        };
        sections.SelectedIndexChanged += (s, e) => ShowSection(sections.SelectedIndex);
        var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.White, Padding = new Padding(8, 4, 8, 4), Font = Theme.F };
        bar.Items.Add(Icons.Tool("decision", "Préparer une décision", () => Decider(null)));
        bar.Items.Add(Icons.Tool("enquete", "Demander un complément", () => Decider("COMPLEMENT")));
        bar.Items.Add(Icons.Tool("edit", "Ajouter une observation", AddObservation));
        bar.Items.Add(Icons.Tool("send", "Transmettre au service", () => { using var d = new TransmettreDialog(id); if (d.ShowDialog(this) == DialogResult.OK) Reload(); }));
        bar.Items.Add(Icons.Tool("personne", "Ajouter une personne", () => { using var d = new AddPersonneDialog(id); if (d.ShowDialog(this) == DialogResult.OK) Reload(); }));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(Icons.Tool("history", "Consulter l'historique", () => sections.SelectedIndex = 12));
        bar.Items.Add(Icons.Tool("print", "Imprimer le dossier", () => Imprimer(false)));
        bar.Items.Add(Icons.Tool("check", "Sélectionner des pièces…", () => Imprimer(true)));
        bar.Items.Add(Icons.Tool("admin", "Modifier la fiche", () => { using var d = new DossierDialog(id); if (d.ShowDialog(this) == DialogResult.OK) Reload(); }));
        bar.Items.Add(Icons.Tool("refresh", "Actualiser", Reload));
        var left = new Panel { Dock = DockStyle.Left, Width = 270, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0) };
        left.Controls.Add(sections);
        var line = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Gray };
        Controls.Add(content); Controls.Add(line); Controls.Add(left); Controls.Add(bar); Controls.Add(head);
        Reload();
        sections.SelectedIndex = 0;
    }

    public void Reload()
    {
        var d = Db.Rows(@"SELECT numero, intitule, situation, priorite, categorie, gravite, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=referent_id), (SELECT nom FROM organismes WHERE id=service_enqueteur_id), date_transmission, statut, qualification FROM dossiers WHERE id=@p0", id)[0];
        Text = $"Dossier {d[0]} — {d[1]}";
        head.Controls.Clear();
        head.Controls.Add(new Label { Text = $"{d[0]}  ·  {d[1]}", Font = new Font("Segoe UI Semibold", 15f), ForeColor = Color.White, Location = new Point(18, 10), AutoSize = true });
        head.Controls.Add(new Label { Text = U.S(d[10]), ForeColor = Color.FromArgb(185, 205, 230), Location = new Point(20, 42), AutoSize = true });
        int x = 20;
        void Badge(string t, Color c) { var l = new Label { Text = t, AutoSize = true, BackColor = c, ForeColor = Color.White, Padding = new Padding(6, 3, 6, 3), Location = new Point(x, 64), Font = Theme.FB }; head.Controls.Add(l); x += TextRenderer.MeasureText(t, Theme.FB).Width + 22; }
        string sit = U.S(d[2]);
        Badge(sit, sit.StartsWith("En attente") ? Theme.Orange : U.S(d[9]) == "Clôturé" ? Color.FromArgb(90, 100, 115) : Theme.Accent);
        Badge("Priorité " + U.S(d[3]).ToLower(), U.S(d[3]) == "Haute" ? Theme.Red : Theme.Blue);
        Badge(U.S(d[4]), Theme.Blue);
        Badge("Gravité " + d[5] + "/3", Theme.Blue);
        Badge("Référent : " + d[6], Theme.Blue);
        Badge("Service enquêteur : " + d[7], Theme.Blue);
        var c = Db.Rows(@"SELECT 0, 0, (SELECT COUNT(*) FROM dossier_chrono WHERE dossier_id=@p0), (SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0),
            (SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND type IN ('Audition','Interrogatoire')), (SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND type='Procès-verbal'),
            (SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND type='Rapport'), (SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0), (SELECT COUNT(*) FROM scelles WHERE dossier_id=@p0)+(SELECT COUNT(*) FROM dossier_vehicules WHERE dossier_id=@p0),
            (SELECT COUNT(*) FROM dossier_notes WHERE dossier_id=@p0), (SELECT COUNT(*) FROM messages WHERE dossier_id=@p0)+(SELECT COUNT(*) FROM appels WHERE dossier_id=@p0), (SELECT COUNT(*) FROM decisions WHERE dossier_id=@p0), 0", id)[0];
        for (int i = 0; i < 13; i++) counts[i] = U.L(c[i]);
        sections.Invalidate();
        if (sections.SelectedIndex >= 0) ShowSection(sections.SelectedIndex);
    }

    void Decider(string code)
    {
        using var w = new DecisionWizard(id, code, 0, 0);
        if (w.ShowDialog(this) == DialogResult.OK) { Reload(); MainForm.Instance?.RefreshAll(true); }
    }

    void AddObservation()
    {
        var t = Ui.Prompt("Ajouter une observation", "Votre observation sur le dossier :", "", true);
        if (string.IsNullOrWhiteSpace(t)) return;
        Db.Tx(c => Biz.NoteDossier(c, id, t));
        Reload(); sections.SelectedIndex = 9;
    }

    void Imprimer(bool selection)
    {
        HashSet<string> secs = DossierPdf.Sections.ToHashSet(); HashSet<long> pcs = null;
        if (selection)
        {
            using var dlg = new PrintSelectDialog(id);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            secs = dlg.Sections; pcs = dlg.Pieces;
        }
        string num = Db.Str("SELECT numero FROM dossiers WHERE id=@p0", id);
        using var sfd = new SaveFileDialog { Filter = "Document PDF (*.pdf)|*.pdf", FileName = $"Dossier {num}{(selection ? " - sélection" : " - complet")}.pdf", InitialDirectory = Db.ExportDir };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;
        Cursor = Cursors.WaitCursor;
        try { DossierPdf.Generer(id, sfd.FileName, secs, pcs); } finally { Cursor = Cursors.Default; }
        Db.Tx(c => Biz.DHist(c, id, Auth.CollabId, "Édition du dossier en PDF" + (selection ? " (sélection de pièces)" : " (dossier complet)")));
        if (Ui.Confirm("Dossier généré :\n" + sfd.FileName + "\n\nOuvrir le document ?"))
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
    }

    Control Pieces(string key, string where) => new PiecesPane(key, (q, o, l) => Db.Table($@"SELECT id, cote AS 'Cote', titre AS 'Titre', type AS 'Nature', substr(date,1,16) AS 'Date', auteur AS 'Rédacteur', CASE lue WHEN 1 THEN 'Lue' ELSE 'Non lue' END AS 'Lecture'
        FROM pieces WHERE dossier_id=@p0 AND {where} AND (titre LIKE @p1 OR contenu LIKE @p1) ORDER BY id LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o));

    void ShowSection(int i)
    {
        content.SuspendLayout();
        foreach (Control ctl in content.Controls) ctl.Dispose();
        content.Controls.Clear();
        Control ctrl;
        var title = new Label { Text = "  " + Noms[Math.Max(0, i)], Dock = DockStyle.Top, Height = 38, Font = Theme.FT, ForeColor = Theme.Navy, BackColor = Color.White, TextAlign = ContentAlignment.MiddleLeft };
        switch (i)
        {
            case 0: ctrl = Synthese(); break;
            case 1:
            {
                var rt = Rtf.Reader(11f);
                var f = Db.Rows("SELECT faits, lieu_faits, date_faits, qualification, (SELECT nom FROM organismes WHERE id=service_enqueteur_id), date_transmission FROM dossiers WHERE id=@p0", id)[0];
                Rtf.KV(rt, "Qualification retenue (simulée)", U.S(f[3])); Rtf.KV(rt, "Lieu", U.S(f[1])); Rtf.KV(rt, "Date des faits", U.Aff(f[2])); Rtf.KV(rt, "Transmis par", $"{f[4]} le {U.Aff(f[5])}");
                Rtf.Sep(rt); Rtf.H(rt, "Exposé des faits"); Rtf.P(rt, U.S(f[0]));
                ctrl = Pad(rt); break;
            }
            case 2:
                ctrl = new GridPanel("dw_chrono") { Loader = (q, o, l) => Db.Table(@"SELECT * FROM (SELECT id, substr(date,1,16) AS 'Date', libelle AS 'Événement', source AS 'Source' FROM dossier_chrono WHERE dossier_id=@p0
                    UNION ALL SELECT -id, substr(date,1,16), 'Décision '||ref||' : '||libelle||' — '||orientation, 'Parquet' FROM decisions WHERE dossier_id=@p0) WHERE Événement LIKE @p1 ORDER BY Date LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                break;
            case 3:
            {
                var g = new GridPanel("dw_pers") { Loader = (q, o, l) => Db.Table(@"SELECT p.id, dp.role AS 'Rôle', p.nom AS 'Nom', p.prenom AS 'Prénom', substr(p.date_naissance,1,10) AS 'Naissance', p.ville AS 'Ville', IFNULL(dp.declaration,'') AS 'Position',
                    p.statut AS 'Statut', (SELECT COUNT(*) FROM casier WHERE personne_id=p.id) AS 'Casier', (SELECT COUNT(*) FROM dossier_personnes x WHERE x.personne_id=p.id)-1 AS 'Autres dossiers', p.ref AS 'Référence'
                    FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE dp.dossier_id=@p0 AND (p.nom LIKE @p1 OR p.prenom LIKE @p1) ORDER BY dp.role LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                g.Activated += pid => PersonneWindow.Open(this, pid);
                g.Bar.Items.Insert(0, Icons.Tool("view", "Ouvrir la fiche", () => { if (g.SelectedId != 0) PersonneWindow.Open(this, g.SelectedId); }));
                g.Bar.Items.Insert(1, Icons.Tool("casier", "Consulter le casier", () => { if (g.SelectedId != 0) PersonneWindow.Open(this, g.SelectedId, "Casier judiciaire"); }));
                g.Bar.Items.Insert(2, Icons.Tool("decision", "Décision pour cette personne", () => { if (g.SelectedId == 0) return; using var w = new DecisionWizard(id, "PERSONNE", g.SelectedId, 0); if (w.ShowDialog(this) == DialogResult.OK) Reload(); }));
                g.Bar.Items.Insert(3, Icons.Tool("add", "Ajouter une personne", () => { using var d = new AddPersonneDialog(id); if (d.ShowDialog(this) == DialogResult.OK) Reload(); }));
                ctrl = g; break;
            }
            case 4: ctrl = Pieces("dw_aud", "type IN ('Audition','Interrogatoire')"); break;
            case 5: ctrl = Pieces("dw_pv", "type='Procès-verbal'"); break;
            case 6: ctrl = Pieces("dw_rap", "type='Rapport'"); break;
            case 7:
                ctrl = new PiecesPane("dw_all", (q, o, l) => Db.Table(@"SELECT * FROM (SELECT id, cote AS 'Cote', titre AS 'Titre', type AS 'Nature', substr(date,1,16) AS 'Date', auteur AS 'Rédacteur' FROM pieces WHERE dossier_id=@p0
                    UNION ALL SELECT -id, numero, titre, 'Document : '||categorie, substr(date,1,16), (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id) FROM documents WHERE dossier_id=@p0) WHERE Titre LIKE @p1 ORDER BY Date LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o));
                break;
            case 8:
            {
                var sp = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
                var gs = new GridPanel("dw_sc") { Loader = (q, o, l) => Db.Table("SELECT id, numero AS 'Scellé', description AS 'Description', lieu AS 'Lieu de découverte', substr(date,1,16) AS 'Date', statut AS 'Statut', (SELECT cote FROM pieces WHERE id=piece_id) AS 'PV' FROM scelles WHERE dossier_id=@p0 AND description LIKE @p1 LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                gs.Bar.Items.Insert(0, new ToolStripLabel("Scellés") { Font = Theme.FB });
                var gv = new GridPanel("dw_veh") { Loader = (q, o, l) => Db.Table("SELECT v.id, v.immatriculation AS 'Immatriculation', v.marque||' '||v.modele AS 'Véhicule', v.couleur AS 'Couleur', dv.role AS 'Rôle', (SELECT prenom||' '||nom FROM personnes WHERE id=v.proprietaire_id) AS 'Propriétaire', (SELECT COUNT(*) FROM dossier_vehicules x WHERE x.vehicule_id=v.id) AS 'Dossiers' FROM dossier_vehicules dv JOIN vehicules v ON v.id=dv.vehicule_id WHERE dv.dossier_id=@p0 AND v.immatriculation LIKE @p1 LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                gv.Bar.Items.Insert(0, new ToolStripLabel("Véhicules") { Font = Theme.FB });
                gv.Activated += vid => MainForm.Instance?.OpenRef("vehicule", vid);
                sp.Panel1.Controls.Add(gs); sp.Panel2.Controls.Add(gv); gs.Reload(); gv.Reload();
                ctrl = sp; break;
            }
            case 9:
            {
                var g = new GridPanel("dw_obs") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', IFNULL(type,'Observation') AS 'Type', IFNULL(auteur_ext, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id)) AS 'Auteur', texte AS 'Observation' FROM dossier_notes WHERE dossier_id=@p0 AND texte LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                g.Bar.Items.Insert(0, Icons.Tool("add", "Ajouter une observation", AddObservation));
                g.Bar.Items.Insert(1, Icons.Tool("decision", "Solliciter des observations", () => Decider("OBSERVATIONS")));
                g.Activated += nid => Ui.ShowText("Observation", Db.Str("SELECT texte FROM dossier_notes WHERE id=@p0", nid) ?? "", this);
                ctrl = g; break;
            }
            case 10:
            {
                var g = new GridPanel("dw_com") { Loader = (q, o, l) => Db.Table(@"SELECT * FROM (SELECT id, substr(date,1,16) AS 'Date', 'Message' AS 'Canal', IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id), exp_ext) AS 'De', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=dest_id) AS 'À', objet AS 'Objet' FROM messages WHERE dossier_id=@p0 AND brouillon=0
                    UNION ALL SELECT -id, substr(date,1,16), 'Appel '||lower(sens), interlocuteur, '', motif||' — '||statut FROM appels WHERE dossier_id=@p0) WHERE Objet LIKE @p1 ORDER BY Date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                g.Activated += mid => { if (mid > 0) MessageViewer.Open(this, mid); };
                g.Bar.Items.Insert(0, Icons.Tool("mail", "Écrire au référent", () => { using var d = new MessageDialog(Db.Long("SELECT IFNULL(referent_id,0) FROM dossiers WHERE id=@p0", id), $"Dossier {Db.Str("SELECT numero FROM dossiers WHERE id=@p0", id)}", "", 0, id); d.ShowDialog(this); Reload(); }));
                ctrl = g; break;
            }
            case 11:
            {
                var g = new GridPanel("dw_dec") { Loader = (q, o, l) => Db.Table(@"SELECT id, ref AS 'Référence', substr(date,1,16) AS 'Date', libelle AS 'Décision', orientation AS 'Orientation', (SELECT prenom||' '||nom FROM personnes WHERE id=personne_id) AS 'Personne',
                    statut AS 'Statut', etat_execution AS 'Effet', IFNULL(resultat,'') AS 'Résultat', source AS 'Origine' FROM decisions WHERE dossier_id=@p0 AND (libelle LIKE @p1 OR orientation LIKE @p1) ORDER BY date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                g.Activated += did => { using var v = new DecisionView(did); v.ShowDialog(this); Reload(); };
                g.Bar.Items.Insert(0, Icons.Tool("decision", "Préparer une décision", () => Decider(null)));
                ctrl = g; break;
            }
            default:
                ctrl = new GridPanel("dw_hist") { PageSize = 500, Loader = (q, o, l) => Db.Table(@"SELECT * FROM (SELECT id, substr(date,1,16) AS 'Date', IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id),'Moteur / service') AS 'Auteur', texte AS 'Détail', 'Dossier' AS 'Source' FROM dossier_hist WHERE dossier_id=@p0
                    UNION ALL SELECT -id, substr(date,1,16), origine, description, type FROM evenements WHERE ref_type='dossier' AND ref_id=@p0) WHERE Détail LIKE @p1 ORDER BY Date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
                break;
        }
        content.Controls.Add(ctrl); content.Controls.Add(title);
        content.ResumeLayout();
        if (ctrl is GridPanel gp) gp.Reload();
        if (ctrl is PiecesPane pp) pp.Grid.Reload();
    }

    public void TestAll() { for (int i = 0; i < 13; i++) { sections.SelectedIndex = i; Application.DoEvents(); } }

    static Panel Pad(Control c) { var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 10, 24, 10), BackColor = Color.White }; p.Controls.Add(c); return p; }

    Control Synthese()
    {
        var rt = Rtf.Reader(10.5f);
        Db.Q(c =>
        {
            var d = c.Rows(@"SELECT numero, intitule, (SELECT nom FROM organismes WHERE id=organisme_id), date_transmission, (SELECT nom FROM organismes WHERE id=service_enqueteur_id), categorie, situation, priorite,
                (SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=referent_id), qualification, faits, date_ouverture, (SELECT nom FROM services WHERE id=service_id), avancement FROM dossiers WHERE id=@p0", id)[0];
            Rtf.H(rt, "Fiche du dossier", 13f);
            Rtf.KV(rt, "Référence", U.S(d[0])); Rtf.KV(rt, "Intitulé", U.S(d[1])); Rtf.KV(rt, "Origine", U.S(d[2])); Rtf.KV(rt, "Date de transmission", U.Aff(d[3]));
            Rtf.KV(rt, "Service enquêteur (simulé)", U.S(d[4])); Rtf.KV(rt, "Catégorie", U.S(d[5])); Rtf.KV(rt, "Qualification", U.S(d[9])); Rtf.KV(rt, "Situation", U.S(d[6]));
            Rtf.KV(rt, "Priorité", U.S(d[7])); Rtf.KV(rt, "Collaborateur référent", U.S(d[8])); Rtf.KV(rt, "Service du parquet", U.S(d[12])); Rtf.KV(rt, "Avancement de l'enquête", U.L(d[13]) + " %");
            Rtf.Sep(rt); Rtf.H(rt, "Personnes concernées", 12f);
            var ps = c.Rows("SELECT dp.role, p.prenom||' '||p.nom, p.statut, IFNULL(dp.declaration,''), (SELECT COUNT(*) FROM casier WHERE personne_id=p.id) FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE dp.dossier_id=@p0 ORDER BY dp.role", id);
            if (ps.Count == 0) Rtf.P(rt, "Aucune personne enregistrée.");
            foreach (var p in ps) Rtf.P(rt, $"• {p[0]} : {p[1]} — {p[2]}{(U.S(p[3]) != "" ? " — " + p[3] : "")}{(U.L(p[4]) > 0 ? $" — {p[4]} mention(s) au casier simulé" : "")}");
            Rtf.Sep(rt); Rtf.H(rt, "Résumé", 12f);
            Rtf.P(rt, U.S(d[10]));
            Rtf.Sep(rt); Rtf.H(rt, "Décisions", 12f);
            var ds = c.Rows("SELECT ref, libelle, orientation, etat_execution, IFNULL(resultat,'') FROM decisions WHERE dossier_id=@p0 ORDER BY date DESC LIMIT 5", id);
            if (ds.Count == 0) Rtf.P(rt, "Aucune décision à ce jour.", false, Color.Gray);
            foreach (var x in ds) Rtf.P(rt, $"• {x[0]} — {x[1]} ({x[2]}) — {x[3]}{(U.S(x[4]) != "" ? " : " + U.S(x[4]).Split('\n')[0] : "")}");
            Rtf.Sep(rt); Rtf.H(rt, "Derniers événements", 12f);
            foreach (var e in c.Rows("SELECT date, libelle FROM dossier_chrono WHERE dossier_id=@p0 ORDER BY date DESC LIMIT 6", id)) Rtf.P(rt, $"• {U.Aff(e[0])} — {e[1]}");
            return 0;
        });
        rt.SelectionStart = 0;
        var top = Ui.Bar(Icons.Btn("decision", "Préparer une décision", (s, e) => Decider(null), true), Icons.Btn("view", "Consulter les pièces", (s, e) => sections.SelectedIndex = 7),
            Icons.Btn("personne", "Personnes concernées", (s, e) => sections.SelectedIndex = 3), Icons.Btn("print", "Imprimer le dossier", (s, e) => Ui.Try(() => Imprimer(false))));
        var p0 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        p0.Controls.Add(Pad(rt)); p0.Controls.Add(top);
        return p0;
    }
}

public class PrintSelectDialog : Dlg
{
    public HashSet<string> Sections = new(); public HashSet<long> Pieces = new();
    public PrintSelectDialog(long dos) : base("Imprimer une sélection du dossier", 760, 680)
    {
        var cs = new CheckedListBox { CheckOnClick = true, IntegralHeight = false };
        foreach (var s in DossierPdf.Sections) cs.Items.Add(s, true);
        Row("Sections", cs, 200);
        var cp = new CheckedListBox { CheckOnClick = true, IntegralHeight = false };
        var ps = Db.Rows("SELECT id, cote||' — '||titre||' ('||type||')' FROM pieces WHERE dossier_id=@p0 ORDER BY id", dos);
        foreach (var p in ps) cp.Items.Add(new Item(U.L(p[0]), U.S(p[1])), true);
        Row("Pièces", cp, 300);
        var fl = new FlowLayoutPanel { Height = 34 };
        fl.Controls.Add(Ui.Btn("Tout cocher", (s, e) => { for (int i = 0; i < cp.Items.Count; i++) cp.SetItemChecked(i, true); }));
        fl.Controls.Add(Ui.Btn("Tout décocher", (s, e) => { for (int i = 0; i < cp.Items.Count; i++) cp.SetItemChecked(i, false); }));
        Row("", fl);
        Cancel("Annuler");
        Btn("Générer le PDF", () =>
        {
            Sections = cs.CheckedItems.Cast<string>().ToHashSet();
            Pieces = cp.CheckedItems.Cast<Item>().Select(i => i.Id).ToHashSet();
            if (Sections.Count == 0) throw new InvalidOperationException("Sélectionnez au moins une section.");
        }, true);
    }
}

public class AddPersonneDialog : Dlg
{
    public AddPersonneDialog(long dos) : base("Ajouter une personne au dossier", 720, 560)
    {
        var mode = Row("Personne", Ui.Combo(new[] { "Personne déjà enregistrée", "Nouvelle personne (identité fictive)" }, 300));
        var cbP = Row("Personne existante", new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems });
        foreach (var r in Db.Rows("SELECT id, nom||' '||prenom||' — né(e) le '||strftime('%d/%m/%Y',date_naissance)||' — '||ref FROM personnes ORDER BY nom, prenom")) cbP.Items.Add(new Item(U.L(r[0]), U.S(r[1])));
        var tNom = Row("Nom", new TextBox()); var tPre = Row("Prénom", new TextBox());
        var cSexe = Row("Sexe", Ui.Combo(new[] { "M", "F" }, 60));
        var dN = Row("Date de naissance", new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(-35) });
        var tAdr = Row("Adresse (fictive)", new TextBox()); var tVille = Row("Ville", new TextBox { Text = "Valmont" }); var tProf = Row("Profession", new TextBox());
        var cRole = Row("Rôle dans le dossier", Ui.Combo(new[] { "Mis en cause", "Victime", "Témoin", "Plaignant" }, 200));
        var cDecl = Row("Position (mis en cause)", Ui.Combo(new[] { "Conteste les faits", "Reconnaît partiellement les faits", "Reconnaît les faits", "Non entendu(e)" }, 260));
        void Sync() { bool ex = mode.SelectedIndex == 0; cbP.Enabled = ex; foreach (Control c in new Control[] { tNom, tPre, cSexe, dN, tAdr, tVille, tProf }) c.Enabled = !ex; cDecl.Enabled = cRole.SelectedIndex == 0; }
        mode.SelectedIndexChanged += (s, e) => Sync(); cRole.SelectedIndexChanged += (s, e) => Sync(); Sync();
        Cancel("Annuler");
        Btn("Ajouter au dossier", () => Db.Tx(c =>
        {
            Auth.Check("dossiers.gerer");
            long pid;
            if (mode.SelectedIndex == 0) { pid = ComboId(cbP); if (pid == 0) throw new InvalidOperationException("Sélectionnez une personne dans la liste."); }
            else
            {
                if (string.IsNullOrWhiteSpace(tNom.Text) || string.IsNullOrWhiteSpace(tPre.Text)) throw new InvalidOperationException("Nom et prénom obligatoires.");
                string nom = tNom.Text.Trim().ToUpperInvariant(), pre = tPre.Text.Trim(), dn = dN.Value.ToString("yyyy-MM-dd");
                pid = c.Long("SELECT id FROM personnes WHERE nom=@p0 AND prenom=@p1 AND date_naissance=@p2", nom, pre, dn);
                if (pid == 0)
                {
                    pid = c.Insert("INSERT INTO personnes(nom,prenom,sexe,date_naissance,lieu_naissance,adresse,ville,profession,statut,date_creation) VALUES(@p0,@p1,@p2,@p3,'Non renseigné',@p4,@p5,@p6,'Sans antécédent',@p7)", nom, pre, cSexe.Text, dn, tAdr.Text, tVille.Text, tProf.Text, U.D(Engine.Now));
                    c.Exec("UPDATE personnes SET ref=@p1 WHERE id=@p0", pid, $"P-{pid:000000}");
                }
            }
            string role = cRole.Text;
            if (c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0 AND personne_id=@p1 AND role=@p2", dos, pid) > 0) throw new InvalidOperationException("Cette personne figure déjà au dossier avec ce rôle.");
            Jud.Lier(c, dos, pid, role, "Ajout par " + Biz.Name(c, Auth.CollabId), role == "Mis en cause" && cDecl.SelectedIndex < 3 ? cDecl.Text : null);
            c.Exec("INSERT INTO dossier_chrono(dossier_id,date,libelle,source) VALUES(@p0,@p1,@p2,'Parquet')", dos, U.D(Engine.Now), $"Ajout de {Jud.NomP(c, pid)} en qualité de {role.ToLower()}");
            Biz.DHist(c, dos, Auth.CollabId, $"Ajout de {Jud.NomP(c, pid)} ({role})");
            Jud.MajStatut(c, pid);
        }), true);
    }
}

public class TransmettreDialog : Dlg
{
    public TransmettreDialog(long dos) : base("Transmettre le dossier à un service", 720, 440)
    {
        var d = Db.Rows("SELECT numero, intitule, IFNULL(referent_id,0) FROM dossiers WHERE id=@p0", dos)[0];
        Row("Dossier", new Label { Text = $"{d[0]} — {d[1]}", AutoSize = true, Font = Theme.FB });
        var cDest = Row("Destinataire", CollabCombo(U.L(d[2]), "c.id<>@p0", Auth.CollabId));
        var cObj = Row("Objet", Ui.Combo(new[] { $"Analyse du dossier {d[0]}", $"Préparation d'un projet de décision — {d[0]}", $"Vérification des pièces — {d[0]}", $"Suivi des investigations — {d[0]}" }, 400, true));
        var tIns = Row("Instructions", Multi("Me rendre compte sous 48 heures."), 100);
        var cPrio = Row("Priorité", Ui.Combo(Biz.Priorites, 120)); cPrio.SelectedIndex = 1;
        Cancel("Annuler");
        Btn("Transmettre", () => Db.Tx(c =>
        {
            Auth.Check("missions.creer");
            long dest = ComboId(cDest); if (dest == 0) throw new InvalidOperationException("Choisissez un destinataire.");
            Biz.CreerMission(c, Auth.CollabId, dest, cObj.Text, $"Transmission du dossier {d[0]} « {d[1]} » pour traitement.", cPrio.Text, Engine.Now.AddDays(2), tIns.Text, "Interne", 0, dos, 0, null, "Transmission de dossier");
            Biz.DHist(c, dos, Auth.CollabId, $"Dossier transmis à {Biz.NameF(c, dest)} : {cObj.Text}");
        }), true);
    }
}

// ============================================================ fiche d'une personne
public class PersonneWindow : Form
{
    public static void Open(IWin32Window owner, long pid, string tab = null) { var f = new PersonneWindow(pid, tab) { Icon = Program.AppIcon }; f.Show(owner); }

    public PersonneWindow(long pid, string tab)
    {
        var p = Db.Rows("SELECT ref, nom, prenom, sexe, date_naissance, lieu_naissance, adresse, ville, profession, statut, IFNULL(mesure,''), IFNULL(notes,''), date_creation FROM personnes WHERE id=@p0", pid)[0];
        Text = $"Fiche personne — {p[1]} {p[2]} ({p[0]})"; Width = 1180; Height = 780; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false; BackColor = Theme.Back;
        var head = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Theme.Navy };
        var pic = new PictureBox { Image = Icons.Get("personne", 48, Color.White), Location = new Point(18, 16), Size = new Size(48, 48) };
        head.Controls.Add(pic);
        head.Controls.Add(new Label { Text = $"{p[1]} {p[2]}", Font = new Font("Segoe UI Semibold", 16f), ForeColor = Color.White, Location = new Point(78, 10), AutoSize = true });
        int age = Engine.Now.Year - U.P(p[4]).Year - (Engine.Now.DayOfYear < U.P(p[4]).DayOfYear ? 1 : 0);
        head.Controls.Add(new Label { Text = $"{p[0]}  ·  {(U.S(p[3]) == "F" ? "Née" : "Né")} le {U.P(p[4]):dd/MM/yyyy} ({age} ans) à {p[5]}  ·  {p[8]}  ·  identité fictive", ForeColor = Color.FromArgb(185, 205, 230), Location = new Point(80, 46), AutoSize = true });
        var st = new Label { Text = U.S(p[9]), AutoSize = true, BackColor = U.S(p[9]).StartsWith("Détenu") ? Theme.Red : U.S(p[9]).StartsWith("Mis en cause") || U.S(p[9]).Contains("contrôle") ? Theme.Orange : Theme.Accent, ForeColor = Color.White, Font = Theme.FB, Padding = new Padding(8, 4, 8, 4), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        head.Controls.Add(st);
        head.Resize += (s, e) => st.Location = new Point(head.Width - st.Width - 20, 28);
        var info = Rtf.Reader(10f);
        Rtf.H(info, "Identité (fictive)", 12f);
        Rtf.KV(info, "Nom", U.S(p[1])); Rtf.KV(info, "Prénom", U.S(p[2])); Rtf.KV(info, "Sexe", U.S(p[3])); Rtf.KV(info, "Naissance", $"{U.P(p[4]):dd/MM/yyyy} à {p[5]}");
        Rtf.KV(info, "Adresse", $"{p[6]}, {p[7]}"); Rtf.KV(info, "Profession", U.S(p[8])); Rtf.Sep(info);
        Rtf.H(info, "Statut judiciaire simulé", 12f); Rtf.P(info, U.S(p[9]), true, Theme.Navy);
        if (U.S(p[10]) != "") Rtf.KV(info, "Mesure en cours", U.S(p[10]));
        var cnt = Db.Rows("SELECT (SELECT COUNT(DISTINCT dossier_id) FROM dossier_personnes WHERE personne_id=@p0), (SELECT COUNT(*) FROM casier WHERE personne_id=@p0), (SELECT COUNT(*) FROM detentions WHERE personne_id=@p0), (SELECT COUNT(*) FROM pieces WHERE personne_id=@p0)", pid)[0];
        Rtf.Sep(info); Rtf.H(info, "En bref", 12f);
        Rtf.KV(info, "Dossiers", U.S(cnt[0])); Rtf.KV(info, "Mentions au casier simulé", U.S(cnt[1])); Rtf.KV(info, "Périodes de détention simulées", U.S(cnt[2])); Rtf.KV(info, "Pièces la concernant", U.S(cnt[3]));
        if (U.S(p[11]) != "") { Rtf.Sep(info); Rtf.KV(info, "Notes", U.S(p[11])); }
        var left = new Panel { Dock = DockStyle.Left, Width = 330, BackColor = Color.White, Padding = new Padding(16) };
        left.Controls.Add(info);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        TabPage T(string t, Control c) { var tp = new TabPage(t); tp.Controls.Add(c); tabs.TabPages.Add(tp); return tp; }
        var gD = new GridPanel("pw_dos") { Loader = (q, o, l) => Db.Table("SELECT d.id, d.numero AS 'Dossier', d.intitule AS 'Intitulé', dp.role AS 'Rôle', IFNULL(dp.declaration,'') AS 'Position', d.situation AS 'Situation', substr(d.date_ouverture,1,10) AS 'Ouverture', dp.source AS 'Origine du lien' FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id WHERE dp.personne_id=@p0 AND d.intitule LIKE @p1 ORDER BY d.date_ouverture DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) };
        gD.Activated += DossierWindow.Open;
        gD.Bar.Items.Insert(0, Icons.Tool("dossier", "Ouvrir le dossier", () => DossierWindow.Open(gD.SelectedId)));
        T("Dossiers et rôles", gD);
        T("Auditions et déclarations", new PiecesPane("pw_aud", (q, o, l) => Db.Table("SELECT p.id, d.numero AS 'Dossier', p.cote AS 'Cote', p.titre AS 'Titre', substr(p.date,1,16) AS 'Date', p.auteur AS 'Rédacteur' FROM pieces p JOIN dossiers d ON d.id=p.dossier_id WHERE p.personne_id=@p0 AND p.type IN ('Audition','Interrogatoire','Procès-verbal') AND p.titre LIKE @p1 ORDER BY p.date DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o)));
        T("Documents", new PiecesPane("pw_docs", (q, o, l) => Db.Table("SELECT p.id, d.numero AS 'Dossier', p.cote AS 'Cote', p.titre AS 'Titre', p.type AS 'Nature', substr(p.date,1,16) AS 'Date' FROM pieces p JOIN dossiers d ON d.id=p.dossier_id WHERE p.personne_id=@p0 AND p.type NOT IN ('Audition','Interrogatoire','Procès-verbal') AND p.titre LIKE @p1 ORDER BY p.date DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o)));
        var gV = new GridPanel("pw_veh") { Loader = (q, o, l) => Db.Table(@"SELECT v.id, v.immatriculation AS 'Immatriculation', v.marque||' '||v.modele AS 'Véhicule', v.couleur AS 'Couleur', CASE WHEN v.proprietaire_id=@p0 THEN 'Propriétaire' ELSE 'Cité dans un dossier de la personne' END AS 'Lien',
            (SELECT group_concat(d.numero, ', ') FROM dossier_vehicules dv JOIN dossiers d ON d.id=dv.dossier_id WHERE dv.vehicule_id=v.id) AS 'Dossiers'
            FROM vehicules v WHERE (v.proprietaire_id=@p0 OR v.id IN (SELECT dv.vehicule_id FROM dossier_vehicules dv JOIN dossier_personnes dp ON dp.dossier_id=dv.dossier_id WHERE dp.personne_id=@p0)) AND v.immatriculation LIKE @p1 LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) };
        gV.Activated += vid => MainForm.Instance?.OpenRef("vehicule", vid);
        T("Véhicules", gV);
        var gC = new GridPanel("pw_casier") { Loader = (q, o, l) => Db.Table("SELECT c.id, substr(c.date,1,10) AS 'Date', c.nature AS 'Nature des faits', c.mesure AS 'Décision', c.juridiction AS 'Juridiction', IFNULL(d.numero, c.ref_externe) AS 'Dossier', c.statut AS 'Statut', c.source AS 'Origine', (SELECT ref FROM decisions WHERE id=c.decision_id) AS 'Décision parquet' FROM casier c LEFT JOIN dossiers d ON d.id=c.dossier_id WHERE c.personne_id=@p0 AND c.nature LIKE @p1 ORDER BY c.date DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) };
        gC.Bar.Items.Insert(0, Icons.Tool("print", "Relevé du casier (PDF)", () => CasierModule.Releve(pid)));
        gC.Activated += cid => { long d = Db.Long("SELECT IFNULL(dossier_id,0) FROM casier WHERE id=@p0", cid); if (d != 0) DossierWindow.Open(d); };
        T("Casier judiciaire", gC);
        var sp = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        var gDet = new GridPanel("pw_det") { Loader = (q, o, l) => Db.Table("SELECT d.id, d.regime AS 'Régime', d.etablissement AS 'Établissement', substr(d.date_entree,1,10) AS 'Entrée', IFNULL(substr(d.date_sortie,1,10),'—') AS 'Sortie', d.statut AS 'Statut', d.duree_mois AS 'Durée (mois)', IFNULL(x.numero, d.ref_externe) AS 'Dossier', d.motif AS 'Motif' FROM detentions d LEFT JOIN dossiers x ON x.id=d.dossier_id WHERE d.personne_id=@p0 AND IFNULL(d.motif,'') LIKE @p1 ORDER BY d.date_entree DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) };
        var gDH = new GridPanel("pw_deth") { Loader = (q, o, l) => Db.Table("SELECT h.id, substr(h.date,1,16) AS 'Date', h.ancien_statut AS 'Avant', h.nouveau_statut AS 'Après', h.commentaire AS 'Commentaire', (SELECT ref FROM decisions WHERE id=h.decision_id) AS 'Décision' FROM detention_hist h JOIN detentions d ON d.id=h.detention_id WHERE d.personne_id=@p0 AND IFNULL(h.commentaire,'') LIKE @p1 ORDER BY h.date DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) };
        sp.Panel1.Controls.Add(gDet); sp.Panel2.Controls.Add(gDH);
        T("Détention", sp);
        var gR = new GridPanel("pw_rapp") { Loader = (q, o, l) => Db.Table(@"SELECT * FROM (
            SELECT p2.id, 'personne' AS '_t', p2.prenom||' '||p2.nom AS 'Élément lié', 'Figure dans le même dossier ('||dp1.role||' / '||dp2.role||')' AS 'Nature du lien', d.numero AS 'Dossier', 'Liaison enregistrée — '||IFNULL(dp2.source,'') AS 'Origine'
              FROM dossier_personnes dp1 JOIN dossier_personnes dp2 ON dp2.dossier_id=dp1.dossier_id AND dp2.personne_id<>dp1.personne_id JOIN personnes p2 ON p2.id=dp2.personne_id JOIN dossiers d ON d.id=dp1.dossier_id WHERE dp1.personne_id=@p0
            UNION ALL SELECT v.id, 'vehicule', v.immatriculation||' ('||v.marque||' '||v.modele||')', 'Propriétaire déclaré', IFNULL((SELECT group_concat(d.numero, ', ') FROM dossier_vehicules dv JOIN dossiers d ON d.id=dv.dossier_id WHERE dv.vehicule_id=v.id),'—'), 'Fichier des véhicules (simulé)' FROM vehicules v WHERE v.proprietaire_id=@p0
            UNION ALL SELECT d.id, 'dossier', d.numero||' — '||d.intitule, 'Dossier où la personne apparaît ('||dp.role||')', d.numero, 'Liaison enregistrée — '||IFNULL(dp.source,'') FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id WHERE dp.personne_id=@p0
            ) WHERE [Élément lié] LIKE @p1 LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) };
        gR.Activated += rid => MainForm.Instance?.OpenRef(U.S(gR.Cell("_t")), rid);
        T("Rapprochements", gR);
        T("Historique", new GridPanel("pw_hist") { Loader = (q, o, l) => Db.Table(@"SELECT * FROM (SELECT p.id, substr(p.date,1,16) AS 'Date', 'Pièce '||p.cote||' ('||d.numero||')' AS 'Source', p.titre AS 'Événement' FROM pieces p JOIN dossiers d ON d.id=p.dossier_id WHERE p.personne_id=@p0
            UNION ALL SELECT -x.id, substr(x.date,1,16), 'Décision '||x.ref, x.libelle||' — '||x.orientation FROM decisions x WHERE x.personne_id=@p0
            UNION ALL SELECT -100000-h.id, substr(h.date,1,16), 'Détention', h.nouveau_statut||' — '||IFNULL(h.commentaire,'') FROM detention_hist h JOIN detentions dd ON dd.id=h.detention_id WHERE dd.personne_id=@p0
            UNION ALL SELECT -200000-c.id, substr(c.date,1,16), 'Casier', c.mesure FROM casier c WHERE c.personne_id=@p0) WHERE Événement LIKE @p1 ORDER BY Date DESC LIMIT @p2 OFFSET @p3", pid, U.Like(q), l, o) });
        foreach (TabPage tp in tabs.TabPages)
        {
            bool loaded = false; var page = tp;
            void L() { if (loaded) return; loaded = true; foreach (Control c in page.Controls) Load1(c); }
            tp.Enter += (s, e) => L();
            if (tabs.TabPages.IndexOf(tp) == 0) L();
        }
        if (tab != null) foreach (TabPage tp in tabs.TabPages) if (tp.Text == tab) { tabs.SelectedTab = tp; foreach (Control c in tp.Controls) Load1(c); }
        var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.White, Padding = new Padding(8, 3, 8, 3) };
        bar.Items.Add(Icons.Tool("print", "Imprimer la fiche (PDF)", () =>
        {
            var pdf = new PdfDoc { Ref = U.S(p[0]), Title = $"Fiche personne — {p[1]} {p[2]}" };
            pdf.Toc = false;
            pdf.Section($"Fiche personne — {p[1]} {p[2]}", true);
            pdf.Para(info.Text);
            pdf.Sub("Dossiers");
            foreach (var r in Db.Rows("SELECT d.numero, d.intitule, dp.role FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id WHERE dp.personne_id=@p0", pid)) pdf.Para($"- {r[0]} {r[1]} ({r[2]})");
            using var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = $"Fiche {p[1]} {p[2]}.pdf", InitialDirectory = Db.ExportDir };
            if (sfd.ShowDialog(this) == DialogResult.OK) { pdf.Save(sfd.FileName); try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { } }
        }));
        bar.Items.Add(Icons.Tool("casier", "Consulter le casier", () => { foreach (TabPage tp in tabs.TabPages) if (tp.Text == "Casier judiciaire") tabs.SelectedTab = tp; }));
        bar.Items.Add(Icons.Tool("recherche", "Rechercher des rapprochements", () => { foreach (TabPage tp in tabs.TabPages) if (tp.Text == "Rapprochements") tabs.SelectedTab = tp; }));
        Controls.Add(tabs); Controls.Add(new Splitter { Dock = DockStyle.Left, Width = 3 }); Controls.Add(left); Controls.Add(bar); Controls.Add(head);
    }

    static void Load1(Control c) { if (c is GridPanel g) g.Reload(); else if (c is PiecesPane p) p.Grid.Reload(); else foreach (Control x in c.Controls) Load1(x); }
}

// ============================================================ parcours de décision
public class DecisionWizard : Form
{
    readonly long dos, remplace;
    string code; long pid; string orient = ""; string motivation = "";
    int step;
    readonly ListBox steps = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 44, IntegralHeight = false };
    readonly Panel body = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20) };
    readonly Button bPrev, bNext, bOk;
    static readonly string[] Etapes = { "Contexte et nature", "Informations disponibles", "Éléments à examiner", "Vérification des prérequis", "Choix de l'orientation", "Motivation", "Validation et traçabilité" };

    public DecisionWizard(long dossier, string code, long personne, long remplaceId)
    {
        dos = dossier; this.code = code; pid = personne; remplace = remplaceId;
        if (remplace != 0)
        {
            var d = Db.Rows("SELECT type, IFNULL(personne_id,0), orientation, motivation FROM decisions WHERE id=@p0", remplace)[0];
            this.code = U.S(d[0]); pid = U.L(d[1]); orient = U.S(d[2]); motivation = U.S(d[3]);
        }
        var num = Db.Str("SELECT numero||' — '||intitule FROM dossiers WHERE id=@p0", dos);
        Text = "Préparer une décision — " + num; Width = 1100; Height = 760; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false; MinimizeBox = false;
        foreach (var e in Etapes) steps.Items.Add(e);
        steps.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool cur = e.Index == step, done = e.Index < step;
            e.Graphics.FillRectangle(new SolidBrush(cur ? Theme.Light : Color.White), e.Bounds);
            var circ = new Rectangle(e.Bounds.X + 14, e.Bounds.Y + 10, 24, 24);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(new SolidBrush(done ? Theme.Green : cur ? Theme.Accent : Theme.Gray), circ);
            var t = done ? "✓" : (e.Index + 1).ToString();
            var sz = e.Graphics.MeasureString(t, Theme.FB);
            e.Graphics.DrawString(t, Theme.FB, done || cur ? Brushes.White : Brushes.Gray, circ.X + (24 - sz.Width) / 2, circ.Y + 4);
            e.Graphics.DrawString(Etapes[e.Index], cur ? Theme.FB : Theme.F, new SolidBrush(cur ? Theme.Navy : Theme.Anthracite), e.Bounds.X + 48, e.Bounds.Y + 13);
        };
        steps.SelectionMode = SelectionMode.None;
        var left = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = Color.White, Padding = new Padding(0, 10, 0, 0) };
        left.Controls.Add(steps);
        left.Controls.Add(new Label { Text = "  PARCOURS DE DÉCISION", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.Gray });
        var head = new Label { Dock = DockStyle.Top, Height = 54, BackColor = Theme.Navy, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 13f), Text = "   " + num + (remplace != 0 ? "  (modification d'une décision)" : ""), TextAlign = ContentAlignment.MiddleLeft };
        var foot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10, 8, 10, 8), BackColor = Theme.Back };
        bOk = Icons.Btn("check", "Valider la décision", (s, e) => Valider(), true); bOk.Width = 190; bOk.AutoSize = false;
        bNext = Ui.PrimaryBtn("Suivant ›", (s, e) => Go(step + 1), 120);
        bPrev = Ui.Btn("‹ Précédent", (s, e) => Go(step - 1), 120);
        var bCancel = Ui.Btn("Annuler", (s, e) => Close(), 100);
        foot.Controls.AddRange(new Control[] { bOk, bNext, bPrev, bCancel });
        foot.Controls.Add(new Label { Text = U.Mention + " — aucune décision n'est un acte juridique réel.", AutoSize = true, ForeColor = Theme.Red, Padding = new Padding(0, 8, 30, 0) });
        Controls.Add(body); Controls.Add(new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Gray }); Controls.Add(left); Controls.Add(foot); Controls.Add(head);
        Go(0);
    }

    public void TestWalk(string c, long p)
    {
        code = c; pid = p; orient = ""; motivation = "";
        for (int i = 0; i < 7; i++)
        {
            if (i == 5 && orient == "") orient = Db.Q(x => Jud.Orientations(x, code, dos, pid)).FirstOrDefault() ?? "";
            if (i == 6) motivation = "Motivation de test automatique : " + new string('m', 250);
            Go(i); Application.DoEvents();
        }
    }

    List<(long id, string cote, string titre, bool lue)> Essentielles()
    {
        string extra = code is "PERSONNE" or "DML" or "ALTERNATIVE" && pid != 0 ? $" AND (type<>'Interrogatoire' OR personne_id={pid})" : "";
        var r = Db.Rows($@"SELECT id, cote, titre, lue FROM pieces WHERE dossier_id=@p0 AND (type IN ('Rapport','Interrogatoire','Jugement') OR titre LIKE 'Procès-verbal de plainte%' OR titre LIKE 'Procès-verbal de saisine%' OR titre LIKE 'Requête%' OR titre LIKE 'Demande de mise en liberté%'){extra} ORDER BY id", dos);
        if (r.Count == 0) r = Db.Rows("SELECT id, cote, titre, lue FROM pieces WHERE dossier_id=@p0 ORDER BY id LIMIT 6", dos);
        return r.Select(x => (U.L(x[0]), U.S(x[1]), U.S(x[2]), U.L(x[3]) == 1)).ToList();
    }
    bool Examine => Essentielles().All(e => e.lue);

    void Go(int s)
    {
        if (s > step)
        {
            if (step == 0)
            {
                if (code == null) { Ui.Info("Choisissez la nature de la décision."); return; }
                if (Jud.TypeOf(code).AvecPersonne && pid == 0) { Ui.Info("Choisissez la personne concernée."); return; }
            }
            if (step == 4 && orient == "") { Ui.Info("Choisissez une orientation."); return; }
        }
        step = Math.Clamp(s, 0, Etapes.Length - 1);
        steps.Invalidate();
        bPrev.Enabled = step > 0; bNext.Visible = step < Etapes.Length - 1; bOk.Visible = step == Etapes.Length - 1;
        foreach (Control c in body.Controls) c.Dispose();
        body.Controls.Clear();
        var title = new Label { Text = $"Étape {step + 1} / {Etapes.Length} — {Etapes[step]}", Dock = DockStyle.Top, Height = 36, Font = Theme.FT, ForeColor = Theme.Navy };
        Control c0 = step switch { 0 => Etape1(), 1 => Etape2(), 2 => Etape3(), 3 => Etape4(), 4 => Etape5(), 5 => Etape6(), _ => Etape7() };
        body.Controls.Add(c0); body.Controls.Add(title);
    }

    string ContexteTexte() => Db.Q(c =>
    {
        var d = c.Rows("SELECT numero, intitule, qualification, situation, gravite, (SELECT nom FROM organismes WHERE id=service_enqueteur_id) FROM dossiers WHERE id=@p0", dos)[0];
        return $"Dossier {d[0]} — {d[1]}\nQualification : {d[2]}\nSituation : {d[3]} — gravité {d[4]}/3 — service enquêteur : {d[5]}" + (pid != 0 ? $"\nPersonne concernée : {Jud.IdentiteP(c, pid)} — statut : {c.Str("SELECT statut FROM personnes WHERE id=@p0", pid)}" : "");
    });

    Control Etape1()
    {
        var p = new Panel { Dock = DockStyle.Fill };
        var rt = Rtf.Reader(10f); rt.Height = 110; rt.Dock = DockStyle.Top;
        Rtf.P(rt, ContexteTexte());
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var lp = new Label { Text = "Personne concernée :", AutoSize = true, Margin = new Padding(3, 12, 3, 3), Font = Theme.FB };
        var cbP = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 560 };
        void FillPers()
        {
            cbP.Items.Clear();
            string w = code == "DML" ? "AND dp.personne_id IN (SELECT personne_id FROM detentions WHERE date_sortie IS NULL)" : "";
            foreach (var r in Db.Rows($"SELECT p.id, p.nom||' '||p.prenom||' — '||p.statut||' — '||IFNULL(dp.declaration,'') FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE dp.dossier_id=@p0 AND dp.role='Mis en cause' {w}", dos)) cbP.Items.Add(new Item(U.L(r[0]), U.S(r[1])));
            Item.Select(cbP, pid);
            bool need = code != null && Jud.TypeOf(code).AvecPersonne;
            lp.Visible = cbP.Visible = need;
        }
        cbP.SelectedIndexChanged += (s, e) => { pid = Item.Of(cbP); orient = ""; };
        flow.Controls.Add(new Label { Text = "Décisions possibles selon la situation du dossier :", AutoSize = true, Font = Theme.FB, Margin = new Padding(3, 10, 3, 6) });
        var dispo = Db.Q(c => Jud.Disponibles(c, dos));
        foreach (var (t, ok, raison) in dispo)
        {
            var rb = new RadioButton { Text = t.Libelle + (ok ? "" : "   — indisponible : " + raison), AutoSize = true, Enabled = ok && (remplace == 0 || t.Code == code), Checked = t.Code == code, Font = ok ? Theme.FB : Theme.F, Margin = new Padding(6, 4, 3, 0), Tag = t.Code };
            var desc = new Label { Text = t.Description, AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(26, 0, 3, 6), MaximumSize = new Size(720, 0) };
            rb.CheckedChanged += (s, e) => { if (rb.Checked) { if (code != (string)rb.Tag) { orient = ""; } code = (string)rb.Tag; FillPers(); } };
            flow.Controls.Add(rb); flow.Controls.Add(desc);
        }
        flow.Controls.Add(lp); flow.Controls.Add(cbP);
        FillPers();
        p.Controls.Add(flow); p.Controls.Add(rt);
        return p;
    }

    Control Etape2()
    {
        var rt = Rtf.Reader(10.5f);
        Db.Q(c =>
        {
            Rtf.H(rt, "Contexte"); Rtf.P(rt, ContexteTexte()); Rtf.Sep(rt);
            Rtf.H(rt, "Personnes");
            foreach (var p in c.Rows("SELECT dp.role, p.id, p.prenom||' '||p.nom, p.statut, IFNULL(dp.declaration,'') FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE dp.dossier_id=@p0", dos))
                Rtf.P(rt, $"• {p[0]} : {p[2]} — {p[3]}{(U.S(p[4]) != "" ? " — " + p[4] : "")} — casier : {c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", U.L(p[1]))} mention(s) — autres dossiers : {c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE personne_id=@p0 AND dossier_id<>@p1", U.L(p[1]), dos)}");
            Rtf.Sep(rt); Rtf.H(rt, "Pièces versées");
            foreach (var t in c.Rows("SELECT type, COUNT(*) FROM pieces WHERE dossier_id=@p0 GROUP BY type", dos)) Rtf.P(rt, $"• {t[0]} : {t[1]}");
            Rtf.P(rt, $"• Scellés : {c.Long("SELECT COUNT(*) FROM scelles WHERE dossier_id=@p0", dos)} — véhicules : {c.Long("SELECT COUNT(*) FROM dossier_vehicules WHERE dossier_id=@p0", dos)}");
            Rtf.Sep(rt); Rtf.H(rt, "Décisions antérieures");
            var ds = c.Rows("SELECT ref, libelle, orientation, etat_execution FROM decisions WHERE dossier_id=@p0 ORDER BY date", dos);
            if (ds.Count == 0) Rtf.P(rt, "Aucune.", false, Color.Gray);
            foreach (var x in ds) Rtf.P(rt, $"• {x[0]} — {x[1]} ({x[2]}) — {x[3]}");
            Rtf.Sep(rt); Rtf.H(rt, "Indicateur de simulation");
            int s = Jud.Score(c, dos, pid);
            Rtf.P(rt, $"Éléments réunis au dossier : {s} / 12 — indicateur de jeu calculé à partir des pièces, scellés, témoignages et déclarations enregistrés (il ne constitue pas une appréciation juridique).", false, Theme.Anthracite);
            return 0;
        });
        rt.SelectionStart = 0;
        return rt;
    }

    Control Etape3()
    {
        var p = new Panel { Dock = DockStyle.Fill };
        var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, Font = new Font("Segoe UI", 10f) };
        lv.Columns.Add("Cote", 70); lv.Columns.Add("Pièce", 560); lv.Columns.Add("État", 120);
        void Fill() { lv.Items.Clear(); foreach (var e in Essentielles()) { var it = new ListViewItem(new[] { e.cote, e.titre, e.lue ? "✔ Examinée" : "À examiner" }) { Tag = e.id, ForeColor = e.lue ? Theme.Green : Theme.Red }; lv.Items.Add(it); } }
        Fill();
        void OpenSel() { if (lv.SelectedItems.Count == 0) return; PieceViewer.Open(this, (long)lv.SelectedItems[0].Tag, true); Fill(); }
        lv.DoubleClick += (s, e) => OpenSel();
        var bar = Ui.Bar(Icons.Btn("view", "Consulter la pièce sélectionnée", (s, e) => OpenSel(), true),
            Icons.Btn("dossier", "Ouvrir le dossier complet", (s, e) => DossierWindow.Open(dos)),
            Icons.Btn("casier", "Casier de la personne", (s, e) => { if (pid != 0) PersonneWindow.Open(this, pid, "Casier judiciaire"); }));
        p.Controls.Add(lv); p.Controls.Add(bar);
        p.Controls.Add(new Label { Text = "Consultez chaque pièce essentielle avant de décider (double-clic). L'examen est tracé dans la décision.", Dock = DockStyle.Top, Height = 28, ForeColor = Theme.Anthracite });
        return p;
    }

    Control Etape4()
    {
        var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, Font = new Font("Segoe UI", 10f), ShowGroups = true };
        lv.Columns.Add("Vérification", 620); lv.Columns.Add("Résultat", 160);
        Db.Q(c =>
        {
            foreach (var o in Jud.Orientations(c, code, dos, pid))
            {
                var g = new ListViewGroup(o); lv.Groups.Add(g);
                foreach (var ch in Jud.Prerequis(c, code, o, dos, pid, new string('x', 400), Examine).Where(x => !x.Libelle.StartsWith("Motivation")))
                    lv.Items.Add(new ListViewItem(new[] { ch.Libelle, ch.Ok ? "✔ Rempli" : ch.Bloquant ? "✖ Bloquant" : "⚠ Avertissement" }, g) { ForeColor = ch.Ok ? Theme.Green : ch.Bloquant ? Theme.Red : Theme.Orange });
            }
            return 0;
        });
        var p = new Panel { Dock = DockStyle.Fill };
        p.Controls.Add(lv);
        p.Controls.Add(new Label { Text = "Prérequis de la simulation pour chaque orientation possible (la motivation sera vérifiée à l'étape 6).", Dock = DockStyle.Top, Height = 28, ForeColor = Theme.Anthracite });
        return p;
    }

    Control Etape5()
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        Db.Q(c =>
        {
            foreach (var o in Jud.Orientations(c, code, dos, pid))
            {
                var bloc = Jud.Prerequis(c, code, o, dos, pid, new string('x', 400), Examine).Where(x => x.Bloquant && !x.Ok && !x.Libelle.StartsWith("Motivation")).ToList();
                var rb = new RadioButton { Text = o, AutoSize = true, Enabled = bloc.Count == 0, Checked = o == orient && bloc.Count == 0, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Margin = new Padding(6, 10, 3, 0) };
                rb.CheckedChanged += (s, e) => { if (rb.Checked) orient = o; };
                flow.Controls.Add(rb);
                flow.Controls.Add(new Label { Text = bloc.Count == 0 ? Jud.Effets(code, o) : "Indisponible : " + string.Join(" ; ", bloc.Select(b => b.Libelle)), AutoSize = true, MaximumSize = new Size(760, 0), ForeColor = bloc.Count == 0 ? Theme.Anthracite : Theme.Red, Margin = new Padding(28, 2, 3, 6) });
            }
            return 0;
        });
        return flow;
    }

    Control Etape6()
    {
        var p = new Panel { Dock = DockStyle.Fill };
        var t = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 10.5f), Text = motivation };
        var cnt = new Label { Dock = DockStyle.Bottom, Height = 24, ForeColor = Theme.Anthracite };
        void Upd() { motivation = t.Text; cnt.Text = $"{t.Text.Trim().Length} caractères"; }
        t.TextChanged += (s, e) => Upd(); Upd();
        string[] modeles =
        {
            "Au vu des pièces de la procédure, et notamment du rapport de synthèse du service enquêteur, ",
            "Compte tenu de la gravité des faits reprochés et de leur retentissement, ",
            "Les déclarations recueillies au cours de l'enquête établissent que ",
            "Les antécédents enregistrés dans la simulation et le risque de réitération justifient ",
            "La situation personnelle de l'intéressé(e) et ses garanties de représentation conduisent à ",
            "Les investigations n'ont pas permis de réunir des éléments suffisants pour "
        };
        var fl = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 74, AutoScroll = true };
        foreach (var m in modeles) { var mm = m; fl.Controls.Add(Ui.Btn(mm.Length > 48 ? mm[..48] + "…" : mm, (s, e) => { t.AppendText(mm); t.Focus(); })); }
        p.Controls.Add(t); p.Controls.Add(cnt); p.Controls.Add(fl);
        p.Controls.Add(new Label { Text = $"Orientation retenue : {orient}. Rédigez la motivation (insertions possibles ci-dessous).", Dock = DockStyle.Top, Height = 28, Font = Theme.FB, ForeColor = Theme.Navy });
        return p;
    }

    List<Jud.Check> Checks() => Db.Q(c => Jud.Prerequis(c, code, orient, dos, pid, motivation, Examine));

    Control Etape7()
    {
        var rt = Rtf.Reader(10.5f);
        Rtf.H(rt, Jud.TypeOf(code).Libelle);
        Rtf.KV(rt, "Orientation", orient);
        Rtf.P(rt, ContexteTexte());
        Rtf.Sep(rt); Rtf.H(rt, "Motivation", 12f); Rtf.P(rt, motivation);
        Rtf.Sep(rt); Rtf.H(rt, "Conséquences dans la simulation", 12f); Rtf.P(rt, Jud.Effets(code, orient));
        Rtf.Sep(rt); Rtf.H(rt, "Vérifications", 12f);
        var ch = Checks();
        foreach (var x in ch) Rtf.P(rt, (x.Ok ? "✔ " : x.Bloquant ? "✖ " : "⚠ ") + x.Libelle, false, x.Ok ? Theme.Green : x.Bloquant ? Theme.Red : Theme.Orange);
        bool ok = ch.All(x => x.Ok || !x.Bloquant);
        Rtf.Sep(rt);
        Rtf.P(rt, ok ? "La décision peut être validée. Elle sera tracée (référence, auteur, date, version) et versée au dossier." : "Des prérequis bloquants ne sont pas remplis : revenez aux étapes précédentes.", true, ok ? Theme.Navy : Theme.Red);
        bOk.Enabled = ok;
        rt.SelectionStart = 0;
        return rt;
    }

    void Valider()
    {
        var ch = Checks();
        if (ch.Any(x => !x.Ok && x.Bloquant)) { Ui.Info("Des prérequis bloquants ne sont pas remplis."); return; }
        string verifs = string.Join("\n", ch.Select(x => (x.Ok ? "[OK] " : x.Bloquant ? "[KO] " : "[!] ") + x.Libelle)) + "\nPièces examinées : " + string.Join(", ", Essentielles().Where(e => e.lue).Select(e => e.cote));
        if (Ui.Try(() =>
        {
            long id = Db.Tx(c => Jud.Enregistrer(c, code, orient, dos, pid, ContexteTexte(), verifs, motivation, remplace));
            Ui.Info($"Décision {Db.Str("SELECT ref FROM decisions WHERE id=@p0", id)} enregistrée.\n\n{Jud.Effets(code, orient)}");
        })) { DialogResult = DialogResult.OK; Close(); }
    }
}

public class DecisionView : Form
{
    public DecisionView(long id)
    {
        var d = Db.Rows(@"SELECT ref, libelle, orientation, contexte, verifications, motivation, statut, etat_execution, echeance_effet, IFNULL(resultat,''), date, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id), version, dossier_id, IFNULL(personne_id,0), IFNULL(piece_id,0), source,
            (SELECT ref FROM decisions x WHERE x.id=decisions.remplace_id) FROM decisions WHERE id=@p0", id)[0];
        Text = $"Décision {d[0]}"; Width = 880; Height = 720; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false;
        var rt = Rtf.Reader(10.5f);
        Rtf.H(rt, $"{d[0]} — {d[1]}", 14f);
        Rtf.KV(rt, "Orientation", U.S(d[2])); Rtf.KV(rt, "Date", U.Aff(d[10])); Rtf.KV(rt, "Auteur", U.S(d[11])); Rtf.KV(rt, "Version", U.S(d[12]) + (U.S(d[17]) != "" ? $" (remplace {d[17]})" : ""));
        Rtf.KV(rt, "Statut", U.S(d[6])); Rtf.KV(rt, "Effet", U.S(d[7]) + (U.S(d[7]) == "En attente" ? $" — échéance prévue {U.Aff(d[8])}" : "")); Rtf.KV(rt, "Origine", U.S(d[16]));
        if (U.S(d[9]) != "") { Rtf.Sep(rt); Rtf.H(rt, "Résultat", 12f); Rtf.P(rt, U.S(d[9])); }
        Rtf.Sep(rt); Rtf.H(rt, "Contexte", 12f); Rtf.P(rt, U.S(d[3]));
        Rtf.Sep(rt); Rtf.H(rt, "Vérifications", 12f); Rtf.P(rt, U.S(d[4]));
        Rtf.Sep(rt); Rtf.H(rt, "Motivation", 12f); Rtf.P(rt, U.S(d[5]));
        Rtf.Sep(rt); Rtf.P(rt, U.Mention, true, Theme.Red);
        rt.SelectionStart = 0;
        var pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 12), BackColor = Color.White }; pad.Controls.Add(rt);
        var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.White, Padding = new Padding(8, 3, 8, 3) };
        long dos = U.L(d[13]), pid = U.L(d[14]), piece = U.L(d[15]);
        bar.Items.Add(Icons.Tool("dossier", "Ouvrir le dossier", () => DossierWindow.Open(dos)));
        if (piece != 0) bar.Items.Add(Icons.Tool("document", "Consulter l'acte", () => PieceViewer.Open(this, piece)));
        if (pid != 0) bar.Items.Add(Icons.Tool("personne", "Fiche de la personne", () => PersonneWindow.Open(this, pid)));
        if (Db.Q(c => Jud.Modifiable(c, id)))
        {
            bar.Items.Add(Icons.Tool("edit", "Modifier (nouvelle version)", () => { using var w = new DecisionWizard(dos, null, 0, id); if (w.ShowDialog(this) == DialogResult.OK) Close(); }));
            bar.Items.Add(Icons.Tool("cancel", "Retirer la décision", () => { var m = Ui.Prompt("Retirer la décision", "Motif du retrait :"); if (!string.IsNullOrWhiteSpace(m) && Ui.Try(() => Db.Tx(c => Jud.Retirer(c, id, m)))) Close(); }));
        }
        bar.Items.Add(Icons.Tool("print", "Imprimer", () => Export.Print($"Décision {d[0]}", rt.Text)));
        Controls.Add(pad); Controls.Add(bar);
    }
}
