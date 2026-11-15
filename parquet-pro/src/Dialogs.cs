using System.Data;

namespace ParquetPro;

public class Dlg : Form
{
    protected readonly TableLayoutPanel Tbl;
    protected readonly FlowLayoutPanel Buttons;

    public Dlg(string title, int w = 680, int h = 600)
    {
        Text = title; Width = w; Height = h; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false; MinimizeBox = false; BackColor = Color.White;
        Tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(10, 10, 16, 6) };
        Tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        Tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 6, 8, 6), BackColor = Theme.Back };
        var head = new Label { Dock = DockStyle.Top, Height = 40, Text = "  " + title, Font = Theme.FT, ForeColor = Color.White, BackColor = Theme.Blue, TextAlign = ContentAlignment.MiddleLeft };
        Controls.Add(Tbl); Controls.Add(Buttons); Controls.Add(head);
    }

    protected TC Row<TC>(string label, TC ctl, int height = 0) where TC : Control
    {
        Tbl.RowStyles.Add(height > 0 ? new RowStyle(SizeType.Absolute, height) : new RowStyle(SizeType.AutoSize));
        var l = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = height > 40 ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft, ForeColor = Theme.Anthracite, Padding = new Padding(0, height > 40 ? 6 : 0, 0, 0) };
        ctl.Dock = DockStyle.Fill; ctl.Margin = new Padding(3, 4, 3, 4);
        Tbl.Controls.Add(l); Tbl.Controls.Add(ctl);
        return ctl;
    }

    protected static TextBox Multi(string text = "") => new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Text = text, AcceptsReturn = true };
    protected static DateTimePicker Dtp(DateTime d) => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dddd dd/MM/yyyy  HH:mm", Value = d < DateTimePicker.MinimumDateTime ? DateTime.Now : d };

    protected static ComboBox CollabCombo(long sel = 0, string where = "1=1", params object[] a)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, MaxDropDownItems = 25 };
        foreach (var i in Item.Collabs(where, a)) c.Items.Add(i);
        if (sel != 0) Item.Select(c, sel);
        return c;
    }
    protected static long ComboId(ComboBox c)
    {
        if (c.SelectedItem is Item it) return it.Id;
        foreach (var o in c.Items) if (o is Item i && string.Equals(i.Text, c.Text, StringComparison.OrdinalIgnoreCase)) return i.Id;
        foreach (var o in c.Items) if (o is Item i && i.Text.StartsWith(c.Text, StringComparison.OrdinalIgnoreCase) && c.Text.Length > 2) return i.Id;
        return 0;
    }

    protected void Btn(string text, Action a, bool primary = false, bool close = true)
    {
        var b = primary ? Ui.PrimaryBtn(text, null, 130) : Ui.Btn(text, null, 130);
        b.Click += (s, e) => { if (Ui.Try(a) && close) { DialogResult = DialogResult.OK; Close(); } };
        Buttons.Controls.Add(b);
    }
    protected void Cancel(string text = "Fermer")
    {
        var b = Ui.Btn(text, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }, 110);
        Buttons.Controls.Add(b); CancelButton = b;
    }
}

// ============================================================ Mission
public class MissionDialog : Dlg
{
    public long CreatedId;
    public MissionDialog(long dest = 0, long dossier = 0, string objet = "", string desc = "") : base("Nouvelle délégation / mission", 760, 680)
    {
        var tObjet = Row("Objet *", new TextBox { Text = objet });
        var tDesc = Row("Description", Multi(desc), 90);
        var cDest = Row("Destinataire *", CollabCombo(dest == 0 ? Db.Long("SELECT valeur FROM parametres WHERE cle='adjoint_id'") : dest, "c.id<>@p0", Auth.CollabId));
        var cSvc = Row("Service concerné", Ui.Combo(new object[] { new Item(0, "(Service du destinataire)") }.Concat(Item.Services()), 300));
        var cPrio = Row("Priorité", Ui.Combo(Biz.Priorites, 120)); cPrio.SelectedIndex = 1;
        var dEch = Row("Échéance", Dtp(Engine.Now.Date.AddDays(3).AddHours(18)));
        var tInstr = Row("Instructions", Multi("Me rendre compte à l'issue de la mission."), 80);
        var cConf = Row("Confidentialité interne", Ui.Combo(new[] { "Interne", "Restreint", "Confidentiel (interne)" }, 200));
        var cDos = Row("Dossier associé", Ui.Combo(Item.Dossiers(), 400));
        Item.Select(cDos, dossier);
        var docs = new List<object> { new Item(0, "(Aucun document)") };
        docs.AddRange(Db.Rows("SELECT id, numero||' — '||titre FROM documents WHERE archive=0 ORDER BY id DESC LIMIT 300").Select(r => (object)new Item(U.L(r[0]), U.S(r[1]))));
        var cDoc = Row("Document associé", Ui.Combo(docs, 400));
        Row("Numéro / statut", new Label { Text = "Attribués automatiquement à la création (statut initial : Nouvelle).", ForeColor = Color.Gray, AutoSize = true });
        Cancel("Annuler");
        Btn("Confier la mission", () =>
        {
            Auth.Check("missions.creer");
            long d = ComboId(cDest);
            if (d == 0) throw new InvalidOperationException("Veuillez sélectionner un destinataire dans la liste.");
            CreatedId = Db.Tx(c => Biz.CreerMission(c, Auth.CollabId, d, tObjet.Text.Trim(), tDesc.Text, cPrio.Text, dEch.Value, tInstr.Text, cConf.Text, Item.Of(cSvc), Item.Of(cDos), 0, Item.Of(cDoc), "Utilisateur"));
        }, true);
    }
}

