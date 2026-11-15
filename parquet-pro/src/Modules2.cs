using System.Data;
using System.Text;

namespace ParquetPro;

// ============================================================ ORGANIGRAMME
public class OrganisationModule : Module
{
    public override string Titre => "Organigramme interactif";
    readonly TreeView tree = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f), HideSelection = false, AllowDrop = true, ItemHeight = 22, FullRowSelect = true };
    readonly ComboBox cMode = Ui.Combo(new[] { "Vue hiérarchique", "Vue par service" }, 160);
    readonly TextBox tSearch = new() { Width = 220, PlaceholderText = "Nom, fonction ou service…", Margin = new Padding(3, 6, 3, 3) };
    readonly ComboBox cDispo = Ui.Combo(new[] { "Tous", "Disponibles uniquement" }, 160);
    readonly TextBox detail = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.White, Font = new Font("Segoe UI", 9.5f), ScrollBars = ScrollBars.Vertical };
    readonly GridPanel gEff;

    public OrganisationModule()
    {
        tree.BeforeExpand += (s, e) => Expand(e.Node);
        tree.AfterSelect += (s, e) => ShowNode(e.Node);
        tree.NodeMouseDoubleClick += (s, e) => { if (e.Node.Tag is long id) new FicheCollab(id).Show(this); };
        tree.ItemDrag += (s, e) => { if (e.Item is TreeNode n && n.Tag is long) DoDragDrop(n, DragDropEffects.Move); };
        tree.DragOver += (s, e) => e.Effect = DragDropEffects.Move;
        tree.DragDrop += OnDrop;
        cMode.SelectedIndexChanged += (s, e) => Build();
        cDispo.SelectedIndexChanged += (s, e) => Build();
        tSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Find(); e.SuppressKeyPress = true; } };
        gEff = new GridPanel("org_eff") { Loader = (q, o, l) => Db.Table(@"SELECT s.id, s.nom AS 'Service', s.effectif_cible AS 'Effectif cible', COUNT(c.id) AS 'Effectif', SUM(c.dispo='Disponible') AS 'Disponibles', SUM(c.dispo='Absent') AS 'Absents', ROUND(AVG(c.charge)) AS 'Charge',
            (SELECT prenom||' '||nom FROM collaborateurs WHERE id=s.responsable_id) AS 'Responsable' FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id AND c.actif=1 WHERE s.nom LIKE @p0 GROUP BY s.id ORDER BY s.id LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        var bar = Ui.Bar(cMode, tSearch, Ui.Btn("Rechercher", (s, e) => Find()), cDispo, Ui.Btn("Tout déplier (service)", (s, e) => { tree.SelectedNode?.ExpandAll(); }),
            Ui.Btn("Ouvrir la fiche", (s, e) => { if (tree.SelectedNode?.Tag is long id) new FicheCollab(id).Show(this); }),
            Ui.Btn("Changer d'affectation", (s, e) => { if (tree.SelectedNode?.Tag is long id) { using var d = new AffectationDialog(id); if (d.ShowDialog() == DialogResult.OK) Build(); } }),
            Ui.Btn("Confier une mission", (s, e) => { if (tree.SelectedNode?.Tag is long id) { using var d = new MissionDialog(id); d.ShowDialog(); } }),
            new Label { Text = "Glisser-déposer un collaborateur sur un responsable pour le réaffecter.", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(10, 8, 0, 0) });
        var split = new SplitContainer { Dock = DockStyle.Fill };
        split.Panel1.Controls.Add(Card(tree, "Organisation — 300 collaborateurs"));
        var rs = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        rs.Panel1.Controls.Add(Card(detail, "Détail"));
        rs.Panel2.Controls.Add(Card(gEff, "Effectifs, disponibilités et charge par service"));
        split.Panel2.Controls.Add(rs);
        Controls.Add(split); Controls.Add(bar); Controls.Add(Ui.Title("Organigramme interactif"));
        Load += (s, e) => { try { split.SplitterDistance = split.Width / 2; rs.SplitterDistance = rs.Height * 45 / 100; } catch { } };
        Build();
    }

    public override bool AutoRefresh => false;

    string Where => cDispo.SelectedIndex == 1 ? " AND c.dispo='Disponible'" : "";

    TreeNode Node(object[] r)
    {
        long id = U.L(r[0]); long nb = U.L(r[4]);
        var n = new TreeNode($"{r[1]} — {r[2]}" + (nb > 0 ? $"  [{nb}]" : "") + $"   · {r[3]} · {r[5]} %") { Tag = id };
        n.ForeColor = U.S(r[3]) switch { "Absent" => Theme.Red, "En réunion" => Theme.Accent, "Hors horaires" => Color.Gray, _ => Color.Black };
        if (nb > 0 && cMode.SelectedIndex == 0) n.Nodes.Add(new TreeNode("…"));
        return n;
    }

    const string Cols = "c.id, c.nom||' '||c.prenom, c.fonction, c.dispo, (SELECT COUNT(*) FROM collaborateurs x WHERE x.superieur_id=c.id AND x.actif=1), c.charge";

    void Build()
    {
        tree.BeginUpdate(); tree.Nodes.Clear();
        if (cMode.SelectedIndex == 0)
        {
            foreach (var r in Db.Rows($"SELECT {Cols} FROM collaborateurs c WHERE c.superieur_id IS NULL AND c.actif=1")) { var n = Node(r); tree.Nodes.Add(n); Expand(n); n.Expand(); }
        }
        else
        {
            foreach (var s in Db.Rows("SELECT s.id, s.nom, COUNT(c.id) FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id AND c.actif=1 GROUP BY s.id ORDER BY s.id"))
            {
                var sn = new TreeNode($"{s[1]}  ({s[2]})") { Tag = "S" + s[0], NodeFont = Theme.FB, ForeColor = Theme.Navy };
                foreach (var r in Db.Rows($"SELECT {Cols} FROM collaborateurs c WHERE c.service_id=@p0 AND c.actif=1 {Where} ORDER BY c.niveau, c.nom", s[0])) sn.Nodes.Add(Node(r));
                tree.Nodes.Add(sn);
            }
        }
        tree.EndUpdate();
        gEff.Reload();
    }

    void Expand(TreeNode n)
    {
        if (n.Tag is not long id) return;
        if (n.Nodes.Count == 1 && n.Nodes[0].Tag == null)
        {
            n.Nodes.Clear();
            foreach (var r in Db.Rows($"SELECT {Cols} FROM collaborateurs c WHERE c.superieur_id=@p0 AND c.actif=1 {Where} ORDER BY c.niveau, c.nom", id)) n.Nodes.Add(Node(r));
        }
    }

    void ShowNode(TreeNode n)
    {
        if (n.Tag is long id)
        {
            var x = Db.Rows(@"SELECT c.prenom||' '||c.nom, c.fonction, s.nom, (SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=c.superieur_id), c.competences, c.profil, c.autonomie, c.horaire_debut, c.horaire_fin, c.charge, c.dispo, c.telephone, c.email, c.matricule,
                (SELECT COUNT(*) FROM missions WHERE destinataire_id=c.id AND statut IN ('Nouvelle','En analyse','En cours','À modifier')), (SELECT COUNT(*) FROM collaborateurs WHERE superieur_id=c.id)
                FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.id=@p0", id)[0];
            detail.Text = $"{x[0]}\r\n{x[1]}\r\n\r\nService : {x[2]}\r\nRend compte à : {x[3]}\r\nPersonnes encadrées : {x[15]}\r\nMatricule : {x[13]}\r\n\r\nCompétences : {x[4]}\r\nProfil : {x[5]}\r\nAutonomie : niveau {x[6]}\r\nHoraires : {x[7]}h – {x[8]}h\r\nDisponibilité : {x[10]}\r\nCharge de travail : {x[9]} %\r\nMissions en cours : {x[14]}\r\n\r\n{x[11]} — {x[12]}";
        }
        else if (n.Tag is string s && s.StartsWith("S"))
        {
            long sid = long.Parse(s[1..]);
            var x = Db.Rows("SELECT s.nom, s.categorie, s.effectif_cible, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=s.responsable_id), COUNT(c.id), ROUND(AVG(c.charge)), SUM(c.dispo='Disponible') FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id WHERE s.id=@p0 GROUP BY s.id", sid)[0];
            detail.Text = $"{x[0]}\r\nCatégorie : {x[1]}\r\nResponsable : {x[3]}\r\nEffectif : {x[4]} (cible {x[2]})\r\nDisponibles : {x[6]}\r\nCharge moyenne : {x[5]} %";
        }
    }

    void Find()
    {
        var q = tSearch.Text.Trim(); if (q == "") return;
        var r = Db.Rows("SELECT c.id FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.actif=1 AND (c.nom LIKE @p0 OR c.prenom LIKE @p0 OR c.fonction LIKE @p0 OR s.nom LIKE @p0 OR c.prenom||' '||c.nom LIKE @p0) ORDER BY c.niveau LIMIT 1", U.Like(q));
        if (r.Count == 0) { Ui.Info("Aucun collaborateur trouvé."); return; }
        long id = U.L(r[0][0]);
        if (cMode.SelectedIndex == 1)
        {
            foreach (TreeNode sn in tree.Nodes) foreach (TreeNode n in sn.Nodes) if (n.Tag is long x && x == id) { sn.Expand(); tree.SelectedNode = n; n.EnsureVisible(); tree.Focus(); return; }
            return;
        }
        var chain = new List<long>(); long cur = id; int g = 0;
        while (cur != 0 && g++ < 15) { chain.Insert(0, cur); cur = Db.Long("SELECT IFNULL(superieur_id,0) FROM collaborateurs WHERE id=@p0", cur); }
        TreeNodeCollection col = tree.Nodes; TreeNode found = null;
        foreach (var cid in chain)
        {
            found = col.Cast<TreeNode>().FirstOrDefault(n => n.Tag is long t && t == cid);
            if (found == null) break;
            Expand(found); if (cid != id) found.Expand();
            col = found.Nodes;
        }
        if (found != null) { tree.SelectedNode = found; found.EnsureVisible(); tree.Focus(); }
    }

    void OnDrop(object s, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode src || src.Tag is not long cid) return;
        var target = tree.GetNodeAt(tree.PointToClient(new Point(e.X, e.Y)));
        if (target == null || target == src) return;
        long svc, sup;
        if (target.Tag is long tid) { sup = tid; svc = Db.Long("SELECT service_id FROM collaborateurs WHERE id=@p0", tid); }
        else if (target.Tag is string ts && ts.StartsWith("S")) { svc = long.Parse(ts[1..]); sup = Db.Long("SELECT responsable_id FROM services WHERE id=@p0", svc); }
        else return;
        string msg = $"Réaffecter {Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", cid)} sous l'autorité de {Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", sup)} ({Db.Str("SELECT nom FROM services WHERE id=@p0", svc)}) ?";
        if (!Ui.Confirm(msg)) return;
        if (Ui.Try(() => Db.Tx(c => Biz.Affecter(c, cid, svc, sup, "Déplacement dans l'organigramme")))) Build();
    }

    public override void RefreshData() => gEff.Reload();
}

