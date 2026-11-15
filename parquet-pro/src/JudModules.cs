using System.Data;
using System.Text.RegularExpressions;

namespace ParquetPro;

// ============================================================ carte de liste (cabinet)
public class CardItem
{
    public string Titre, Sous, Type; public long Id; public Color Accent;
    public CardItem(string t, string s, string type, long id, Color? a = null) { Titre = t; Sous = s; Type = type; Id = id; Accent = a ?? Theme.Accent; }
}

public class CardList : Panel
{
    readonly ListBox lb = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 42, IntegralHeight = false };
    readonly Label count = new() { Dock = DockStyle.Right, Width = 60, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Theme.Accent, Padding = new Padding(0, 0, 10, 0) };
    public event Action<CardItem> Open;
    public CardList(string icon, string titre)
    {
        Dock = DockStyle.Fill; BackColor = Theme.Gray; Padding = new Padding(1); Margin = new Padding(6);
        var inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var head = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White };
        head.Controls.Add(new Label { Text = titre, Dock = DockStyle.Fill, Font = Theme.FH, ForeColor = Theme.Navy, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) });
        head.Controls.Add(count);
        head.Controls.Add(new PictureBox { Image = Icons.Get(icon, 20, Theme.Accent), Dock = DockStyle.Left, Width = 34, SizeMode = PictureBoxSizeMode.CenterImage });
        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Gray };
        lb.DrawItem += (s, e) =>
        {
            if (e.Index < 0 || e.Index >= lb.Items.Count) return;
            var it = (CardItem)lb.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(new SolidBrush(sel ? Theme.Light : Color.White), e.Bounds);
            e.Graphics.FillRectangle(new SolidBrush(it.Accent), e.Bounds.X + 8, e.Bounds.Y + 9, 4, 24);
            var r1 = new RectangleF(e.Bounds.X + 20, e.Bounds.Y + 4, e.Bounds.Width - 26, 18);
            var r2 = new RectangleF(e.Bounds.X + 20, e.Bounds.Y + 22, e.Bounds.Width - 26, 17);
            using var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            e.Graphics.DrawString(it.Titre, Theme.FB, new SolidBrush(Color.FromArgb(25, 30, 40)), r1, sf);
            e.Graphics.DrawString(it.Sous, Theme.F, Brushes.Gray, r2, sf);
            e.Graphics.DrawLine(new Pen(Color.FromArgb(238, 240, 244)), e.Bounds.X + 8, e.Bounds.Bottom - 1, e.Bounds.Right - 8, e.Bounds.Bottom - 1);
        };
        lb.DoubleClick += (s, e) => { if (lb.SelectedItem is CardItem it) Open?.Invoke(it); };
        lb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && lb.SelectedItem is CardItem it) Open?.Invoke(it); };
        inner.Controls.Add(lb); inner.Controls.Add(sep); inner.Controls.Add(head);
        Controls.Add(inner);
    }
    public void Set(List<CardItem> items, long total = -1)
    {
        int sel = lb.SelectedIndex, top = lb.TopIndex;
        lb.BeginUpdate(); lb.Items.Clear(); foreach (var i in items) lb.Items.Add(i); lb.EndUpdate();
        if (items.Count == 0) { lb.Items.Add(new CardItem("Rien à signaler", "", "", 0, Theme.Gray)); }
        if (sel >= 0 && sel < lb.Items.Count) lb.SelectedIndex = sel;
        if (top < lb.Items.Count) lb.TopIndex = Math.Max(0, top);
        count.Text = (total >= 0 ? total : items.Count).ToString();
    }
}

// ============================================================ CABINET (centre de commandement)
public class CabinetModule : Module
{
    public override string Titre => "Cabinet du procureur";
    readonly Label hello = new() { Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI Semibold", 16f), ForeColor = Theme.Navy, Padding = new Padding(10, 6, 0, 0) };
    readonly Label resume = new() { Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10.5f), ForeColor = Theme.Anthracite, Padding = new Padding(12, 0, 0, 0) };
    readonly CardList cAff = new("dossier", "Nouvelles affaires reçues"), cDec = new("decision", "Décisions en attente"), cPrio = new("warning", "Dossiers prioritaires"),
        cMsg = new("mail", "Communications non lues"), cAg = new("agenda", "Ma journée — rendez-vous et réunions"), cRap = new("services", "Rapports de l'adjoint et des chefs de service"),
        cAl = new("notif", "Alertes importantes"), cEv = new("history", "Événements récents"), cEff = new("enquete", "Décisions en cours d'effet");

    public CabinetModule()
    {
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(8, 6, 8, 0), WrapContents = false, AutoScroll = true };
        actions.Controls.AddRange(new Control[]
        {
            Icons.Btn("decision", "Dossiers à décider", (s, e) => MainForm.Instance.ShowModule("Décisions"), true),
            Icons.Btn("dossier", "Ouvrir un dossier", (s, e) => MainForm.Instance.ShowModule("Dossiers")),
            Icons.Btn("recherche", "Rechercher une personne / immatriculation", (s, e) => MainForm.Instance.ShowModule("Fichiers de recherche")),
            Icons.Btn("mail", "Écrire un message", (s, e) => MainForm.Instance.NewMessage()),
            Icons.Btn("delegation", "Confier une mission", (s, e) => MainForm.Instance.NewMission()),
            Icons.Btn("agenda", "Planifier une réunion", (s, e) => MainForm.Instance.NewReunion()),
            Icons.Btn("phone", "Appeler", (s, e) => { using var d = new AppelDialog(); d.ShowDialog(); })
        });
        var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, Padding = new Padding(4) };
        for (int i = 0; i < 3; i++) { tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); }
        var cards = new[] { cDec, cAff, cAg, cMsg, cPrio, cRap, cAl, cEff, cEv };
        for (int i = 0; i < cards.Length; i++) { tl.Controls.Add(cards[i], i % 3, i / 3); cards[i].Open += it => Ui.Try(() => { if (it.Id != 0 || it.Type == "module") MainForm.Instance.OpenRef(it.Type, it.Id); RefreshData(); }); }
        Controls.Add(tl); Controls.Add(actions); Controls.Add(resume); Controls.Add(hello);
    }

    public override void Nouveau() => MainForm.Instance.NewMission();

    public override void RefreshData()
    {
        long me = Auth.CollabId; var now = Engine.Now; string N = U.D(now), jour = now.ToString("yyyy-MM-dd");
        string nom = Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", me);
        hello.Text = $"{(now.Hour < 18 ? "Bonjour" : "Bonsoir")}, {nom} — {U.AffJ(now)}, {now:HH:mm}";
        var k = Db.Rows($@"SELECT (SELECT COUNT(*) FROM dossiers WHERE date_transmission>=@p0), (SELECT COUNT(*) FROM dossiers WHERE situation='En attente de décision'),
            (SELECT COUNT(*) FROM messages WHERE dest_id=@p1 AND lu=0 AND etat_dest='actif' AND brouillon=0), (SELECT COUNT(*) FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p1 AND substr(r.debut,1,10)=@p2 AND r.statut<>'Annulée'),
            (SELECT COUNT(*) FROM demandes WHERE statut='En attente')", U.D(now.AddHours(-48)), me, jour)[0];
        resume.Text = $"{k[0]} nouvelle(s) affaire(s) en 48 h  ·  {k[1]} dossier(s) attendent votre décision  ·  {k[2]} message(s) non lu(s)  ·  {k[3]} réunion(s) aujourd'hui  ·  {k[4]} demande(s) en attente";
        cAff.Set(Db.Rows("SELECT id, numero||' — '||intitule, categorie||' · '||IFNULL((SELECT nom FROM organismes WHERE id=service_enqueteur_id),'')||' · '||substr(date_transmission,1,16), priorite FROM dossiers WHERE date_transmission IS NOT NULL ORDER BY date_transmission DESC LIMIT 25")
            .Select(r => new CardItem(U.S(r[1]), U.S(r[2]), "dossier", U.L(r[0]), U.S(r[3]) == "Haute" ? Theme.Red : Theme.Accent)).ToList(), U.L(k[0]));
        var dec = Db.Rows("SELECT id, numero||' — '||intitule, 'En attente de décision depuis le rapport de synthèse · '||priorite, priorite FROM dossiers WHERE situation='En attente de décision' ORDER BY CASE priorite WHEN 'Haute' THEN 0 WHEN 'Normale' THEN 1 ELSE 2 END, id DESC LIMIT 30")
            .Select(r => new CardItem(U.S(r[1]), U.S(r[2]), "dossier", U.L(r[0]), U.S(r[3]) == "Haute" ? Theme.Red : Theme.Orange)).ToList();
        dec.InsertRange(0, Db.Rows("SELECT id, type||' — '||objet, substr(date,1,16) FROM demandes WHERE statut='En attente' ORDER BY id DESC LIMIT 10").Select(r => new CardItem(U.S(r[1]), "Demande · " + r[2], "demande", U.L(r[0]), Theme.Orange)));
        dec.InsertRange(0, Db.Rows("SELECT id, 'Rapport à valider : '||numero||' '||objet, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=destinataire_id) FROM missions WHERE emetteur_id=@p0 AND statut='Rapport transmis' LIMIT 10", me).Select(r => new CardItem(U.S(r[1]), "Délégation · " + r[2], "mission", U.L(r[0]), Theme.Orange)));
        cDec.Set(dec, U.L(k[1]) + U.L(k[4]));
        cPrio.Set(Db.Rows("SELECT id, numero||' — '||intitule, situation||' · '||IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=referent_id),'') FROM dossiers WHERE priorite='Haute' AND statut<>'Clôturé' AND archive=0 ORDER BY id DESC LIMIT 25")
            .Select(r => new CardItem(U.S(r[1]), U.S(r[2]), "dossier", U.L(r[0]), Theme.Red)).ToList());
        cMsg.Set(Db.Rows("SELECT id, IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id), exp_ext), objet, important FROM messages WHERE dest_id=@p0 AND lu=0 AND etat_dest='actif' AND brouillon=0 ORDER BY date DESC LIMIT 30", me)
            .Select(r => new CardItem(U.S(r[2]), U.S(r[1]), "message", U.L(r[0]), U.L(r[3]) == 1 ? Theme.Red : Theme.Accent)).ToList(), U.L(k[2]));
        cAg.Set(Db.Rows("SELECT r.id, substr(r.debut,12,5)||'  '||r.titre, substr(r.debut,1,10)||' · '||r.lieu||' · '||r.statut, r.statut FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND r.fin>=@p1 AND r.statut IN ('Planifiée','Proposée','En cours') ORDER BY r.debut LIMIT 20", me, N)
            .Select(r => new CardItem(U.S(r[1]), U.S(r[2]), "reunion", U.L(r[0]), U.S(r[3]) == "Proposée" ? Theme.Orange : Theme.Green)).ToList());
        cRap.Set(Db.Rows("SELECT id, objet, (SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=exp_id)||' · '||substr(date,1,16) FROM messages WHERE dest_id=@p0 AND exp_id IS NOT NULL AND categorie IN ('Synthèse','Bilan','Rapport','Dossier','Demande') ORDER BY date DESC LIMIT 25", me)
            .Select(r => new CardItem(U.S(r[1]), U.S(r[2]), "message", U.L(r[0]))).ToList());
        cAl.Set(Db.Rows("SELECT ref_type, ref_id, titre, texte, id FROM notifications WHERE lu=0 AND archive=0 AND importance='Haute' ORDER BY id DESC LIMIT 25")
            .Select(r => new CardItem(U.S(r[2]), U.S(r[3]), U.S(r[0]), U.L(r[1]), Theme.Red)).ToList());
        cEff.Set(Db.Rows("SELECT dossier_id, ref||' — '||libelle, orientation||' · effet attendu le '||substr(echeance_effet,1,16) FROM decisions WHERE etat_execution='En attente' AND statut='Validée' ORDER BY echeance_effet LIMIT 25")
            .Select(r => new CardItem(U.S(r[1]), U.S(r[2]), "dossier", U.L(r[0]), Theme.Blue)).ToList());
        cEv.Set(Db.Rows("SELECT ref_type, ref_id, description, substr(date,1,16)||' · '||type FROM evenements ORDER BY id DESC LIMIT 40")
            .Select(r => new CardItem(U.S(r[2]), U.S(r[3]), U.S(r[0]), U.L(r[1]), Theme.Gray)).ToList());
    }
}

