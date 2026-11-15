using System.Data;

namespace ParquetPro;

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
    readonly ComboBox cStat = Ui.Combo(new[] { "Dossiers actifs", "Enquête en cours", "En attente de décision", "Transmis / information (sim.)", "Clôturés", "Archivés", "Tous" }, 200);
    readonly ComboBox cSvc = Ui.Combo(Item.Services(true), 240);
    readonly ComboBox cCat = Ui.Combo(new[] { "(Toutes catégories)" }.Concat(Seed.Categories), 200);
    readonly ComboBox cPrio = Ui.Combo(new[] { "(Toutes priorités)", "Haute", "Normale", "Basse" }, 130);
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly TextBox tFiche = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 9f), BackColor = Color.White };
    readonly GridPanel gNotes, gHist, gMis, gDocs;

    public DossiersModule()
    {
        grid = new GridPanel("dossiers") { Loader = LoadData };
        grid.Activated += id => DossierWindow.Open(id);
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

        var bar = Ui.Bar(Icons.Btn("dossier", "Ouvrir le dossier", (s, e) => DossierWindow.Open(grid.SelectedId), true), Icons.Btn("decision", "Préparer une décision", (s, e) => Sel(id => { using var w = new DecisionWizard(id, null, 0, 0); w.ShowDialog(); })), Ui.Btn("Nouveau dossier", (s, e) => Nouveau()), Ui.Btn("Modifier / affecter", (s, e) => Edit(grid.SelectedId)),
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
        w.Add(cStat.SelectedIndex switch { 0 => "d.statut<>'Clôturé' AND d.archive=0", 1 => "d.situation IN ('Enquête en cours','Complément demandé') AND d.archive=0", 2 => "d.situation='En attente de décision' AND d.archive=0", 3 => "d.situation IN ('Transmis à juridiction (sim.)','Information judiciaire (sim.)','Alternative en cours (sim.)')", 4 => "d.statut='Clôturé'", 5 => "d.archive=1", _ => "1=1" });
        if (Item.Of(cSvc) != 0) w.Add("d.service_id=" + Item.Of(cSvc));
        if (cCat.SelectedIndex > 0) w.Add("d.categorie=@p4");
        if (cPrio.SelectedIndex > 0) w.Add("d.priorite=@p5");
        return Db.Table($@"SELECT d.id, d.numero AS 'N°', d.intitule AS 'Intitulé', d.categorie AS 'Catégorie', (SELECT code FROM services WHERE id=d.service_id) AS 'Service',
            (SELECT prenom||' '||nom FROM collaborateurs WHERE id=d.referent_id) AS 'Référent', d.situation AS 'Situation', d.statut AS 'Statut', d.priorite AS 'Priorité', d.avancement AS 'Avancement',
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
    public void Focus(long id) => DossierWindow.Open(id);
    public override void Nouveau() { using var d = new DossierDialog(); if (d.ShowDialog() == DialogResult.OK) RefreshData(); }
    public override void RefreshData() { grid.Reload(); ShowDetail(); }
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
            Icons.Btn("dossier", "Ouvrir le dossier", (s, e) => { long d = Db.Long("SELECT IFNULL(dossier_id,0) FROM appels WHERE id=@p0", grid.SelectedId); if (d != 0) DossierWindow.Open(d); else Ui.Info("Cet appel n'est associé à aucun dossier."); }),
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