// ============================================================ PERSONNEL / RH
public class PersonnelModule : Module
{
    public override string Titre => "Personnel et ressources humaines";
    readonly GridPanel gAnn, gCharge, gAbs, gHist;
    readonly ComboBox cSvc = Ui.Combo(Item.Services(true), 240);
    readonly ComboBox cDispo = Ui.Combo(new[] { "(Toutes disponibilités)", "Disponible", "En réunion", "Absent", "Hors horaires" }, 170);
    readonly ComboBox cNiv = Ui.Combo(new[] { "(Tous niveaux)", "Direction (1-3)", "Responsables (4-6)", "Collaborateurs (7)", "Assistants (8)" }, 170);
    readonly Kpi kEff, kDisp, kAbs, kMis, kCh, kRes;
    readonly Chart chEff = new() { Type = Chart.Kind.HBars, Title = "Effectifs par service" }, chCh = new() { Type = Chart.Kind.HBars, Title = "Charge moyenne par service (%)" }, chDisp = new() { Type = Chart.Kind.Pie, Title = "Disponibilités" };
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };

    public PersonnelModule()
    {
        gAnn = new GridPanel("rh_annuaire") { Loader = (q, o, l) =>
        {
            var w = new List<string> { "c.actif=1" };
            if (Item.Of(cSvc) != 0) w.Add("c.service_id=" + Item.Of(cSvc));
            if (cDispo.SelectedIndex > 0) w.Add("c.dispo=@p4");
            w.Add(cNiv.SelectedIndex switch { 1 => "c.niveau<=3", 2 => "c.niveau BETWEEN 4 AND 6", 3 => "c.niveau=7", 4 => "c.niveau=8", _ => "1=1" });
            return Db.Table($@"SELECT c.id, c.matricule AS 'Matricule', c.nom AS 'Nom', c.prenom AS 'Prénom', c.fonction AS 'Fonction', s.code AS 'Service', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=c.superieur_id) AS 'Supérieur',
                c.niveau AS 'Niveau', c.dispo AS 'Dispo', c.charge AS 'Charge', (SELECT COUNT(*) FROM missions WHERE destinataire_id=c.id AND statut IN {Biz.SqlActifs}) AS 'Missions', c.profil AS 'Profil', c.autonomie AS 'Autonomie',
                c.horaire_debut||'h-'||c.horaire_fin||'h' AS 'Horaires', c.competences AS 'Compétences', c.telephone AS 'Téléphone', c.email AS 'Courriel'
                FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE {string.Join(" AND ", w)} AND (c.nom LIKE @p0 OR c.prenom LIKE @p0 OR c.fonction LIKE @p0 OR c.competences LIKE @p0 OR c.matricule LIKE @p0)
                ORDER BY c.service_id, c.niveau, c.nom LIMIT @p1 OFFSET @p2", U.Like(q), l, o, 0, cDispo.Text);
        } };
        gAnn.Activated += id => new FicheCollab(id).Show(this);
        foreach (var c in new[] { cSvc, cDispo, cNiv }) c.SelectedIndexChanged += (s, e) => { gAnn.ResetPage(); gAnn.Reload(); };
        gCharge = new GridPanel("rh_charge") { Loader = (q, o, l) => Db.Table($@"SELECT s.id, s.nom AS 'Service', COUNT(c.id) AS 'Effectif', SUM(c.dispo='Disponible') AS 'Disponibles', SUM(c.dispo='Absent') AS 'Absents',
            (SELECT COUNT(*) FROM missions m WHERE m.service_id=s.id AND m.statut IN {Biz.SqlActifs}) AS 'Missions en cours', ROUND(AVG(c.charge)) AS 'Charge', MAX(c.charge) AS 'Charge max',
            (SELECT COUNT(*) FROM evenements e WHERE e.service_id=s.id AND e.date>=@p3) AS 'Activité 7 j'
            FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id AND c.actif=1 WHERE s.nom LIKE @p0 GROUP BY s.id ORDER BY AVG(c.charge) DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, U.D(Engine.Now.AddDays(-7))) };
        gAbs = new GridPanel("rh_abs") { Loader = (q, o, l) => Db.Table("SELECT c.id, c.nom||' '||c.prenom AS 'Collaborateur', c.fonction AS 'Fonction', s.nom AS 'Service', c.dispo AS 'Dispo' FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.dispo='Absent' AND c.nom LIKE @p0 LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gAbs.Activated += id => new FicheCollab(id).Show(this);
        gHist = new GridPanel("rh_hist") { Loader = (q, o, l) => Db.Table(@"SELECT h.id, substr(h.date,1,16) AS 'Date', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=h.collab_id) AS 'Collaborateur', (SELECT nom FROM services WHERE id=h.ancien_service) AS 'Ancien service',
            (SELECT nom FROM services WHERE id=h.nouveau_service) AS 'Nouveau service', h.motif AS 'Motif', h.auteur AS 'Auteur' FROM affectations_hist h WHERE IFNULL(h.motif,'') LIKE @p0 ORDER BY h.id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        void Tab(string t, Control c) { var tp = new TabPage(t); tp.Controls.Add(c); tabs.TabPages.Add(tp); }
        var pAnn = new Panel { Dock = DockStyle.Fill };
        pAnn.Controls.Add(gAnn);
        pAnn.Controls.Add(Ui.Bar(new Label { Text = "Filtres :", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }, cSvc, cDispo, cNiv,
            Ui.PrimaryBtn("Fiche individuelle", (s, e) => { if (gAnn.SelectedId != 0) new FicheCollab(gAnn.SelectedId).Show(this); }),
            Ui.Btn("Affectation…", (s, e) => { if (gAnn.SelectedId != 0) { using var d = new AffectationDialog(gAnn.SelectedId); if (d.ShowDialog() == DialogResult.OK) RefreshData(); } }),
            Ui.Btn("Déclarer absent", (s, e) => SetDispo("Absent")), Ui.Btn("Déclarer présent", (s, e) => SetDispo("Disponible")),
            Ui.Btn("Confier une mission", (s, e) => { if (gAnn.SelectedId != 0) { using var d = new MissionDialog(gAnn.SelectedId); d.ShowDialog(); } })));
        Tab("Annuaire du personnel", pAnn);
        var ind = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        for (int i = 0; i < 3; i++) ind.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        ind.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); ind.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var kp = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        kEff = new Kpi("Effectif total", Theme.Blue); kDisp = new Kpi("Disponibles", Theme.Green); kAbs = new Kpi("Absents", Theme.Red); kMis = new Kpi("Missions en cours", Theme.Accent); kCh = new Kpi("Charge moyenne (%)", Theme.Orange); kRes = new Kpi("Responsables", Theme.Palette[3]);
        kp.Controls.AddRange(new Control[] { kEff, kDisp, kAbs, kMis, kCh, kRes });
        ind.Controls.Add(kp, 0, 0); ind.SetColumnSpan(kp, 3);
        ind.Controls.Add(Card(chEff), 0, 1); ind.Controls.Add(Card(chCh), 1, 1); ind.Controls.Add(Card(chDisp), 2, 1);
        Tab("Indicateurs", ind);
        Tab("Charge et répartition du travail", gCharge);
        Tab("Absences du jour", gAbs);
        Tab("Évolution des affectations", gHist);
        tabs.SelectedIndexChanged += (s, e) => RefreshData();
        Controls.Add(tabs); Controls.Add(Ui.Title("Personnel et ressources humaines — 300 collaborateurs"));
    }

    void SetDispo(string d)
    {
        long id = gAnn.SelectedId; if (id == 0) return;
        if (Ui.Try(() => Db.Tx(c =>
        {
            Auth.Check("personnel.modifier");
            c.Exec("UPDATE collaborateurs SET dispo=@p1 WHERE id=@p0", id, d);
            Biz.Admin(c, "Disponibilité", $"{Biz.NameF(c, id)} : {d}");
            Biz.Event(c, d == "Absent" ? "Absence" : "Retour", "Ressources humaines", 0, $"{Biz.NameF(c, id)} déclaré(e) {d.ToLower()}", "collaborateur", id);
        }))) RefreshData();
    }

    public override void RefreshData()
    {
        switch (tabs.SelectedIndex)
        {
            case 0: gAnn.Reload(); break;
            case 1:
                var r = Db.Rows($"SELECT COUNT(*), SUM(dispo='Disponible'), SUM(dispo='Absent'), (SELECT COUNT(*) FROM missions WHERE statut IN {Biz.SqlActifs}), ROUND(AVG(charge)), SUM(niveau<=6) FROM collaborateurs WHERE actif=1")[0];
                kEff.Set(r[0]); kDisp.Set(r[1]); kAbs.Set(r[2]); kMis.Set(r[3]); kCh.Set(r[4]); kRes.Set(r[5]);
                var e = Db.Rows("SELECT s.code||' — '||s.nom, COUNT(c.id), AVG(c.charge) FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id AND c.actif=1 GROUP BY s.id ORDER BY s.id");
                chEff.Set(e.Select(x => U.S(x[0])).ToArray(), ("Effectif", e.Select(x => U.Dv(x[1])).ToArray()));
                chCh.Set(e.Select(x => U.S(x[0])).ToArray(), ("Charge", e.Select(x => U.Dv(x[2])).ToArray()));
                var d = Db.Rows("SELECT dispo, COUNT(*) FROM collaborateurs WHERE actif=1 GROUP BY dispo");
                chDisp.Set(d.Select(x => U.S(x[0])).ToArray(), ("Dispo", d.Select(x => U.Dv(x[1])).ToArray()));
                break;
            case 2: gCharge.Reload(); break;
            case 3: gAbs.Reload(); break;
            case 4: gHist.Reload(); break;
        }
    }
}

// ============================================================ UNIVERS INSTITUTIONNEL
public class UniversModule : Module
{
    public override string Titre => "Univers institutionnel";
    readonly GridPanel gOrg, gInt, gEch, gCom, gApp, gDos;
    readonly ComboBox cCat = Ui.Combo(new[] { "(Toutes catégories)", "Service d'enquête", "Tribunal", "Institution", "Administration partenaire", "Interlocuteur extérieur", "Structure de coordination" }, 220);
    readonly TextBox fiche = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Top, Height = 120, BackColor = Color.White, ScrollBars = ScrollBars.Vertical };
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };

    public UniversModule()
    {
        gOrg = new GridPanel("orgs") { Loader = (q, o, l) => Db.Table("SELECT id, nom AS 'Organisme', categorie AS 'Catégorie', activite AS 'Activité', (SELECT COUNT(*) FROM dossiers WHERE organisme_id=organismes.id) AS 'Dossiers', telephone AS 'Téléphone' FROM organismes WHERE (@p3='(Toutes catégories)' OR categorie=@p3) AND nom LIKE @p0 ORDER BY categorie, nom LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cCat.Text) };
        gOrg.SelChanged += Detail;
        cCat.SelectedIndexChanged += (s, e) => gOrg.Reload();
        long Sel() => gOrg.SelectedId;
        gInt = new GridPanel("org_int") { Loader = (q, o, l) => Db.Table("SELECT id, prenom||' '||nom AS 'Interlocuteur', fonction AS 'Fonction', telephone AS 'Téléphone', email AS 'Courriel' FROM interlocuteurs WHERE organisme_id=@p0 AND nom LIKE @p1 LIMIT @p2 OFFSET @p3", Sel(), U.Like(q), l, o) };
        gInt.Activated += id => { using var d = new AppelDialog(0, id); d.ShowDialog(); };
        gEch = new GridPanel("org_ech") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', type AS 'Type', description AS 'Description', (SELECT nom FROM services WHERE id=service_id) AS 'Service' FROM evenements WHERE organisme_id=@p0 AND description LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", Sel(), U.Like(q), l, o) };
        gCom = new GridPanel("org_com") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', exp_ext AS 'De', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=dest_id) AS 'À', objet AS 'Objet' FROM messages WHERE organisme_id=@p0 AND objet LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", Sel(), U.Like(q), l, o) };
        gCom.Activated += id => MessageViewer.Open(this, id);
        gApp = new GridPanel("org_app") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', sens AS 'Sens', statut AS 'Statut', interlocuteur AS 'Interlocuteur', motif AS 'Motif' FROM appels WHERE organisme_id=@p0 AND motif LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", Sel(), U.Like(q), l, o) };
        gDos = new GridPanel("org_dos") { Loader = (q, o, l) => Db.Table("SELECT id, numero AS 'N°', intitule AS 'Intitulé', statut AS 'Statut', avancement AS 'Avancement' FROM dossiers WHERE organisme_id=@p0 AND intitule LIKE @p1 ORDER BY id DESC LIMIT @p2 OFFSET @p3", Sel(), U.Like(q), l, o) };
        gDos.Activated += id => MainForm.Instance.OpenRef("dossier", id);
        void Tab(string t, Control c) { var tp = new TabPage(t); tp.Controls.Add(c); tabs.TabPages.Add(tp); }
        var pf = new Panel { Dock = DockStyle.Fill }; pf.Controls.Add(gInt); pf.Controls.Add(fiche);
        Tab("Fiche et interlocuteurs", pf); Tab("Échanges et événements", gEch); Tab("Communications", gCom); Tab("Appels", gApp); Tab("Dossiers liés", gDos);
        tabs.SelectedIndexChanged += (s, e) => Detail();
        var bar = Ui.Bar(cCat,
            Ui.PrimaryBtn("☎ Appeler l'interlocuteur", (s, e) => { if (gInt.SelectedId != 0) { using var d = new AppelDialog(0, gInt.SelectedId); d.ShowDialog(); } else Ui.Info("Sélectionnez un interlocuteur dans la fiche."); }),
            Ui.Btn("Adresser un courrier de simulation", (s, e) => Courrier()),
            Ui.Btn("Ajouter un interlocuteur", (s, e) => Ui.Try(() =>
            {
                long org = Sel(); if (org == 0) return;
                Auth.Check("admin");
                var n = Ui.Prompt("Interlocuteur", "Prénom Nom — Fonction :", "Prénom NOM — Fonction"); if (string.IsNullOrWhiteSpace(n)) return;
                var parts = n.Split('—'); var pn = parts[0].Trim().Split(' ', 2);
                Db.Exec("INSERT INTO interlocuteurs(organisme_id,prenom,nom,fonction,telephone) VALUES(@p0,@p1,@p2,@p3,'01 00 00 00 00')", org, pn[0], pn.Length > 1 ? pn[1] : "", parts.Length > 1 ? parts[1].Trim() : "Interlocuteur");
                Detail();
            })),
            Ui.Btn("Nouvel organisme", (s, e) => Ui.Try(() =>
            {
                Auth.Check("admin");
                var n = Ui.Prompt("Organisme", "Nom de l'organisme (simulé) :"); if (string.IsNullOrWhiteSpace(n)) return;
                Db.Exec("INSERT INTO organismes(nom,categorie,description) VALUES(@p0,@p1,'Organisme simulé ajouté par l''administration.')", n, cCat.SelectedIndex > 0 ? cCat.Text : "Interlocuteur extérieur");
                gOrg.Reload();
            })));
        var split = new SplitContainer { Dock = DockStyle.Fill };
        split.Panel1.Controls.Add(gOrg); split.Panel2.Controls.Add(tabs);
        Controls.Add(split); Controls.Add(bar); Controls.Add(Ui.Title("Univers institutionnel — organismes et interlocuteurs simulés"));
        Load += (s, e) => { try { split.SplitterDistance = split.Width * 45 / 100; } catch { } };
    }

    void Courrier()
    {
        long org = gOrg.SelectedId; if (org == 0) { Ui.Info("Sélectionnez un organisme."); return; }
        string nom = Db.Str("SELECT nom FROM organismes WHERE id=@p0", org);
        using var d = new DocumentDialog(0, "Courriers de simulation", "Courrier à " + nom, $"{U.Mention}\n\nÀ l'attention de : {nom}\n\nObjet : \n\nMadame, Monsieur,\n\n\n\nLe procureur de la République (simulation)");
        if (d.ShowDialog() == DialogResult.OK)
        {
            Db.Tx(c => { Biz.Event(c, "Courrier adressé", Biz.Name(c, Auth.CollabId), Auth.CollabId, "Courrier de simulation adressé à " + nom, "organisme", org, orgId: org); c.Exec("UPDATE organismes SET activite=activite+1 WHERE id=@p0", org); });
            Detail();
        }
    }

    void Detail()
    {
        long id = gOrg.SelectedId; if (id == 0) return;
        var o = Db.Rows("SELECT nom, categorie, description, adresse, telephone, activite FROM organismes WHERE id=@p0", id)[0];
        fiche.Text = $"{o[0]}\r\nCatégorie : {o[1]}\r\n{o[2]}\r\nAdresse : {o[3]}\r\nTéléphone : {o[4]}\r\nVolume d'activité enregistré : {o[5]}\r\n\r\nÉchanges et communications : éléments de simulation sans valeur officielle.";
        switch (tabs.SelectedIndex) { case 0: gInt.Reload(); break; case 1: gEch.Reload(); break; case 2: gCom.Reload(); break; case 3: gApp.Reload(); break; case 4: gDos.Reload(); break; }
    }

    public void Focus(long id) { gOrg.Search.Text = Db.Str("SELECT nom FROM organismes WHERE id=@p0", id) ?? ""; cCat.SelectedIndex = 0; gOrg.Reload(); Detail(); }
    public override void RefreshData() { gOrg.Reload(); Detail(); }
}

// ============================================================ DOCUMENTS
public class DocumentsModule : Module
{
    public override string Titre => "Documents";
    readonly ListBox cats = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), IntegralHeight = false, ItemHeight = 22 };
    readonly GridPanel grid;
    readonly TextBox preview = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9f), BackColor = Color.White, WordWrap = false };

    public DocumentsModule()
    {
        cats.Items.Add("Tous les documents");
        foreach (var c in DocumentDialog.Categories) cats.Items.Add(c);
        cats.Items.Add("Documents archivés");
        cats.SelectedIndex = 0;
        cats.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        grid = new GridPanel("documents") { Loader = (q, o, l) =>
        {
            int i = cats.SelectedIndex;
            string w = i == 0 ? "d.archive=0" : i == cats.Items.Count - 1 ? "d.archive=1" : "d.archive=0 AND d.categorie=@p3";
            return Db.Table($@"SELECT d.id, d.numero AS 'N°', d.titre AS 'Titre', d.categorie AS 'Catégorie', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=d.auteur_id) AS 'Auteur', substr(d.date,1,16) AS 'Date', d.version AS 'Version',
                (SELECT numero FROM dossiers WHERE id=d.dossier_id) AS 'Dossier', (SELECT numero FROM missions WHERE id=d.mission_id) AS 'Mission'
                FROM documents d WHERE {w} AND (d.titre LIKE @p0 OR d.contenu LIKE @p0 OR d.numero LIKE @p0) ORDER BY d.date DESC, d.id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cats.Text);
        } };
        grid.SelChanged += () => { long id = grid.SelectedId; preview.Text = id == 0 ? "" : (U.Mention + "\r\n\r\n" + (Db.Str("SELECT contenu FROM documents WHERE id=@p0", id) ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n")); };
        grid.Activated += id => Edit(id);
        var bar = Ui.Bar(Ui.PrimaryBtn("❐ Nouveau document", (s, e) => Nouveau()), Ui.Btn("Modifier / associer", (s, e) => Edit(grid.SelectedId)),
            Ui.Btn("Exporter PDF", (s, e) => Sel(id => { var d = Db.Rows("SELECT titre, contenu FROM documents WHERE id=@p0", id)[0]; Export.PdfOpen(U.S(d[0]), U.S(d[1])); })),
            Ui.Btn("Imprimer", (s, e) => Sel(id => { var d = Db.Rows("SELECT titre, contenu FROM documents WHERE id=@p0", id)[0]; Export.Print(U.S(d[0]), U.S(d[1])); })),
            Ui.Btn("Archiver / désarchiver", (s, e) => Sel(id => { Auth.Check("documents.gerer"); Db.Exec("UPDATE documents SET archive=1-archive WHERE id=@p0", id); })),
            Ui.Btn("Dupliquer", (s, e) => Sel(id => { Auth.Check("documents.gerer"); var d = Db.Rows("SELECT titre, categorie, contenu, dossier_id, mission_id FROM documents WHERE id=@p0", id)[0]; Db.Tx(c => Biz.EnregistrerDocument(c, 0, "Copie de " + d[0], U.S(d[1]), U.S(d[2]), U.L(d[3]), U.L(d[4]))); })));
        var left = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Color.White, Padding = new Padding(4) };
        left.Controls.Add(cats); left.Controls.Add(new Label { Text = "Catégories", Dock = DockStyle.Top, Height = 26, Font = Theme.FB, ForeColor = Theme.Navy });
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(grid); split.Panel2.Controls.Add(Card(preview, "Prévisualisation"));
        Controls.Add(split); Controls.Add(left); Controls.Add(bar); Controls.Add(Ui.Title("Bibliothèque documentaire"));
        Load += (s, e) => { try { split.SplitterDistance = split.Height / 2; } catch { } };
    }

    void Sel(Action<long> a) { long id = grid.SelectedId; if (id == 0) { Ui.Info("Sélectionnez un document."); return; } if (Ui.Try(() => a(id))) grid.Reload(); }
    void Edit(long id) { if (id == 0) return; using var d = new DocumentDialog(id); if (d.ShowDialog() == DialogResult.OK) grid.Reload(); }
    public override void Nouveau() { using var d = new DocumentDialog(0, cats.SelectedIndex > 0 && cats.SelectedIndex < cats.Items.Count - 1 ? cats.Text : "Notes internes"); if (d.ShowDialog() == DialogResult.OK) grid.Reload(); }
    public override void RefreshData() => grid.Reload();
}

// ============================================================ STATISTIQUES
public class StatsModule : Module
{
    public override string Titre => "Statistiques et pilotage";
    readonly ComboBox cPer = Ui.Combo(new[] { "7 derniers jours", "30 derniers jours", "90 derniers jours", "12 derniers mois" }, 170);
    readonly ComboBox cSvc = Ui.Combo(Item.Services(true), 260);
    readonly ComboBox cCat = Ui.Combo(new[] { "(Toutes catégories de dossiers)" }.Concat(Seed.Categories), 230);
    readonly Kpi[] k;
    readonly Chart c1 = new() { Type = Chart.Kind.Line, Title = "Évolution de l'activité (événements)" }, c2 = new() { Type = Chart.Kind.Bars, Title = "Missions créées / terminées" },
        c3 = new() { Type = Chart.Kind.HBars, Title = "Activité par service (missions)" }, c4 = new() { Type = Chart.Kind.Line, Title = "Évolution des dossiers (ouverts / clôturés)" },
        c5 = new() { Type = Chart.Kind.Pie, Title = "Répartition des priorités (missions actives)" }, c6 = new() { Type = Chart.Kind.HBars, Title = "Temps de traitement interne moyen (heures)" },
        c7 = new() { Type = Chart.Kind.Bars, Title = "Charge des collaborateurs (répartition)" }, c8 = new() { Type = Chart.Kind.Pie, Title = "Répartition des missions par statut" };
    readonly GridPanel gTab;

    public override bool AutoRefresh => false;

    public StatsModule()
    {
        string[] noms = { "Dossiers ouverts", "Dossiers clôturés (période)", "Missions actives", "Missions terminées (période)", "Retards", "Délai moyen interne (h)", "Messages (période)", "Réunions (période)", "Événements (période)" };
        var kp = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 86, WrapContents = false, AutoScroll = true };
        k = noms.Select((n, i) => new Kpi(n, Theme.Palette[i % Theme.Palette.Length])).ToArray();
        kp.Controls.AddRange(k);
        foreach (var c in new[] { cPer, cSvc, cCat }) c.SelectedIndexChanged += (s, e) => RefreshData();
        cPer.SelectedIndex = 1;
        gTab = new GridPanel("stats_tab") { Loader = (q, o, l) => Db.Table($@"SELECT s.id, s.code AS 'Service', COUNT(DISTINCT c.id) AS 'Effectif', ROUND(AVG(c.charge)) AS 'Charge',
            (SELECT COUNT(*) FROM missions m WHERE m.service_id=s.id AND m.statut IN {Biz.SqlActifs}) AS 'Missions actives',
            (SELECT COUNT(*) FROM missions m WHERE m.service_id=s.id AND m.date_rapport>=@p3) AS 'Terminées',
            (SELECT COUNT(*) FROM missions m WHERE m.service_id=s.id AND m.statut IN {Biz.SqlActifs} AND m.echeance<@p4) AS 'Retards',
            (SELECT COUNT(*) FROM dossiers d WHERE d.service_id=s.id AND d.statut<>'Clôturé' AND d.archive=0) AS 'Dossiers actifs'
            FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id WHERE s.nom LIKE @p0 GROUP BY s.id ORDER BY s.id LIMIT @p1 OFFSET @p2", U.Like(q), l, o, U.D(Start), U.D(Engine.Now)) };
        var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, AutoScroll = true };
        for (int i = 0; i < 3; i++) { tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f)); }
        var charts = new Control[] { c1, c2, c3, c4, c5, c6, c7, c8 };
        for (int i = 0; i < charts.Length; i++) tl.Controls.Add(Card(charts[i]), i % 3, i / 3);
        tl.Controls.Add(Card(gTab, "Indicateurs par service"), 2, 2);
        var bar = Ui.Bar(new Label { Text = "Période :", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }, cPer, new Label { Text = "Service :", AutoSize = true, Padding = new Padding(8, 8, 0, 0) }, cSvc, cCat,
            Ui.PrimaryBtn("⟳ Actualiser", (s, e) => RefreshData()),
            Ui.Btn("Générer le rapport de pilotage", (s, e) => Ui.Try(() =>
            {
                var sb = new StringBuilder($"RAPPORT DE PILOTAGE — {cPer.Text} — {cSvc.Text}\n{U.Mention}\n\n");
                for (int i = 0; i < k.Length; i++) sb.AppendLine($"{noms[i]} : {k[i].Controls.OfType<Label>().First(l => l.Font.Size > 15).Text}");
                sb.AppendLine("\n" + Db.Q(c => Biz.Synthese(c)));
                Db.Tx(c => Biz.EnregistrerDocument(c, 0, $"Rapport de pilotage — {Engine.Now:dd/MM/yyyy}", "Rapports d'activité", sb.ToString(), 0, 0));
                Ui.ShowText("Rapport de pilotage", sb.ToString());
            })));
        Controls.Add(tl); Controls.Add(kp); Controls.Add(bar); Controls.Add(Ui.Title("Statistiques et tableaux de bord de pilotage"));
    }

    int Days => cPer.SelectedIndex switch { 0 => 7, 1 => 30, 2 => 90, _ => 365 };
    DateTime Start => Engine.Now.Date.AddDays(-Days + 1);

    public override void RefreshData()
    {
        try
        {
            string st = U.D(Start), now = U.D(Engine.Now);
            long svc = Item.Of(cSvc);
            string ms = svc == 0 ? "" : $" AND service_id={svc}";
            string dc = cCat.SelectedIndex > 0 ? " AND categorie=@p2" : "";
            var r = Db.Rows($@"SELECT (SELECT COUNT(*) FROM dossiers WHERE statut<>'Clôturé' AND archive=0{ms}{dc}), (SELECT COUNT(*) FROM dossiers WHERE date_cloture>=@p0{ms}{dc}),
                (SELECT COUNT(*) FROM missions WHERE statut IN {Biz.SqlActifs}{ms}), (SELECT COUNT(*) FROM missions WHERE date_rapport>=@p0{ms}),
                (SELECT COUNT(*) FROM missions WHERE statut IN {Biz.SqlActifs} AND echeance<@p1{ms}), (SELECT ROUND(AVG((julianday(date_rapport)-julianday(date_creation))*24),1) FROM missions WHERE date_rapport>=@p0{ms}),
                (SELECT COUNT(*) FROM messages WHERE date>=@p0), (SELECT COUNT(*) FROM reunions WHERE debut>=@p0 AND debut<=@p1), (SELECT COUNT(*) FROM evenements WHERE date>=@p0{(svc == 0 ? "" : $" AND service_id={svc}")})", st, now, cCat.Text)[0];
            for (int i = 0; i < k.Length; i++) k[i].Set(r[i] ?? 0);
            bool week = Days > 60;
            string key = week ? "strftime('%Y-S%W', {0})" : "substr({0},1,10)";
            var labels = new List<string>();
            if (week) { for (var d = Start; d <= Engine.Now.Date; d = d.AddDays(7)) labels.Add(d.ToString("yyyy-'S'") + System.Globalization.ISOWeek.GetWeekOfYear(d).ToString("00")); }
            else for (var d = Start; d <= Engine.Now.Date; d = d.AddDays(1)) labels.Add(d.ToString("yyyy-MM-dd"));
            if (week) labels = Db.Rows($"SELECT DISTINCT {string.Format(key, "date")} FROM evenements WHERE date>=@p0 ORDER BY 1", st).Select(x => U.S(x[0])).ToList();
            double[] Series(string table, string col, string extra = "")
            {
                var map = Db.Rows($"SELECT {string.Format(key, col)}, COUNT(*) FROM {table} WHERE {col}>=@p0 {extra} GROUP BY 1", st, now, cCat.Text).ToDictionary(x => U.S(x[0]), x => U.Dv(x[1]));
                return labels.Select(l => map.TryGetValue(l, out var v) ? v : 0).ToArray();
            }
            string[] short_ = labels.Select(l => week ? l[5..] : l[8..] + "/" + l[5..7]).ToArray();
            c1.Set(short_, ("Événements", Series("evenements", "date", svc == 0 ? "" : $" AND service_id={svc}")));
            c2.Set(short_, ("Créées", Series("missions", "date_creation", ms)), ("Terminées", Series("missions", "date_rapport", ms)));
            c4.Set(short_, ("Ouverts", Series("dossiers", "date_ouverture", ms + dc)), ("Clôturés", Series("dossiers", "date_cloture", ms + dc)));
            var a3 = Db.Rows("SELECT s.code, COUNT(m.id) FROM services s LEFT JOIN missions m ON m.service_id=s.id AND m.date_creation>=@p0 GROUP BY s.id ORDER BY COUNT(m.id) DESC", st);
            c3.Set(a3.Select(x => U.S(x[0])).ToArray(), ("Missions", a3.Select(x => U.Dv(x[1])).ToArray()));
            var a5 = Db.Rows($"SELECT priorite, COUNT(*) FROM missions WHERE statut IN {Biz.SqlActifs}{ms} GROUP BY priorite");
            c5.Set(a5.Select(x => U.S(x[0])).ToArray(), ("Priorités", a5.Select(x => U.Dv(x[1])).ToArray()));
            var a6 = Db.Rows("SELECT s.code, ROUND(AVG((julianday(m.date_rapport)-julianday(m.date_creation))*24),1) FROM services s JOIN missions m ON m.service_id=s.id WHERE m.date_rapport>=@p0 GROUP BY s.id ORDER BY 2 DESC", st);
            c6.Set(a6.Select(x => U.S(x[0])).ToArray(), ("Heures", a6.Select(x => U.Dv(x[1])).ToArray()));
            var a7 = Db.Rows($"SELECT (charge/20)*20, COUNT(*) FROM collaborateurs WHERE actif=1{ms} GROUP BY 1 ORDER BY 1");
            c7.Set(a7.Select(x => $"{U.L(x[0])}-{Math.Min(100, U.L(x[0]) + 19)}%").ToArray(), ("Collaborateurs", a7.Select(x => U.Dv(x[1])).ToArray()));
            var a8 = Db.Rows($"SELECT statut, COUNT(*) FROM missions WHERE date_creation>=@p0{ms} GROUP BY statut", st);
            c8.Set(a8.Select(x => U.S(x[0])).ToArray(), ("Statuts", a8.Select(x => U.Dv(x[1])).ToArray()));
            gTab.Reload();
        }
        catch (Exception e) { Log.Error(e); }
    }
}

