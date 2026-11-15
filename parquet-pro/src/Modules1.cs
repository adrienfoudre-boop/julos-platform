using System.Data;

namespace ParquetPro;

// ============================================================ MON CABINET
public class CabinetModule : Module
{
    public override string Titre => "Mon cabinet";
    readonly Kpi kMsg, kRap, kDem, kDel, kRet, kReu, kDos, kAl;
    readonly GridPanel gMsg, gRap, gDem, gDel, gReu, gSyn;
    readonly Label lSynth = new() { Dock = DockStyle.Fill, Padding = new Padding(8), Font = new Font("Consolas", 9f), ForeColor = Theme.Anthracite };

    public CabinetModule()
    {
        long me = Auth.CollabId;
        long adj = long.Parse(Db.Get("adjoint_id", "0")), secp = long.Parse(Db.Get("secp_id", "0")), sg = long.Parse(Db.Get("sg_id", "0"));
        var kp = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(4), WrapContents = false, AutoScroll = true };
        kMsg = new Kpi("Messages non lus", Theme.Accent); kRap = new Kpi("Rapports à décider", Theme.Orange); kDem = new Kpi("Demandes en attente", Theme.Orange); kDel = new Kpi("Délégations actives", Theme.Blue);
        kRet = new Kpi("Missions en retard", Theme.Red); kReu = new Kpi("Réunions aujourd'hui", Theme.Green); kDos = new Kpi("Dossiers prioritaires", Theme.Palette[3]); kAl = new Kpi("Alertes non lues", Theme.Red);
        kMsg.Click += (s, e) => MainForm.Instance.ShowModule("Messagerie"); kRap.Click += (s, e) => MainForm.Instance.ShowModule("Délégations"); kDel.Click += (s, e) => MainForm.Instance.ShowModule("Délégations");
        kReu.Click += (s, e) => MainForm.Instance.ShowModule("Agenda"); kAl.Click += (s, e) => MainForm.Instance.ShowModule("Notifications"); kDos.Click += (s, e) => MainForm.Instance.ShowModule("Dossiers");
        kp.Controls.AddRange(new Control[] { kMsg, kRap, kDem, kDel, kRet, kReu, kDos, kAl });

        var bar = Ui.Bar(
            Ui.PrimaryBtn("✎ Nouvelle délégation", (s, e) => { MainForm.Instance.NewMission(); }),
            Ui.Btn("Instruction au procureur adjoint", (s, e) => { using var d = new MissionDialog(adj); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }),
            Ui.Btn("Demander une synthèse à l'adjoint", (s, e) => Ui.Try(() =>
            {
                Auth.Check("missions.creer");
                Db.Tx(c => Biz.CreerMission(c, Auth.CollabId, adj, $"Synthèse de situation des services — {Engine.Now:dd/MM HH:mm}", "Établir une synthèse consolidée de la situation des services.", "Haute", Engine.Now.AddDays(1).Date.AddHours(17), "Consolider les retours des chefs de service.", "Interne", 0, 0, 0, null, "Utilisateur"));
                Ui.Info("Mission de synthèse confiée au procureur adjoint."); RefreshData();
            })),
            Ui.Btn("Message au secrétaire particulier", (s, e) => { using var d = new MessageDialog(secp); d.ShowDialog(); }),
            Ui.Btn("Message au secrétaire général", (s, e) => { using var d = new MessageDialog(sg); d.ShowDialog(); }),
            Ui.Btn("Convoquer une réunion de direction", (s, e) =>
            {
                using var d = new ReunionDialog(0, Engine.Now.Date.AddDays(1).AddHours(9));
                d.ShowDialog(); RefreshData();
            }),
            Ui.Btn("Synthèse de situation", (s, e) => Ui.ShowText("Synthèse de situation", Db.Q(c => Biz.Synthese(c)))));