// ============================================================ Dossier
public class DossierDialog : Dlg
{
    public DossierDialog(long id = 0) : base(id == 0 ? "Nouveau dossier" : "Modifier le dossier", 760, 640)
    {
        object[] d = id == 0 ? null : Db.Rows("SELECT intitule,categorie,resume,service_id,referent_id,priorite,echeance,statut,personnes,organisme_id FROM dossiers WHERE id=@p0", id)[0];
        var tInt = Row("Intitulé *", new TextBox { Text = d == null ? "" : U.S(d[0]) });
        var cCat = Row("Catégorie", Ui.Combo(Seed.Categories, 260));
        if (d != null) cCat.SelectedItem = U.S(d[1]);
        var tRes = Row("Résumé", Multi(d == null ? "" : U.S(d[2])), 90);
        var cSvc = Row("Service responsable", Ui.Combo(Item.Services(), 320));
        Item.Select(cSvc, d == null ? Db.Long("SELECT id FROM services WHERE code='GEST'") : U.L(d[3]));
        var cRef = Row("Collaborateur référent", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        void FillRef(long sel)
        {
            cRef.Items.Clear(); cRef.Items.Add(new Item(0, "(Aucun)"));
            foreach (var i in Item.Collabs("c.service_id=@p0", Item.Of(cSvc))) cRef.Items.Add(i);
            cRef.SelectedIndex = 0; Item.Select(cRef, sel);
        }
        FillRef(d == null ? 0 : U.L(d[4]));
        cSvc.SelectedIndexChanged += (s, e) => FillRef(0);
        var cPrio = Row("Priorité", Ui.Combo(Biz.Priorites, 120)); cPrio.SelectedItem = d == null ? "Normale" : U.S(d[5]);
        var dEch = Row("Échéance", Dtp(d == null ? Engine.Now.AddDays(30) : U.P(d[6])));
        var cStat = Row("Statut", Ui.Combo(new[] { "Ouvert", "En cours d'instruction", "En attente", "Clôturé" }, 220));
        cStat.SelectedItem = d == null ? "Ouvert" : U.S(d[7]); cStat.Enabled = d != null;
        var tPers = Row("Personnes associées", new TextBox { Text = d == null ? "Personnes fictives (simulation)" : U.S(d[8]) });
        var orgs = new List<object> { new Item(0, "(Aucun)") };
        orgs.AddRange(Db.Rows("SELECT id, nom FROM organismes ORDER BY nom").Select(r => (object)new Item(U.L(r[0]), U.S(r[1]))));
        var cOrg = Row("Organisme d'origine", Ui.Combo(orgs, 360));
        if (d != null) Item.Select(cOrg, U.L(d[9]));
        Row("", new Label { Text = U.Mention, ForeColor = Theme.Red, AutoSize = true });
        Cancel("Annuler");
        Btn("Enregistrer", () =>
        {
            Db.Tx(c =>
            {
                if (id == 0) Biz.CreerDossier(c, tInt.Text.Trim(), cCat.Text, tRes.Text, Item.Of(cSvc), Item.Of(cRef), cPrio.Text, dEch.Value, tPers.Text, Item.Of(cOrg));
                else Biz.ModifierDossier(c, id, tInt.Text.Trim(), cCat.Text, tRes.Text, Item.Of(cSvc), Item.Of(cRef), cPrio.Text, dEch.Value, cStat.Text, tPers.Text, Item.Of(cOrg));
            });
        }, true);
    }
}

// ============================================================ Message
public class MessageDialog : Dlg
{
    readonly List<long> ccIds = new();
    public MessageDialog(long dest = 0, string objet = "", string corps = "", long mission = 0, long dossier = 0, IEnumerable<long> cc = null, long brouillonId = 0, long parentId = 0) : base(parentId != 0 ? "Répondre" : "Nouveau message", 780, 640)
    {
        var cDest = Row("À *", CollabCombo(dest, "c.id<>@p0", Auth.CollabId));
        var lCc = new Label { AutoSize = false, Height = 22, ForeColor = Theme.Anthracite, Text = "(aucun)" };
        if (cc != null) ccIds.AddRange(cc.Where(x => x != Auth.CollabId && x != dest).Distinct());
        void RefreshCc() => lCc.Text = ccIds.Count == 0 ? "(aucun)" : string.Join("; ", ccIds.Select(i => Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", i)));
        RefreshCc();
        var pCc = new FlowLayoutPanel { Height = 32, WrapContents = false };
        pCc.Controls.Add(Ui.Btn("Ajouter des destinataires…", (s, e) =>
        {
            var pick = PickCollabs.Show(this, ccIds);
            if (pick != null) { ccIds.Clear(); ccIds.AddRange(pick); RefreshCc(); }
        }));
        pCc.Controls.Add(lCc); lCc.Width = 420;
        Row("Copie (Cc)", pCc);
        var tObj = Row("Objet", new TextBox { Text = objet });
        var tCorps = Row("Message", Multi(corps), 260);
        var chk = Row("Options", new CheckBox { Text = "Marquer comme important", AutoSize = true });
        var cDos = Row("Dossier lié", Ui.Combo(Item.Dossiers(), 400));
        Item.Select(cDos, dossier);
        var docs = new List<object> { new Item(0, "(Aucune pièce jointe)") };
        docs.AddRange(Db.Rows("SELECT id, numero||' — '||titre FROM documents WHERE archive=0 ORDER BY id DESC LIMIT 300").Select(r => (object)new Item(U.L(r[0]), U.S(r[1]))));
        var cDoc = Row("Joindre un document", Ui.Combo(docs, 400));
        Cancel("Annuler");
        Btn("Enregistrer le brouillon", () => Db.Tx(c =>
        {
            if (brouillonId != 0) c.Exec("DELETE FROM messages WHERE id=@p0 AND brouillon=1", brouillonId);
            Biz.EnvoyerMessage(c, ComboId(cDest), string.Join(";", ccIds), tObj.Text, tCorps.Text, chk.Checked, mission, Item.Of(cDos), Item.Of(cDoc), true);
        }));
        Btn("Envoyer", () => Db.Tx(c =>
        {
            long d = ComboId(cDest);
            if (d == 0) throw new InvalidOperationException("Veuillez choisir un destinataire dans la liste.");
            Biz.EnvoyerMessage(c, d, string.Join(";", ccIds), tObj.Text, tCorps.Text, chk.Checked, mission, Item.Of(cDos), Item.Of(cDoc), false, parentId);
            if (brouillonId != 0) c.Exec("DELETE FROM messages WHERE id=@p0 AND brouillon=1", brouillonId);
        }), true);
    }
}

public static class PickCollabs
{
    public static List<long> Show(IWin32Window owner, IEnumerable<long> pre)
    {
        using var f = new Form { Text = "Sélection de collaborateurs", Width = 820, Height = 560, StartPosition = FormStartPosition.CenterParent, Font = Theme.F, ShowIcon = false };
        var all = Item.Collabs();
        var sel = all.Where(a => pre.Contains(a.Id)).ToList();
        var filter = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Filtrer par nom, fonction, service…" };
        var lAll = new ListBox { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, IntegralHeight = false };
        var lSel = new ListBox { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, IntegralHeight = false };
        void Fill() { lAll.BeginUpdate(); lAll.Items.Clear(); foreach (var i in all.Where(i => i.Text.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) && !sel.Any(s => s.Id == i.Id))) lAll.Items.Add(i); lAll.EndUpdate(); lSel.Items.Clear(); foreach (var s in sel) lSel.Items.Add(s); }
        filter.TextChanged += (s, e) => Fill();
        var svc = Ui.Combo(Item.Services(), 260);
        var mid = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(4, 40, 4, 4) };
        mid.Controls.Add(Ui.Btn("Ajouter →", (s, e) => { sel.AddRange(lAll.SelectedItems.Cast<Item>()); Fill(); }, 130));
        mid.Controls.Add(Ui.Btn("← Retirer", (s, e) => { foreach (Item i in lSel.SelectedItems) sel.RemoveAll(x => x.Id == i.Id); Fill(); }, 130));
        mid.Controls.Add(new Label { Text = "Ajouter un service :", AutoSize = true });
        mid.Controls.Add(svc);
        mid.Controls.Add(Ui.Btn("Ajouter le service", (s, e) =>
        {
            var ids = Db.Rows("SELECT id FROM collaborateurs WHERE service_id=@p0 AND actif=1", Item.Of(svc)).Select(r => U.L(r[0])).ToHashSet();
            sel.AddRange(all.Where(a => ids.Contains(a.Id) && !sel.Any(x => x.Id == a.Id))); Fill();
        }, 130));
        lAll.DoubleClick += (s, e) => { if (lAll.SelectedItem is Item i) { sel.Add(i); Fill(); } };
        var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); tl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280)); tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var left = new Panel { Dock = DockStyle.Fill }; left.Controls.Add(lAll); left.Controls.Add(filter);
        var right = new Panel { Dock = DockStyle.Fill }; right.Controls.Add(lSel); right.Controls.Add(new Label { Text = "Sélectionnés", Dock = DockStyle.Top, Font = Theme.FB });
        tl.Controls.Add(left, 0, 0); tl.Controls.Add(mid, 1, 0); tl.Controls.Add(right, 2, 0);
        var ok = new Button { Text = "Valider", DialogResult = DialogResult.OK, Width = 100 };
        var bp = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft }; bp.Controls.Add(ok);
        f.Controls.Add(tl); f.Controls.Add(bp);
        Fill();
        return f.ShowDialog(owner) == DialogResult.OK ? sel.Select(s => s.Id).ToList() : null;
    }
}