// ============================================================ NOTIFICATIONS
public class NotificationsModule : Module
{
    public override string Titre => "Centre de notifications";
    public static readonly string[] Types = { "Nouveau message", "Nouvelle mission", "Rapport terminé", "Mission en retard", "Demande de validation", "Réunion proposée", "Réunion imminente", "Nouveau dossier", "Difficulté signalée", "Rapport de service disponible", "Événement institutionnel", "Appel", "Alerte technique" };
    readonly GridPanel grid;
    readonly ComboBox cEtat = Ui.Combo(new[] { "Non lues", "Toutes (hors archives)", "Archivées" }, 170);
    readonly ComboBox cType = Ui.Combo(new[] { "(Tous les types)" }.Concat(Types), 220);
    readonly ComboBox cImp = Ui.Combo(new[] { "(Toutes importances)", "Haute", "Normale", "Basse" }, 160);

    public NotificationsModule()
    {
        grid = new GridPanel("notifs") { Loader = (q, o, l) =>
        {
            string w = cEtat.SelectedIndex switch { 0 => "lu=0 AND archive=0", 1 => "archive=0", _ => "archive=1" };
            if (cType.SelectedIndex > 0) w += " AND type=@p3";
            if (cImp.SelectedIndex > 0) w += " AND importance=@p4";
            return Db.Table($"SELECT id, lu AS 'Lu', substr(date,1,16) AS 'Date', importance AS 'Importance', type AS 'Type', titre AS 'Titre', texte AS 'Détail', ref_type AS '_t', ref_id AS '_r' FROM notifications WHERE {w} AND (titre LIKE @p0 OR texte LIKE @p0) ORDER BY CASE importance WHEN 'Haute' THEN 0 WHEN 'Normale' THEN 1 ELSE 2 END, id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cType.Text, cImp.Text);
        } };
        grid.Activated += id => OpenSel();
        foreach (var c in new[] { cEtat, cType, cImp }) c.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        var bar = Ui.Bar(Ui.PrimaryBtn("Accéder à l'élément", (s, e) => OpenSel()), Ui.Btn("Marquer comme lu", (s, e) => Exec("UPDATE notifications SET lu=1 WHERE id=@p0")),
            Ui.Btn("Tout marquer comme lu", (s, e) => { Db.Exec("UPDATE notifications SET lu=1 WHERE lu=0"); RefreshData(); }), Ui.Btn("Archiver", (s, e) => Exec("UPDATE notifications SET archive=1, lu=1 WHERE id=@p0")),
            Ui.Btn("Archiver les notifications lues", (s, e) => { Db.Exec("UPDATE notifications SET archive=1 WHERE lu=1 AND archive=0"); RefreshData(); }),
            Ui.Btn("Configurer les alertes…", (s, e) => Config()), cEtat, cType, cImp);
        Controls.Add(grid); Controls.Add(bar); Controls.Add(Ui.Title("Centre de notifications"));
    }

    void Exec(string sql) { if (grid.SelectedId != 0) { Db.Exec(sql, grid.SelectedId); RefreshData(); } }

    void OpenSel()
    {
        long id = grid.SelectedId; if (id == 0) return;
        Db.Exec("UPDATE notifications SET lu=1 WHERE id=@p0", id);
        string t = U.S(grid.Cell("_t")); long r = U.L(grid.Cell("_r"));
        if (r != 0 || t == "module") MainForm.Instance.OpenRef(t, r); else RefreshData();
    }

    void Config()
    {
        using var f = new Form { Text = "Configuration des alertes", Width = 460, Height = 520, StartPosition = FormStartPosition.CenterParent, Font = Theme.F, ShowIcon = false };
        var cl = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        foreach (var t in Types) cl.Items.Add(t, Db.Get("notif_off_" + t, "0") != "1");
        var pop = new CheckBox { Text = "Afficher les alertes en fenêtre contextuelle", Checked = Db.Get("popup_notifs", "1") == "1", Dock = DockStyle.Bottom, Height = 30 };
        var ok = new Button { Text = "Enregistrer", Dock = DockStyle.Bottom, Height = 34, DialogResult = DialogResult.OK };
        f.Controls.Add(cl); f.Controls.Add(new Label { Text = "Types de notifications actifs :", Dock = DockStyle.Top, Height = 24 }); f.Controls.Add(pop); f.Controls.Add(ok);
        if (f.ShowDialog() == DialogResult.OK)
            Db.Tx(c => { for (int i = 0; i < Types.Length; i++) c.Set("notif_off_" + Types[i], cl.GetItemChecked(i) ? 0 : 1); c.Set("popup_notifs", pop.Checked ? 1 : 0); });
    }

    public override void RefreshData() => grid.Reload();
}

// ============================================================ ARCHIVES
public class ArchivesModule : Module
{
    public override string Titre => "Archives";
    readonly GridPanel grid;
    readonly ComboBox cType = Ui.Combo(new[] { "(Tous types)", "Dossier", "Mission", "Message archivé", "Rapport / compte rendu", "Réunion passée", "Événement", "Changement d'organisation" }, 220);
    readonly DateTimePicker d1 = new() { Format = DateTimePickerFormat.Short, Width = 110, Margin = new Padding(3, 6, 3, 3) }, d2 = new() { Format = DateTimePickerFormat.Short, Width = 110, Margin = new Padding(3, 6, 3, 3) };

