using System.Data;
using System.Text;

namespace ParquetPro;

// ============================================================ ADMINISTRATION
public class AdminModule : Module
{
    public override string Titre => "Administration";
    public override bool AutoRefresh => false;
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly GridPanel gUsers, gSvc, gJA, gJT, gBk, gCol;
    readonly DataGridView gDroits = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    readonly DataGridView gKeys = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    readonly TextBox diag = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9.5f), BackColor = Color.White };

    public void ShowTab(string name) { foreach (TabPage t in tabs.TabPages) if (t.Text == name) tabs.SelectedTab = t; }

    public AdminModule()
    {
        TabPage Tab(string t, Control c, params Control[] buttons)
        {
            var tp = new TabPage(t) { BackColor = Theme.Back };
            tp.Controls.Add(c);
            if (buttons.Length > 0) tp.Controls.Add(Ui.Bar(buttons));
            tabs.TabPages.Add(tp); return tp;
        }
        // utilisateurs
        gUsers = new GridPanel("adm_users") { Loader = (q, o, l) => Db.Table("SELECT u.id, u.login AS 'Identifiant', u.role AS 'Profil', (SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=u.collab_id) AS 'Collaborateur associé', CASE u.actif WHEN 1 THEN 'Actif' ELSE 'Désactivé' END AS 'État', u.derniere_connexion AS 'Dernière connexion' FROM utilisateurs u WHERE u.login LIKE @p0 LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        Tab("Profils et utilisateurs", gUsers, Ui.PrimaryBtn("Ajouter un utilisateur", (s, e) => AddUser()),
            Ui.Btn("Réinitialiser le mot de passe", (s, e) => A(() => { long id = gUsers.SelectedId; if (id == 0) return; var p = Ui.Prompt("Mot de passe", "Nouveau mot de passe :"); if (string.IsNullOrWhiteSpace(p)) return; Db.Exec("UPDATE utilisateurs SET mdp=@p1 WHERE id=@p0", id, U.Hash(p)); Biz.Admin("Mot de passe", "Réinitialisation pour l'utilisateur n°" + id); })),
            Ui.Btn("Changer de profil", (s, e) => A(() => { long id = gUsers.SelectedId; if (id == 0) return; var r = Ui.Prompt("Profil", "Profil (" + string.Join(", ", Auth.Roles) + ") :", U.S(gUsers.Cell("Profil"))); if (r == null || !Auth.Roles.Contains(r)) throw new InvalidOperationException("Profil inconnu."); Db.Exec("UPDATE utilisateurs SET role=@p1 WHERE id=@p0", id, r); Biz.Admin("Profil", $"Utilisateur n°{id} : {r}"); })),
            Ui.Btn("Activer / désactiver", (s, e) => A(() => { long id = gUsers.SelectedId; if (id == 0) return; if (id == Auth.UserId) throw new InvalidOperationException("Vous ne pouvez pas désactiver votre propre compte."); Db.Exec("UPDATE utilisateurs SET actif=1-actif WHERE id=@p0", id); Biz.Admin("Utilisateur", "Activation/désactivation n°" + id); })));
        // droits
        Tab("Gestion des droits", gDroits, Ui.PrimaryBtn("Enregistrer les droits", (s, e) => A(SaveDroits)), new Label { Text = "Les autorisations sont contrôlées par la logique métier à chaque opération.", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(8, 8, 0, 0) });
        // services
        gSvc = new GridPanel("adm_svc") { Loader = (q, o, l) => Db.Table("SELECT s.id, s.code AS 'Code', s.nom AS 'Service', s.categorie AS 'Catégorie', s.effectif_cible AS 'Effectif cible', (SELECT COUNT(*) FROM collaborateurs WHERE service_id=s.id AND actif=1) AS 'Effectif réel', (SELECT prenom||' '||nom FROM collaborateurs WHERE id=s.responsable_id) AS 'Responsable', CASE s.actif WHEN 1 THEN 'Actif' ELSE 'Inactif' END AS 'État' FROM services s WHERE s.nom LIKE @p0 ORDER BY s.id LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        Tab("Services", gSvc, Ui.PrimaryBtn("Nouveau service", (s, e) => A(() =>
            {
                var n = Ui.Prompt("Nouveau service", "Nom du service :"); if (string.IsNullOrWhiteSpace(n)) return;
                var code = Ui.Prompt("Nouveau service", "Code court (ex. NOUV) :", n.Length >= 4 ? n[..4].ToUpperInvariant() : n.ToUpperInvariant()); if (string.IsNullOrWhiteSpace(code)) return;
                Db.Exec("INSERT INTO services(code,nom,categorie,effectif_cible,responsable_id) VALUES(@p0,@p1,'Transversal',0,@p2)", code.Trim().ToUpperInvariant(), n.Trim(), Auth.CollabId); Biz.Admin("Service", "Création du service " + n);
            })),
            Ui.Btn("Renommer", (s, e) => A(() => { long id = gSvc.SelectedId; if (id == 0) return; var n = Ui.Prompt("Renommer", "Nouveau nom :", U.S(gSvc.Cell("Service"))); if (string.IsNullOrWhiteSpace(n)) return; Db.Exec("UPDATE services SET nom=@p1 WHERE id=@p0", id, n); Biz.Admin("Service", $"Renommage du service n°{id} : {n}"); })),
            Ui.Btn("Effectif cible", (s, e) => A(() => { long id = gSvc.SelectedId; if (id == 0) return; var n = Ui.Prompt("Effectif cible", "Effectif cible :", U.S(gSvc.Cell("Effectif cible"))); if (!int.TryParse(n, out var v)) return; Db.Exec("UPDATE services SET effectif_cible=@p1 WHERE id=@p0", id, v); Biz.Admin("Service", $"Effectif cible du service n°{id} : {v}"); })),
            Ui.Btn("Désigner le responsable", (s, e) => A(() =>
            {
                long id = gSvc.SelectedId; if (id == 0) return;
                using var f = new Form { Text = "Responsable du service", Width = 600, Height = 140, StartPosition = FormStartPosition.CenterParent, Font = Theme.F };
                var cb = Ui.Combo(Item.Collabs("c.service_id=@p0", id).Cast<object>(), 560); cb.Location = new Point(12, 12);
                var ok = new Button { Text = "Valider", DialogResult = DialogResult.OK, Location = new Point(470, 50) };
                f.Controls.Add(cb); f.Controls.Add(ok);
                if (f.ShowDialog() == DialogResult.OK && Item.Of(cb) != 0) { Db.Exec("UPDATE services SET responsable_id=@p1 WHERE id=@p0", id, Item.Of(cb)); Biz.Admin("Service", $"Nouveau responsable du service n°{id} : {cb.Text}"); }
            })),
            Ui.Btn("Contrôler les effectifs", (s, e) => { long t = Db.Long("SELECT COUNT(*) FROM collaborateurs WHERE actif=1"); long c = Db.Long("SELECT SUM(effectif_cible) FROM services WHERE actif=1"); Ui.Info($"Effectif réel : {t} collaborateurs actifs.\nSomme des effectifs cibles : {c}.\n{(t == 300 ? "Contrôle conforme : 300 collaborateurs." : "Écart par rapport à l'effectif initial de 300.")}"); }));
        // collaborateurs
        gCol = new GridPanel("adm_col") { Loader = (q, o, l) => Db.Table("SELECT c.id, c.matricule AS 'Matricule', c.nom||' '||c.prenom AS 'Nom', c.fonction AS 'Fonction', s.code AS 'Service', c.niveau AS 'Niveau', c.autonomie AS 'Autonomie', CASE c.actif WHEN 1 THEN 'Actif' ELSE 'Inactif' END AS 'État' FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.nom LIKE @p0 OR c.prenom LIKE @p0 OR c.matricule LIKE @p0 ORDER BY c.id LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gCol.Activated += id => new FicheCollab(id).Show(this);
        Tab("Collaborateurs", gCol, Ui.PrimaryBtn("Ajouter un collaborateur", (s, e) => A(AddCollab)),
            Ui.Btn("Activer / désactiver", (s, e) => A(() =>
            {
                long id = gCol.SelectedId; if (id == 0 || id == 1) throw new InvalidOperationException("Sélectionnez un collaborateur (le procureur ne peut pas être désactivé).");
                if (Db.Long("SELECT COUNT(*) FROM collaborateurs WHERE superieur_id=@p0 AND actif=1", id) > 0) throw new InvalidOperationException("Ce collaborateur encadre des personnes : réaffectez-les d'abord.");
                Db.Exec("UPDATE collaborateurs SET actif=1-actif WHERE id=@p0", id); Biz.Admin("Collaborateur", "Activation/désactivation du collaborateur n°" + id);
            })),
            Ui.Btn("Fiche", (s, e) => { if (gCol.SelectedId != 0) new FicheCollab(gCol.SelectedId).Show(this); }));
        // moteur
        Tab("Paramètres du moteur", BuildMoteur());
        // raccourcis
        Tab("Raccourcis", gKeys, Ui.PrimaryBtn("Modifier le raccourci…", (s, e) => EditKey()), Ui.Btn("Rétablir les raccourcis par défaut", (s, e) => A(() => { Db.Exec("DELETE FROM parametres WHERE cle LIKE 'key_%'"); MainForm.LoadKeys(); FillKeys(); })));
        // sauvegardes
        gBk = new GridPanel("adm_bk") { Loader = (q, o, l) =>
        {
            var dt = new DataTable(); dt.Columns.Add("id", typeof(long)); dt.Columns.Add("Fichier"); dt.Columns.Add("Date"); dt.Columns.Add("Taille (Ko)", typeof(long)); dt.Columns.Add("_path");
            long i = 0;
            foreach (var f in new DirectoryInfo(Db.BackupDir).GetFiles("*.db").OrderByDescending(f => f.LastWriteTime).Where(f => f.Name.Contains(q ?? "")))
                dt.Rows.Add(++i, f.Name, f.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss"), f.Length / 1024, f.FullName);
            return dt;
        } };
        Tab("Sauvegarde et restauration", gBk, Ui.PrimaryBtn("Sauvegarder maintenant", (s, e) => A(() => { Ui.Info("Sauvegarde créée :\n" + Db.Backup("manuelle")); gBk.Reload(); })),
            Ui.Btn("Restaurer la sauvegarde sélectionnée", (s, e) => A(() => { var p = U.S(gBk.Cell("_path")); if (p != "") Restore(p); })),
            Ui.Btn("Restaurer depuis un fichier…", (s, e) => A(() => { using var o = new OpenFileDialog { Filter = "Base PARQUET PRO (*.db)|*.db", InitialDirectory = Db.BackupDir }; if (o.ShowDialog() == DialogResult.OK) Restore(o.FileName); })),
            Ui.Btn("Intervalle automatique…", (s, e) => A(() => { var v = Ui.Prompt("Sauvegarde automatique", "Intervalle en minutes (minimum 5) :", Db.Get("backup_min", "30")); if (int.TryParse(v, out var m) && m >= 5) { Db.Set("backup_min", m); Engine.ReloadSettings(); } })),
            Ui.Btn("Ouvrir le dossier", (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Db.BackupDir) { UseShellExecute = true })));
        // journaux
        gJA = new GridPanel("adm_ja") { PageSize = 500, Loader = (q, o, l) => Db.Table("SELECT id, date_reelle AS 'Date réelle', substr(date,1,16) AS 'Date simulée', auteur AS 'Auteur', action AS 'Action', details AS 'Détails' FROM journal_admin WHERE details LIKE @p0 OR action LIKE @p0 ORDER BY id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        Tab("Journal d'administration", gJA);
        gJT = new GridPanel("adm_jt") { PageSize = 500, Loader = (q, o, l) => Db.Table("SELECT id, date_reelle AS 'Date', niveau AS 'Niveau', message AS 'Message' FROM journal_tech WHERE message LIKE @p0 ORDER BY id DESC LIMIT @p1 OFFSET @p2", U.Like(q), l, o) };
        gJT.Activated += id => Ui.ShowText("Journal technique", Db.Str("SELECT message FROM journal_tech WHERE id=@p0", id) ?? "");
        Tab("Journal technique", gJT, Ui.Btn("Purger le journal technique", (s, e) => A(() => { if (Ui.Confirm("Purger le journal technique ?")) { Db.Exec("DELETE FROM journal_tech"); gJT.Reload(); } })));
        // diagnostic / maintenance
        Tab("Diagnostic et maintenance", diag, Ui.PrimaryBtn("Lancer le diagnostic", (s, e) => Diagnostic()), Ui.Btn("Contrôle de cohérence", (s, e) => A(Coherence)),
            Ui.Btn("Optimiser la base (VACUUM)", (s, e) => A(() => { lock (Db.WriteLock) { using var c = new DbCtx(); c.Exec("PRAGMA wal_checkpoint(TRUNCATE); VACUUM; ANALYZE;"); } Biz.Admin("Maintenance", "Optimisation de la base"); Diagnostic(); })),
            Ui.Btn("Réindexer", (s, e) => A(() => { Db.Exec("REINDEX"); Biz.Admin("Maintenance", "Réindexation"); Ui.Info("Réindexation terminée."); })));
        tabs.SelectedIndexChanged += (s, e) => RefreshData();
        Controls.Add(tabs); Controls.Add(Ui.Title("Administration — configuration de PARQUET PRO"));
    }

    void A(Action a) { if (Ui.Try(() => { Auth.Check("admin"); a(); })) RefreshData(); }

    Control BuildMoteur()
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16), AutoScroll = true, BackColor = Color.White };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300)); t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        TC R<TC>(string l, TC c) where TC : Control { t.RowStyles.Add(new RowStyle(SizeType.AutoSize)); t.Controls.Add(new Label { Text = l, AutoSize = true, Padding = new Padding(0, 8, 0, 0) }); c.Margin = new Padding(3, 5, 3, 5); t.Controls.Add(c); return c; }
        var cV = R("Vitesse de simulation", Ui.Combo(Engine.Vitesses.Select(v => (object)Engine.VitesseLabel(v)), 240)); cV.SelectedIndex = Math.Max(0, Array.IndexOf(Engine.Vitesses, Engine.Speed));
        var cF = R("Fréquence des événements autonomes", Ui.Combo(new[] { "0.25", "0.5", "1.0", "1.5", "2.0", "3.0" }, 100)); cF.SelectedItem = Db.Get("frequence", "1.0"); if (cF.SelectedIndex < 0) cF.SelectedIndex = 2;
        var cA = R("Niveau d'autonomie par défaut", Ui.Combo(new[] { "1 — Exécution", "2 — Organisation", "3 — Autonomie avancée" }, 240)); cA.SelectedIndex = (int)Math.Clamp(long.Parse(Db.Get("autonomie_defaut", "3")) - 1, 0, 2);
        var chkAll = R("", new CheckBox { Text = "Appliquer ce niveau à tous les collaborateurs", AutoSize = true });
        var cR = R("Au démarrage de l'application", Ui.Combo(new[] { "Reprise automatique du moteur", "Démarrer le moteur en pause" }, 280)); cR.SelectedIndex = Db.Get("reprise", "auto") == "pause" ? 1 : 0;
        var chkF = R("Fonctionnement en tâche de fond", new CheckBox { Text = "Continuer en tâche de fond à la fermeture de la fenêtre (icône de notification)", AutoSize = true, Checked = Db.Get("fond", "0") == "1" });
        var chkW = R("Démarrage de Windows", new CheckBox { Text = "Lancer PARQUET PRO en tâche de fond à l'ouverture de session Windows", AutoSize = true, Checked = Db.Get("demarrage_windows", "0") == "1" });
        var chkP = R("Notifications", new CheckBox { Text = "Afficher les alertes en fenêtre contextuelle", AutoSize = true, Checked = Db.Get("popup_notifs", "1") == "1" });
        var cD = R("Densité d'affichage", Ui.Combo(new[] { "Compacte", "Normale", "Confortable" }, 160)); cD.SelectedItem = Db.Get("densite", "Normale");
        R("Temps simulé actuel", new Label { Text = U.Aff(U.D(Engine.Now)) + " — le moteur ne simule jamais de période pendant laquelle il était arrêté.", AutoSize = true });
        R("", Ui.PrimaryBtn("Enregistrer les paramètres", (s, e) => A(() =>
        {
            Db.Tx(c =>
            {
                c.Set("frequence", cF.Text); c.Set("autonomie_defaut", cA.SelectedIndex + 1); c.Set("reprise", cR.SelectedIndex == 1 ? "pause" : "auto");
                c.Set("fond", chkF.Checked ? 1 : 0); c.Set("demarrage_windows", chkW.Checked ? 1 : 0); c.Set("popup_notifs", chkP.Checked ? 1 : 0); c.Set("densite", cD.Text);
                if (chkAll.Checked) c.Exec("UPDATE collaborateurs SET autonomie=@p0", cA.SelectedIndex + 1);
                Biz.Admin(c, "Paramètres du moteur", $"Fréquence {cF.Text}, autonomie {cA.SelectedIndex + 1}{(chkAll.Checked ? " (appliquée à tous)" : "")}, reprise {cR.Text}, tâche de fond {chkF.Checked}, démarrage Windows {chkW.Checked}");
            });
            Engine.SetSpeed(Engine.Vitesses[cV.SelectedIndex]);
            Engine.ReloadSettings();
            try { Program.SetAutostart(chkW.Checked); } catch (Exception ex) { Log.Error(ex); }
            Ui.Info("Paramètres enregistrés." + (cD.Text != Theme.RowHeight switch { 20 => "Compacte", 30 => "Confortable", _ => "Normale" } ? "\nLa densité d'affichage s'appliquera au prochain démarrage (ou via le menu Affichage)." : ""));
        }), 220));
        return t;
    }

    void AddUser()
    {
        Auth.Check("admin");
        var login = Ui.Prompt("Nouvel utilisateur", "Identifiant :"); if (string.IsNullOrWhiteSpace(login)) return;
        var mdp = Ui.Prompt("Nouvel utilisateur", "Mot de passe :"); if (string.IsNullOrWhiteSpace(mdp)) return;
        var role = Ui.Prompt("Nouvel utilisateur", "Profil (" + string.Join(", ", Auth.Roles) + ") :", "Consultation"); if (role == null || !Auth.Roles.Contains(role)) throw new InvalidOperationException("Profil inconnu.");
        var mat = Ui.Prompt("Nouvel utilisateur", "Matricule du collaborateur associé (ex. PP-0001) :", "PP-0001");
        long cid = Db.Long("SELECT id FROM collaborateurs WHERE matricule=@p0", mat?.Trim() ?? "");
        if (cid == 0) throw new InvalidOperationException("Matricule introuvable.");
        Db.Exec("INSERT INTO utilisateurs(login,mdp,role,collab_id) VALUES(@p0,@p1,@p2,@p3)", login.Trim(), U.Hash(mdp), role, cid);
        Biz.Admin("Utilisateur", $"Création de l'utilisateur {login} ({role})");
        gUsers.Reload();
    }

    void AddCollab()
    {
        var p = Ui.Prompt("Nouveau collaborateur", "Prénom :"); if (string.IsNullOrWhiteSpace(p)) return;
        var n = Ui.Prompt("Nouveau collaborateur", "Nom :"); if (string.IsNullOrWhiteSpace(n)) return;
        var f = Ui.Prompt("Nouveau collaborateur", "Fonction :", "Chargé de mission"); if (string.IsNullOrWhiteSpace(f)) return;
        var code = Ui.Prompt("Nouveau collaborateur", "Code du service (ex. GEST, ENQ, SG…) :", "GEST");
        long svc = Db.Long("SELECT id FROM services WHERE code=@p0", (code ?? "").Trim().ToUpperInvariant());
        if (svc == 0) throw new InvalidOperationException("Service introuvable.");
        long sup = Db.Long("SELECT responsable_id FROM services WHERE id=@p0", svc);
        Db.Tx(c =>
        {
            long max = c.Long("SELECT COUNT(*) FROM collaborateurs") + 1;
            long id = c.Insert("INSERT INTO collaborateurs(matricule,prenom,nom,fonction,niveau,service_id,superieur_id,competences,responsabilites,profil,autonomie,horaire_debut,horaire_fin,charge_base,telephone,email,date_entree) VALUES(@p0,@p1,@p2,@p3,7,@p4,@p5,'Rédaction, Synthèse','Missions du service','Méthodique',@p6,9,18,10,@p7,'nouveau@parquetpro.sim',@p8)",
                $"PP-{max:0000}", p.Trim(), n.Trim().ToUpperInvariant(), f.Trim(), svc, sup, long.Parse(c.Get("autonomie_defaut", "3")), $"Poste {4000 + max}", U.D(Engine.Now));
            Biz.Admin(c, "Collaborateur", $"Création du collaborateur {p} {n} ({f}) — service {code}");
            Biz.Event(c, "Changement d'organisation", "Ressources humaines", 0, $"Arrivée de {p} {n.ToUpperInvariant()} ({f})", "collaborateur", id, svc: svc);
        });
    }

    void FillDroits()
    {
        var dt = new DataTable(); dt.Columns.Add("Droit");
        foreach (var r in Auth.Roles) dt.Columns.Add(r, typeof(bool));
        var have = Db.Rows("SELECT role, droit FROM autorisations").Select(x => U.S(x[0]) + "|" + U.S(x[1])).ToHashSet();
        foreach (var d in Auth.Droits) { var row = dt.NewRow(); row[0] = d; foreach (var r in Auth.Roles) row[r] = have.Contains(r + "|" + d); dt.Rows.Add(row); }
        gDroits.DataSource = dt;
        if (gDroits.Columns.Count > 0) gDroits.Columns[0].ReadOnly = true;
    }

    void SaveDroits()
    {
        if (gDroits.DataSource is not DataTable dt) return;
        gDroits.EndEdit();
        if (!(bool)dt.Rows.Cast<DataRow>().First(r => (string)r[0] == "admin")["Procureur"] && !(bool)dt.Rows.Cast<DataRow>().First(r => (string)r[0] == "admin")["Administrateur"])
            throw new InvalidOperationException("Au moins un profil doit conserver le droit « admin ».");
        Db.Tx(c =>
        {
            c.Exec("DELETE FROM autorisations");
            foreach (DataRow r in dt.Rows) foreach (var role in Auth.Roles) if (r[role] is bool b && b) c.Exec("INSERT INTO autorisations VALUES(@p0,@p1)", role, r[0]);
            Biz.Admin(c, "Droits", "Mise à jour de la matrice des autorisations");
        });
        Auth.Reload();
        Ui.Info("Droits enregistrés.");
    }

    void FillKeys()
    {
        var dt = new DataTable(); dt.Columns.Add("Action"); dt.Columns.Add("Raccourci");
        var kc = new KeysConverter();
        foreach (var a in MainForm.Actions) dt.Rows.Add(a.name, kc.ConvertToString(MainForm.KeyOf(a.name)));
        gKeys.DataSource = dt;
    }

    void EditKey()
    {
        if (gKeys.CurrentRow == null) return;
        string name = U.S(gKeys.CurrentRow.Cells[0].Value);
        using var f = new Form { Text = "Nouveau raccourci — " + name, Width = 420, Height = 150, StartPosition = FormStartPosition.CenterParent, Font = Theme.F, KeyPreview = true, FormBorderStyle = FormBorderStyle.FixedDialog };
        var l = new Label { Text = "Appuyez sur la combinaison de touches souhaitée… (Échap : aucun)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        Keys chosen = Keys.None; bool done = false;
        f.Controls.Add(l);
        f.KeyDown += (s, e) =>
        {
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu) return;
            chosen = e.KeyCode == Keys.Escape ? Keys.None : e.KeyData; done = true; f.Close();
        };
        f.ShowDialog();
        if (!done) return;
        Db.Set("key_" + name, (int)chosen);
        MainForm.LoadKeys(); FillKeys();
    }

    void Restore(string path)
    {
        if (!Ui.Confirm("Restaurer cette sauvegarde ?\nL'état actuel sera d'abord sauvegardé, puis l'application redémarrera.")) return;
        Db.Backup("avant_restauration");
        Engine.Stop();
        Db.RestoreFrom(path);
        Log.Info("Restauration effectuée depuis " + path);
        Application.Restart();
        Environment.Exit(0);
    }

    void Diagnostic()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PARQUET PRO 1.0 — diagnostic du {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Base de données : {Db.PathDb} ({new FileInfo(Db.PathDb).Length / 1024} Ko)");
        sb.AppendLine($"Version du schéma : {Db.Long("PRAGMA user_version")} — mode journal : {Db.Str("PRAGMA journal_mode")}");
        sb.AppendLine($"Intégrité SQLite : {Db.Str("PRAGMA integrity_check")}");
        sb.AppendLine($"Moteur : {Engine.Etat} — vitesse {Engine.VitesseLabel(Engine.Speed)} — temps simulé {U.Aff(U.D(Engine.Now))}");
        sb.AppendLine($"Utilisateur : {Auth.Login} ({Auth.Role})\n");
        foreach (var t in new[] { "services", "collaborateurs", "organismes", "interlocuteurs", "dossiers", "missions", "sous_taches", "messages", "documents", "reunions", "appels", "evenements", "notifications", "demandes", "journal_admin", "journal_tech" })
            sb.AppendLine($"  {t,-18} {Db.Long($"SELECT COUNT(*) FROM {t}"),8}");
        sb.AppendLine($"\nCollaborateurs actifs : {Db.Long("SELECT COUNT(*) FROM collaborateurs WHERE actif=1")}");
        sb.AppendLine($"Erreurs journalisées : {Db.Long("SELECT COUNT(*) FROM journal_tech WHERE niveau='ERREUR'")}");
        sb.AppendLine($"Sauvegardes disponibles : {Directory.GetFiles(Db.BackupDir, "*.db").Length}");
        diag.Text = sb.ToString().Replace("\n", "\r\n");
    }

    void Coherence()
    {
        var sb = new StringBuilder("CONTRÔLE DE COHÉRENCE\n\n");
        int fixes = 0;
        Db.Tx(c =>
        {
            int a = c.Exec($"UPDATE missions SET statut='Clôturée', date_cloture=@p0 WHERE statut IN {Biz.SqlActifs} AND parent_id IN (SELECT id FROM missions WHERE statut IN ('Clôturée','Refusée','Validée'))", U.D(Engine.Now));
            sb.AppendLine($"Sous-missions actives rattachées à une mission terminée : {a} corrigée(s)."); fixes += a;
            int b = c.Exec("DELETE FROM sous_taches WHERE mission_id NOT IN (SELECT id FROM missions)");
            sb.AppendLine($"Sous-tâches orphelines : {b} supprimée(s)."); fixes += b;
            int d = c.Exec("DELETE FROM reunion_participants WHERE reunion_id NOT IN (SELECT id FROM reunions)");
            sb.AppendLine($"Participations orphelines : {d} supprimée(s)."); fixes += d;
            int e = c.Exec("UPDATE collaborateurs SET superieur_id=NULL WHERE superieur_id IS NOT NULL AND superieur_id NOT IN (SELECT id FROM collaborateurs)");
            sb.AppendLine($"Liens hiérarchiques invalides : {e} corrigé(s)."); fixes += e;
            long conf = c.Long(@"SELECT COUNT(*) FROM reunion_participants a JOIN reunions ra ON ra.id=a.reunion_id JOIN reunion_participants b ON b.collab_id=a.collab_id AND b.reunion_id>a.reunion_id JOIN reunions rb ON rb.id=b.reunion_id
                WHERE ra.statut IN ('Planifiée','Proposée') AND rb.statut IN ('Planifiée','Proposée') AND ra.debut<rb.fin AND ra.fin>rb.debut");
            sb.AppendLine($"Conflits d'agenda détectés : {conf}.");
            long rap = c.Long("SELECT COUNT(*) FROM missions WHERE statut IN ('Rapport transmis','Validée') AND (rapport IS NULL OR rapport='') AND origine<>'Historique'");
            sb.AppendLine($"Rapports sans contenu : {rap}.");
            long dup = c.Long("SELECT COUNT(*) FROM (SELECT uid FROM evenements GROUP BY uid HAVING COUNT(*)>1)");
            sb.AppendLine($"Événements en doublon (identifiant) : {dup}.");
            Biz.Admin(c, "Maintenance", $"Contrôle de cohérence : {fixes} correction(s)");
        });
        diag.Text = sb.ToString().Replace("\n", "\r\n");
    }

    public override void RefreshData()
    {
        switch (tabs.SelectedTab?.Text)
        {
            case "Profils et utilisateurs": gUsers.Reload(); break;
            case "Gestion des droits": FillDroits(); break;
            case "Services": gSvc.Reload(); break;
            case "Collaborateurs": gCol.Reload(); break;
            case "Raccourcis": FillKeys(); break;
            case "Sauvegarde et restauration": gBk.Reload(); break;
            case "Journal d'administration": gJA.Reload(); break;
            case "Journal technique": gJT.Reload(); break;
            case "Diagnostic et maintenance": Diagnostic(); break;
        }
    }
}

// ============================================================ ASSISTANT
public interface IAssistantProvider
{
    string Nom { get; }
    AssistantReponse Traiter(string demande);
}

public class AssistantReponse
{
    public string Texte;
    public string ActionLibelle;
    public Action Action;
}

/// Assistant local à règles (fonctionne sans abonnement). Interface prévue pour brancher un modèle de langage ultérieurement :
/// toute action proposée passe par les règles métier et une validation humaine explicite.
public class AssistantLocal : IAssistantProvider
{
    public string Nom => "Assistant local (règles, hors ligne)";

    static string N(string s) => new string(s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

    static long TrouverCollab(string texte)
    {
        var t = N(texte);
        if (t.Contains("adjoint")) return long.Parse(Db.Get("adjoint_id", "0"));
        if (t.Contains("secretaire general") || t.Contains("secretariat general")) return long.Parse(Db.Get("sg_id", "0"));
        if (t.Contains("secretaire particulier") || t.Contains("secretariat particulier")) return long.Parse(Db.Get("secp_id", "0"));
        if (t.Contains("directeur de cabinet")) return long.Parse(Db.Get("dircab_id", "0"));
        foreach (var r in Db.Rows("SELECT id, prenom, nom FROM collaborateurs WHERE actif=1 ORDER BY niveau"))
        {
            string nom = N(U.S(r[2]));
            if (nom.Length > 2 && System.Text.RegularExpressions.Regex.IsMatch(t, @"\b" + System.Text.RegularExpressions.Regex.Escape(nom) + @"\b")) return U.L(r[0]);
        }
        foreach (var s in Db.Rows("SELECT code, responsable_id, nom FROM services"))
            if (t.Contains(" " + N(U.S(s[0])) + " ") || t.Contains(N(U.S(s[2])))) return U.L(s[1]);
        return 0;
    }

    public AssistantReponse Traiter(string demande)
    {
        var q = N(demande.Trim());
        var rep = new AssistantReponse();
        if (q == "" || q.Contains("aide") || q == "?")
        {
            rep.Texte = "Je peux vous aider à :\n • créer une mission (« confie à l'adjoint la préparation d'un bilan »)\n • résumer un rapport (« résume le rapport M-2026-00001 »)\n • préparer une synthèse (« synthèse de la situation »)\n • rechercher (« cherche escroqueries », « qui est Martin »)\n • proposer une répartition du travail (« répartition pour le service GEST »)\n • rédiger une note interne (« rédige une note sur les permanences »)\n • expliquer les événements récents (« explique les derniers événements »)\n • lister les retards (« missions en retard »)\n\nJe n'exécute aucune action sans votre validation et je ne m'appuie que sur les données réellement enregistrées.";
            return rep;
        }
        if (q.StartsWith("confie") || q.StartsWith("delegue") || q.Contains("demande a ") || q.StartsWith("charge "))
        {
            long dest = TrouverCollab(demande);
            var m = System.Text.RegularExpressions.Regex.Match(demande, @"(?:de |d'|la |le |l')(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string objet = demande;
            int idx = N(demande).IndexOf(" de ", StringComparison.Ordinal);
            if (idx > 0) objet = demande[(idx + 4)..]; else if (m.Success) objet = m.Groups[1].Value;
            objet = objet.Trim().TrimEnd('.'); if (objet.Length > 0) objet = char.ToUpper(objet[0]) + objet[1..];
            if (dest == 0) { rep.Texte = "Je n'ai pas identifié le destinataire. Précisez un nom, « l'adjoint », « le secrétaire général » ou un code de service."; return rep; }
            string nom = Db.Q(c => Biz.NameF(c, dest));
            rep.Texte = $"Proposition de mission :\n • Destinataire : {nom}\n • Objet : {objet}\n\nCliquez sur « {(rep.ActionLibelle = "Préparer la mission")} » pour ouvrir le formulaire pré-rempli. La mission ne sera créée qu'après votre validation.";
            rep.Action = () => { using var d = new MissionDialog(dest, 0, objet, "Mission préparée avec l'assistant à partir de l'instruction : « " + demande + " »"); d.ShowDialog(); };
            return rep;
        }
        var num = System.Text.RegularExpressions.Regex.Match(demande, @"M-\d{4}-[A-Z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (q.Contains("resum"))
        {
            var r = num.Success ? Db.Rows("SELECT numero, objet, rapport, statut FROM missions WHERE numero=@p0", num.Value.ToUpperInvariant())
                                : Db.Rows("SELECT numero, objet, rapport, statut FROM missions WHERE rapport IS NOT NULL AND emetteur_id=@p0 ORDER BY date_rapport DESC LIMIT 1", Auth.CollabId);
            if (r.Count == 0) { rep.Texte = "Aucun rapport correspondant n'a été reçu. Je ne peux pas résumer un rapport qui n'existe pas."; return rep; }
            if (string.IsNullOrEmpty(U.S(r[0][2]))) { rep.Texte = $"La mission {r[0][0]} est au statut « {r[0][3]} » : aucun rapport n'a encore été transmis."; return rep; }
            var lines = U.S(r[0][2]).Split('\n');
            var res = lines.Where(l => l.Trim().StartsWith("✓") || l.Trim().StartsWith("•") || l.Trim().StartsWith("-")).Take(10).Select(l => "  " + l.Trim());
            rep.Texte = $"Résumé du rapport {r[0][0]} — {r[0][1]} (statut : {r[0][3]}) :\n" + string.Join("\n", res);
            return rep;
        }
        if (q.Contains("synthese") || q.Contains("situation") || q.Contains("bilan"))
        {
            rep.Texte = Db.Q(c => Biz.Synthese(c));
            rep.ActionLibelle = "Enregistrer comme document";
            var txt = rep.Texte;
            rep.Action = () => { using var d = new DocumentDialog(0, "Synthèses", $"Synthèse de situation — {Engine.Now:dd/MM/yyyy HH:mm}", txt); d.ShowDialog(); };
            return rep;
        }
        if (q.Contains("retard"))
        {
            var r = Db.Rows($"SELECT numero, objet, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=destinataire_id), substr(echeance,1,16), ROUND(avancement) FROM missions WHERE statut IN {Biz.SqlActifs} AND echeance<@p0 ORDER BY echeance LIMIT 25", U.D(Engine.Now));
            rep.Texte = r.Count == 0 ? "Aucune mission en retard à ce jour." : $"{r.Count} mission(s) en retard :\n" + string.Join("\n", r.Select(x => $" • {x[0]} {x[1]} — {x[2]} — échéance {x[3]} — {x[4]} %"));
            return rep;
        }
        if (q.Contains("repartition") || q.Contains("repartir"))
        {
            long svc = 0;
            foreach (var s in Db.Rows("SELECT id, code, nom FROM services")) if (q.Contains(N(U.S(s[1]))) || q.Contains(N(U.S(s[2])))) svc = U.L(s[0]);
            var r = Db.Rows($"SELECT prenom||' '||nom, fonction, charge, dispo, (SELECT COUNT(*) FROM missions WHERE destinataire_id=c.id AND statut IN {Biz.SqlActifs}) FROM collaborateurs c WHERE actif=1 AND dispo<>'Absent' {(svc != 0 ? "AND service_id=" + svc : "")} ORDER BY charge ASC LIMIT 8");
            rep.Texte = "Proposition de répartition (collaborateurs les moins chargés" + (svc != 0 ? " du service" : "") + ") :\n" + string.Join("\n", r.Select(x => $" • {x[0]} — {x[1]} — charge {x[2]} % — {x[4]} mission(s) — {x[3]}"));
            return rep;
        }
        if (q.Contains("redige") || q.Contains("note"))
        {
            string sujet = demande; int i = N(demande).IndexOf(" sur ", StringComparison.Ordinal); if (i > 0) sujet = demande[(i + 5)..];
            string txt = $"NOTE INTERNE\n{U.Mention}\n\nDe : {Db.Q(c => Biz.NameF(c, Auth.CollabId))}\nÀ : l'ensemble des chefs de service\nDate : {U.Aff(U.D(Engine.Now))}\nObjet : {sujet}\n\n1. Contexte\n   [À compléter]\n\n2. Instructions\n   [À compléter]\n\n3. Calendrier\n   [À compléter]\n\nÉléments de contexte issus du système :\n{Db.Q(c => Biz.Synthese(c))}";
            rep.Texte = "Projet de note préparé. Il sera ouvert dans l'éditeur de documents pour relecture : rien n'est enregistré sans votre validation.";
            rep.ActionLibelle = "Ouvrir le projet de note";
            rep.Action = () => { using var d = new DocumentDialog(0, "Notes internes", "Note interne — " + sujet, txt); d.ShowDialog(); };
            return rep;
        }
        if (q.Contains("explique") || q.Contains("evenement") || q.Contains("recent"))
        {
            var r = Db.Rows("SELECT type, COUNT(*) FROM evenements WHERE date>=@p0 GROUP BY type ORDER BY COUNT(*) DESC", U.D(Engine.Now.AddHours(-24)));
            var last = Db.Rows("SELECT substr(date,12,5), description FROM evenements ORDER BY id DESC LIMIT 10");
            rep.Texte = "Sur les dernières 24 heures simulées :\n" + string.Join("\n", r.Select(x => $" • {x[0]} : {x[1]}")) + "\n\nDerniers événements :\n" + string.Join("\n", last.Select(x => $" {x[0]}  {x[1]}"));
            return rep;
        }
        if (q.StartsWith("qui est") || q.StartsWith("cherche") || q.StartsWith("recherche") || q.StartsWith("trouve"))
        {
            var terme = System.Text.RegularExpressions.Regex.Replace(demande, @"^(qui est|cherche|recherche|trouve)\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            var col = Db.Rows("SELECT c.prenom||' '||c.nom, c.fonction, s.nom, c.dispo FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.nom LIKE @p0 OR c.prenom LIKE @p0 OR c.fonction LIKE @p0 LIMIT 8", U.Like(terme));
            var dos = Db.Rows("SELECT numero, intitule, statut FROM dossiers WHERE intitule LIKE @p0 OR numero LIKE @p0 LIMIT 8", U.Like(terme));
            var mis = Db.Rows("SELECT numero, objet, statut FROM missions WHERE objet LIKE @p0 OR numero LIKE @p0 ORDER BY id DESC LIMIT 8", U.Like(terme));
            var sb = new StringBuilder($"Résultats pour « {terme} » :\n");
            if (col.Count > 0) sb.AppendLine("Collaborateurs :\n" + string.Join("\n", col.Select(x => $" • {x[0]} — {x[1]} — {x[2]} ({x[3]})")));
            if (dos.Count > 0) sb.AppendLine("Dossiers :\n" + string.Join("\n", dos.Select(x => $" • {x[0]} {x[1]} — {x[2]}")));
            if (mis.Count > 0) sb.AppendLine("Missions :\n" + string.Join("\n", mis.Select(x => $" • {x[0]} {x[1]} — {x[2]}")));
            if (col.Count + dos.Count + mis.Count == 0) sb.AppendLine("Aucun résultat dans les données autorisées.");
            rep.Texte = sb.ToString();
            return rep;
        }
        rep.Texte = "Je n'ai pas compris la demande. Tapez « aide » pour voir ce que je sais faire.";
        return rep;
    }
}

public class AssistantModule : Module
{
    public override string Titre => "Assistant";
    public override bool AutoRefresh => false;
    readonly IAssistantProvider provider = new AssistantLocal();
    readonly RichTextBox output = new() { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f) };
    readonly TextBox input = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f) };
    readonly Button bAction;
    Action pending;

    public AssistantModule()
    {
        bAction = Ui.PrimaryBtn("Exécuter", (s, e) => { var a = pending; pending = null; bAction.Visible = false; if (a != null) Ui.Try(a); }, 220);
        bAction.Visible = false;
        var send = Ui.PrimaryBtn("Envoyer", (s, e) => Send(), 100);
        var p = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 44, ColumnCount = 3, Padding = new Padding(6) };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        p.Controls.Add(input, 0, 0); p.Controls.Add(send, 1, 0); p.Controls.Add(bAction, 2, 0);
        input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Send(); e.SuppressKeyPress = true; } };
        var quick = Ui.Bar(new Label { Text = "Fournisseur : " + provider.Nom + "   ", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(4, 8, 0, 0) },
            Ui.Btn("Synthèse", (s, e) => Ask("synthèse de la situation")), Ui.Btn("Retards", (s, e) => Ask("missions en retard")), Ui.Btn("Événements récents", (s, e) => Ask("explique les derniers événements")),
            Ui.Btn("Résumer le dernier rapport", (s, e) => Ask("résume le dernier rapport")), Ui.Btn("Aide", (s, e) => Ask("aide")));
        Controls.Add(Card(output, "Assistant de cabinet — l'assistant n'invente aucune donnée et ne réalise aucune action sans validation")); Controls.Add(p); Controls.Add(quick); Controls.Add(Ui.Title("Assistant"));
        Write("Assistant", provider.Traiter("aide").Texte);
    }

    void Ask(string q) { input.Text = q; Send(); }

    void Send()
    {
        var q = input.Text.Trim(); if (q == "") return;
        input.Clear();
        Write("Vous", q);
        try
        {
            var r = provider.Traiter(q);
            Write("Assistant", r.Texte);
            pending = r.Action; bAction.Visible = r.Action != null; bAction.Text = r.ActionLibelle ?? "Exécuter";
        }
        catch (Exception e) { Log.Error(e); Write("Assistant", "Erreur : " + e.Message); }
    }

    void Write(string who, string text)
    {
        output.SelectionStart = output.TextLength;
        output.SelectionFont = Theme.FB; output.SelectionColor = who == "Vous" ? Theme.Accent : Theme.Navy;
        output.AppendText($"{who} — {Engine.Now:HH:mm}\n");
        output.SelectionFont = new Font("Segoe UI", 10f); output.SelectionColor = Color.Black;
        output.AppendText(text + "\n\n");
        output.ScrollToCaret();
    }
}