        gMsg = new GridPanel("cab_msg") { PageSize = 50, Loader = (q, o, l) => Db.Table(@"SELECT id, lu AS 'Lu', important AS '!', substr(date,6,11) AS 'Date', IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id), exp_ext) AS 'De', objet AS 'Objet'
            FROM messages WHERE dest_id=@p0 AND etat_dest='actif' AND brouillon=0 AND (lu=0 OR important=1) AND objet LIKE @p1 ORDER BY important DESC, date DESC LIMIT @p2 OFFSET @p3", me, U.Like(q), l, o) };
        gMsg.Activated += id => { MessageViewer.Open(this, id); RefreshData(); };
        gRap = new GridPanel("cab_rap") { PageSize = 50, Loader = (q, o, l) => Db.Table(@"SELECT id, numero AS 'N°', objet AS 'Objet', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=destinataire_id) AS 'Responsable', substr(date_rapport,1,16) AS 'Transmis le'
            FROM missions WHERE emetteur_id=@p0 AND statut='Rapport transmis' AND objet LIKE @p1 ORDER BY date_rapport DESC LIMIT @p2 OFFSET @p3", me, U.Like(q), l, o) };
        gRap.Activated += id => new MissionView(id).Show(this);
        gDem = new GridPanel("cab_dem") { PageSize = 50, Loader = (q, o, l) => Db.Table(@"SELECT id, substr(date,6,11) AS 'Date', type AS 'Type', objet AS 'Objet', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=demandeur_id) AS 'Demandeur'
            FROM demandes WHERE statut='En attente' AND objet LIKE @p0 ORDER BY id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gDem.Activated += id => { using var d = new DemandeDialog(id); d.ShowDialog(); RefreshData(); };
        gDel = new GridPanel("cab_del") { PageSize = 50, Loader = (q, o, l) => Db.Table($@"SELECT id, numero AS 'N°', objet AS 'Objet', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=destinataire_id) AS 'Destinataire', statut AS 'Statut', ROUND(avancement) AS 'Avancement', substr(echeance,1,16) AS 'Échéance'
            FROM missions WHERE emetteur_id=@p0 AND parent_id IS NULL AND statut IN {Biz.SqlActifs} AND objet LIKE @p1 ORDER BY echeance LIMIT @p2 OFFSET @p3", me, U.Like(q), l, o) };
        gDel.Activated += id => new MissionView(id).Show(this);
        gReu = new GridPanel("cab_reu") { PageSize = 50, Loader = (q, o, l) => Db.Table(@"SELECT r.id, substr(r.debut,1,16) AS 'Début', r.titre AS 'Titre', r.lieu AS 'Lieu', r.statut AS 'Statut' FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id
            WHERE p.collab_id=@p0 AND r.fin>=@p1 AND r.statut IN ('Planifiée','Proposée','En cours') AND r.titre LIKE @p2 ORDER BY r.debut LIMIT @p3 OFFSET @p4", me, U.D(Engine.Now), U.Like(q), l, o) };
        gReu.Activated += id => { using var d = new ReunionDialog(id); d.ShowDialog(); RefreshData(); };
        gSyn = new GridPanel("cab_syn") { PageSize = 50, Loader = (q, o, l) => Db.Table(@"SELECT id, substr(date,6,11) AS 'Date', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id) AS 'De', objet AS 'Objet' FROM messages
            WHERE dest_id=@p0 AND categorie IN ('Synthèse','Bilan','Rapport','Agenda','Dossier','Demande') AND objet LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", me, U.Like(q), l, o) };
        gSyn.Activated += id => { MessageViewer.Open(this, id); };

        var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(4) };
        for (int i = 0; i < 3; i++) tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tl.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        tl.Controls.Add(Card(gMsg, "Messages prioritaires et non lus"), 0, 0);
        tl.Controls.Add(Card(gRap, "Rapports en attente de décision"), 1, 0);
        tl.Controls.Add(Card(gDem, "Demandes de validation et d'arbitrage"), 2, 0);
        tl.Controls.Add(Card(gDel, "Missions déléguées en cours"), 0, 1);
        tl.Controls.Add(Card(gReu, "Réunions et rendez-vous à venir"), 1, 1);
        tl.Controls.Add(Card(gSyn, "Synthèses, bilans et comptes rendus"), 2, 1);
        Controls.Add(tl); Controls.Add(bar); Controls.Add(kp); Controls.Add(Ui.Title("Mon cabinet — tableau de bord personnel"));
    }

    public override void Nouveau() => MainForm.Instance.NewMission();

    public override void RefreshData()
    {
        long me = Auth.CollabId; string now = U.D(Engine.Now);
        var r = Db.Rows($@"SELECT (SELECT COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND etat_dest='actif' AND brouillon=0),
            (SELECT COUNT(*) FROM missions WHERE emetteur_id=@p0 AND statut='Rapport transmis'),
            (SELECT COUNT(*) FROM demandes WHERE statut='En attente'),
            (SELECT COUNT(*) FROM missions WHERE emetteur_id=@p0 AND statut IN {Biz.SqlActifs}),
            (SELECT COUNT(*) FROM missions WHERE statut IN {Biz.SqlActifs} AND echeance<@p1),
            (SELECT COUNT(*) FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND substr(r.debut,1,10)=@p2 AND r.statut<>'Annulée'),
            (SELECT COUNT(*) FROM dossiers WHERE priorite='Haute' AND statut<>'Clôturé' AND archive=0),
            (SELECT COUNT(*) FROM notifications WHERE lu=0 AND archive=0)", me, now, Engine.Now.ToString("yyyy-MM-dd"))[0];
        var ks = new[] { kMsg, kRap, kDem, kDel, kRet, kReu, kDos, kAl };
        for (int i = 0; i < ks.Length; i++) ks[i].Set(r[i]);
        foreach (var g in new[] { gMsg, gRap, gDem, gDel, gReu, gSyn }) g.Reload();
    }
}

// ============================================================ DÉLÉGATIONS
public class DelegationsModule : Module
{
    public override string Titre => "Mes délégations";
    readonly GridPanel grid;
    readonly ComboBox filtre = Ui.Combo(new[] { "Mes délégations (niveau 1)", "Toute la chaîne de mes délégations", "Rapports à valider", "Missions en retard", "Toutes les missions actives de l'organisation", "Missions terminées / clôturées", "Toutes les missions" }, 280);
    readonly ComboBox cSvc = Ui.Combo(Item.Services(true), 260);
    readonly TextBox preview = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9f), BackColor = Color.White, WordWrap = false };

    public DelegationsModule()
    {
        grid = new GridPanel("delegations") { Loader = LoadData };
        grid.Activated += id => new MissionView(id).Show(this);
        grid.SelChanged += () => { long id = grid.SelectedId; preview.Text = id == 0 ? "" : Db.Q(c => Biz.Chaine(c, id)).Replace("\n", "\r\n"); };
        filtre.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        cSvc.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        var bar = Ui.Bar(Ui.PrimaryBtn("✎ Nouvelle délégation", (s, e) => Nouveau()), Ui.Btn("Ouvrir", (s, e) => Open()),
            Ui.Btn("Accepter le rapport", (s, e) => Act(id => Db.Tx(c => Biz.ValiderMission(c, id, "")))),
            Ui.Btn("Demander des modifications", (s, e) => { var t = Ui.Prompt("Modifications", "Modifications demandées :", "", true); if (!string.IsNullOrWhiteSpace(t)) Act(id => Db.Tx(c => Biz.DemanderModification(c, id, t))); }),
            Ui.Btn("Refuser", (s, e) => { var t = Ui.Prompt("Refus", "Motif :"); if (!string.IsNullOrWhiteSpace(t)) Act(id => Db.Tx(c => Biz.RefuserMission(c, id, t))); }),
            Ui.Btn("Clôturer", (s, e) => { if (Ui.Confirm("Clôturer la mission sélectionnée ?")) Act(id => Db.Tx(c => Biz.CloturerMission(c, id))); }),
            new Label { Text = "Vue :", AutoSize = true, Padding = new Padding(10, 8, 0, 0) }, filtre, new Label { Text = "Service :", AutoSize = true, Padding = new Padding(6, 8, 0, 0) }, cSvc);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 380 };
        split.Panel1.Controls.Add(grid);
        split.Panel2.Controls.Add(Card(preview, "Chaîne de délégation et sous-tâches de la mission sélectionnée"));
        Controls.Add(split); Controls.Add(bar); Controls.Add(Ui.Title("Mes délégations — suivi des missions"));
        Load += (s, e) => { try { split.SplitterDistance = Math.Max(200, split.Height * 6 / 10); } catch { } };
    }

    DataTable LoadData(string q, int o, int l)
    {
        long me = Auth.CollabId; string now = U.D(Engine.Now);
        string w = filtre.SelectedIndex switch
        {
            0 => "m.emetteur_id=@p0 AND m.parent_id IS NULL",
            1 => "m.racine_id IN (SELECT id FROM missions WHERE emetteur_id=@p0 AND parent_id IS NULL)",
            2 => "m.emetteur_id=@p0 AND m.statut='Rapport transmis'",
            3 => $"m.statut IN {Biz.SqlActifs} AND m.echeance<@p1",
            4 => $"m.statut IN {Biz.SqlActifs}",
            5 => "m.statut IN ('Validée','Clôturée','Refusée')",
            _ => "1=1"
        };
        long svc = Item.Of(cSvc);
        if (svc != 0) w += " AND m.service_id=" + svc;
        return Db.Table($@"SELECT m.id, m.numero AS 'N°', m.objet AS 'Objet', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=m.emetteur_id) AS 'Émetteur',
            (SELECT prenom||' '||nom FROM collaborateurs WHERE id=m.destinataire_id) AS 'Responsable', (SELECT code FROM services WHERE id=m.service_id) AS 'Service', m.priorite AS 'Priorité',
            CASE WHEN m.statut IN {Biz.SqlActifs} AND m.echeance<@p1 THEN 'En retard' ELSE m.statut END AS 'Statut', ROUND(m.avancement) AS 'Avancement', m.profondeur AS 'Niveau',
            substr(m.date_creation,1,16) AS 'Créée le', substr(m.echeance,1,16) AS 'Échéance', m.origine AS 'Origine', m.confidentialite AS 'Confidentialité'
            FROM missions m WHERE {w} AND (m.objet LIKE @p2 OR m.numero LIKE @p2) ORDER BY m.id DESC LIMIT @p3 OFFSET @p4", me, now, U.Like(q), l, o);
    }

    void Open() { if (grid.SelectedId != 0) new MissionView(grid.SelectedId).Show(this); }
    void Act(Action<long> a) { long id = grid.SelectedId; if (id == 0) { Ui.Info("Sélectionnez une mission."); return; } if (Ui.Try(() => a(id))) RefreshData(); }
    public override void Nouveau() { using var d = new MissionDialog(); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public override void RefreshData() => grid.Reload();
}

// ============================================================ DOSSIERS
public class DossiersModule : Module
{
    public override string Titre => "Dossiers";
    readonly GridPanel grid;
    readonly ComboBox cStat = Ui.Combo(new[] { "Dossiers actifs", "Ouverts", "En cours d'instruction", "En attente", "Clôturés", "Archivés", "Tous" }, 170);
    readonly ComboBox cSvc = Ui.Combo(Item.Services(true), 240);
    readonly ComboBox cCat = Ui.Combo(new[] { "(Toutes catégories)" }.Concat(Seed.Categories), 200);
    readonly ComboBox cPrio = Ui.Combo(new[] { "(Toutes priorités)", "Haute", "Normale", "Basse" }, 130);
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly TextBox tFiche = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 9f), BackColor = Color.White };
    readonly GridPanel gNotes, gHist, gMis, gDocs;

    public DossiersModule()
    {
        grid = new GridPanel("dossiers") { Loader = LoadData };
        grid.Activated += id => Edit(id);
        grid.SelChanged += ShowDetail;
        foreach (var c in new[] { cStat, cSvc, cCat, cPrio }) c.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        gNotes = new GridPanel("dos_notes") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id) AS 'Auteur', texte AS 'Texte' FROM dossier_notes WHERE dossier_id=@p0 AND texte LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", grid.SelectedId, U.Like(q), l, o) };
        gHist = new GridPanel("dos_hist") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id) AS 'Auteur', texte AS 'Détail' FROM dossier_hist WHERE dossier_id=@p0 AND texte LIKE @p1 ORDER BY date DESC, id DESC LIMIT @p2 OFFSET @p3", grid.SelectedId, U.Like(q), l, o) };
        gMis = new GridPanel("dos_mis") { Loader = (q, o, l) => Db.Table("SELECT id, numero AS 'N°', objet AS 'Objet', statut AS 'Statut', ROUND(avancement) AS 'Avancement', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=destinataire_id) AS 'Responsable' FROM missions WHERE dossier_id=@p0 AND objet LIKE @p1 ORDER BY id DESC LIMIT @p2 OFFSET @p3", grid.SelectedId, U.Like(q), l, o) };
        gMis.Activated += id => new MissionView(id).Show(this);
        gDocs = new GridPanel("dos_docs") { Loader = (q, o, l) => Db.Table("SELECT id, numero AS 'N°', titre AS 'Titre', categorie AS 'Catégorie', substr(date,1,16) AS 'Date' FROM documents WHERE dossier_id=@p0 AND titre LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", grid.SelectedId, U.Like(q), l, o) };
        gDocs.Activated += id => { using var d = new DocumentDialog(id); d.ShowDialog(); };
        void Tab(string t, Control c) { var tp = new TabPage(t); tp.Controls.Add(c); tabs.TabPages.Add(tp); }
        Tab("Fiche", tFiche); Tab("Notes", gNotes); Tab("Historique", gHist); Tab("Tâches et missions liées", gMis); Tab("Documents", gDocs);
        tabs.SelectedIndexChanged += (s, e) => ShowDetail();

        var bar = Ui.Bar(Ui.PrimaryBtn("Nouveau dossier", (s, e) => Nouveau()), Ui.Btn("Modifier / affecter", (s, e) => Edit(grid.SelectedId)),
            Ui.Btn("Ajouter une note", (s, e) => Sel(id => { var t = Ui.Prompt("Note", "Texte de la note :", "", true); if (!string.IsNullOrWhiteSpace(t)) Db.Tx(c => Biz.NoteDossier(c, id, t)); })),
            Ui.Btn("Ajouter un document", (s, e) => Sel(id => { using var d = new DocumentDialog(0, "Documents de suivi", "", "", id); d.ShowDialog(); })),
            Ui.Btn("Créer une tâche liée", (s, e) => Sel(id =>
            {
                long refe = Db.Long("SELECT IFNULL(referent_id,0) FROM dossiers WHERE id=@p0", id);
                using var d = new MissionDialog(refe == 0 ? 0 : refe, id, "Traitement du dossier " + Db.Str("SELECT numero FROM dossiers WHERE id=@p0", id)); d.ShowDialog();
            })),
            Ui.Btn("Archiver / désarchiver", (s, e) => Sel(id => { bool a = Db.Long("SELECT archive FROM dossiers WHERE id=@p0", id) == 0; if (Ui.Confirm(a ? "Archiver ce dossier ?" : "Désarchiver ce dossier ?")) Db.Tx(c => Biz.ArchiverDossier(c, id, a)); })),
            Ui.Btn("Fiche récapitulative", (s, e) => Sel(id =>
            {
                var txt = Db.Q(c => Biz.FicheDossier(c, id));
                if (Ui.Confirm("Enregistrer également la fiche dans la bibliothèque documentaire ?"))
                    Db.Tx(c => Biz.EnregistrerDocument(c, 0, "Fiche récapitulative — " + c.Str("SELECT numero FROM dossiers WHERE id=@p0", id), "Fiches de dossier", txt, id, 0));
                Ui.ShowText("Fiche récapitulative", txt);
            })),
            new Label { Text = "Statut :", AutoSize = true, Padding = new Padding(8, 8, 0, 0) }, cStat, cSvc, cCat, cPrio);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
        split.Panel1.Controls.Add(grid); split.Panel2.Controls.Add(tabs);
        Controls.Add(split); Controls.Add(bar); Controls.Add(Ui.Title("Gestion des dossiers de simulation"));
        Load += (s, e) => { try { split.SplitterDistance = split.Width * 58 / 100; } catch { } };
    }

    void Sel(Action<long> a) { long id = grid.SelectedId; if (id == 0) { Ui.Info("Sélectionnez un dossier."); return; } if (Ui.Try(() => a(id))) RefreshData(); }

    DataTable LoadData(string q, int o, int l)
    {
        var w = new List<string>();
        w.Add(cStat.SelectedIndex switch { 0 => "d.statut<>'Clôturé' AND d.archive=0", 1 => "d.statut='Ouvert' AND d.archive=0", 2 => "d.statut='En cours d''instruction' AND d.archive=0", 3 => "d.statut='En attente' AND d.archive=0", 4 => "d.statut='Clôturé'", 5 => "d.archive=1", _ => "1=1" });
        if (Item.Of(cSvc) != 0) w.Add("d.service_id=" + Item.Of(cSvc));
        if (cCat.SelectedIndex > 0) w.Add("d.categorie=@p4");
        if (cPrio.SelectedIndex > 0) w.Add("d.priorite=@p5");
        return Db.Table($@"SELECT d.id, d.numero AS 'N°', d.intitule AS 'Intitulé', d.categorie AS 'Catégorie', (SELECT code FROM services WHERE id=d.service_id) AS 'Service',
            (SELECT prenom||' '||nom FROM collaborateurs WHERE id=d.referent_id) AS 'Référent', d.statut AS 'Statut', d.priorite AS 'Priorité', d.avancement AS 'Avancement',
            substr(d.date_ouverture,1,10) AS 'Ouverture', substr(d.echeance,1,10) AS 'Échéance', (SELECT nom FROM organismes WHERE id=d.organisme_id) AS 'Origine'
            FROM dossiers d WHERE {string.Join(" AND ", w)} AND (d.intitule LIKE @p0 OR d.numero LIKE @p0 OR d.resume LIKE @p0) ORDER BY d.id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o, 0, cCat.Text, cPrio.Text);
    }

    void ShowDetail()
    {
        long id = grid.SelectedId;
        if (id == 0) { tFiche.Text = ""; return; }
        switch (tabs.SelectedIndex)
        {
            case 0: tFiche.Text = Db.Q(c => Biz.FicheDossier(c, id)).Replace("\n", "\r\n"); break;
            case 1: gNotes.Reload(); break;
            case 2: gHist.Reload(); break;
            case 3: gMis.Reload(); break;
            case 4: gDocs.Reload(); break;
        }
    }

    void Edit(long id) { if (id == 0) return; using var d = new DossierDialog(id); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public void Focus(long id)
    {
        grid.Search.Text = Db.Str("SELECT numero FROM dossiers WHERE id=@p0", id) ?? "";
        cStat.SelectedIndex = 6; grid.ResetPage(); grid.Reload(); ShowDetail();
    }
    public override void Nouveau() { using var d = new DossierDialog(); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public override void RefreshData() { grid.Reload(); ShowDetail(); }
}

// ============================================================ MESSAGERIE
public class MessagerieModule : Module
{
    public override string Titre => "Messagerie";
    readonly ListBox folders = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), ItemHeight = 26, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false };
    readonly string[] names = { "Boîte de réception", "Messages prioritaires", "Suivis", "Messages envoyés", "Brouillons", "Messages archivés", "Corbeille", "Flux interne des services", "Communications extérieures" };
    readonly GridPanel grid;
    readonly Label head = new() { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Light, Padding = new Padding(8, 4, 4, 4) };
    readonly TextBox body = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Font = new Font("Segoe UI", 10f) };
    readonly FlowLayoutPanel links = new() { Dock = DockStyle.Bottom, Height = 36 };
    long[] counts = new long[9];

    public MessagerieModule()
    {
        foreach (var n in names) folders.Items.Add(n);
        folders.SelectedIndex = 0;
        folders.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(204, 224, 245) : Color.White), e.Bounds);
            e.Graphics.DrawString(names[e.Index] + (counts[e.Index] > 0 ? $"  ({counts[e.Index]})" : ""), counts[e.Index] > 0 && e.Index < 2 ? new Font("Segoe UI", 10f, FontStyle.Bold) : folders.Font, new SolidBrush(Theme.Navy), e.Bounds.X + 8, e.Bounds.Y + 4);
        };
        folders.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); Preview(); };
        grid = new GridPanel("messages") { Loader = LoadData };
        grid.SelChanged += Preview;
        grid.Activated += id => MessageViewer.Open(this, id);
        var bar = Ui.Bar(Ui.PrimaryBtn("✉ Nouveau message", (s, e) => Nouveau()), Ui.Btn("Répondre", (s, e) => Reply(false)), Ui.Btn("Répondre à tous", (s, e) => Reply(true)), Ui.Btn("Transférer", (s, e) => Forward()),
            Ui.Btn("Archiver", (s, e) => SetEtat("archive")), Ui.Btn("Supprimer", (s, e) => SetEtat("corbeille")), Ui.Btn("Restaurer", (s, e) => SetEtat("actif")),
            Ui.Btn("★ Important", (s, e) => Toggle("important")), Ui.Btn("⚐ Suivi", (s, e) => Toggle("suivi")), Ui.Btn("Lu / non lu", (s, e) => Toggle("lu")),
            Ui.Btn("Ouvrir le brouillon", (s, e) => OpenDraft()));
        var left = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Color.White, Padding = new Padding(4) };
        left.Controls.Add(folders);
        left.Controls.Add(new Label { Text = "Dossiers", Dock = DockStyle.Top, Height = 26, Font = Theme.FB, ForeColor = Theme.Navy });
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(grid);
        var pv = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pv.Controls.Add(body); pv.Controls.Add(head); pv.Controls.Add(links);
        split.Panel2.Controls.Add(pv);
        Controls.Add(split); Controls.Add(new Splitter { Dock = DockStyle.Left, Width = 4 }); Controls.Add(left); Controls.Add(bar); Controls.Add(Ui.Title("Messagerie interne"));
        Load += (s, e) => { try { split.SplitterDistance = split.Height / 2; } catch { } };
    }

    DataTable LoadData(string q, int o, int l)
    {
        long me = Auth.CollabId;
        string w = folders.SelectedIndex switch
        {
            0 => "m.dest_id=@p0 AND m.etat_dest='actif' AND m.brouillon=0",
            1 => "m.dest_id=@p0 AND m.important=1 AND m.etat_dest<>'corbeille' AND m.brouillon=0",
            2 => "m.suivi=1 AND (m.dest_id=@p0 OR m.exp_id=@p0)",
            3 => "m.exp_id=@p0 AND m.brouillon=0 AND m.etat_exp='actif'",
            4 => "m.exp_id=@p0 AND m.brouillon=1",
            5 => "((m.dest_id=@p0 AND m.etat_dest='archive') OR (m.exp_id=@p0 AND m.etat_exp='archive'))",
            6 => "((m.dest_id=@p0 AND m.etat_dest='corbeille') OR (m.exp_id=@p0 AND m.etat_exp='corbeille'))",
            7 => "IFNULL(m.exp_id,0)<>@p0 AND IFNULL(m.dest_id,0)<>@p0 AND m.exp_id IS NOT NULL",
            _ => "m.organisme_id IS NOT NULL"
        };
        return Db.Table($@"SELECT m.id, CASE WHEN m.dest_id=@p0 THEN m.lu ELSE 1 END AS 'Lu', m.important AS '!', substr(m.date,1,16) AS 'Date',
            IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=m.exp_id), m.exp_ext) AS 'De', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=m.dest_id) AS 'À',
            m.objet AS 'Objet', m.categorie AS 'Catégorie', (SELECT numero FROM missions WHERE id=m.mission_id) AS 'Mission', (SELECT numero FROM dossiers WHERE id=m.dossier_id) AS 'Dossier'
            FROM messages m WHERE {w} AND (m.objet LIKE @p1 OR m.corps LIKE @p1 OR IFNULL(m.exp_ext,'') LIKE @p1) ORDER BY m.date DESC, m.id DESC LIMIT @p2 OFFSET @p3", me, U.Like(q), l, o);
    }

    void Preview()
    {
        links.Controls.Clear();
        long id = grid.SelectedId;
        if (id == 0) { head.Text = ""; body.Text = ""; return; }
        var x = Db.Rows("SELECT date, IFNULL((SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=exp_id), exp_ext), (SELECT prenom||' '||nom FROM collaborateurs WHERE id=dest_id), objet, corps, mission_id, dossier_id, dest_id, lu, cc, document_id FROM messages WHERE id=@p0", id)[0];
        string cc = U.S(x[9]);
        string ccNames = cc == "" ? "" : "   Cc : " + string.Join(", ", cc.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => long.TryParse(s, out var v) ? Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", v) : s));
        head.Text = $"{x[3]}\nDe : {x[1]}   →   À : {x[2]}{ccNames}\n{U.Aff(x[0])}";
        body.Text = U.S(x[4]).Replace("\r\n", "\n").Replace("\n", "\r\n");
        if (U.L(x[7]) == Auth.CollabId && U.L(x[8]) == 0) { Db.Exec("UPDATE messages SET lu=1 WHERE id=@p0", id); }
        if (U.L(x[5]) != 0) links.Controls.Add(Ui.Btn("Ouvrir la mission", (s, e) => new MissionView(U.L(x[5])).Show(this)));
        if (U.L(x[6]) != 0) links.Controls.Add(Ui.Btn("Ouvrir le dossier", (s, e) => MainForm.Instance.OpenRef("dossier", U.L(x[6]))));
        if (U.L(x[10]) != 0) links.Controls.Add(Ui.Btn("Pièce jointe", (s, e) => { using var d = new DocumentDialog(U.L(x[10])); d.ShowDialog(); }));
        links.Controls.Add(Ui.Btn("Exporter PDF", (s, e) => Ui.Try(() => Export.PdfOpen(U.S(x[3]), head.Text + "\n\n" + x[4]))));
    }

    void Reply(bool all)
    {
        long id = grid.SelectedId; if (id == 0) return;
        var x = Db.Rows("SELECT exp_id, objet, corps, mission_id, dossier_id, cc, dest_id FROM messages WHERE id=@p0", id)[0];
        long exp = U.L(x[0]);
        if (exp == 0) { Ui.Info("Ce message provient d'un interlocuteur extérieur simulé : utilisez la téléphonie ou un courrier de simulation."); return; }
        var cc = all ? U.S(x[5]).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => long.TryParse(s, out var v) ? v : 0).Where(v => v != 0).Append(U.L(x[6])).ToList() : null;
        using var d = new MessageDialog(exp == Auth.CollabId ? U.L(x[6]) : exp, "RE: " + x[1], "\r\n\r\n----- Message d'origine -----\r\n" + U.S(x[2]).Replace("\n", "\r\n"), U.L(x[3]), U.L(x[4]), cc);
        if (d.ShowDialog() == DialogResult.OK) RefreshData();
    }

    void Forward()
    {
        long id = grid.SelectedId; if (id == 0) return;
        var x = Db.Rows("SELECT objet, corps, mission_id, dossier_id FROM messages WHERE id=@p0", id)[0];
        using var d = new MessageDialog(0, "TR: " + x[0], "\r\n\r\n----- Message transféré -----\r\n" + U.S(x[1]).Replace("\n", "\r\n"), U.L(x[2]), U.L(x[3]));
        if (d.ShowDialog() == DialogResult.OK) RefreshData();
    }

    void OpenDraft()
    {
        long id = grid.SelectedId; if (id == 0) return;
        var x = Db.Rows("SELECT dest_id, objet, corps, mission_id, dossier_id, brouillon FROM messages WHERE id=@p0 AND exp_id=@p1", id, Auth.CollabId);
        if (x.Count == 0 || U.L(x[0][5]) == 0) { Ui.Info("Sélectionnez un brouillon."); return; }
        using var d = new MessageDialog(U.L(x[0][0]), U.S(x[0][1]), U.S(x[0][2]).Replace("\n", "\r\n"), U.L(x[0][3]), U.L(x[0][4]), null, id);
        if (d.ShowDialog() == DialogResult.OK) RefreshData();
    }

    void SetEtat(string etat)
    {
        long id = grid.SelectedId; if (id == 0) return;
        Ui.Try(() =>
        {
            long me = Auth.CollabId;
            Db.Exec("UPDATE messages SET etat_dest=@p1 WHERE id=@p0 AND dest_id=@p2", id, etat, me);
            Db.Exec("UPDATE messages SET etat_exp=@p1 WHERE id=@p0 AND exp_id=@p2", id, etat, me);
            if (etat == "corbeille" && folders.SelectedIndex == 6 && Ui.Confirm("Supprimer définitivement ce message ?")) Db.Exec("DELETE FROM messages WHERE id=@p0 AND (dest_id=@p1 OR exp_id=@p1)", id, me);
        });
        RefreshData();
    }

    void Toggle(string col)
    {
        long id = grid.SelectedId; if (id == 0) return;
        Db.Exec($"UPDATE messages SET {col}=1-{col} WHERE id=@p0", id);
        RefreshData();
    }

    public override void Nouveau() { using var d = new MessageDialog(); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public override void RefreshData()
    {
        long me = Auth.CollabId;
        var r = Db.Rows("SELECT (SELECT COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND etat_dest='actif' AND brouillon=0), (SELECT COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND important=1 AND etat_dest<>'corbeille'), (SELECT COUNT(*) FROM messages WHERE suivi=1 AND (dest_id=@p0 OR exp_id=@p0)), 0, (SELECT COUNT(*) FROM messages WHERE exp_id=@p0 AND brouillon=1)", me)[0];
        counts = new long[9]; for (int i = 0; i < 5; i++) counts[i] = U.L(r[i]);
        folders.Invalidate();
        grid.Reload();
    }
}