    public override bool AutoRefresh => false;

    public ArchivesModule()
    {
        d1.Value = Engine.Now.Date.AddYears(-1); d2.Value = Engine.Now.Date.AddDays(1);
        grid = new GridPanel("archives") { PageSize = 200, Loader = (q, o, l) => Db.Table($@"SELECT * FROM (
            SELECT 'Dossier' AS 'Type', 'dossier' AS '_t', id, substr(IFNULL(date_cloture,date_ouverture),1,16) AS 'Date', numero||' — '||intitule AS 'Libellé', statut AS 'Détail' FROM dossiers WHERE archive=1 OR statut='Clôturé'
            UNION ALL SELECT 'Mission', 'mission', id, substr(IFNULL(date_cloture,date_rapport),1,16), numero||' — '||objet, statut FROM missions WHERE statut IN ('Validée','Clôturée','Refusée')
            UNION ALL SELECT 'Message archivé', 'message', id, substr(date,1,16), objet, IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id), exp_ext) FROM messages WHERE etat_dest='archive' OR etat_exp='archive'
            UNION ALL SELECT 'Rapport / compte rendu', 'document', id, substr(date,1,16), numero||' — '||titre, categorie FROM documents WHERE categorie IN ('Comptes rendus','Rapports d''activité','Bilans','Synthèses') OR archive=1
            UNION ALL SELECT 'Réunion passée', 'reunion', id, substr(debut,1,16), titre, statut FROM reunions WHERE fin<@p3
            UNION ALL SELECT 'Événement', 'evenement', id, substr(date,1,16), description, type FROM evenements
            UNION ALL SELECT 'Changement d''organisation', 'collaborateur', collab_id, substr(date,1,16), (SELECT prenom||' '||nom FROM collaborateurs WHERE id=collab_id)||' : '||IFNULL(motif,''), (SELECT nom FROM services WHERE id=nouveau_service) FROM affectations_hist
            ) WHERE (Libellé LIKE @p0 OR Détail LIKE @p0) AND Date>=@p4 AND Date<=@p5 AND (@p6='(Tous types)' OR Type=@p6) ORDER BY Date DESC LIMIT @p1 OFFSET @p2",
            U.Like(q), l, o, U.D(Engine.Now), d1.Value.ToString("yyyy-MM-dd"), d2.Value.ToString("yyyy-MM-dd") + " 23:59", cType.Text) };
        grid.Activated += id =>
        {
            string t = U.S(grid.Cell("_t"));
            if (t == "evenement") { var e = Db.Rows("SELECT date, type, origine, description, uid, etat FROM evenements WHERE id=@p0", id)[0]; Ui.Info($"Événement {e[4]}\nDate : {U.Aff(e[0])}\nType : {e[1]}\nOrigine : {e[2]}\nÉtat : {e[5]}\n\n{e[3]}"); }
            else MainForm.Instance.OpenRef(t, id);
        };
        var bar = Ui.Bar(new Label { Text = "Type :", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }, cType, new Label { Text = "Du", AutoSize = true, Padding = new Padding(6, 8, 0, 0) }, d1, new Label { Text = "au", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, d2,
            Ui.PrimaryBtn("Rechercher dans les archives", (s, e) => { grid.ResetPage(); grid.Reload(); }));
        Controls.Add(grid); Controls.Add(bar); Controls.Add(Ui.Title("Archives — recherche globale"));
    }

    public override void RefreshData() { grid.Reload(); grid.Search.Focus(); }
}

