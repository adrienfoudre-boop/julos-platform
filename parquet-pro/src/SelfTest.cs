using System.Reflection;
using System.Text;

namespace ParquetPro;

/// Autotest interne (lancé avec --selftest) : ouvre toutes les fenêtres et vérifie qu'aucune erreur n'est journalisée.
public static class SelfTest
{
    static readonly StringBuilder rep = new();
    static int ok, ko;

    static void Step(string name, Action a)
    {
        long before = Db.Long("SELECT IFNULL(MAX(id),0) FROM journal_tech");
        try { a(); Application.DoEvents(); }
        catch (Exception e) { ko++; rep.AppendLine($"[ÉCHEC] {name} : {e.GetType().Name} {e.Message}\n{e.StackTrace}"); return; }
        var errs = Db.Rows("SELECT message FROM journal_tech WHERE id>@p0 AND niveau='ERREUR'", before);
        if (errs.Count > 0) { ko++; rep.AppendLine($"[ÉCHEC] {name} : erreur journalisée\n{string.Join("\n", errs.Select(r => U.S(r[0])))}"); }
        else { ok++; rep.AppendLine($"[OK] {name}"); }
    }

    static void Walk(Control c)
    {
        foreach (Control x in c.Controls)
        {
            if (x is TabControl tc) for (int i = 0; i < tc.TabPages.Count; i++) { tc.SelectedIndex = i; Application.DoEvents(); foreach (Control y in tc.TabPages[i].Controls) Reload(y); }
            Walk(x);
        }
    }
    static void Reload(Control c) { if (c is GridPanel g) g.Reload(); else if (c is PiecesPane p) p.Grid.Reload(); foreach (Control x in c.Controls) Reload(x); }