// ============================================================ Réunion
public class ReunionDialog : Dlg
{
    public ReunionDialog(long id = 0, DateTime? at = null) : base(id == 0 ? "Nouvelle réunion / rendez-vous" : "Réunion", 800, 700)
    {
        object[] r = id == 0 ? null : Db.Rows("SELECT titre,debut,fin,lieu,organisateur_id,ordre_du_jour,priorite,rappel_min,dossier_id,statut,compte_rendu FROM reunions WHERE id=@p0", id)[0];
        var parts = id == 0 ? new List<long> { Auth.CollabId } : Db.Rows("SELECT collab_id FROM reunion_participants WHERE reunion_id=@p0", id).Select(x => U.L(x[0])).ToList();
        var deb0 = r == null ? (at ?? Engine.Now.Date.AddDays(1).AddHours(10)) : U.P(r[1]);
        var tTit = Row("Titre *", new TextBox { Text = r == null ? "" : U.S(r[0]) });
        var dDeb = Row("Début", Dtp(deb0));
        var nDur = Row("Durée (minutes)", new NumericUpDown { Minimum = 15, Maximum = 600, Increment = 15, Value = r == null ? 60 : (decimal)Math.Max(15, (U.P(r[2]) - U.P(r[1])).TotalMinutes) });
        var cLieu = Row("Lieu", Ui.Combo(new[] { "Bureau du procureur", "Salle du conseil", "Salle de réunion B", "Salle de coordination", "Visioconférence (simulée)" }, 300, true));
        if (r != null) cLieu.Text = U.S(r[3]);
        var cOrg = Row("Organisateur", CollabCombo(r == null ? Auth.CollabId : U.L(r[4])));
        var cPrio = Row("Priorité", Ui.Combo(Biz.Priorites, 120)); cPrio.SelectedItem = r == null ? "Normale" : U.S(r[6]);
        var nRap = Row("Rappel (minutes avant)", new NumericUpDown { Minimum = 0, Maximum = 240, Value = r == null ? 15 : U.L(r[7]) });
        var tOdj = Row("Ordre du jour", Multi(r == null ? "1. \n2. \n3. Questions diverses" : U.S(r[5])), 100);
        var lParts = new ListBox { IntegralHeight = false };
        void FillParts() { lParts.Items.Clear(); foreach (var p in parts.Distinct()) lParts.Items.Add(Db.Str("SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=@p0", p)); }
        FillParts();
        Row("Participants", lParts, 110);
        var pp = new FlowLayoutPanel { Height = 34 };
        pp.Controls.Add(Ui.Btn("Inviter des collaborateurs…", (s, e) => { var p = PickCollabs.Show(this, parts); if (p != null) { parts = p; FillParts(); } }));
        pp.Controls.Add(Ui.Btn("Vérifier les disponibilités", (s, e) => Ui.Try(() =>
        {
            var conf = Db.Q(c => Biz.Conflits(c, dDeb.Value, dDeb.Value.AddMinutes((double)nDur.Value), parts, id));
            Ui.Info(conf.Count == 0 ? "Aucun conflit d'agenda." : "Conflits :\n" + string.Join("\n", conf));
        })));
        Row("", pp);
        var cDos = Row("Dossier lié", Ui.Combo(Item.Dossiers(), 400));
        if (r != null) Item.Select(cDos, U.L(r[8]));
        if (r != null)
        {
            Row("Statut", new Label { Text = U.S(r[9]), Font = Theme.FB, AutoSize = true });
            if (!string.IsNullOrEmpty(U.S(r[10]))) Row("Compte rendu", new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = U.S(r[10]).Replace("\n", "\r\n") }, 140);
        }
        Cancel();
        string statut = r == null ? "Planifiée" : U.S(r[9]);
        if (statut is "Planifiée" or "Proposée" || r == null)
            Btn("Enregistrer", () => Db.Tx(c => Biz.EnregistrerReunion(c, id, tTit.Text.Trim() == "" ? throw new InvalidOperationException("Le titre est obligatoire.") : tTit.Text.Trim(),
                dDeb.Value, dDeb.Value.AddMinutes((double)nDur.Value), cLieu.Text, ComboId(cOrg) == 0 ? Auth.CollabId : ComboId(cOrg), tOdj.Text, cPrio.Text, (int)nRap.Value, parts, Item.Of(cDos), statut == "Proposée" ? "Planifiée" : "Planifiée")), true);
        if (id != 0)
        {
            if (statut == "Proposée")
            {
                Btn("Accepter", () => Db.Tx(c =>
                {
                    Auth.Check("agenda.gerer");
                    c.Exec("UPDATE reunions SET statut='Planifiée' WHERE id=@p0", id);
                    c.Exec("UPDATE reunion_participants SET reponse='Accepté' WHERE reunion_id=@p0 AND collab_id=@p1", id, Auth.CollabId);
                    Biz.Message(c, Auth.CollabId, U.L(r[4]), "Réunion acceptée : " + r[0], "Je confirme ma participation.");
                    Biz.Event(c, "Réunion confirmée", Biz.Name(c, Auth.CollabId), Auth.CollabId, $"Réunion « {r[0]} » confirmée", "reunion", id);
                }));
                Btn("Décliner", () => Db.Tx(c =>
                {
                    Auth.Check("agenda.gerer");
                    c.Exec("UPDATE reunions SET statut='Annulée' WHERE id=@p0", id);
                    Biz.Message(c, Auth.CollabId, U.L(r[4]), "Réunion déclinée : " + r[0], "Je ne pourrai pas donner suite à cette proposition de réunion.");
                }));
            }
            if (statut is "Planifiée" or "Proposée")
                Btn("Annuler la réunion", () => { if (Ui.Confirm("Annuler cette réunion ?")) Db.Tx(c => { Auth.Check("agenda.gerer"); c.Exec("UPDATE reunions SET statut='Annulée' WHERE id=@p0", id); Biz.Event(c, "Réunion annulée", Biz.Name(c, Auth.CollabId), Auth.CollabId, $"« {r[0]} » annulée", "reunion", id); }); });
            Btn("Supprimer", () => { if (Ui.Confirm("Supprimer définitivement cette réunion ?")) Db.Tx(c => { Auth.Check("agenda.gerer"); c.Exec("DELETE FROM reunion_participants WHERE reunion_id=@p0", id); c.Exec("DELETE FROM reunions WHERE id=@p0", id); }); });
            Btn("Générer le compte rendu", () => { var txt = Db.Tx(c => { Auth.Check("documents.gerer"); return Biz.GenererCompteRendu(c, id, Auth.CollabId); }); Ui.ShowText("Compte rendu de réunion", txt); });
        }
    }
}