// ============================================================ JOURNAL
public class JournalModule : Module
{
    public override string Titre => "Journal chronologique global";
    readonly GridPanel grid;
    readonly ComboBox cType;
    public JournalModule()
    {
        cType = Ui.Combo(new[] { "(Tous les types)" }.Concat(Db.Rows("SELECT DISTINCT type FROM evenements ORDER BY type").Select(r => U.S(r[0]))), 240);
        grid = new GridPanel("journal") { PageSize = 400, Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', type AS 'Type', origine AS 'Origine', description AS 'Description', etat AS 'État', (SELECT nom FROM organismes WHERE id=organisme_id) AS 'Organisme', ref_type AS '_t', ref_id AS '_r', uid AS 'Identifiant' FROM evenements WHERE (@p3='(Tous les types)' OR type=@p3) AND (description LIKE @p0 OR origine LIKE @p0) ORDER BY id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cType.Text) };
        grid.Activated += id => { string t = U.S(grid.Cell("_t")); long r = U.L(grid.Cell("_r")); if (r != 0) MainForm.Instance.OpenRef(t, r); };
        cType.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        var bar = Ui.Bar(new Label { Text = "Type d'événement :", AutoSize = true, Padding = new Padding(4, 8, 0, 0) }, cType, new Label { Text = "Chaque événement possède un identifiant unique, une date, une origine, un type et un état. Double-clic : ouvrir l'élément concerné.", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(10, 8, 0, 0) });
        Controls.Add(grid); Controls.Add(bar); Controls.Add(Ui.Title("Journal chronologique global"));
    }
    public override void RefreshData() => grid.Reload();
}