// ============================================================ TÉLÉPHONIE
public class TelephonieModule : Module
{
    public override string Titre => "Téléphonie simulée";
    readonly GridPanel grid;
    readonly ComboBox filtre = Ui.Combo(new[] { "Tous les appels", "Entrants", "Sortants", "Manqués", "À rappeler", "Pris par le secrétariat" }, 200);
    readonly TextBox detail = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Font = new Font("Segoe UI", 10f) };
    readonly Label lStd = new() { Dock = DockStyle.Top, Height = 46, Font = Theme.FH, ForeColor = Color.White, BackColor = Theme.Blue, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };

    public TelephonieModule()
    {
        grid = new GridPanel("appels") { Loader = LoadData };
        grid.SelChanged += Show1;
        filtre.SelectedIndexChanged += (s, e) => { grid.ResetPage(); grid.Reload(); };
        var bar = Ui.Bar(Ui.PrimaryBtn("☎ Appel sortant", (s, e) => Nouveau()), Ui.Btn("Rappeler", (s, e) => Rappeler()),
            Ui.Btn("Notes / compte rendu", (s, e) => Sel(id =>
            {
                var cur = Db.Str("SELECT IFNULL(compte_rendu,'') FROM appels WHERE id=@p0", id);
                var t = Ui.Prompt("Compte rendu d'appel", "Notes et compte rendu :", cur, true);
                if (t != null) { Auth.Check("telephonie"); Db.Exec("UPDATE appels SET compte_rendu=@p1 WHERE id=@p0", id, t); }
            })),
            Ui.Btn("Associer à un dossier", (s, e) => Sel(id =>
            {
                using var f = new Form { Text = "Associer à un dossier", Width = 560, Height = 140, StartPosition = FormStartPosition.CenterParent, Font = Theme.F };
                var cb = Ui.Combo(Item.Dossiers(), 520); cb.Location = new Point(12, 12);
                var ok = new Button { Text = "Associer", DialogResult = DialogResult.OK, Location = new Point(420, 50) };
                f.Controls.Add(cb); f.Controls.Add(ok); f.AcceptButton = ok;
                if (f.ShowDialog() == DialogResult.OK)
                {
                    Auth.Check("telephonie");
                    long d = Item.Of(cb);
                    Db.Tx(c => { c.Exec("UPDATE appels SET dossier_id=@p1 WHERE id=@p0", id, d == 0 ? null : d); if (d != 0) Biz.DHist(c, d, Auth.CollabId, "Appel associé au dossier"); });
                }
            })),
            Ui.Btn("Marquer comme traité", (s, e) => Sel(id => Db.Exec("UPDATE appels SET rappel=0, statut=CASE WHEN statut IN ('Manqué','À rappeler','Rappel prévu') THEN 'Traité' ELSE statut END WHERE id=@p0", id))),
            new Label { Text = "Filtre :", AutoSize = true, Padding = new Padding(10, 8, 0, 0) }, filtre);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
        split.Panel1.Controls.Add(grid); split.Panel2.Controls.Add(Card(detail, "Détail de l'appel"));
        Controls.Add(split); Controls.Add(bar); Controls.Add(lStd); Controls.Add(Ui.Title("Téléphonie simulée — standard du cabinet"));
        Load += (s, e) => { try { split.SplitterDistance = split.Width * 65 / 100; } catch { } };
    }

    void Sel(Action<long> a) { long id = grid.SelectedId; if (id == 0) { Ui.Info("Sélectionnez un appel."); return; } if (Ui.Try(() => a(id))) RefreshData(); }

    DataTable LoadData(string q, int o, int l)
    {
        string w = filtre.SelectedIndex switch { 1 => "sens='Entrant'", 2 => "sens='Sortant'", 3 => "statut='Manqué'", 4 => "rappel=1", 5 => "statut='Pris par le secrétariat'", _ => "1=1" };
        return Db.Table($@"SELECT id, substr(date,1,16) AS 'Date', sens AS 'Sens', statut AS 'Statut', interlocuteur AS 'Interlocuteur', motif AS 'Motif', duree/60||' min '||duree%60||' s' AS 'Durée',
            CASE rappel WHEN 1 THEN 'Oui' ELSE '' END AS 'À rappeler', (SELECT numero FROM dossiers WHERE id=dossier_id) AS 'Dossier'
            FROM appels WHERE {w} AND (interlocuteur LIKE @p0 OR motif LIKE @p0 OR IFNULL(notes,'') LIKE @p0) ORDER BY date DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o);
    }

    void Show1()
    {
        long id = grid.SelectedId; if (id == 0) { detail.Text = ""; return; }
        var a = Db.Rows("SELECT date, sens, statut, interlocuteur, motif, duree, notes, compte_rendu, (SELECT numero||' — '||intitule FROM dossiers WHERE id=dossier_id), (SELECT nom FROM organismes WHERE id=organisme_id) FROM appels WHERE id=@p0", id)[0];
        detail.Text = $"Date : {U.Aff(a[0])}\r\nSens : {a[1]}\r\nStatut : {a[2]}\r\nInterlocuteur : {a[3]}\r\nOrganisme : {a[9]}\r\nMotif : {a[4]}\r\nDurée : {U.L(a[5]) / 60} min {U.L(a[5]) % 60} s\r\nDossier associé : {a[8]}\r\n\r\nNotes :\r\n{a[6]}\r\n\r\nCompte rendu :\r\n{a[7]}";
    }

    void Rappeler()
    {
        long id = grid.SelectedId; if (id == 0) return;
        var a = Db.Rows("SELECT collab_id, interlocuteur_id FROM appels WHERE id=@p0", id)[0];
        using var d = new AppelDialog(U.L(a[0]), U.L(a[1]));
        if (d.ShowDialog() == DialogResult.OK) { Db.Exec("UPDATE appels SET rappel=0, statut=CASE WHEN statut IN ('Manqué','À rappeler','Rappel prévu') THEN 'Rappelé' ELSE statut END WHERE id=@p0", id); RefreshData(); }
    }

    public override void Nouveau() { using var d = new AppelDialog(); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public override void RefreshData()
    {
        var r = Db.Rows("SELECT (SELECT COUNT(*) FROM appels WHERE rappel=1), (SELECT COUNT(*) FROM appels WHERE substr(date,1,10)=@p0), (SELECT interlocuteur||' — '||motif||' ('||statut||')' FROM appels WHERE sens='Entrant' ORDER BY id DESC LIMIT 1)", Engine.Now.ToString("yyyy-MM-dd"))[0];
        lStd.Text = $"☎  Standard — appels du jour : {r[1]}   |   à rappeler : {r[0]}   |   dernier appel entrant : {r[2]}";
        grid.Reload();
    }
}