// ============================================================ Document
public class DocumentDialog : Dlg
{
    public static readonly string[] Categories = { "Notes internes", "Rapports d'activité", "Synthèses", "Comptes rendus", "Courriers de simulation", "Ordres du jour", "Bilans", "Documents de suivi", "Documents d'organisation", "Fiches de dossier" };
    public DocumentDialog(long id = 0, string cat = "Notes internes", string titre = "", string contenu = "", long dossier = 0, long mission = 0) : base(id == 0 ? "Nouveau document" : "Document", 860, 720)
    {
        object[] d = id == 0 ? null : Db.Rows("SELECT titre,categorie,contenu,dossier_id,mission_id,numero,version,date_modif FROM documents WHERE id=@p0", id)[0];
        var tTit = Row("Titre *", new TextBox { Text = d == null ? titre : U.S(d[0]) });
        var cCat = Row("Catégorie", Ui.Combo(Categories, 260)); cCat.SelectedItem = d == null ? cat : U.S(d[1]);
        var cDos = Row("Dossier associé", Ui.Combo(Item.Dossiers(), 400)); Item.Select(cDos, d == null ? dossier : U.L(d[3]));
        var mis = new List<object> { new Item(0, "(Aucune mission)") };
        mis.AddRange(Db.Rows("SELECT id, numero||' — '||objet FROM missions ORDER BY id DESC LIMIT 400").Select(r => (object)new Item(U.L(r[0]), U.S(r[1]))));
        var cMis = Row("Mission associée", Ui.Combo(mis, 400)); Item.Select(cMis, d == null ? mission : U.L(d[4]));
        if (d != null) Row("Référence", new Label { Text = $"{d[5]} — version {d[6]} — modifié le {U.Aff(d[7])}", AutoSize = true });
        var tCont = Row("Contenu", new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, AcceptsReturn = true, AcceptsTab = true, Font = new Font("Consolas", 9.5f), Text = (d == null ? contenu : U.S(d[2])).Replace("\r\n", "\n").Replace("\n", "\r\n"), WordWrap = true }, 360);
        Row("", new Label { Text = "Les exports et impressions portent la mention : " + U.Mention, ForeColor = Theme.Red, AutoSize = true });
        Cancel();
        Btn("Imprimer", () => Export.Print(tTit.Text, tCont.Text), false, false);
        Btn("Exporter PDF", () => Export.PdfOpen(tTit.Text, tCont.Text), false, false);
        Btn("Enregistrer", () => Db.Tx(c => Biz.EnregistrerDocument(c, id, tTit.Text.Trim(), cCat.Text, tCont.Text.Replace("\r\n", "\n"), Item.Of(cDos), Item.Of(cMis))), true);
    }
}