// ============================================================ DÉCISIONS
public class DecisionsModule : Module
{
    public override string Titre => "Décisions";
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly GridPanel gAtt, gEff, gReg;
    readonly ComboBox cType = Ui.Combo(new[] { "(Toutes les natures)" }.Concat(Jud.Types.Select(t => t.Libelle)), 300);

    public DecisionsModule()
    {
        gAtt = new GridPanel("dec_att") { Loader = (q, o, l) => Db.Table(@"SELECT * FROM (SELECT d.id, d.numero AS 'Dossier', d.intitule AS 'Intitulé', d.situation AS 'Situation', d.priorite AS 'Priorité', d.gravite AS 'Gravité',
            (SELECT COUNT(*) FROM pieces WHERE dossier_id=d.id) AS 'Pièces', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=d.referent_id) AS 'Référent', '' AS 'Demande'
            FROM dossiers d WHERE d.situation='En attente de décision'
            UNION ALL SELECT x.dossier_id, (SELECT numero FROM dossiers WHERE id=x.dossier_id), (SELECT intitule FROM dossiers WHERE id=x.dossier_id), 'Personne détenue', 'Haute', NULL, NULL, NULL, dm.objet
            FROM demandes dm JOIN detentions x ON x.id=dm.ref_id WHERE dm.statut='En attente' AND dm.type='Demande de mise en liberté' AND dm.ref_type='detention')
            WHERE Intitulé LIKE @p0 OR Dossier LIKE @p0 ORDER BY CASE Priorité WHEN 'Haute' THEN 0 WHEN 'Normale' THEN 1 ELSE 2 END LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gAtt.Activated += DossierWindow.Open;
        gAtt.Bar.Items.Insert(0, Icons.Tool("decision", "Préparer une décision", () => { long d = gAtt.SelectedId; if (d == 0) return; string code = U.S(gAtt.Cell("Situation")) == "Personne détenue" ? "DML" : null; using var w = new DecisionWizard(d, code, 0, 0); if (w.ShowDialog() == DialogResult.OK) RefreshData(); }));
        gAtt.Bar.Items.Insert(1, Icons.Tool("dossier", "Ouvrir le dossier", () => DossierWindow.Open(gAtt.SelectedId)));
        gEff = new GridPanel("dec_eff") { Loader = (q, o, l) => Db.Table(@"SELECT x.id, x.ref AS 'Référence', d.numero AS 'Dossier', x.libelle AS 'Décision', x.orientation AS 'Orientation', (SELECT prenom||' '||nom FROM personnes WHERE id=x.personne_id) AS 'Personne',
            substr(x.date,1,16) AS 'Date', substr(x.echeance_effet,1,16) AS 'Effet attendu' FROM decisions x JOIN dossiers d ON d.id=x.dossier_id WHERE x.etat_execution='En attente' AND x.statut='Validée' AND (x.libelle LIKE @p0 OR d.numero LIKE @p0) ORDER BY x.echeance_effet LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gEff.Activated += id => { using var v = new DecisionView(id); v.ShowDialog(); RefreshData(); };
        gEff.Bar.Items.Insert(0, Icons.Tool("view", "Consulter la décision", () => { if (gEff.SelectedId != 0) { using var v = new DecisionView(gEff.SelectedId); v.ShowDialog(); RefreshData(); } }));
        gReg = new GridPanel("dec_reg") { Loader = (q, o, l) => Db.Table(@"SELECT x.id, x.ref AS 'Référence', substr(x.date,1,16) AS 'Date', d.numero AS 'Dossier', x.libelle AS 'Décision', x.orientation AS 'Orientation', (SELECT prenom||' '||nom FROM personnes WHERE id=x.personne_id) AS 'Personne',
            x.statut AS 'Statut', x.etat_execution AS 'Effet', IFNULL(x.resultat,'') AS 'Résultat', x.version AS 'Version', x.source AS 'Origine' FROM decisions x JOIN dossiers d ON d.id=x.dossier_id
            WHERE (@p3='(Toutes les natures)' OR x.libelle=@p3) AND (x.ref LIKE @p0 OR x.orientation LIKE @p0 OR d.numero LIKE @p0 OR IFNULL(x.resultat,'') LIKE @p0) ORDER BY x.date DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cType.Text) };
        gReg.Activated += id => { using var v = new DecisionView(id); v.ShowDialog(); RefreshData(); };
        cType.SelectedIndexChanged += (s, e) => gReg.Reload();
        var host = new ToolStripControlHost(cType);
        gReg.Bar.Items.Insert(0, host);
        void T(string t, Control c) { var tp = new TabPage(t); tp.Controls.Add(c); tabs.TabPages.Add(tp); }
        T("Dossiers en attente de décision", gAtt); T("Décisions en cours d'effet", gEff); T("Registre des décisions", gReg);
        tabs.SelectedIndexChanged += (s, e) => RefreshData();
        Controls.Add(tabs);
        Controls.Add(new Label { Text = "  Chaque décision suit un parcours guidé (contexte, examen des pièces, prérequis, orientation, motivation, validation) et fait évoluer la simulation.", Dock = DockStyle.Top, Height = 24, ForeColor = Theme.Anthracite });
        Controls.Add(Ui.Title("Décisions du parquet (simulation)"));
    }
    public override void RefreshData() { switch (tabs.SelectedIndex) { case 0: gAtt.Reload(); break; case 1: gEff.Reload(); break; default: gReg.Reload(); break; } }
}

// ============================================================ ENQUÊTES
public class EnquetesModule : Module
{
    public override string Titre => "Enquêtes";
    readonly GridPanel grid, feed;
    readonly ComboBox cSit = Ui.Combo(new[] { "Enquêtes en cours", "Compléments demandés", "Informations judiciaires (sim.)", "Transmis à juridiction (sim.)", "Toutes les procédures actives" }, 240);

    public EnquetesModule()
    {
        grid = new GridPanel("enquetes") { Loader = (q, o, l) =>
        {
            string w = cSit.SelectedIndex switch { 0 => "d.situation='Enquête en cours'", 1 => "d.situation='Complément demandé'", 2 => "d.situation='Information judiciaire (sim.)'", 3 => "d.situation='Transmis à juridiction (sim.)'", _ => "d.statut<>'Clôturé'" };
            return Db.Table($@"SELECT d.id, d.numero AS 'Dossier', d.intitule AS 'Intitulé', (SELECT nom FROM organismes WHERE id=d.service_enqueteur_id) AS 'Service enquêteur', d.situation AS 'Situation', d.avancement AS 'Avancement',
                (SELECT COUNT(*) FROM pieces WHERE dossier_id=d.id) AS 'Pièces', (SELECT substr(MAX(date),1,16) FROM pieces WHERE dossier_id=d.id) AS 'Dernière pièce', (SELECT COUNT(*) FROM pieces WHERE dossier_id=d.id AND lue=0) AS 'Non lues',
                (SELECT prenom||' '||nom FROM collaborateurs WHERE id=d.referent_id) AS 'Référent', d.priorite AS 'Priorité'
                FROM dossiers d WHERE {w} AND (d.intitule LIKE @p0 OR d.numero LIKE @p0) ORDER BY d.date_transmission DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o);
        } };
        grid.Activated += DossierWindow.Open;
        cSit.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        feed = new GridPanel("enq_feed") { PageSize = 100, Loader = (q, o, l) => Db.Table(@"SELECT p.id, substr(p.date,1,16) AS 'Reçue le', d.numero AS 'Dossier', p.cote AS 'Cote', p.titre AS 'Pièce', p.auteur AS 'Service', CASE p.lue WHEN 1 THEN 'Lue' ELSE 'Non lue' END AS 'Lecture'
            FROM pieces p JOIN dossiers d ON d.id=p.dossier_id WHERE p.origine IN ('Service enquêteur','Complément d''enquête') AND p.titre LIKE @p0 ORDER BY p.id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        feed.Activated += pid => { PieceViewer.Open(this, pid); };
        var bar = Ui.Bar(Icons.Btn("dossier", "Ouvrir le dossier", (s, e) => DossierWindow.Open(grid.SelectedId), true),
            Icons.Btn("enquete", "Demander un complément", (s, e) => { if (grid.SelectedId == 0) return; using var w = new DecisionWizard(grid.SelectedId, "COMPLEMENT", 0, 0); if (w.ShowDialog() == DialogResult.OK) RefreshData(); }),
            Icons.Btn("phone", "Relancer le service enquêteur", (s, e) => Relancer()),
            Icons.Btn("decision", "Préparer une décision", (s, e) => { if (grid.SelectedId == 0) return; using var w = new DecisionWizard(grid.SelectedId, null, 0, 0); if (w.ShowDialog() == DialogResult.OK) RefreshData(); }),
            new Label { Text = "Vue :", AutoSize = true, Padding = new Padding(10, 8, 0, 0) }, cSit);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(grid);
        split.Panel2.Controls.Add(Card(feed, "Dernières pièces versées par les services enquêteurs"));
        Controls.Add(split); Controls.Add(bar); Controls.Add(Ui.Title("Suivi des enquêtes"));
        Load += (s, e) => { try { split.SplitterDistance = split.Height * 58 / 100; } catch { } };
    }

    void Relancer()
    {
        long d = grid.SelectedId; if (d == 0) return;
        Ui.Try(() =>
        {
            bool added = false; string res = "";
            Db.Tx(c =>
            {
                string num = c.Str("SELECT numero FROM dossiers WHERE id=@p0", d);
                string sit = c.Str("SELECT situation FROM dossiers WHERE id=@p0", d);
                Biz.DHist(c, d, Auth.CollabId, "Relance du service enquêteur");
                Biz.Event(c, "Relance", Biz.Name(c, Auth.CollabId), Auth.CollabId, $"Relance du service enquêteur — dossier {num}", "dossier", d);
                if (sit == "Enquête en cours" && Random.Shared.NextDouble() < 0.65)
                {
                    Jud.PieceSuivante(c, d, Engine.Now, true); added = true;
                    res = "Le service enquêteur a versé une nouvelle pièce à la suite de votre relance.";
                }
                else res = "Le service enquêteur indique que les investigations se poursuivent (relance enregistrée).";
                c.Exec("INSERT INTO appels(date,sens,statut,interlocuteur,organisme_id,duree,motif,notes,dossier_id) VALUES(@p0,'Sortant','Terminé',@p1,@p2,@p3,'Relance d''enquête',@p4,@p5)",
                    U.D(Engine.Now), c.Str("SELECT nom FROM organismes WHERE id=(SELECT service_enqueteur_id FROM dossiers WHERE id=@p0)", d), c.Long("SELECT service_enqueteur_id FROM dossiers WHERE id=@p0", d), Random.Shared.Next(60, 300), res, d);
            });
            Ui.Info(res); RefreshData();
            if (added) DossierWindow.RefreshIfOpen(d);
        });
    }

    public override void RefreshData() { grid.Reload(); feed.Reload(); }
}

// ============================================================ FICHIERS DE RECHERCHE
public class RechercheModule : Module
{
    public override string Titre => "Fichiers de recherche";
    readonly TextBox tNom = new(), tPre = new(), tDn = new() { PlaceholderText = "jj/mm/aaaa" }, tDos = new(), tImm = new() { PlaceholderText = "AB-123-CD ou partiel", CharacterCasing = CharacterCasing.Upper }, tOrg = new();
    readonly ComboBox cStat = Ui.Combo(new[] { "(Tous statuts)", "Sans antécédent", "Antécédents enregistrés (sim.)", "Mis en cause (sim.)", "Sous contrôle judiciaire (sim.)", "Détenu(e)" }, 220);
    readonly TabControl res = new() { Dock = DockStyle.Fill };
    readonly GridPanel gP, gV, gD, gO;

    public RechercheModule()
    {
        var crit = new TableLayoutPanel { Dock = DockStyle.Left, Width = 330, ColumnCount = 2, Padding = new Padding(12), BackColor = Color.White, AutoScroll = true };
        crit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); crit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void R(string l, Control c) { crit.RowStyles.Add(new RowStyle(SizeType.AutoSize)); crit.Controls.Add(new Label { Text = l, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); c.Dock = DockStyle.Fill; c.Margin = new Padding(3, 4, 3, 4); crit.Controls.Add(c); }
        var t = new Label { Text = "Critères de recherche", Font = Theme.FH, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        crit.Controls.Add(t); crit.SetColumnSpan(t, 2);
        R("Nom", tNom); R("Prénom", tPre); R("Date de naissance", tDn); R("Statut", cStat); R("N° dossier / référence", tDos); R("Immatriculation", tImm); R("Organisme", tOrg);
        var b = Icons.Btn("recherche", "Rechercher", (s, e) => Rechercher(), true); b.Dock = DockStyle.Fill;
        var bc = Ui.Btn("Effacer les critères", (s, e) => { foreach (var x in new[] { tNom, tPre, tDn, tDos, tImm, tOrg }) x.Clear(); cStat.SelectedIndex = 0; Rechercher(); }); bc.Dock = DockStyle.Fill;
        crit.Controls.Add(new Label()); crit.Controls.Add(b); crit.Controls.Add(new Label()); crit.Controls.Add(bc);
        var note = new Label { Text = "Les résultats et rapprochements proviennent exclusivement des liaisons enregistrées dans la base (dossiers, rôles, véhicules, décisions). Une homonymie n'est pas un rapprochement.", AutoSize = true, MaximumSize = new Size(290, 0), ForeColor = Color.Gray, Margin = new Padding(0, 14, 0, 0) };
        crit.Controls.Add(note); crit.SetColumnSpan(note, 2);
        foreach (var x in new Control[] { tNom, tPre, tDn, tDos, tImm, tOrg }) x.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Rechercher(); e.SuppressKeyPress = true; } };
        gP = new GridPanel("rech_p") { Loader = (q, o, l) =>
        {
            string dn = DateTime.TryParse(tDn.Text, U.Fr, System.Globalization.DateTimeStyles.None, out var dd) ? dd.ToString("yyyy-MM-dd") : "";
            return Db.Table(@"SELECT p.id, p.ref AS 'Référence', p.nom AS 'Nom', p.prenom AS 'Prénom', substr(p.date_naissance,1,10) AS 'Naissance', p.ville AS 'Ville', p.statut AS 'Statut',
                (SELECT COUNT(DISTINCT dossier_id) FROM dossier_personnes WHERE personne_id=p.id) AS 'Dossiers', (SELECT COUNT(*) FROM casier WHERE personne_id=p.id) AS 'Casier',
                (SELECT group_concat(DISTINCT role) FROM dossier_personnes WHERE personne_id=p.id) AS 'Rôles', (SELECT COUNT(*) FROM vehicules WHERE proprietaire_id=p.id) AS 'Véhicules'
                FROM personnes p WHERE p.nom LIKE @p0 AND p.prenom LIKE @p1 AND (@p2='' OR p.date_naissance=@p2) AND (@p3='(Tous statuts)' OR p.statut LIKE @p3||'%')
                AND (@p4='' OR p.id IN (SELECT dp.personne_id FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id WHERE d.numero LIKE @p5) OR p.ref LIKE @p5)
                AND (@p6='' OR p.id IN (SELECT proprietaire_id FROM vehicules WHERE replace(immatriculation,'-','') LIKE @p7) OR p.id IN (SELECT dp.personne_id FROM dossier_personnes dp JOIN dossier_vehicules dv ON dv.dossier_id=dp.dossier_id JOIN vehicules v ON v.id=dv.vehicule_id WHERE replace(v.immatriculation,'-','') LIKE @p7))
                AND (@p8='' OR p.id IN (SELECT dp.personne_id FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id JOIN organismes o ON o.id=d.service_enqueteur_id OR o.id=d.organisme_id WHERE o.nom LIKE @p9))
                AND (p.nom||' '||p.prenom LIKE @p10 OR p.prenom||' '||p.nom LIKE @p10)
                ORDER BY p.nom, p.prenom LIMIT @p11 OFFSET @p12",
                tNom.Text.Trim() + "%", tPre.Text.Trim() + "%", dn, cStat.Text == "Détenu(e)" ? "Détenu" : cStat.Text, tDos.Text.Trim(), U.Like(tDos.Text), tImm.Text.Trim(), U.Like(tImm.Text.Replace("-", "")), tOrg.Text.Trim(), U.Like(tOrg.Text), U.Like(q), l, o);
        } };
        gP.Activated += id => PersonneWindow.Open(this, id);
        gP.Bar.Items.Insert(0, Icons.Tool("personne", "Ouvrir la fiche", () => { if (gP.SelectedId != 0) PersonneWindow.Open(this, gP.SelectedId); }));
        gP.Bar.Items.Insert(1, Icons.Tool("casier", "Casier", () => { if (gP.SelectedId != 0) PersonneWindow.Open(this, gP.SelectedId, "Casier judiciaire"); }));
        gP.Bar.Items.Insert(2, Icons.Tool("recherche", "Rapprochements", () => { if (gP.SelectedId != 0) PersonneWindow.Open(this, gP.SelectedId, "Rapprochements"); }));
        gV = new GridPanel("rech_v") { Loader = (q, o, l) => Db.Table(@"SELECT v.id, v.immatriculation AS 'Immatriculation', v.marque AS 'Marque', v.modele AS 'Modèle', v.couleur AS 'Couleur', (SELECT prenom||' '||nom FROM personnes WHERE id=v.proprietaire_id) AS 'Propriétaire déclaré',
            (SELECT group_concat(d.numero||' ('||dv.role||')', ' ; ') FROM dossier_vehicules dv JOIN dossiers d ON d.id=dv.dossier_id WHERE dv.vehicule_id=v.id) AS 'Dossiers associés'
            FROM vehicules v WHERE replace(v.immatriculation,'-','') LIKE @p0 AND (@p3='' OR v.proprietaire_id IN (SELECT id FROM personnes WHERE nom LIKE @p3)) AND v.immatriculation||v.marque||v.modele LIKE @p4 ORDER BY v.immatriculation LIMIT @p1 OFFSET @p2",
            U.Like(tImm.Text.Replace("-", "")), l, o, tNom.Text.Trim() == "" ? "" : tNom.Text.Trim() + "%", U.Like(q)) };
        gV.Activated += id => MainForm.Instance.OpenRef("vehicule", id);
        gD = new GridPanel("rech_d") { Loader = (q, o, l) => Db.Table(@"SELECT d.id, d.numero AS 'Dossier', d.intitule AS 'Intitulé', d.categorie AS 'Catégorie', d.situation AS 'Situation', (SELECT nom FROM organismes WHERE id=d.service_enqueteur_id) AS 'Service enquêteur', substr(d.date_ouverture,1,10) AS 'Ouverture'
            FROM dossiers d WHERE (d.numero LIKE @p0 OR d.intitule LIKE @p0) AND (@p3='' OR d.id IN (SELECT dossier_id FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE p.nom LIKE @p3))
            AND (@p4='' OR d.id IN (SELECT dossier_id FROM dossier_vehicules dv JOIN vehicules v ON v.id=dv.vehicule_id WHERE replace(v.immatriculation,'-','') LIKE @p5))
            AND (@p6='' OR d.service_enqueteur_id IN (SELECT id FROM organismes WHERE nom LIKE @p7) OR d.organisme_id IN (SELECT id FROM organismes WHERE nom LIKE @p7)) AND d.intitule||d.numero LIKE @p8
            ORDER BY d.id DESC LIMIT @p1 OFFSET @p2", U.Like(tDos.Text), l, o, tNom.Text.Trim() == "" ? "" : tNom.Text.Trim() + "%", tImm.Text.Trim(), U.Like(tImm.Text.Replace("-", "")), tOrg.Text.Trim(), U.Like(tOrg.Text), U.Like(q)) };
        gD.Activated += DossierWindow.Open;
        gO = new GridPanel("rech_o") { Loader = (q, o, l) => Db.Table("SELECT o.id, o.nom AS 'Organisme', o.categorie AS 'Catégorie', (SELECT COUNT(*) FROM dossiers WHERE service_enqueteur_id=o.id OR organisme_id=o.id) AS 'Dossiers', o.activite AS 'Activité' FROM organismes o WHERE o.nom LIKE @p0 AND o.nom LIKE @p3 ORDER BY o.nom LIMIT @p1 OFFSET @p2", U.Like(tOrg.Text), l, o, U.Like(q)) };
        gO.Activated += id => MainForm.Instance.OpenRef("organisme", id);
        void T(string tt, Control c) { var tp = new TabPage(tt); tp.Controls.Add(c); res.TabPages.Add(tp); }
        T("Personnes", gP); T("Véhicules (immatriculations)", gV); T("Dossiers", gD); T("Organismes", gO);
        var right = new Panel { Dock = DockStyle.Fill }; right.Controls.Add(res);
        Controls.Add(right); Controls.Add(new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Gray }); Controls.Add(crit); Controls.Add(Ui.Title("Fichiers de recherche — personnes, véhicules, dossiers, organismes"));
    }

    public void Lancer(string terme)
    {
        foreach (var x in new[] { tNom, tPre, tDn, tDos, tImm, tOrg }) x.Clear();
        terme = (terme ?? "").Trim();
        if (Regex.IsMatch(terme, @"^[A-Za-z]{2}-?\d{1,3}") || Regex.IsMatch(terme, @"^\d{3}-?[A-Za-z]{0,2}$")) { tImm.Text = terme.ToUpperInvariant(); res.SelectedIndex = 1; }
        else if (terme.StartsWith("DOS", StringComparison.OrdinalIgnoreCase) || terme.StartsWith("P-", StringComparison.OrdinalIgnoreCase)) { tDos.Text = terme.ToUpperInvariant(); res.SelectedIndex = terme.StartsWith("P-") ? 0 : 2; }
        else
        {
            var parts = terme.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && Db.Long("SELECT COUNT(*) FROM personnes WHERE nom LIKE @p0", parts[1] + "%") > 0) { tPre.Text = parts[0]; tNom.Text = parts[1]; }
            else if (parts.Length == 2) { tNom.Text = parts[0]; tPre.Text = parts[1]; }
            else tNom.Text = terme;
            res.SelectedIndex = 0;
        }
        Rechercher();
    }

    void Rechercher()
    {
        foreach (var g in new[] { gP, gV, gD, gO }) { g.ResetPage(); g.Reload(); }
        res.TabPages[0].Text = $"Personnes ({gP.Grid.RowCount})"; res.TabPages[1].Text = $"Véhicules ({gV.Grid.RowCount})"; res.TabPages[2].Text = $"Dossiers ({gD.Grid.RowCount})"; res.TabPages[3].Text = $"Organismes ({gO.Grid.RowCount})";
    }

    public override bool AutoRefresh => false;
    public override void RefreshData() => Rechercher();
}

// ============================================================ PERSONNES
public class PersonnesModule : Module
{
    public override string Titre => "Personnes";
    readonly GridPanel grid;
    readonly ComboBox cStat = Ui.Combo(new[] { "(Tous statuts)", "Sans antécédent", "Antécédents enregistrés (sim.)", "Mis en cause (sim.)", "Sous contrôle judiciaire (sim.)", "Détenu(e)" }, 220);
    readonly ComboBox cRole = Ui.Combo(new[] { "(Tous rôles)", "Mis en cause", "Victime", "Témoin", "Plaignant", "Requérant" }, 160);

    public PersonnesModule()
    {
        grid = new GridPanel("personnes") { Loader = (q, o, l) => Db.Table(@"SELECT p.id, p.ref AS 'Référence', p.nom AS 'Nom', p.prenom AS 'Prénom', p.sexe AS 'Sexe', substr(p.date_naissance,1,10) AS 'Naissance', p.ville AS 'Ville', p.profession AS 'Profession', p.statut AS 'Statut',
            (SELECT COUNT(DISTINCT dossier_id) FROM dossier_personnes WHERE personne_id=p.id) AS 'Dossiers', (SELECT COUNT(*) FROM casier WHERE personne_id=p.id) AS 'Casier'
            FROM personnes p WHERE (p.nom LIKE @p0 OR p.prenom LIKE @p0 OR p.ref LIKE @p0 OR p.prenom||' '||p.nom LIKE @p0) AND (@p3='(Tous statuts)' OR p.statut LIKE @p4||'%')
            AND (@p5='(Tous rôles)' OR p.id IN (SELECT personne_id FROM dossier_personnes WHERE role=@p5)) ORDER BY p.nom, p.prenom LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cStat.Text, cStat.Text == "Détenu(e)" ? "Détenu" : cStat.Text, cRole.Text) };
        grid.Activated += id => PersonneWindow.Open(this, id);
        foreach (var c in new[] { cStat, cRole }) c.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        var bar = Ui.Bar(Icons.Btn("personne", "Ouvrir la fiche", (s, e) => { if (grid.SelectedId != 0) PersonneWindow.Open(this, grid.SelectedId); }, true),
            Icons.Btn("casier", "Consulter le casier", (s, e) => { if (grid.SelectedId != 0) PersonneWindow.Open(this, grid.SelectedId, "Casier judiciaire"); }),
            Icons.Btn("recherche", "Recherche avancée", (s, e) => MainForm.Instance.ShowModule("Fichiers de recherche")),
            new Label { Text = "Statut :", AutoSize = true, Padding = new Padding(10, 8, 0, 0) }, cStat, new Label { Text = "Rôle :", AutoSize = true, Padding = new Padding(6, 8, 0, 0) }, cRole);
        Controls.Add(grid); Controls.Add(bar); Controls.Add(Ui.Title("Personnes de l'univers simulé (fiche unique par personne)"));
    }
    public override void RefreshData() => grid.Reload();
}

// ============================================================ CASIER JUDICIAIRE
public class CasierModule : Module
{
    public override string Titre => "Casier judiciaire (simulé)";
    readonly GridPanel gPers, gEnt;

    public CasierModule()
    {
        gPers = new GridPanel("casier_p") { Loader = (q, o, l) => Db.Table(@"SELECT p.id, p.nom||' '||p.prenom AS 'Personne', substr(p.date_naissance,1,10) AS 'Naissance', p.ref AS 'Référence', COUNT(c.id) AS 'Mentions', substr(MAX(c.date),1,10) AS 'Dernière', p.statut AS 'Statut'
            FROM personnes p JOIN casier c ON c.personne_id=p.id WHERE p.nom LIKE @p0 OR p.prenom LIKE @p0 OR p.ref LIKE @p0 GROUP BY p.id ORDER BY p.nom LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gEnt = new GridPanel("casier_e") { Loader = (q, o, l) => Db.Table(@"SELECT c.id, substr(c.date,1,10) AS 'Date', c.nature AS 'Nature des faits', c.mesure AS 'Décision', c.juridiction AS 'Juridiction', IFNULL(d.numero, c.ref_externe) AS 'Dossier', c.statut AS 'Statut', c.source AS 'Origine',
            (SELECT ref FROM decisions WHERE id=c.decision_id) AS 'Décision parquet' FROM casier c LEFT JOIN dossiers d ON d.id=c.dossier_id WHERE c.personne_id=@p0 AND c.nature LIKE @p1 ORDER BY c.date DESC LIMIT @p2 OFFSET @p3", gPers.SelectedId, U.Like(q), l, o) };
        gPers.SelChanged += () => gEnt.Reload();
        gPers.Activated += id => PersonneWindow.Open(this, id, "Casier judiciaire");
        gEnt.Activated += id => { long d = Db.Long("SELECT IFNULL(dossier_id,0) FROM casier WHERE id=@p0", id); if (d != 0) DossierWindow.Open(d); else Ui.Info("Mention issue de l'historique simulé : aucun dossier détaillé n'est disponible."); };
        var bar = Ui.Bar(Icons.Btn("personne", "Fiche de la personne", (s, e) => { if (gPers.SelectedId != 0) PersonneWindow.Open(this, gPers.SelectedId); }, true),
            Icons.Btn("print", "Relevé du casier (PDF)", (s, e) => { if (gPers.SelectedId != 0) Releve(gPers.SelectedId); }),
            Icons.Btn("dossier", "Ouvrir le dossier de la mention", (s, e) => { long d = Db.Long("SELECT IFNULL(dossier_id,0) FROM casier WHERE id=@p0", gEnt.SelectedId); if (d != 0) DossierWindow.Open(d); }));
        var split = new SplitContainer { Dock = DockStyle.Fill };
        split.Panel1.Controls.Add(Card(gPers, "Personnes ayant des mentions"));
        split.Panel2.Controls.Add(Card(gEnt, "Mentions de la personne sélectionnée"));
        Controls.Add(split); Controls.Add(bar);
        Controls.Add(new Label { Text = "  Antécédents fictifs : alimentés uniquement par les décisions enregistrées dans la simulation et par l'historique simulé initial. Aucune donnée réelle.", Dock = DockStyle.Top, Height = 24, ForeColor = Theme.Red });
        Controls.Add(Ui.Title("Casier judiciaire simulé"));
        Load += (s, e) => { try { split.SplitterDistance = split.Width * 42 / 100; } catch { } };
    }

    public static void Releve(long pid)
    {
        var p = Db.Rows("SELECT ref, nom, prenom, date_naissance, lieu_naissance FROM personnes WHERE id=@p0", pid)[0];
        var pdf = new PdfDoc { Ref = U.S(p[0]), Title = $"Relevé du casier simulé — {p[1]} {p[2]}", Toc = false };
        pdf.Cover("RELEVÉ DES MENTIONS — CASIER JUDICIAIRE SIMULÉ", $"{p[1]} {p[2]}", $"Né(e) le {U.P(p[3]):dd/MM/yyyy} à {p[4]} — {p[0]}", "Relevé établi à partir des données de la simulation PARQUET PRO.", "Il ne s'agit en aucun cas d'un bulletin de casier judiciaire réel.");
        pdf.Section("Mentions");
        var rows = Db.Rows("SELECT c.date, c.nature, c.mesure, c.juridiction, IFNULL(d.numero, c.ref_externe), c.statut, c.source FROM casier c LEFT JOIN dossiers d ON d.id=c.dossier_id WHERE c.personne_id=@p0 ORDER BY c.date", pid);
        if (rows.Count == 0) pdf.Para("Néant.");
        int i = 1;
        foreach (var r in rows) { pdf.Sub($"Mention n°{i++} — {U.Aff(r[0])}"); pdf.KV("Nature des faits", U.S(r[1])); pdf.KV("Décision", U.S(r[2])); pdf.KV("Juridiction", U.S(r[3])); pdf.KV("Référence", U.S(r[4])); pdf.KV("Statut", U.S(r[5])); pdf.KV("Origine", U.S(r[6])); }
        using var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = $"Casier simulé {p[1]} {p[2]}.pdf", InitialDirectory = Db.ExportDir };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        pdf.Save(sfd.FileName);
        Biz.Admin("Relevé de casier simulé", $"{p[1]} {p[2]} ({p[0]})");
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
    }

    public override void RefreshData() { gPers.Reload(); gEnt.Reload(); }
}

// ============================================================ PERSONNES DÉTENUES
public class DetenusModule : Module
{
    public override string Titre => "Personnes détenues";
    readonly GridPanel grid, hist;
    readonly ComboBox cEtat = Ui.Combo(new[] { "Détentions en cours", "Détention provisoire (sim.)", "Exécution de peine (sim.)", "Détentions terminées", "Toutes" }, 230);
    readonly ComboBox cEt = Ui.Combo(new[] { "(Tous établissements)" }.Concat(Jud.Etablissements), 300);
    readonly Kpi kTot, kDP, kEP, kSort;

    public DetenusModule()
    {
        grid = new GridPanel("detenus") { Loader = (q, o, l) =>
        {
            string w = cEtat.SelectedIndex switch { 0 => "d.date_sortie IS NULL", 1 => "d.date_sortie IS NULL AND d.regime='Détention provisoire (sim.)'", 2 => "d.date_sortie IS NULL AND d.regime='Exécution de peine (sim.)'", 3 => "d.date_sortie IS NOT NULL", _ => "1=1" };
            return Db.Table($@"SELECT d.id, p.nom||' '||p.prenom AS 'Personne', substr(p.date_naissance,1,10) AS 'Naissance', IFNULL(x.numero, d.ref_externe) AS 'Dossier', d.etablissement AS 'Établissement', substr(d.date_entree,1,10) AS 'Entrée',
                d.regime AS 'Régime', d.statut AS 'Statut', d.duree_mois AS 'Durée (mois)', IFNULL(substr(d.date_sortie,1,10),'—') AS 'Sortie', (SELECT ref FROM decisions WHERE id=d.decision_id) AS 'Décision', d.personne_id AS '_p', d.dossier_id AS '_d', d.decision_id AS '_dec'
                FROM detentions d JOIN personnes p ON p.id=d.personne_id LEFT JOIN dossiers x ON x.id=d.dossier_id WHERE {w} AND (@p3='(Tous établissements)' OR d.etablissement=@p3) AND (p.nom LIKE @p0 OR p.prenom LIKE @p0 OR IFNULL(x.numero, d.ref_externe) LIKE @p0)
                ORDER BY d.date_entree DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, cEt.Text);
        } };
        hist = new GridPanel("detenus_h") { Loader = (q, o, l) => Db.Table("SELECT h.id, substr(h.date,1,16) AS 'Date', h.ancien_statut AS 'Avant', h.nouveau_statut AS 'Après', h.commentaire AS 'Commentaire', (SELECT ref FROM decisions WHERE id=h.decision_id) AS 'Décision' FROM detention_hist h WHERE h.detention_id=@p0 AND IFNULL(h.commentaire,'') LIKE @p1 ORDER BY h.date DESC LIMIT @p2 OFFSET @p3", grid.SelectedId, U.Like(q), l, o) };
        grid.SelChanged += () => hist.Reload();
        grid.Activated += id => PersonneWindow.Open(this, U.L(grid.Cell("_p")), "Détention");
        foreach (var c in new[] { cEtat, cEt }) c.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        var kp = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 86, WrapContents = false };
        kTot = new Kpi("Personnes détenues", Theme.Red); kDP = new Kpi("Détention provisoire", Theme.Orange); kEP = new Kpi("Exécution de peine", Theme.Blue); kSort = new Kpi("Sorties (30 jours)", Theme.Green);
        kp.Controls.AddRange(new Control[] { kTot, kDP, kEP, kSort });
        var bar = Ui.Bar(Icons.Btn("personne", "Fiche de la personne", (s, e) => { long p = U.L(grid.Cell("_p")); if (p != 0) PersonneWindow.Open(this, p, "Détention"); }, true),
            Icons.Btn("dossier", "Ouvrir le dossier", (s, e) => DossierWindow.Open(U.L(grid.Cell("_d")))),
            Icons.Btn("view", "Consulter la décision", (s, e) => { long d = U.L(grid.Cell("_dec")); if (d != 0) { using var v = new DecisionView(d); v.ShowDialog(); } }),
            Icons.Btn("decision", "Préparer une décision", (s, e) =>
            {
                long dos = U.L(grid.Cell("_d")), p = U.L(grid.Cell("_p"));
                if (dos == 0) { Ui.Info("Détention issue de l'historique simulé : aucun dossier actif n'y est rattaché."); return; }
                using var w = new DecisionWizard(dos, "PERSONNE", p, 0); if (w.ShowDialog() == DialogResult.OK) RefreshData();
            }),
            cEtat, cEt);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(grid); split.Panel2.Controls.Add(Card(hist, "Historique des changements de situation"));
        Controls.Add(split); Controls.Add(bar); Controls.Add(kp); Controls.Add(Ui.Title("Personnes détenues (simulation)"));
        Load += (s, e) => { try { split.SplitterDistance = split.Height * 62 / 100; } catch { } };
    }

    public override void RefreshData()
    {
        var r = Db.Rows("SELECT COUNT(*), SUM(regime='Détention provisoire (sim.)'), SUM(regime='Exécution de peine (sim.)'), (SELECT COUNT(*) FROM detentions WHERE date_sortie>=@p0) FROM detentions WHERE date_sortie IS NULL", U.D(Engine.Now.AddDays(-30)))[0];
        kTot.Set(r[0] ?? 0); kDP.Set(r[1] ?? 0); kEP.Set(r[2] ?? 0); kSort.Set(r[3] ?? 0);
        grid.Reload(); hist.Reload();
    }
}

// ============================================================ SERVICES ET COLLABORATEURS (organigramme + RH)
public class ServicesModule : Module
{
    public override string Titre => "Services et collaborateurs";
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly OrganisationModule org = new();
    readonly PersonnelModule rh = new();
    public ServicesModule()
    {
        var t1 = new TabPage("Organigramme"); t1.Controls.Add(org);
        var t2 = new TabPage("Personnel et ressources humaines"); t2.Controls.Add(rh);
        tabs.TabPages.Add(t1); tabs.TabPages.Add(t2);
        tabs.SelectedIndexChanged += (s, e) => RefreshData();
        Controls.Add(tabs);
    }
    public override bool AutoRefresh => false;
    public override void RefreshData() { if (tabs.SelectedIndex == 0) org.RefreshData(); else rh.RefreshData(); }
}

// ============================================================ MESSAGERIE (boîte mail professionnelle)
public class MessagerieModule : Module
{
    public override string Titre => "Messagerie";
    record Folder(string Key, string Nom, string Icon, long Custom);
    record Msg(long Id, string De, string Objet, string Apercu, string Date, bool NonLu, bool Important, bool Piece, bool Lien, bool Suivi);
    readonly ListBox folders = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 32, IntegralHeight = false };
    readonly ListBox list = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 70, IntegralHeight = false };
    readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "Rechercher dans les messages…" };
    readonly CheckBox unreadOnly = new() { Text = "Non lus", AutoSize = true, Dock = DockStyle.Right };
    readonly Label lCount = new() { Dock = DockStyle.Bottom, Height = 22, ForeColor = Color.Gray, Padding = new Padding(8, 3, 0, 0) };
    readonly Label rSubject = new() { Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI Semibold", 14f), ForeColor = Theme.Navy, AutoEllipsis = true };
    readonly Label rFrom = new() { Dock = DockStyle.Top, Height = 44, ForeColor = Theme.Anthracite };
    readonly FlowLayoutPanel rLinks = new() { Dock = DockStyle.Top, Height = 30, WrapContents = false, AutoScroll = true };
    readonly RichTextBox rBody = Rtf.Reader(10.5f);
    readonly ListBox rThread = new() { Dock = DockStyle.Bottom, Height = 120, BorderStyle = BorderStyle.None, BackColor = Theme.Back, IntegralHeight = false };
    readonly Label rThreadTitle = new() { Dock = DockStyle.Bottom, Height = 24, Font = Theme.FB, ForeColor = Theme.Navy, Text = "Historique des échanges" };
    readonly ToolStripDropDownButton move = new("Déplacer vers", Icons.Get("dossier", 18, Theme.Blue));
    readonly Dictionary<string, long> counts = new();
    long current;
    int limit = 200;

    public MessagerieModule()
    {
        folders.DrawItem += DrawFolder;
        folders.SelectedIndexChanged += (s, e) => { limit = 200; LoadList(); };
        var fm = new ContextMenuStrip();
        fm.Items.Add("Nouveau dossier…", null, (s, e) => NewFolder());
        fm.Items.Add("Renommer le dossier…", null, (s, e) => RenameFolder());
        fm.Items.Add("Supprimer le dossier", null, (s, e) => DeleteFolder());
        folders.ContextMenuStrip = fm;
        list.DrawItem += DrawMsg;
        list.SelectedIndexChanged += (s, e) => { if (list.SelectedItem is Msg m) ShowMsg(m.Id); };
        list.DoubleClick += (s, e) => { if (list.SelectedItem is Msg m) MessageViewer.Open(this, m.Id); };
        search.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { LoadList(); e.SuppressKeyPress = true; } };
        unreadOnly.CheckedChanged += (s, e) => LoadList();
        rThread.DoubleClick += (s, e) => { if (rThread.SelectedItem is Item it) ShowMsg(it.Id); };

        var tb = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.White, Padding = new Padding(6, 4, 6, 4) };
        tb.Items.Add(Icons.Tool("add", "Nouveau message", Nouveau));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(Icons.Tool("reply", "Répondre", () => Reply(false)));
        tb.Items.Add(Icons.Tool("reply", "Répondre à tous", () => Reply(true)));
        tb.Items.Add(Icons.Tool("forward", "Transférer", Forward));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(Icons.Tool("archive", "Archiver", () => SetEtat("archive")));
        tb.Items.Add(Icons.Tool("delete", "Supprimer", () => SetEtat("corbeille")));
        tb.Items.Add(Icons.Tool("refresh", "Restaurer", () => SetEtat("actif")));
        move.DropDownOpening += (s, e) => FillMove();
        move.DropDownItems.Add("…");
        tb.Items.Add(move);
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(Icons.Tool("important", "Important", () => Toggle("important")));
        tb.Items.Add(Icons.Tool("flag", "Suivi", () => Toggle("suivi")));
        tb.Items.Add(Icons.Tool("mail", "Marquer non lu", () => { if (current != 0) { Db.Exec("UPDATE messages SET lu=0 WHERE id=@p0 AND dest_id=@p1", current, Auth.CollabId); LoadList(); } }));
        tb.Items.Add(Icons.Tool("edit", "Reprendre le brouillon", OpenDraft));

        // colonne gauche
        var left = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Color.White };
        var fb = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(4) };
        fb.Controls.Add(Icons.Btn("add", "Nouveau dossier", (s, e) => NewFolder()));
        left.Controls.Add(folders); left.Controls.Add(fb);
        left.Controls.Add(new Label { Text = "  BOÎTE AUX LETTRES", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.Gray, TextAlign = ContentAlignment.BottomLeft });
        // liste
        var mid = new Panel { Dock = DockStyle.Left, Width = 430, BackColor = Color.White };
        var sp = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(8, 6, 8, 2) };
        sp.Controls.Add(search); sp.Controls.Add(unreadOnly);
        var more = Ui.Btn("Afficher plus de messages", (s, e) => { limit += 200; LoadList(); }); more.Dock = DockStyle.Bottom;
        mid.Controls.Add(list); mid.Controls.Add(lCount); mid.Controls.Add(more); mid.Controls.Add(sp);
        // lecture
        var read = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(22, 10, 22, 8) };
        var sepr = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Gray };
        read.Controls.Add(rBody); read.Controls.Add(sepr); read.Controls.Add(rLinks); read.Controls.Add(rFrom); read.Controls.Add(rSubject); read.Controls.Add(rThreadTitle); read.Controls.Add(rThread);
        rThreadTitle.BringToFront(); rThread.SendToBack();
        Controls.Add(read);
        Controls.Add(new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Gray });
        Controls.Add(mid);
        Controls.Add(new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Gray });
        Controls.Add(left);
        Controls.Add(tb);
        BuildFolders();
        folders.SelectedIndex = 0;
    }

    List<Folder> fl = new();
    void BuildFolders()
    {
        int sel = Math.Max(0, folders.SelectedIndex);
        fl = new List<Folder> { new("inbox", "Boîte de réception", "mail", 0), new("important", "Messages importants", "important", 0), new("suivi", "Suivis", "flag", 0), new("sent", "Messages envoyés", "send", 0),
            new("drafts", "Brouillons", "edit", 0), new("archive", "Messages archivés", "archive", 0), new("trash", "Corbeille", "delete", 0) };
        foreach (var r in Db.Rows("SELECT id, nom FROM mail_dossiers WHERE owner_id=@p0 ORDER BY nom", Auth.CollabId)) fl.Add(new("custom", U.S(r[1]), "dossier", U.L(r[0])));
        fl.Add(new("ext", "Communications extérieures", "organisme", 0));
        fl.Add(new("interne", "Flux interne des services", "services", 0));
        folders.BeginUpdate(); folders.Items.Clear(); foreach (var f in fl) folders.Items.Add(f); folders.EndUpdate();
        if (sel < folders.Items.Count) folders.SelectedIndex = sel;
    }

    void DrawFolder(object s, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var f = (Folder)folders.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;
        e.Graphics.FillRectangle(new SolidBrush(sel ? Theme.Light : Color.White), e.Bounds);
        if (sel) e.Graphics.FillRectangle(new SolidBrush(Theme.Accent), e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
        e.Graphics.DrawImage(Icons.Get(f.Icon, 16, sel ? Theme.Accent : Theme.Anthracite), e.Bounds.X + (f.Key == "custom" ? 26 : 12), e.Bounds.Y + 8);
        string k = f.Key + f.Custom;
        long n = counts.TryGetValue(k, out var v) ? v : 0;
        e.Graphics.DrawString(f.Nom, n > 0 && f.Key is "inbox" or "important" or "custom" ? Theme.FB : Theme.F, new SolidBrush(Theme.Navy), e.Bounds.X + (f.Key == "custom" ? 48 : 34), e.Bounds.Y + 7);
        if (n > 0) { var t = n.ToString(); var sz = e.Graphics.MeasureString(t, Theme.FB); e.Graphics.DrawString(t, Theme.FB, new SolidBrush(Theme.Accent), e.Bounds.Right - sz.Width - 10, e.Bounds.Y + 7); }
    }

    void DrawMsg(object s, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= list.Items.Count) return;
        var m = (Msg)list.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;
        var g = e.Graphics; var b = e.Bounds;
        g.FillRectangle(new SolidBrush(sel ? Color.FromArgb(214, 230, 248) : m.NonLu ? Color.FromArgb(246, 249, 254) : Color.White), b);
        if (m.NonLu) g.FillEllipse(new SolidBrush(Theme.Accent), b.X + 7, b.Y + 11, 8, 8);
        using var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        var fDe = m.NonLu ? new Font("Segoe UI", 10f, FontStyle.Bold) : new Font("Segoe UI", 10f);
        g.DrawString(m.De, fDe, new SolidBrush(Color.FromArgb(20, 25, 35)), new RectangleF(b.X + 22, b.Y + 5, b.Width - 140, 20), sf);
        var dsz = g.MeasureString(m.Date, Theme.F);
        g.DrawString(m.Date, Theme.F, new SolidBrush(m.NonLu ? Theme.Accent : Color.Gray), b.Right - dsz.Width - 8, b.Y + 7);
        float x = b.X + 22;
        if (m.Important) { g.DrawImage(Icons.Get("important", 14, Theme.Red), x, b.Y + 27); x += 16; }
        if (m.Suivi) { g.DrawImage(Icons.Get("flag", 14, Theme.Orange), x, b.Y + 27); x += 16; }
        g.DrawString(m.Objet, m.NonLu ? Theme.FB : Theme.F, new SolidBrush(Theme.Navy), new RectangleF(x, b.Y + 26, b.Right - x - 30, 18), sf);
        if (m.Piece) g.DrawImage(Icons.Get("attach", 14, Theme.Anthracite), b.Right - 24, b.Y + 27);
        else if (m.Lien) g.DrawImage(Icons.Get("dossier", 14, Theme.Anthracite), b.Right - 24, b.Y + 27);
        g.DrawString(m.Apercu, Theme.F, Brushes.Gray, new RectangleF(b.X + 22, b.Y + 46, b.Width - 30, 18), sf);
        g.DrawLine(new Pen(Color.FromArgb(232, 235, 240)), b.X, b.Bottom - 1, b.Right, b.Bottom - 1);
    }

    string Where(Folder f) => f.Key switch
    {
        "inbox" => "m.dest_id=@p0 AND m.etat_dest='actif' AND m.brouillon=0 AND m.mail_dossier_id IS NULL",
        "important" => "m.dest_id=@p0 AND m.important=1 AND m.etat_dest<>'corbeille' AND m.brouillon=0",
        "suivi" => "m.suivi=1 AND (m.dest_id=@p0 OR m.exp_id=@p0)",
        "sent" => "m.exp_id=@p0 AND m.brouillon=0 AND m.etat_exp='actif'",
        "drafts" => "m.exp_id=@p0 AND m.brouillon=1",
        "archive" => "((m.dest_id=@p0 AND m.etat_dest='archive') OR (m.exp_id=@p0 AND m.etat_exp='archive'))",
        "trash" => "((m.dest_id=@p0 AND m.etat_dest='corbeille') OR (m.exp_id=@p0 AND m.etat_exp='corbeille'))",
        "custom" => $"m.dest_id=@p0 AND m.mail_dossier_id={f.Custom} AND m.etat_dest='actif'",
        "ext" => "m.organisme_id IS NOT NULL AND m.dest_id=@p0 AND m.etat_dest<>'corbeille'",
        _ => "IFNULL(m.exp_id,0)<>@p0 AND IFNULL(m.dest_id,0)<>@p0 AND m.exp_id IS NOT NULL"
    };

    void LoadList()
    {
        if (folders.SelectedItem is not Folder f) return;
        long me = Auth.CollabId;
        string w = Where(f) + (unreadOnly.Checked ? " AND m.dest_id=@p0 AND m.lu=0" : "");
        bool sent = f.Key is "sent" or "drafts";
        var rows = Db.Rows($@"SELECT m.id, {(sent ? "'À : '||IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=m.dest_id),'(destinataire à choisir)')" : "IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=m.exp_id), m.exp_ext)")},
            m.objet, replace(replace(substr(m.corps,1,160),char(10),' '),char(13),' '), m.date, CASE WHEN m.dest_id=@p0 AND m.lu=0 THEN 1 ELSE 0 END, m.important, m.document_id IS NOT NULL, (m.dossier_id IS NOT NULL OR m.mission_id IS NOT NULL), m.suivi
            FROM messages m WHERE {w} AND (m.objet LIKE @p1 OR m.corps LIKE @p1 OR IFNULL(m.exp_ext,'') LIKE @p1) ORDER BY m.date DESC, m.id DESC LIMIT @p2", me, U.Like(search.Text), limit);
        int sel = list.SelectedIndex;
        list.BeginUpdate(); list.Items.Clear();
        foreach (var r in rows)
        {
            var d = U.P(r[4]);
            string ds = d.Date == Engine.Now.Date ? d.ToString("HH:mm") : d.Year == Engine.Now.Year ? d.ToString("dd MMM HH:mm", U.Fr) : d.ToString("dd/MM/yyyy");
            list.Items.Add(new Msg(U.L(r[0]), U.S(r[1]), U.S(r[2]), U.S(r[3]), ds, U.L(r[5]) == 1, U.L(r[6]) == 1, U.L(r[7]) == 1, U.L(r[8]) == 1, U.L(r[9]) == 1));
        }
        list.EndUpdate();
        lCount.Text = $"{rows.Count} message(s){(rows.Count >= limit ? " — d'autres messages plus anciens existent" : "")}";
        if (current != 0) { for (int i = 0; i < list.Items.Count; i++) if (((Msg)list.Items[i]).Id == current) { list.SelectedIndex = i; break; } }
        else if (sel >= 0 && sel < list.Items.Count) list.SelectedIndex = sel;
        UpdateCounts();
    }

    void UpdateCounts()
    {
        long me = Auth.CollabId;
        counts.Clear();
        counts["inbox0"] = Db.Long("SELECT COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND etat_dest='actif' AND brouillon=0 AND mail_dossier_id IS NULL", me);
        counts["important0"] = Db.Long("SELECT COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND important=1 AND etat_dest<>'corbeille'", me);
        counts["drafts0"] = Db.Long("SELECT COUNT(*) FROM messages WHERE exp_id=@p0 AND brouillon=1", me);
        counts["ext0"] = Db.Long("SELECT COUNT(*) FROM messages WHERE organisme_id IS NOT NULL AND dest_id=@p0 AND lu=0", me);
        foreach (var r in Db.Rows("SELECT mail_dossier_id, COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND mail_dossier_id IS NOT NULL GROUP BY mail_dossier_id", me)) counts["custom" + U.L(r[0])] = U.L(r[1]);
        folders.Invalidate();
    }

    static readonly Regex RefRx = new(@"\b(DOS-\d{4}-[A-Z]?\d+|M-\d{4}-[A-Z]?\d+|DEC-\d{4}-\d+|DOC-\d{4}-\d+|P-\d{6})\b");

    void ShowMsg(long id)
    {
        current = id;
        var x = Db.Rows(@"SELECT m.date, m.exp_id, m.exp_ext, m.dest_id, m.cc, m.objet, m.corps, m.mission_id, m.dossier_id, m.document_id, m.organisme_id, IFNULL(m.conversation_id, m.id), m.ref_type, m.ref_id, m.lu
            FROM messages m WHERE m.id=@p0", id);
        if (x.Count == 0) return;
        var r = x[0];
        if (U.L(r[3]) == Auth.CollabId && U.L(r[14]) == 0) { Db.Exec("UPDATE messages SET lu=1 WHERE id=@p0", id); if (list.SelectedItem is Msg mm && mm.NonLu) { list.Items[list.SelectedIndex] = mm with { NonLu = false }; } UpdateCounts(); }
        rSubject.Text = U.S(r[5]);
        string de = U.L(r[1]) != 0 ? Db.Str("SELECT prenom||' '||nom||' — '||fonction||', '||(SELECT nom FROM services WHERE id=service_id) FROM collaborateurs WHERE id=@p0", U.L(r[1])) : U.S(r[2]) + (U.L(r[10]) != 0 ? " — " + Db.Str("SELECT categorie FROM organismes WHERE id=@p0", U.L(r[10])) + " (simulé)" : "");
        string a = Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", U.L(r[3])) ?? "";
        string cc = U.S(r[4]) == "" ? "" : "   Cc : " + string.Join(", ", U.S(r[4]).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => long.TryParse(s, out var v) ? Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", v) : s));
        rFrom.Text = $"De : {de}\nÀ : {a}{cc}     ·     {U.AffJ(U.P(r[0]))} à {U.P(r[0]):HH:mm}";
        rLinks.Controls.Clear();
        void Link(string t, string type, long rid) { var l = new LinkLabel { Text = t, AutoSize = true, Margin = new Padding(0, 6, 14, 0), LinkColor = Theme.Accent }; l.Click += (s, e) => Ui.Try(() => MainForm.Instance.OpenRef(type, rid)); rLinks.Controls.Add(new PictureBox { Image = Icons.Get(type == "dossier" ? "dossier" : type == "mission" ? "delegation" : type == "decision" ? "decision" : type == "personne" ? "personne" : "document", 16, Theme.Accent), Size = new Size(18, 24), SizeMode = PictureBoxSizeMode.CenterImage }); rLinks.Controls.Add(l); }
        var vus = new HashSet<string>();
        if (U.L(r[8]) != 0) { var n = Db.Str("SELECT numero FROM dossiers WHERE id=@p0", U.L(r[8])); vus.Add(n); Link("Dossier " + n, "dossier", U.L(r[8])); }
        if (U.L(r[7]) != 0) { var n = Db.Str("SELECT numero FROM missions WHERE id=@p0", U.L(r[7])); vus.Add(n); Link("Mission " + n, "mission", U.L(r[7])); }
        if (U.S(r[12]) == "decision" && U.L(r[13]) != 0) { var n = Db.Str("SELECT ref FROM decisions WHERE id=@p0", U.L(r[13])); vus.Add(n); Link("Décision " + n, "decision", U.L(r[13])); }
        if (U.L(r[9]) != 0) { var n = Db.Str("SELECT numero FROM documents WHERE id=@p0", U.L(r[9])); vus.Add(n); Link("Pièce jointe " + n, "document", U.L(r[9])); }
        foreach (Match m in RefRx.Matches(U.S(r[6]) + " " + U.S(r[5])))
        {
            if (!vus.Add(m.Value)) continue;
            string v = m.Value;
            if (v.StartsWith("DOS-")) { long i = Db.Long("SELECT id FROM dossiers WHERE numero=@p0", v); if (i != 0) Link("Dossier " + v, "dossier", i); }
            else if (v.StartsWith("M-")) { long i = Db.Long("SELECT id FROM missions WHERE numero=@p0", v); if (i != 0) Link("Mission " + v, "mission", i); }
            else if (v.StartsWith("DEC-")) { long i = Db.Long("SELECT id FROM decisions WHERE ref=@p0", v); if (i != 0) Link("Décision " + v, "decision", i); }
            else if (v.StartsWith("DOC-")) { long i = Db.Long("SELECT id FROM documents WHERE numero=@p0", v); if (i != 0) Link("Document " + v, "document", i); }
            else if (v.StartsWith("P-")) { long i = Db.Long("SELECT id FROM personnes WHERE ref=@p0", v); if (i != 0) Link("Personne " + v, "personne", i); }
        }
        if (rLinks.Controls.Count == 0) rLinks.Controls.Add(new Label { Text = "Aucun élément lié.", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 6, 0, 0) });
        rBody.Clear();
        Rtf.P(rBody, U.S(r[6]).Replace("\r", ""));
        rBody.SelectionStart = 0;
        long conv = U.L(r[11]);
        var th = Db.Rows("SELECT id, substr(date,1,16)||'  ·  '||IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id), exp_ext)||'  ·  '||objet FROM messages WHERE (conversation_id=@p0 OR id=@p0) AND brouillon=0 ORDER BY date", conv);
        rThread.Items.Clear(); foreach (var t in th) rThread.Items.Add(new Item(U.L(t[0]), (U.L(t[0]) == id ? "▶ " : "   ") + U.S(t[1])));
        rThreadTitle.Text = $"Historique des échanges ({th.Count} message(s) dans ce fil) — double-clic pour afficher";
    }

    void Reply(bool all)
    {
        if (current == 0) return;
        var x = Db.Rows("SELECT exp_id, objet, corps, mission_id, dossier_id, cc, dest_id, date, IFNULL(exp_ext,'') FROM messages WHERE id=@p0", current)[0];
        long exp = U.L(x[0]);
        if (exp == 0) { Ui.Info("Ce message provient d'un interlocuteur extérieur simulé : utilisez la téléphonie, un courrier de simulation ou une décision sur le dossier."); return; }
        var cc = all ? U.S(x[5]).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => long.TryParse(s, out var v) ? v : 0).Where(v => v != 0).Append(U.L(x[6])).ToList() : null;
        string quote = $"\r\n\r\n----- Le {U.Aff(x[7])}, {(exp != 0 ? Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", exp) : x[8])} a écrit -----\r\n" + U.S(x[2]).Replace("\n", "\r\n");
        using var d = new MessageDialog(exp == Auth.CollabId ? U.L(x[6]) : exp, U.S(x[1]).StartsWith("RE: ") ? U.S(x[1]) : "RE: " + x[1], quote, U.L(x[3]), U.L(x[4]), cc, 0, current);
        if (d.ShowDialog() == DialogResult.OK) LoadList();
    }

    void Forward()
    {
        if (current == 0) return;
        var x = Db.Rows("SELECT objet, corps, mission_id, dossier_id FROM messages WHERE id=@p0", current)[0];
        using var d = new MessageDialog(0, "TR: " + x[0], "\r\n\r\n----- Message transféré -----\r\n" + U.S(x[1]).Replace("\n", "\r\n"), U.L(x[2]), U.L(x[3]));
        if (d.ShowDialog() == DialogResult.OK) LoadList();
    }

    void OpenDraft()
    {
        if (current == 0) return;
        var x = Db.Rows("SELECT dest_id, objet, corps, mission_id, dossier_id, brouillon FROM messages WHERE id=@p0 AND exp_id=@p1", current, Auth.CollabId);
        if (x.Count == 0 || U.L(x[0][5]) == 0) { Ui.Info("Sélectionnez un brouillon (dossier « Brouillons »)."); return; }
        using var d = new MessageDialog(U.L(x[0][0]), U.S(x[0][1]), U.S(x[0][2]).Replace("\n", "\r\n"), U.L(x[0][3]), U.L(x[0][4]), null, current);
        if (d.ShowDialog() == DialogResult.OK) { current = 0; LoadList(); }
    }

    void SetEtat(string etat)
    {
        if (current == 0) return;
        long me = Auth.CollabId;
        Db.Exec("UPDATE messages SET etat_dest=@p1, mail_dossier_id=CASE WHEN @p1='actif' THEN mail_dossier_id ELSE NULL END WHERE id=@p0 AND dest_id=@p2", current, etat, me);
        Db.Exec("UPDATE messages SET etat_exp=@p1 WHERE id=@p0 AND exp_id=@p2", current, etat, me);
        if (etat == "corbeille" && folders.SelectedItem is Folder f && f.Key == "trash" && Ui.Confirm("Supprimer définitivement ce message ?")) Db.Exec("DELETE FROM messages WHERE id=@p0 AND (dest_id=@p1 OR exp_id=@p1)", current, me);
        current = 0; LoadList();
    }

    void Toggle(string col) { if (current == 0) return; Db.Exec($"UPDATE messages SET {col}=1-{col} WHERE id=@p0", current); LoadList(); }

    void FillMove()
    {
        move.DropDownItems.Clear();
        move.DropDownItems.Add("Boîte de réception", null, (s, e) => MoveTo(0));
        foreach (var r in Db.Rows("SELECT id, nom FROM mail_dossiers WHERE owner_id=@p0 ORDER BY nom", Auth.CollabId)) { long fid = U.L(r[0]); move.DropDownItems.Add(U.S(r[1]), Icons.Get("dossier", 16, Theme.Blue), (s, e) => MoveTo(fid)); }
        move.DropDownItems.Add(new ToolStripSeparator());
        move.DropDownItems.Add("Nouveau dossier…", null, (s, e) => { var id = NewFolder(); if (id != 0) MoveTo(id); });
    }

    void MoveTo(long fid)
    {
        if (current == 0) return;
        Db.Exec("UPDATE messages SET mail_dossier_id=@p1, etat_dest='actif' WHERE id=@p0 AND dest_id=@p2", current, fid == 0 ? null : fid, Auth.CollabId);
        current = 0; LoadList();
    }

    long NewFolder()
    {
        var n = Ui.Prompt("Nouveau dossier", "Nom du dossier :");
        if (string.IsNullOrWhiteSpace(n)) return 0;
        long id = Db.Insert("INSERT INTO mail_dossiers(owner_id,nom) VALUES(@p0,@p1)", Auth.CollabId, n.Trim());
        BuildFolders(); return id;
    }
    void RenameFolder()
    {
        if (folders.SelectedItem is not Folder f || f.Key != "custom") { Ui.Info("Sélectionnez un dossier personnel."); return; }
        var n = Ui.Prompt("Renommer", "Nouveau nom :", f.Nom); if (string.IsNullOrWhiteSpace(n)) return;
        Db.Exec("UPDATE mail_dossiers SET nom=@p1 WHERE id=@p0", f.Custom, n.Trim()); BuildFolders();
    }
    void DeleteFolder()
    {
        if (folders.SelectedItem is not Folder f || f.Key != "custom") { Ui.Info("Sélectionnez un dossier personnel."); return; }
        if (!Ui.Confirm($"Supprimer le dossier « {f.Nom} » ? Ses messages retourneront dans la boîte de réception.")) return;
        Db.Tx(c => { c.Exec("UPDATE messages SET mail_dossier_id=NULL WHERE mail_dossier_id=@p0", f.Custom); c.Exec("DELETE FROM mail_dossiers WHERE id=@p0", f.Custom); });
        folders.SelectedIndex = 0; BuildFolders();
    }

    public override void Nouveau() { using var d = new MessageDialog(); if (d.ShowDialog() == DialogResult.OK) LoadList(); }
    public override void RefreshData() => LoadList();
    public int TestFolders()
    {
        int n = 0;
        for (int i = 0; i < folders.Items.Count; i++) { folders.SelectedIndex = i; Application.DoEvents(); if (list.Items.Count > 0) { list.SelectedIndex = 0; Application.DoEvents(); n++; } }
        folders.SelectedIndex = 0; return n;
    }
}