// ============================================================ AGENDA
public class AgendaModule : Module
{
    public override string Titre => "Agenda et réunions";
    readonly AgendaView view = new();
    readonly GridPanel list;
    readonly MonthCalendar cal = new() { MaxSelectionCount = 1, Dock = DockStyle.Top };
    readonly ComboBox cMode = Ui.Combo(new[] { "Journée", "Semaine", "Mois", "Liste" }, 120);
    readonly ComboBox cQui = Ui.Combo(new[] { "Mon agenda personnel", "Agenda d'un service", "Agenda d'un responsable", "Toutes les réunions" }, 220);
    readonly ComboBox cSvc = Ui.Combo(Item.Services(), 220);
    readonly ComboBox cResp;
    readonly Panel center = new() { Dock = DockStyle.Fill };
    readonly GridPanel gProp;

    public AgendaModule()
    {
        cMode.SelectedIndex = 1;
        cResp = Ui.Combo(Item.Collabs("c.niveau<=5").Cast<object>(), 260);
        view.Open += id => { using var d = new ReunionDialog(id); if (d.ShowDialog() == DialogResult.OK) RefreshData(); };
        view.NewAt += at => { using var d = new ReunionDialog(0, at); if (d.ShowDialog() == DialogResult.OK) RefreshData(); };
        list = new GridPanel("agenda_list") { Loader = (q, o, l) => { var (w, a) = Where(); return Db.Table($"SELECT DISTINCT r.id, substr(r.debut,1,16) AS 'Début', substr(r.fin,12,5) AS 'Fin', r.titre AS 'Titre', r.lieu AS 'Lieu', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=r.organisateur_id) AS 'Organisateur', r.priorite AS 'Priorité', r.statut AS 'Statut', (SELECT COUNT(*) FROM reunion_participants WHERE reunion_id=r.id) AS 'Participants' FROM reunions r LEFT JOIN reunion_participants p ON p.reunion_id=r.id WHERE {w} AND r.titre LIKE @p9 ORDER BY r.debut DESC LIMIT @p10 OFFSET @p11", a.Concat(new object[] { U.Like(q), l, o }).ToArray()); } };
        list.Activated += id => { using var d = new ReunionDialog(id); if (d.ShowDialog() == DialogResult.OK) RefreshData(); };
        gProp = new GridPanel("agenda_prop") { PageSize = 30, Loader = (q, o, l) => Db.Table("SELECT r.id, substr(r.debut,6,11) AS 'Date', r.titre AS 'Réunion proposée' FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND r.statut='Proposée' AND r.titre LIKE @p1 ORDER BY r.debut LIMIT @p2 OFFSET @p3", Auth.CollabId, U.Like(q), l, o) };
        gProp.Activated += id => { using var d = new ReunionDialog(id); d.ShowDialog(); RefreshData(); };
        cal.DateSelected += (s, e) => { view.Date = cal.SelectionStart; RefreshData(); };
        cMode.SelectedIndexChanged += (s, e) => Layout2();
        foreach (var c in new[] { cQui, cSvc, cResp }) c.SelectedIndexChanged += (s, e) => RefreshData();
        var bar = Ui.Bar(Ui.PrimaryBtn("▦ Nouvelle réunion", (s, e) => Nouveau()), Ui.Btn("◀", (s, e) => Move(-1), 40), Ui.Btn("Aujourd'hui", (s, e) => { view.Date = Engine.Now.Date; cal.SetDate(view.Date); RefreshData(); }), Ui.Btn("▶", (s, e) => Move(1), 40),
            new Label { Text = "Vue :", AutoSize = true, Padding = new Padding(10, 8, 0, 0) }, cMode, new Label { Text = "Agenda :", AutoSize = true, Padding = new Padding(10, 8, 0, 0) }, cQui, cSvc, cResp);
        var left = new Panel { Dock = DockStyle.Left, Width = 250, Padding = new Padding(6), BackColor = Color.White };
        var gp = Card(gProp, "Propositions à confirmer"); gp.Dock = DockStyle.Fill;
        left.Controls.Add(gp);
        left.Controls.Add(new Label { Text = "Double-cliquez dans le calendrier pour créer une réunion.", Dock = DockStyle.Top, Height = 40, ForeColor = Color.Gray });
        left.Controls.Add(cal);
        Controls.Add(center); Controls.Add(left); Controls.Add(bar); Controls.Add(Ui.Title("Agenda, rendez-vous et réunions"));
        view.Date = Engine.Now.Date;
        Layout2();
    }