// ============================================================ Appel sortant
public class AppelDialog : Dlg
{
    public AppelDialog(long collab = 0, long inter = 0) : base("Appel sortant (simulé)", 700, 420)
    {
        var cType = Row("Correspondant", Ui.Combo(new[] { "Collaborateur interne", "Interlocuteur extérieur" }, 220));
        var cCol = Row("Collaborateur", CollabCombo(collab, "c.id<>@p0", Auth.CollabId));
        var ints = Db.Rows("SELECT i.id, i.prenom||' '||i.nom||' — '||i.fonction||' ('||o.nom||')', o.id FROM interlocuteurs i JOIN organismes o ON o.id=i.organisme_id ORDER BY o.nom").Select(r => (object)new Item(U.L(r[0]), U.S(r[1]))).ToList();
        var cInt = Row("Interlocuteur", Ui.Combo(ints, 400));
        if (inter != 0) { cType.SelectedIndex = 1; Item.Select(cInt, inter); }
        void Sync() { cCol.Enabled = cType.SelectedIndex == 0; cInt.Enabled = cType.SelectedIndex == 1; }
        cType.SelectedIndexChanged += (s, e) => Sync(); Sync();
        var cMot = Row("Motif", Ui.Combo(Engine.MotifsAppel, 300, true));
        var cDos = Row("Dossier associé", Ui.Combo(Item.Dossiers(), 400));
        Row("", new Label { Text = "Aucune connexion à un réseau téléphonique réel.", ForeColor = Color.Gray, AutoSize = true });
        Cancel("Annuler");
        Btn("Appeler", () =>
        {
            long id;
            if (cType.SelectedIndex == 0)
            {
                long c0 = ComboId(cCol); if (c0 == 0) throw new InvalidOperationException("Choisissez un collaborateur.");
                id = Db.Tx(c => Biz.AppelSortant(c, Biz.NameF(c, c0), c0, 0, 0, cMot.Text, Item.Of(cDos)));
            }
            else
            {
                long i0 = Item.Of(cInt); if (i0 == 0) throw new InvalidOperationException("Choisissez un interlocuteur.");
                long org = Db.Long("SELECT organisme_id FROM interlocuteurs WHERE id=@p0", i0);
                id = Db.Tx(c => Biz.AppelSortant(c, cInt.Text, 0, i0, org, cMot.Text, Item.Of(cDos)));
            }
            var r = Db.Rows("SELECT statut, duree, notes FROM appels WHERE id=@p0", id)[0];
            Ui.Info($"Appel terminé — {r[0]}\nDurée simulée : {U.L(r[1]) / 60} min {U.L(r[1]) % 60} s\n\n{r[2]}");
        }, true);
    }
}