    public static void Run()
    {
        Ui.Silent = true;
        rep.AppendLine($"AUTOTEST PARQUET PRO — {DateTime.Now}");
        Auth.Connect("procureur", "parquet");
        var adv = typeof(Engine).GetMethod("Advance", BindingFlags.NonPublic | BindingFlags.Static);
        Step("Moteur : 3 jours simulés", () => { for (int i = 0; i < 3 * 24 * 4; i++) adv.Invoke(null, new object[] { 15.0 }); });
        var main = new MainForm(null) { WindowState = FormWindowState.Normal, Width = 1500, Height = 950 };
        main.Show(); Application.DoEvents();
        foreach (var d in MainForm.Defs)
            Step("Module " + d.Name, () => { var m = main.ShowModule(d.Name); if (m == null) throw new Exception("module non ouvert"); Application.DoEvents(); Walk(m); m.RefreshData(); if (m is MessagerieModule mm) rep.AppendLine($"     dossiers de messagerie avec messages : {mm.TestFolders()}"); });
        Step("Rafraîchissement global", () => main.RefreshAll(true));
        var dossiers = Db.Rows("SELECT id FROM dossiers WHERE id IN (SELECT MIN(id) FROM dossiers GROUP BY situation) LIMIT 12").Select(r => U.L(r[0])).ToList();
        dossiers.AddRange(Db.Rows("SELECT id FROM dossiers ORDER BY id DESC LIMIT 3").Select(r => U.L(r[0])));
        foreach (var dos in dossiers)
            Step($"Fenêtre dossier {Db.Str("SELECT numero||' ('||situation||')' FROM dossiers WHERE id=@p0", dos)} — 13 sections", () => { DossierWindow.Open(dos); var w = Application.OpenForms.OfType<DossierWindow>().Last(); w.TestAll(); w.Close(); });
        foreach (var pid in Db.Rows("SELECT id FROM personnes WHERE id IN (SELECT personne_id FROM detentions) OR id IN (SELECT personne_id FROM dossier_personnes WHERE role='Mis en cause') LIMIT 6").Select(r => U.L(r[0])))
            Step($"Fiche personne {pid} — tous les onglets", () => { var f = new PersonneWindow(pid, null); f.Show(); Application.DoEvents(); Walk(f); f.Close(); });
        var att = Db.Long("SELECT id FROM dossiers WHERE situation='En attente de décision' AND gravite>=2 AND EXISTS(SELECT 1 FROM pieces WHERE dossier_id=dossiers.id AND type='Interrogatoire') AND EXISTS(SELECT 1 FROM dossier_personnes WHERE dossier_id=dossiers.id AND role='Mis en cause') LIMIT 1");
        long mec = Db.Long("SELECT personne_id FROM pieces WHERE dossier_id=@p0 AND type='Interrogatoire' LIMIT 1", att);
        foreach (var t in Jud.Types)
            Step($"Parcours de décision « {t.Libelle} » — 7 étapes", () => { var w = new DecisionWizard(att, null, 0, 0); w.Show(); w.TestWalk(t.Code, t.AvecPersonne ? mec : 0); w.Close(); });
        Step("Enregistrement : orientation (comparution immédiate) + détention provisoire", () =>
        {
            Db.Exec("UPDATE pieces SET lue=1 WHERE dossier_id=@p0", att);
            Db.Tx(c => Jud.Enregistrer(c, "ORIENTATION", "Comparution immédiate (simulation)", att, 0, "ctx", "verif", new string('x', 200)));
            Db.Tx(c => Jud.Enregistrer(c, "PERSONNE", "Réquisitions de placement en détention provisoire (simulation)", att, mec, "ctx", "verif", new string('x', 300)));
        });
        Step("Moteur : effets des décisions (2 jours)", () => { for (int i = 0; i < 2 * 24 * 4; i++) adv.Invoke(null, new object[] { 15.0 }); });
        rep.AppendLine("     décisions utilisateur : " + string.Join(" | ", Db.Rows("SELECT ref, libelle, etat_execution, substr(IFNULL(resultat,''),1,80) FROM decisions WHERE source='Utilisateur'").Select(r => string.Join(", ", r))));
        foreach (var did in Db.Rows("SELECT id FROM decisions ORDER BY id DESC LIMIT 3").Select(r => U.L(r[0])))
            Step($"Consultation décision {did}", () => { var v = new DecisionView(did); v.Show(); Application.DoEvents(); v.Close(); });
        Step("Lecteur de pièces", () => { foreach (var p in Db.Rows("SELECT id FROM pieces ORDER BY RANDOM() LIMIT 5")) { var f = new PieceViewer(U.L(p[0])); f.Show(); Application.DoEvents(); f.Close(); } });
        string pdf = Path.Combine(Db.ExportDir, "autotest_dossier.pdf");
        Step("PDF du dossier complet", () => { DossierPdf.Generer(att, pdf, DossierPdf.Sections.ToHashSet(), null); if (new FileInfo(pdf).Length < 2000) throw new Exception("PDF trop petit"); rep.AppendLine($"     {pdf} ({new FileInfo(pdf).Length / 1024} Ko)"); });
        Step("PDF sélection de pièces", () => { var sel = Db.Rows("SELECT id FROM pieces WHERE dossier_id=@p0 LIMIT 2", att).Select(r => U.L(r[0])).ToHashSet(); DossierPdf.Generer(att, pdf.Replace(".pdf", "_sel.pdf"), new HashSet<string> { "Page de présentation", "Sommaire", "Procès-verbaux", "Comptes rendus d'audition" }, sel); });
        Step("Recherche centrale (nom, immatriculation, dossier)", () =>
        {
            var r = main.Get<RechercheModule>("Fichiers de recherche");
            r.Lancer(Db.Str("SELECT nom FROM personnes LIMIT 1")); r.Lancer(Db.Str("SELECT immatriculation FROM vehicules LIMIT 1") ?? "AB-123"); r.Lancer(Db.Str("SELECT numero FROM dossiers LIMIT 1"));
        });
        Step("Vue mission / fiche collaborateur / message", () =>
        {
            var m = new MissionView(Db.Long("SELECT MAX(id) FROM missions")); m.Show(); Application.DoEvents(); m.Close();
            var f = new FicheCollab(2); f.Show(); Application.DoEvents(); Walk(f); f.Close();
            MessageViewer.Open(main, Db.Long("SELECT MAX(id) FROM messages")); Application.DoEvents();
        });
        Step("Cohérence : pas de doublon de personne", () => { long d = Db.Long("SELECT COUNT(*) FROM (SELECT nom, prenom, date_naissance FROM personnes GROUP BY 1,2,3 HAVING COUNT(*)>1)"); if (d > 0) throw new Exception(d + " doublon(s)"); });
        Step("Cohérence : casier ↔ décisions", () => { long d = Db.Long("SELECT COUNT(*) FROM casier WHERE source='Décision PARQUET PRO' AND decision_id NOT IN (SELECT id FROM decisions)"); if (d > 0) throw new Exception(d + " mention(s) orpheline(s)"); });
        Step("Cohérence : statut des personnes détenues", () => { long d = Db.Long("SELECT COUNT(*) FROM personnes p WHERE EXISTS(SELECT 1 FROM detentions WHERE personne_id=p.id AND date_sortie IS NULL) AND p.statut NOT LIKE 'Détenu%'"); if (d > 0) throw new Exception(d + " statut(s) incohérent(s)"); });
        rep.AppendLine($"\nRÉSULTAT : {ok} réussi(s), {ko} échec(s).");
        rep.AppendLine("Données : " + string.Join(" | ", new[] { "personnes", "pieces", "decisions", "casier", "detentions", "vehicules", "messages", "dossiers" }.Select(t => $"{t}={Db.Long($"SELECT COUNT(*) FROM {t}")}")));
        File.WriteAllText(Path.Combine(Db.Dir, "selftest.txt"), rep.ToString());
        main.Detach();
        Environment.Exit(ko == 0 ? 0 : 1);
    }
}