    void Move(int dir)
    {
        view.Date = cMode.SelectedIndex switch { 0 => view.Date.AddDays(dir), 2 => view.Date.AddMonths(dir), _ => view.Date.AddDays(7 * dir) };
        cal.SetDate(view.Date); RefreshData();
    }

    void Layout2()
    {
        center.Controls.Clear();
        if (cMode.SelectedIndex == 3) center.Controls.Add(list);
        else { view.Vue = (AgendaView.Mode)cMode.SelectedIndex; center.Controls.Add(Card(view)); }
        RefreshData();
    }

    (string, object[]) Where()
    {
        var a = new object[9];
        a[0] = Auth.CollabId; a[1] = Item.Of(cSvc); a[2] = Item.Of(cResp);
        string w = cQui.SelectedIndex switch
        {
            0 => "p.collab_id=@p0",
            1 => "p.collab_id IN (SELECT id FROM collaborateurs WHERE service_id=@p1)",
            2 => "p.collab_id=@p2",
            _ => "1=1"
        };
        cSvc.Visible = cQui.SelectedIndex == 1; cResp.Visible = cQui.SelectedIndex == 2;
        return (w, a);
    }

    public override void Nouveau() { using var d = new ReunionDialog(0, view.Date.Date.AddHours(10)); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public override void RefreshData()
    {
        var (w, a) = Where();
        if (cMode.SelectedIndex == 3) list.Reload();
        else
        {
            var args = a.ToList(); args.Add(U.D(view.Debut)); args.Add(U.D(view.Fin));
            view.Items = Db.Rows($"SELECT DISTINCT r.id, r.titre, r.debut, r.fin, r.priorite, r.statut FROM reunions r LEFT JOIN reunion_participants p ON p.reunion_id=r.id WHERE {w} AND r.debut>=@p9 AND r.debut<@p10 AND r.statut<>'Annulée'", args.ToArray())
                .Select(r => (U.L(r[0]), U.S(r[1]), U.P(r[2]), U.P(r[3]), U.S(r[4]), U.S(r[5]))).ToList();
            view.Invalidate();
        }
        gProp.Reload();
    }
}