// ============================================================ Fiche collaborateur
public class FicheCollab : Form
{
    public FicheCollab(long id)
    {
        var x = Db.Rows(@"SELECT c.matricule,c.prenom,c.nom,c.fonction,c.niveau,s.nom,c.superieur_id,c.competences,c.responsabilites,c.profil,c.autonomie,c.horaire_debut,c.horaire_fin,c.charge,c.dispo,c.telephone,c.email,c.date_entree,c.notes,c.service_id
            FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.id=@p0", id)[0];
        Text = $"Fiche individuelle — {x[1]} {x[2]}"; Width = 980; Height = 720; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false;
        var head = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Blue };
        head.Controls.Add(new Label { Text = $"{x[1]} {x[2]}", Font = new Font("Segoe UI Semibold", 16f), ForeColor = Color.White, Location = new Point(16, 8), AutoSize = true });
        head.Controls.Add(new Label { Text = $"{x[3]} — {x[5]}   |   {x[0]}   |   {x[14]}   |   Charge {x[13]} %", ForeColor = Color.FromArgb(220, 230, 245), Location = new Point(18, 46), AutoSize = true });
        var tabs = new TabControl { Dock = DockStyle.Fill };
        // --- fiche
        var tp = new TabPage("Fiche professionnelle") { BackColor = Color.White };
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10), AutoScroll = true };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        TC R<TC>(string l, TC c, int h = 0) where TC : Control { t.RowStyles.Add(h > 0 ? new RowStyle(SizeType.Absolute, h) : new RowStyle(SizeType.AutoSize)); t.Controls.Add(new Label { Text = l, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Anthracite }); c.Dock = DockStyle.Fill; t.Controls.Add(c); return c; }
        string sup = U.L(x[6]) == 0 ? "—" : Db.Str("SELECT prenom||' '||nom||' ('||fonction||')' FROM collaborateurs WHERE id=@p0", U.L(x[6]));
        R("Identifiant interne", new Label { Text = $"{x[0]} (n° {id})", AutoSize = true });
        R("Service", new Label { Text = U.S(x[5]), AutoSize = true });
        R("Supérieur hiérarchique", new Label { Text = sup, AutoSize = true });
        R("Niveau hiérarchique", new Label { Text = U.S(x[4]), AutoSize = true });
        var tF = R("Fonction", new TextBox { Text = U.S(x[3]) });
        var tC = R("Compétences", new TextBox { Text = U.S(x[7]) });
        var tR = R("Responsabilités", new TextBox { Text = U.S(x[8]), Multiline = true }, 50);
        R("Profil de fonctionnement", new Label { Text = U.S(x[9]), AutoSize = true });
        var cA = R("Niveau d'autonomie", Ui.Combo(new[] { "1 — Exécution", "2 — Organisation", "3 — Autonomie avancée" }, 220)); cA.SelectedIndex = (int)Math.Clamp(U.L(x[10]) - 1, 0, 2);
        var pH = new FlowLayoutPanel { Height = 32 };
        var nD = new NumericUpDown { Minimum = 6, Maximum = 12, Value = U.L(x[11]), Width = 60 }; var nF = new NumericUpDown { Minimum = 13, Maximum = 22, Value = U.L(x[12]), Width = 60 };
        pH.Controls.AddRange(new Control[] { new Label { Text = "de", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, nD, new Label { Text = "h à", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, nF, new Label { Text = "h", AutoSize = true, Padding = new Padding(0, 6, 0, 0) } });
        R("Horaires", pH);
        var cD = R("Disponibilité", Ui.Combo(new[] { "Disponible", "En réunion", "Absent", "Hors horaires" }, 200)); cD.SelectedItem = U.S(x[14]);
        R("Charge de travail", new ProgressBar { Value = (int)Math.Clamp(U.L(x[13]), 0, 100), Height = 18 });
        R("Téléphone / courriel", new Label { Text = $"{x[15]}  —  {x[16]}", AutoSize = true });
        R("Date d'entrée", new Label { Text = U.Aff(x[17]), AutoSize = true });
        var tN = R("Notes", new TextBox { Text = U.S(x[18]), Multiline = true }, 60);
        var bp = new FlowLayoutPanel { Height = 40, Dock = DockStyle.Fill };
        bp.Controls.Add(Ui.PrimaryBtn("Enregistrer la fiche", (s, e) => { if (Ui.Try(() => Db.Tx(c => Biz.ModifierFiche(c, id, tF.Text, tC.Text, tR.Text, cA.SelectedIndex + 1, (int)nD.Value, (int)nF.Value, cD.Text, tN.Text)))) Ui.Info("Fiche enregistrée."); }));
        bp.Controls.Add(Ui.Btn("Changer d'affectation…", (s, e) => { using var d = new AffectationDialog(id); if (d.ShowDialog(this) == DialogResult.OK) Close(); }));
        bp.Controls.Add(Ui.Btn("Confier une mission…", (s, e) => { using var d = new MissionDialog(id); d.ShowDialog(this); }));
        bp.Controls.Add(Ui.Btn("Envoyer un message…", (s, e) => { using var d = new MessageDialog(id); d.ShowDialog(this); }));
        bp.Controls.Add(Ui.Btn("Appeler…", (s, e) => { using var d = new AppelDialog(id); d.ShowDialog(this); }));
        bp.Controls.Add(Ui.Btn("Imprimer la fiche", (s, e) => Ui.Try(() => Export.Print("Fiche individuelle " + x[1] + " " + x[2], $"Fiche individuelle\n\n{x[1]} {x[2]}\n{x[3]} — {x[5]}\nSupérieur : {sup}\nCompétences : {x[7]}\nResponsabilités : {x[8]}\nProfil : {x[9]}\nHoraires : {x[11]}h-{x[12]}h\nCharge : {x[13]} %"))));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); t.Controls.Add(new Label()); t.Controls.Add(bp);
        tp.Controls.Add(t); tabs.TabPages.Add(tp);
        // --- autres onglets
        tabs.TabPages.Add(GridTab("Missions", $"fc_mis", (q, o, l) => Db.Table($@"SELECT id, numero AS 'N°', objet AS 'Objet', statut AS 'Statut', ROUND(avancement) AS 'Avancement', priorite AS 'Priorité', substr(echeance,1,16) AS 'Échéance',
            (SELECT prenom||' '||nom FROM collaborateurs WHERE id=emetteur_id) AS 'Émetteur' FROM missions WHERE destinataire_id=@p0 AND (objet LIKE @p1 OR numero LIKE @p1) ORDER BY id DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o),
            mid => new MissionView(mid).Show(this)));
        tabs.TabPages.Add(GridTab("Sous-tâches", "fc_st", (q, o, l) => Db.Table(@"SELECT st.id, m.numero AS 'Mission', st.libelle AS 'Sous-tâche', st.statut AS 'Statut', ROUND(st.avancement) AS 'Avancement' FROM sous_taches st JOIN missions m ON m.id=st.mission_id WHERE st.assigne_id=@p0 AND st.libelle LIKE @p1 ORDER BY st.id DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o), null));
        tabs.TabPages.Add(GridTab("Équipe", "fc_eq", (q, o, l) => Db.Table(@"SELECT id, nom||' '||prenom AS 'Nom', fonction AS 'Fonction', dispo AS 'Dispo', charge AS 'Charge' FROM collaborateurs WHERE superieur_id=@p0 AND (nom LIKE @p1 OR prenom LIKE @p1) ORDER BY niveau, nom LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o),
            cid => new FicheCollab(cid).Show(this)));
        tabs.TabPages.Add(GridTab("Historique d'activité", "fc_hist", (q, o, l) => Db.Table(@"SELECT id, substr(date,1,16) AS 'Date', type AS 'Type', description AS 'Description' FROM evenements WHERE (origine_id=@p0 OR (ref_type='collaborateur' AND ref_id=@p0)) AND description LIKE @p1 ORDER BY date DESC, id DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o), null));
        tabs.TabPages.Add(GridTab("Affectations", "fc_aff", (q, o, l) => Db.Table(@"SELECT id, substr(date,1,16) AS 'Date', (SELECT nom FROM services WHERE id=ancien_service) AS 'Ancien service', (SELECT nom FROM services WHERE id=nouveau_service) AS 'Nouveau service', motif AS 'Motif', auteur AS 'Auteur' FROM affectations_hist WHERE collab_id=@p0 AND IFNULL(motif,'') LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o), null));
        tabs.TabPages.Add(GridTab("Communications", "fc_msg", (q, o, l) => Db.Table(@"SELECT id, substr(date,1,16) AS 'Date', IFNULL((SELECT prenom||' '||nom FROM collaborateurs WHERE id=exp_id), exp_ext) AS 'De', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=dest_id) AS 'À', objet AS 'Objet' FROM messages WHERE (exp_id=@p0 OR dest_id=@p0) AND brouillon=0 AND objet LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o),
            mid => { var b = Db.Rows("SELECT objet, corps FROM messages WHERE id=@p0", mid)[0]; Ui.ShowText(U.S(b[0]), U.S(b[1]), this); }));
        tabs.TabPages.Add(GridTab("Décisions et comptes rendus", "fc_cr", (q, o, l) => Db.Table(@"SELECT id, numero AS 'N°', titre AS 'Titre', categorie AS 'Catégorie', substr(date,1,16) AS 'Date' FROM documents WHERE auteur_id=@p0 AND titre LIKE @p1 ORDER BY date DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o),
            did => new DocumentDialog(did).ShowDialog(this)));
        Controls.Add(tabs); Controls.Add(head);
    }

    TabPage GridTab(string title, string key, Func<string, int, int, DataTable> loader, Action<long> open)
    {
        var tp = new TabPage(title);
        var g = new GridPanel(key) { Loader = loader };
        if (open != null) g.Activated += id => Ui.Try(() => open(id));
        tp.Controls.Add(g);
        bool loaded = false;
        tp.Enter += (s, e) => { if (!loaded) { loaded = true; g.Reload(); } };
        return tp;
    }
}

public class AffectationDialog : Dlg
{
    public AffectationDialog(long id) : base("Changement d'affectation", 680, 360)
    {
        var x = Db.Rows("SELECT service_id, superieur_id, prenom||' '||nom FROM collaborateurs WHERE id=@p0", id)[0];
        Row("Collaborateur", new Label { Text = U.S(x[2]), Font = Theme.FB, AutoSize = true });
        var cSvc = Row("Nouveau service", Ui.Combo(Item.Services(), 360)); Item.Select(cSvc, U.L(x[0]));
        var cSup = Row("Nouveau supérieur", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        void Fill(long sel)
        {
            cSup.Items.Clear();
            foreach (var i in Item.Collabs("(c.service_id=@p0 OR c.niveau<=3) AND c.id<>@p1 AND c.niveau<=6", Item.Of(cSvc), id)) cSup.Items.Add(i);
            if (cSup.Items.Count > 0) cSup.SelectedIndex = 0;
            Item.Select(cSup, sel);
        }
        Fill(U.L(x[1]));
        cSvc.SelectedIndexChanged += (s, e) => Fill(Db.Long("SELECT responsable_id FROM services WHERE id=@p0", Item.Of(cSvc)));
        var tMot = Row("Motif", new TextBox { Text = "Réorganisation" });
        Row("", new Label { Text = "Opération contrôlée par les autorisations (droit « personnel.modifier ») et journalisée.", ForeColor = Color.Gray, AutoSize = true });
        Cancel("Annuler");
        Btn("Valider l'affectation", () => Db.Tx(c => Biz.Affecter(c, id, Item.Of(cSvc), Item.Of(cSup), tMot.Text)), true);
    }
}

// ============================================================ Vue mission détaillée
public class MissionView : Form
{
    readonly long id;
    public MissionView(long id)
    {
        this.id = id;
        Width = 1000; Height = 740; StartPosition = FormStartPosition.CenterParent; Font = Theme.F; ShowIcon = false;
        Build();
    }

    void Build()
    {
        Controls.Clear();
        var m = Db.Rows(@"SELECT numero, objet, description, emetteur_id, destinataire_id, (SELECT nom FROM services WHERE id=service_id), priorite, statut, avancement, date_creation, echeance, instructions, confidentialite,
            (SELECT numero||' — '||intitule FROM dossiers WHERE id=dossier_id), rapport, parent_id, (SELECT numero||' — '||titre FROM documents WHERE id=document_id) FROM missions WHERE id=@p0", id)[0];
        Text = $"Mission {m[0]} — {m[1]}";
        string em = Db.Q(c => Biz.NameF(c, U.L(m[3]))), de = Db.Q(c => Biz.NameF(c, U.L(m[4])));
        var head = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.Blue };
        head.Controls.Add(new Label { Text = $"{m[0]} — {m[1]}", Font = new Font("Segoe UI Semibold", 14f), ForeColor = Color.White, Location = new Point(14, 8), AutoSize = true, MaximumSize = new Size(960, 0) });
        head.Controls.Add(new Label { Text = $"Statut : {m[7]}   |   Priorité : {m[6]}   |   Échéance : {U.Aff(m[10])}   |   {em} → {de}", ForeColor = Color.FromArgb(220, 230, 245), Location = new Point(16, 42), AutoSize = true });
        var pb = new ProgressBar { Location = new Point(16, 66), Width = 400, Height = 14, Value = (int)Math.Clamp(U.Dv(m[8]), 0, 100) };
        head.Controls.Add(pb);
        head.Controls.Add(new Label { Text = $"{U.Dv(m[8]):0} %", ForeColor = Color.White, Location = new Point(424, 64), AutoSize = true });
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var info = $"Numéro : {m[0]}\nObjet : {m[1]}\nÉmetteur : {em}\nResponsable principal : {de}\nService concerné : {m[5]}\nPriorité : {m[6]}\nStatut : {m[7]}\nAvancement : {U.Dv(m[8]):0} %\nCréée le : {U.Aff(m[9])}\nÉchéance : {U.Aff(m[10])}\nConfidentialité interne : {m[12]}\nDossier associé : {m[13]}\nDocument associé : {m[16]}\n\nDescription :\n{m[2]}\n\nInstructions :\n{m[11]}";
        var assoc = Db.Rows("SELECT DISTINCT c.prenom||' '||c.nom||' — '||c.fonction FROM sous_taches st JOIN collaborateurs c ON c.id=st.assigne_id WHERE st.mission_id IN (SELECT id FROM missions WHERE id=@p0 OR parent_id=@p0)", id);
        info += "\n\nCollaborateurs associés :\n" + string.Join("\n", assoc.Select(a => " - " + a[0]));
        tabs.TabPages.Add(TextTab("Détail", info));
        var tpS = new TabPage("Sous-tâches");
        var gs = new GridPanel("mv_st") { Loader = (q, o, l) => Db.Table("SELECT st.id, st.libelle AS 'Sous-tâche', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=st.assigne_id) AS 'Assignée à', st.statut AS 'Statut', ROUND(st.avancement) AS 'Avancement', substr(st.date_fin,1,16) AS 'Terminée le' FROM sous_taches st WHERE st.mission_id=@p0 AND st.libelle LIKE @p1 LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
        tpS.Controls.Add(gs); gs.Reload(); tabs.TabPages.Add(tpS);
        tabs.TabPages.Add(TextTab("Chaîne de délégation", Db.Q(c => Biz.Chaine(c, id))));
        var tpH = new TabPage("Historique");
        var gh = new GridPanel("mv_hist") { Loader = (q, o, l) => Db.Table("SELECT id, substr(date,1,16) AS 'Date', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id) AS 'Auteur', type AS 'Type', texte AS 'Détail' FROM mission_hist WHERE mission_id IN (SELECT id FROM missions WHERE id=@p0 OR parent_id=@p0) AND texte LIKE @p1 ORDER BY date DESC, id DESC LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
        tpH.Controls.Add(gh); gh.Reload(); tabs.TabPages.Add(tpH);
        var tpD = new TabPage("Documents");
        var gd = new GridPanel("mv_docs") { Loader = (q, o, l) => Db.Table("SELECT id, numero AS 'N°', titre AS 'Titre', categorie AS 'Catégorie', substr(date,1,16) AS 'Date' FROM documents WHERE mission_id IN (SELECT id FROM missions WHERE id=@p0 OR parent_id=@p0) AND titre LIKE @p1 LIMIT @p2 OFFSET @p3", id, U.Like(q), l, o) };
        gd.Activated += did => new DocumentDialog(did).ShowDialog(this);
        tpD.Controls.Add(gd); gd.Reload(); tabs.TabPages.Add(tpD);
        tabs.TabPages.Add(TextTab("Rapport final", string.IsNullOrEmpty(U.S(m[14])) ? "Le rapport final n'a pas encore été transmis." : U.S(m[14])));
        var bar = Ui.Bar(
            Ui.PrimaryBtn("Accepter le rapport", (s, e) => Act(() => Db.Tx(c => Biz.ValiderMission(c, id, Ui.Prompt("Accepter", "Commentaire (facultatif) :") ?? "")))),
            Ui.Btn("Demander des modifications", (s, e) => { var t = Ui.Prompt("Modifications", "Modifications demandées :", "", true); if (!string.IsNullOrWhiteSpace(t)) Act(() => Db.Tx(c => Biz.DemanderModification(c, id, t))); }),
            Ui.Btn("Refuser", (s, e) => { var t = Ui.Prompt("Refus", "Motif du refus :"); if (!string.IsNullOrWhiteSpace(t)) Act(() => Db.Tx(c => Biz.RefuserMission(c, id, t))); }),
            Ui.Btn("Clôturer", (s, e) => { if (Ui.Confirm("Clôturer cette mission (et ses sous-missions actives) ?")) Act(() => Db.Tx(c => Biz.CloturerMission(c, id))); }),
            Ui.Btn("Ajouter un commentaire", (s, e) => { var t = Ui.Prompt("Commentaire", "Commentaire :", "", true); if (!string.IsNullOrWhiteSpace(t)) Act(() => Db.Tx(c => Biz.MHist(c, id, Auth.CollabId, "Commentaire", t))); }),
            Ui.Btn("Ajouter une sous-tâche", (s, e) => { var t = Ui.Prompt("Sous-tâche", "Libellé de la sous-tâche :"); if (!string.IsNullOrWhiteSpace(t)) Act(() => Db.Tx(c => Biz.AjouterSousTache(c, id, t, 0))); }),
            Ui.Btn("Écrire au responsable", (s, e) => { using var d = new MessageDialog(U.L(m[4]), $"[{m[0]}] {m[1]}", "", id); d.ShowDialog(this); }),
            Ui.Btn("Exporter PDF", (s, e) => Ui.Try(() => Export.PdfOpen($"Mission {m[0]}", info + "\n\n" + Db.Q(c => Biz.Chaine(c, id)) + "\n\n" + U.S(m[14])))),
            Ui.Btn("Actualiser", (s, e) => Build()));
        bar.Height = 42; bar.Dock = DockStyle.Bottom;
        Controls.Add(tabs); Controls.Add(bar); Controls.Add(head);
    }

    void Act(Action a) { if (Ui.Try(a)) Build(); }

    static TabPage TextTab(string title, string text)
    {
        var tp = new TabPage(title);
        tp.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9.5f), Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n"), BackColor = Color.White, WordWrap = false });
        return tp;
    }
}

// ============================================================ Connexion / initialisation
public class LoginForm : Form
{
    public LoginForm()
    {
        Text = "PARQUET PRO — Connexion"; Width = 520; Height = 400; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; Font = Theme.F; BackColor = Color.White;
        var head = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = Theme.Navy };
        head.Controls.Add(new Label { Text = "PARQUET PRO", Font = new Font("Segoe UI", 24f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(24, 18), AutoSize = true });
        head.Controls.Add(new Label { Text = "Simulation institutionnelle — poste du procureur de la République", ForeColor = Color.FromArgb(190, 210, 235), Location = new Point(28, 70), AutoSize = true });
        var lU = new Label { Text = "Identifiant", Location = new Point(60, 135), AutoSize = true };
        var tU = new TextBox { Location = new Point(60, 156), Width = 380, Text = "procureur" };
        var lP = new Label { Text = "Mot de passe", Location = new Point(60, 190), AutoSize = true };
        var tP = new TextBox { Location = new Point(60, 211), Width = 380, UseSystemPasswordChar = true };
        var err = new Label { Location = new Point(60, 240), ForeColor = Theme.Red, AutoSize = true };
        var ok = Ui.PrimaryBtn("Se connecter", null, 180); ok.Location = new Point(260, 262);
        var hint = new Label { Text = "Comptes : procureur / parquet  •  adjoint / adjoint  •  admin / admin  •  consultation / consultation", Location = new Point(20, 320), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8f) };
        ok.Click += (s, e) =>
        {
            try
            {
                if (Auth.Connect(tU.Text, tP.Text)) { DialogResult = DialogResult.OK; Close(); }
                else { err.Text = "Identifiant ou mot de passe incorrect."; tP.SelectAll(); tP.Focus(); }
            }
            catch (Exception ex) { Log.Error(ex); err.Text = "Erreur : " + ex.Message; }
        };
        AcceptButton = ok;
        Controls.AddRange(new Control[] { head, lU, tU, lP, tP, err, ok, hint });
        Shown += (s, e) => tP.Focus();
    }
}

public class InitForm : Form
{
    public Exception Error;
    public InitForm(string titre = "Initialisation de votre organisation", Action<Action<int, string>> work = null, string detail = "20 services, 300 collaborateurs, organigramme, univers institutionnel.")
    {
        work ??= Seed.Run;
        Text = "PARQUET PRO"; Width = 560; Height = 230; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; ControlBox = false; Font = Theme.F; BackColor = Color.White;
        var head = new Label { Dock = DockStyle.Top, Height = 54, Text = "  " + titre, Font = Theme.FT, ForeColor = Color.White, BackColor = Theme.Navy, TextAlign = ContentAlignment.MiddleLeft };
        var lbl = new Label { Location = new Point(24, 76), Width = 500, Text = "Préparation…" };
        var pb = new ProgressBar { Location = new Point(24, 104), Width = 500, Height = 20, Style = ProgressBarStyle.Marquee };
        var l2 = new Label { Location = new Point(24, 136), Width = 500, Height = 40, ForeColor = Color.Gray, Text = detail };
        Controls.AddRange(new Control[] { head, lbl, pb, l2 });
        Shown += async (s, e) =>
        {
            try
            {
                await Task.Run(() => work((p, t) => BeginInvoke(() => { if (pb.Style != ProgressBarStyle.Continuous && p > 0) pb.Style = ProgressBarStyle.Continuous; if (p > 0) pb.Value = Math.Min(100, p); lbl.Text = t; })));
            }
            catch (Exception ex) { Error = ex; }
            Close();
        };
    }
}
