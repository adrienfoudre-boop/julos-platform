using System.Text;

namespace ParquetPro;

public static class Engine
{
    static DateTime now = new(2026, 1, 5, 8, 0, 0);
    static readonly object nowLock = new();
    static readonly object advLock = new();
    public static DateTime Now { get { lock (nowLock) return now; } }
    public static void SetNow(DateTime d) { lock (nowLock) now = d; }
    public static Random Rnd => Random.Shared;

    public static volatile bool Running = true;
    public static volatile int Speed = 60;
    public static long Version;
    public static volatile string Etat = "Arrêté";
    public static volatile int EvtsDerniereMinute;
    static System.Threading.Timer timer;
    static DateTime lastReal = DateTime.Now, lastBackup = DateTime.Now;
    static int lastHour = -1;
    static double freq = 1.0;
    static int backupMin = 30;

    public static readonly int[] Vitesses = { 1, 2, 5, 10, 30, 60, 300, 900 };
    public static string VitesseLabel(int v) => v switch
    {
        1 => "x1 — temps réel", 60 => "x60 — 1 s = 1 min", 300 => "x300 — avance rapide", 900 => "x900 — avance très rapide", _ => "x" + v + " — accéléré"
    };

    public static double ProfilMult(string p) => p switch
    {
        "Très réactif" => 1.35, "Méthodique" => 0.9, "Très organisé" => 1.15, "Sollicite fréquemment son responsable" => 0.95, _ => 1.0
    };

    // ------------------------------------------------------------------ contenus de référence
    public static readonly string[] ObjetsInternes = { "Mise à jour du tableau de suivi des échéances", "Préparation d'une note de synthèse hebdomadaire", "Vérification de la complétude des dossiers en attente", "Relance des partenaires sur les pièces manquantes",
        "Préparation du calendrier des audiences du mois (simulation)", "Contrôle qualité des fiches de dossiers", "Mise à jour des statistiques du service", "Classement et archivage des pièces traitées", "Préparation d'un point de situation pour la direction",
        "Analyse de la charge de travail de l'équipe", "Rédaction d'une procédure interne", "Organisation d'une réunion de coordination", "Recensement des difficultés rencontrées", "Revue des dossiers prioritaires", "Préparation des éléments de bilan mensuel" };
    public static readonly string[] SousTachesModeles = { "Recenser les éléments disponibles", "Analyser la situation", "Consulter les services concernés", "Rédiger un projet de note", "Vérifier la cohérence des données",
        "Préparer les annexes", "Relire et finaliser", "Mettre à jour le tableau de suivi", "Collecter les indicateurs", "Identifier les difficultés", "Formuler des propositions", "Transmettre les éléments au responsable" };
    public static readonly string[] Volets = { "état des lieux", "analyse des effectifs", "recensement des difficultés", "volet statistique", "propositions d'amélioration", "volet organisationnel", "calendrier et échéances", "coordination partenariale" };
    public static readonly string[] Decisions = { "Point validé, mise en œuvre confiée au service concerné.", "Approfondissement demandé pour la prochaine réunion.", "Arbitrage rendu, diffusion d'une note interne.", "Calendrier arrêté, échéance fixée.", "Sujet reporté faute d'éléments suffisants.", "Groupe de travail constitué." };
    public static readonly string[] ReponsesTel = { "point de situation effectué, éléments transmis par écrit.", "accord sur la marche à suivre.", "demande d'informations complémentaires, rappel convenu.", "échange constructif, réunion à prévoir.", "information prise en compte." };
    public static readonly string[] Difficultes = { "Pièces manquantes en provenance d'un partenaire simulé", "Charge de travail élevée dans l'équipe", "Délai de réponse d'un service tiers", "Données statistiques incomplètes", "Besoin d'un arbitrage sur la priorité", "Indisponibilité d'un collaborateur clé", "Outil informatique momentanément indisponible (simulation)" };
    public static readonly string[] Propositions = { "Mettre en place un point hebdomadaire de suivi.", "Désigner un référent unique pour les échanges partenaires.", "Harmoniser les modèles de comptes rendus.", "Renforcer temporairement l'équipe concernée.", "Automatiser la mise à jour du tableau de bord.", "Prévoir une réunion de coordination interservices." };
    public static readonly string[] MotifsAppel = { "Point de situation sur un dossier", "Demande de rendez-vous", "Transmission d'une information", "Organisation d'une réunion partenariale", "Question sur un calendrier", "Demande d'arbitrage", "Signalement d'un événement (simulation)" };
    public static readonly string[] InfosPartenaires = { "Transmission d'un compte rendu d'activité", "Information sur l'évolution d'un dossier", "Proposition de réunion de coordination", "Demande de point de situation", "Communication d'un calendrier", "Information sur un événement local (simulation)" };
    public static readonly string[] TitresReunions = { "Point de coordination", "Réunion d'équipe élargie", "Revue des dossiers prioritaires", "Point d'étape sur les délégations", "Réunion de pilotage", "Réunion interservices", "Point organisation et effectifs" };
    static readonly Dictionary<string, string> CatService = new() { ["Atteintes aux biens"] = "GEST", ["Atteintes aux personnes"] = "ENQ", ["Économique et financier"] = "EDC", ["Circulation routière"] = "GEST", ["Stupéfiants"] = "ENQ", ["Environnement"] = "PCT", ["Mineurs"] = "EMF", ["Exécution des peines"] = "COORD", ["Parquet civil"] = "GEST", ["Coopération"] = "UTC" };

    // ------------------------------------------------------------------ cycle de vie
    public static void Load()
    {
        using var c = new DbCtx();
        SetNow(U.P(c.Get("sim_time")));
        Speed = int.TryParse(c.Get("vitesse", "60"), out var v) ? v : 60;
        Running = c.Get("moteur_actif", "1") == "1" && c.Get("reprise", "auto") != "pause";
        double.TryParse(c.Get("frequence", "1.0"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out freq);
        if (freq <= 0) freq = 1;
        backupMin = int.TryParse(c.Get("backup_min", "30"), out var b) ? Math.Max(5, b) : 30;
    }

    public static void ReloadSettings()
    {
        double.TryParse(Db.Get("frequence", "1.0"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out freq);
        if (freq <= 0) freq = 1;
        backupMin = int.TryParse(Db.Get("backup_min", "30"), out var b) ? Math.Max(5, b) : 30;
    }

    public static void Start()
    {
        string last = Db.Get("derniere_fermeture", "");
        Db.Tx(c =>
        {
            string txt = string.IsNullOrEmpty(last)
                ? $"Démarrage du moteur au temps simulé {U.Aff(U.D(Now))}."
                : $"Reprise du moteur. Application arrêtée du {U.Aff(last)} au {U.Aff(U.D(DateTime.Now))} (temps réel) : aucun événement n'a été simulé pendant l'arrêt. Reprise au temps simulé {U.Aff(U.D(Now))}.";
            Biz.Event(c, "Moteur", "Moteur de simulation", 0, txt, "moteur", 0);
            Hourly(c, Now);
        });
        Log.Info("Moteur démarré. " + (Running ? "En marche" : "En pause"));
        lastReal = DateTime.Now;
        Etat = Running ? "En marche" : "En pause";
        timer = new System.Threading.Timer(Tick, null, 1000, 1000);
    }

    public static void Stop()
    {
        timer?.Dispose(); timer = null;
        lock (advLock)
        {
            try { Db.Tx(c => { c.Set("sim_time", U.D(Now)); c.Set("derniere_fermeture", U.D(DateTime.Now)); c.Set("vitesse", Speed); }); } catch (Exception e) { Log.Error(e); }
        }
        Etat = "Arrêté";
    }

    public static void SetRunning(bool r)
    {
        Running = r; Etat = r ? "En marche" : "En pause";
        Db.Set("moteur_actif", r ? 1 : 0);
        lastReal = DateTime.Now;
    }
    public static void SetSpeed(int s) { Speed = s; Db.Set("vitesse", s); }

    static void Tick(object _)
    {
        if (!Monitor.TryEnter(advLock)) return;
        try
        {
            var real = DateTime.Now;
            double sec = Math.Min(5, Math.Max(0, (real - lastReal).TotalSeconds));
            lastReal = real;
            if (Running && Speed > 0) Advance(sec * Speed / 60.0);
            if ((real - lastBackup).TotalMinutes >= backupMin)
            {
                lastBackup = real;
                try { Db.Backup("auto"); } catch (Exception e) { Log.Error(e); }
            }
        }
        catch (Exception e) { Etat = "Erreur — voir journal"; Log.Error(e); }
        finally { Monitor.Exit(advLock); }
    }

    /// Avance le temps simulé (en minutes) ; toutes les écritures sont transactionnelles.
    static void Advance(double simMin)
    {
        if (simMin <= 0) return;
        var before = Now;
        try
        {
            using var c = new DbCtx(true);
            double remaining = simMin;
            long evtBefore = c.Long("SELECT IFNULL(MAX(id),0) FROM evenements");
            while (remaining > 1e-9)
            {
                double step = Math.Min(15, remaining);
                var t = Now.AddSeconds(step * 60);
                SetNow(t);
                Step(c, t, step);
                remaining -= step;
            }
            c.Set("sim_time", U.D(Now));
            EvtsDerniereMinute = (int)(c.Long("SELECT IFNULL(MAX(id),0) FROM evenements") - evtBefore);
            c.Commit();
            Interlocked.Increment(ref Version);
            Etat = Running ? "En marche" : "En pause";
        }
        catch
        {
            SetNow(before);
            throw;
        }
    }

    public static void AvancerJusquA(DateTime cible, Action done)
    {
        Task.Run(() =>
        {
            lock (advLock)
            {
                try
                {
                    Etat = "Avance rapide…";
                    while (Now < cible)
                    {
                        double m = Math.Min(240, (cible - Now).TotalMinutes);
                        Advance(m);
                    }
                    lastReal = DateTime.Now;
                }
                catch (Exception e) { Log.Error(e); }
                finally { Etat = Running ? "En marche" : "En pause"; }
            }
            done?.Invoke();
        });
    }

    public static DateTime ProchaineJournee()
    {
        var d = Now.Date.AddDays(1);
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        return d.AddHours(8);
    }

    static bool Ouvre(DateTime t) => t.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && t.Hour >= 8 && t.Hour < 19;
    static double P(double lambdaParHeure, double dtMin) => 1 - Math.Exp(-lambdaParHeure * freq * dtMin / 60.0);
    static long Me => Auth.CollabId == 0 ? 1 : Auth.CollabId;

    // ------------------------------------------------------------------ pas de simulation
    static void Step(DbCtx c, DateTime t, double dt)
    {
        DayChange(c, t);
        if (t.Hour != lastHour) { lastHour = t.Hour; Hourly(c, t); }
        Meetings(c, t);
        bool work = Ouvre(t);
        if (work)
        {
            Missions(c, t, dt);
            Replies(c, t);
            RandomEvents(c, t, dt);
        }
        Scheduled(c, t);
        Delays(c, t);
    }

    static void DayChange(DbCtx c, DateTime t)
    {
        string d = t.ToString("yyyy-MM-dd");
        if (c.Get("jour_courant", "") == d) return;
        c.Set("jour_courant", d);
        int n = c.Exec("UPDATE collaborateurs SET dispo='Hors horaires' WHERE dispo='Absent'");
        Biz.Event(c, "Nouvelle journée", "Moteur de simulation", 0, $"Début de la journée du {U.AffJ(t)}" + (n > 0 ? $" — {n} retour(s) d'absence." : ""), "moteur", 0);
    }

    static void Hourly(DbCtx c, DateTime t)
    {
        c.Exec($@"UPDATE collaborateurs SET charge = MIN(100, charge_base
            + 16 * (SELECT COUNT(*) FROM missions m WHERE m.destinataire_id=collaborateurs.id AND m.statut IN {Biz.SqlActifs})
            + 6 * (SELECT COUNT(*) FROM sous_taches st JOIN missions m ON m.id=st.mission_id WHERE st.assigne_id=collaborateurs.id AND st.statut<>'Terminée' AND m.statut IN {Biz.SqlActifs})
            + 4 * (SELECT COUNT(*) FROM dossiers d WHERE d.referent_id=collaborateurs.id AND d.statut<>'Clôturé' AND d.archive=0))");
        bool we = t.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        c.Exec("UPDATE collaborateurs SET dispo = CASE WHEN dispo IN ('Absent','En réunion') THEN dispo WHEN @p1=1 OR @p0<horaire_debut OR @p0>=horaire_fin THEN 'Hors horaires' ELSE 'Disponible' END", t.Hour, we ? 1 : 0);
    }

    // ------------------------------------------------------------------ missions
    static void Missions(DbCtx c, DateTime t, double dt)
    {
        string T = U.D(t);
        // 1. accusé de réception
        foreach (var r in c.Rows("SELECT id, emetteur_id, destinataire_id, numero, objet FROM missions WHERE statut='Nouvelle' AND date_creation<=@p0 AND destinataire_id NOT IN (SELECT id FROM collaborateurs WHERE dispo='Absent') LIMIT 25", U.D(t.AddMinutes(-10))))
        {
            long id = U.L(r[0]), em = U.L(r[1]), de = U.L(r[2]);
            c.Exec("UPDATE missions SET statut='En analyse' WHERE id=@p0 AND statut='Nouvelle'", id);
            Biz.MHist(c, id, de, "Analyse", $"{Biz.Name(c, de)} accuse réception et analyse les instructions.");
            if (em == Me) Biz.Message(c, de, em, $"Accusé de réception — {r[3]}", $"Bien reçu. J'analyse la mission « {r[4]} » et vous tiens informé de son organisation.", id, categorie: "Mission");
        }
        // 2. organisation
        foreach (var r in c.Rows("SELECT id FROM missions WHERE statut='En analyse' AND datetime(date_creation, '+'||analyse_min||' minutes')<=@p0 AND destinataire_id NOT IN (SELECT id FROM collaborateurs WHERE dispo='Absent') LIMIT 15", T))
            Organiser(c, U.L(r[0]), t);
        // 3. progression
        string actifsSansEnfants = "SELECT 1 FROM missions ch WHERE ch.parent_id=sous_taches.mission_id AND ch.statut IN ('Nouvelle','En analyse','En cours','À modifier','Rapport transmis')";
        c.Exec($@"UPDATE sous_taches SET avancement = MIN(100, avancement + @p0 * (SELECT m.vitesse * CASE m.priorite WHEN 'Haute' THEN 1.25 WHEN 'Basse' THEN 0.85 ELSE 1 END FROM missions m WHERE m.id=sous_taches.mission_id) * (0.6 + ABS(RANDOM() % 80) / 100.0)),
              statut = 'En cours'
            WHERE statut<>'Terminée' AND mission_id IN (SELECT id FROM missions WHERE statut IN ('En cours','À modifier'))
              AND IFNULL(assigne_id,0) NOT IN (SELECT id FROM collaborateurs WHERE dispo IN ('Absent','En réunion'))
              AND NOT EXISTS({actifsSansEnfants})
              AND id = (SELECT MIN(s2.id) FROM sous_taches s2 WHERE s2.mission_id=sous_taches.mission_id AND s2.statut<>'Terminée' AND IFNULL(s2.assigne_id,0)=IFNULL(sous_taches.assigne_id,0))", dt);
        c.Exec("UPDATE sous_taches SET statut='Terminée', avancement=100, date_fin=@p0 WHERE avancement>=100 AND statut<>'Terminée'", T);
        c.Exec(@"UPDATE missions SET avancement = MIN(100,
              ((SELECT IFNULL(SUM(CASE WHEN ch.statut IN ('Validée','Clôturée') THEN 100 WHEN ch.statut='Rapport transmis' THEN 99 ELSE ch.avancement END),0) FROM missions ch WHERE ch.parent_id=missions.id AND ch.statut<>'Refusée')
             + (SELECT IFNULL(SUM(avancement),0) FROM sous_taches WHERE mission_id=missions.id))
             / MAX(1, (SELECT COUNT(*) FROM missions ch WHERE ch.parent_id=missions.id AND ch.statut<>'Refusée') + (SELECT COUNT(*) FROM sous_taches WHERE mission_id=missions.id)))
            WHERE statut IN ('En cours','À modifier')");
        // 4. achèvement
        foreach (var r in c.Rows(@"SELECT id FROM missions m WHERE statut IN ('En cours','À modifier')
              AND NOT EXISTS(SELECT 1 FROM sous_taches WHERE mission_id=m.id AND statut<>'Terminée')
              AND NOT EXISTS(SELECT 1 FROM missions ch WHERE ch.parent_id=m.id AND ch.statut IN ('Nouvelle','En analyse','En cours','À modifier','Rapport transmis'))
              AND (EXISTS(SELECT 1 FROM sous_taches WHERE mission_id=m.id) OR EXISTS(SELECT 1 FROM missions ch WHERE ch.parent_id=m.id)) LIMIT 20"))
            Achever(c, U.L(r[0]), t);
        // 5. validation hiérarchique automatique des rapports intermédiaires
        foreach (var r in c.Rows("SELECT id, emetteur_id, destinataire_id, numero FROM missions WHERE statut='Rapport transmis' AND emetteur_id NOT IN (1, @p1) AND date_rapport<=@p0 LIMIT 25", U.D(t.AddMinutes(-25)), Me))
        {
            long id = U.L(r[0]), em = U.L(r[1]), de = U.L(r[2]);
            c.Exec("UPDATE missions SET statut='Validée', date_cloture=@p1 WHERE id=@p0 AND statut='Rapport transmis'", id, T);
            Biz.MHist(c, id, em, "Validation", $"Rapport validé par {Biz.Name(c, em)}.");
            Biz.Message(c, em, de, $"Rapport validé — {r[3]}", "Merci pour votre rapport, il est validé et intégré à la consolidation.", id, categorie: "Mission");
            Biz.Event(c, "Mission validée", Biz.Name(c, em), em, $"Validation du rapport {r[3]} de {Biz.Name(c, de)}", "mission", id);
        }
    }

    static void Organiser(DbCtx c, long id, DateTime t)
    {
        var m = c.Rows("SELECT emetteur_id, destinataire_id, profondeur, objet, priorite, echeance, instructions, confidentialite, service_id, dossier_id, numero, description FROM missions WHERE id=@p0", id)[0];
        long em = U.L(m[0]), de = U.L(m[1]); int depth = (int)U.L(m[2]); string objet = U.S(m[3]), prio = U.S(m[4]); DateTime ech = U.P(m[5]);
        long svcCible = U.L(m[8]), dos = U.L(m[9]);
        int autonomie = (int)c.Long("SELECT autonomie FROM collaborateurs WHERE id=@p0", de);
        int niveauDe = (int)c.Long("SELECT niveau FROM collaborateurs WHERE id=@p0", de);
        var subs = c.Rows(@"SELECT c.id, c.charge, (SELECT COUNT(*) FROM collaborateurs x WHERE x.superieur_id=c.id) nb, c.service_id FROM collaborateurs c
            WHERE c.superieur_id=@p0 AND c.actif=1 AND c.dispo<>'Absent' ORDER BY nb DESC, c.charge ASC", de);
        var noms = new List<string>();
        bool cascade = autonomie >= 2 && subs.Count > 0 && depth < 2 && subs.Any(s => U.L(s[2]) > 0) && (em == Me || depth == 0 || Rnd.NextDouble() < 0.4);
        if (cascade)
        {
            var managers = subs.Where(s => U.L(s[2]) > 0).ToList();
            if (niveauDe <= 2 && svcCible != 0)
            {
                var pref = managers.Where(s => U.L(s[3]) == svcCible).ToList();
                if (pref.Count > 0) managers = pref.Concat(managers.Except(pref)).ToList();
            }
            int k = Math.Min(managers.Count, depth == 0 ? Rnd.Next(2, 4) : Rnd.Next(1, 3));
            var volets = Volets.OrderBy(_ => Rnd.Next()).ToList();
            for (int i = 0; i < k; i++)
            {
                long sub = U.L(managers[i][0]);
                var e2 = ech.AddHours(-Rnd.Next(3, 10));
                if (e2 <= t.AddHours(2)) e2 = ech > t.AddHours(3) ? ech.AddHours(-1) : t.AddHours(6);
                try
                {
                    Biz.CreerMission(c, de, sub, $"{objet} — {volets[i % volets.Count]}", $"Contribution demandée dans le cadre de la mission {m[10]}.\n{m[11]}", prio, e2,
                        $"Me transmettre votre contribution avant le {U.Aff(U.D(e2))}. " + U.S(m[6]), U.S(m[7]), 0, dos, id, null, "Délégation en cascade");
                    noms.Add(Biz.Name(c, sub));
                }
                catch (InvalidOperationException) { }
                catch (UnauthorizedAccessException) { }
            }
            c.Exec("INSERT INTO sous_taches(mission_id,libelle,assigne_id) VALUES(@p0,'Consolider les contributions et rédiger la synthèse',@p1)", id, de);
            Biz.MHist(c, id, de, "Organisation", "Répartition de la mission entre : " + string.Join(", ", noms));
            if (noms.Count > 0)
            {
                string txt = $"J'ai réparti la mission « {objet} » entre {string.Join(", ", noms)}. Je consoliderai leurs contributions et vous transmettrai la synthèse.";
                if (em == Me) Biz.Message(c, de, em, $"Organisation de la mission {m[10]}", txt, id, categorie: "Mission");
                else c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,mission_id,categorie) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Mission')", U.D(t), de, em, $"Organisation de la mission {m[10]}", txt, id);
            }
        }
        else
        {
            int n = Rnd.Next(3, 5);
            var modeles = SousTachesModeles.OrderBy(_ => Rnd.Next()).Take(n).ToList();
            var executants = autonomie >= 2 && subs.Count > 0 ? subs.OrderBy(s => U.L(s[1])).Select(s => U.L(s[0])).Take(3).ToList() : new List<long>();
            for (int i = 0; i < n; i++)
            {
                long a = executants.Count > 0 && i < n - 1 ? executants[i % executants.Count] : de;
                c.Exec("INSERT INTO sous_taches(mission_id,libelle,assigne_id) VALUES(@p0,@p1,@p2)", id, modeles[i], a);
                if (a != de) noms.Add(Biz.Name(c, a));
            }
            Biz.MHist(c, id, de, "Organisation", $"Mission décomposée en {n} sous-tâches" + (noms.Count > 0 ? " (contributeurs : " + string.Join(", ", noms.Distinct()) + ")" : "") + ".");
        }
        c.Exec("UPDATE missions SET statut='En cours' WHERE id=@p0 AND statut='En analyse'", id);
        Biz.Event(c, "Mission organisée", Biz.Name(c, de), de, $"Organisation de la mission {m[10]}" + (cascade ? " (délégation en cascade)" : ""), "mission", id);
    }

    public static string Rapport(DbCtx c, long id, DateTime t)
    {
        var m = c.Rows("SELECT numero, objet, emetteur_id, destinataire_id, date_creation, echeance, racine_id FROM missions WHERE id=@p0", id)[0];
        var sb = new StringBuilder();
        bool retard = U.P(m[5]) < t;
        sb.AppendLine($"COMPTE RENDU DE MISSION {m[0]}"); sb.AppendLine(U.Mention); sb.AppendLine();
        sb.AppendLine($"Objet : {m[1]}\nDemandeur : {Biz.NameF(c, U.L(m[2]))}\nResponsable : {Biz.NameF(c, U.L(m[3]))}\nCréée le : {U.Aff(m[4])} — Transmise le : {U.Aff(U.D(t))}\nÉchéance : {U.Aff(m[5])} ({(retard ? "dépassée" : "respectée")})\n");
        sb.AppendLine("1. Résultats obtenus");
        foreach (var s in c.Rows("SELECT libelle, assigne_id FROM sous_taches WHERE mission_id=@p0 AND statut='Terminée'", id)) sb.AppendLine($"   ✓ {s[0]} — {Biz.Name(c, U.L(s[1]))}");
        var enfants = c.Rows("SELECT numero, objet, destinataire_id, statut, rapport FROM missions WHERE parent_id=@p0", id);
        if (enfants.Count > 0)
        {
            sb.AppendLine("\n2. Contributions consolidées");
            foreach (var e in enfants)
            {
                var resume = U.S(e[4]).Split('\n').Where(l => l.Trim().StartsWith("✓")).Take(3).Select(l => l.Trim());
                sb.AppendLine($"   • {e[0]} — {e[1]} ({Biz.Name(c, U.L(e[2]))}) : {e[3]}");
                foreach (var l in resume) sb.AppendLine("       " + l);
            }
        }
        sb.AppendLine("\n3. Difficultés rencontrées");
        var diffs = c.Rows("SELECT h.texte FROM mission_hist h JOIN missions x ON x.id=h.mission_id WHERE (x.id=@p0 OR x.parent_id=@p0) AND h.type IN ('Difficulté','Retard')", id);
        if (diffs.Count == 0) sb.AppendLine("   Aucune difficulté majeure signalée.");
        foreach (var d in diffs.Take(6)) sb.AppendLine("   - " + d[0]);
        sb.AppendLine("\n4. Tâches restantes");
        var rest = c.Rows("SELECT libelle FROM sous_taches WHERE mission_id=@p0 AND statut<>'Terminée'", id);
        sb.AppendLine(rest.Count == 0 ? "   Aucune." : string.Join("\n", rest.Select(x => "   - " + x[0])));
        sb.AppendLine("\n5. Propositions");
        sb.AppendLine("   - " + Rnd.Pick(Propositions));
        sb.AppendLine("\n6. Documents produits");
        var docs = c.Rows("SELECT numero, titre FROM documents WHERE mission_id IN (SELECT id FROM missions WHERE id=@p0 OR parent_id=@p0)", id);
        sb.AppendLine(docs.Count == 0 ? "   Le présent compte rendu." : string.Join("\n", docs.Select(d => $"   - {d[0]} {d[1]}")));
        return sb.ToString();
    }

    static void Achever(DbCtx c, long id, DateTime t)
    {
        var m = c.Rows("SELECT emetteur_id, destinataire_id, numero, objet, dossier_id FROM missions WHERE id=@p0", id)[0];
        long em = U.L(m[0]), de = U.L(m[1]);
        string rap = Rapport(c, id, t);
        c.Exec("UPDATE missions SET statut='Rapport transmis', avancement=100, rapport=@p1, date_rapport=@p2 WHERE id=@p0", id, rap, U.D(t));
        long doc = c.Insert("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif,mission_id,dossier_id) VALUES(@p0,@p1,'Comptes rendus',@p2,@p3,@p4,@p4,@p5,@p6)",
            Biz.Seq(c, "DOC"), $"Compte rendu — {m[2]} {m[3]}", rap, de, U.D(t), id, m[4]);
        Biz.MHist(c, id, de, "Rapport", "Compte rendu transmis à " + Biz.Name(c, em));
        if (em == Me)
        {
            Biz.Message(c, de, em, $"Compte rendu de mission {m[2]}", rap, id, U.L(m[4]), true, categorie: "Rapport", docId: doc);
            Biz.Notify(c, "Rapport terminé", "Haute", $"Rapport transmis — {m[2]}", $"{Biz.Name(c, de)} a transmis le compte rendu de « {m[3]} ». Décision attendue.", "mission", id);
        }
        else c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,mission_id,categorie,document_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Rapport',@p6)", U.D(t), de, em, $"Compte rendu {m[2]}", rap, id, doc);
        Biz.Event(c, "Rapport transmis", Biz.Name(c, de), de, $"Compte rendu de la mission {m[2]} transmis à {Biz.Name(c, em)}", "mission", id);
    }

    static void Delays(DbCtx c, DateTime t)
    {
        foreach (var r in c.Rows($"SELECT id, emetteur_id, destinataire_id, numero, objet, avancement FROM missions WHERE statut IN {Biz.SqlActifs} AND echeance<@p0 AND retard_signale=0 LIMIT 20", U.D(t)))
        {
            long id = U.L(r[0]), em = U.L(r[1]), de = U.L(r[2]);
            c.Exec("UPDATE missions SET retard_signale=1 WHERE id=@p0", id);
            string txt = $"La mission {r[3]} « {r[4]} » a dépassé son échéance (avancement {U.Dv(r[5]):0} %). Motif principal : {Rnd.Pick(Difficultes).ToLowerInvariant()}. Nouvelle estimation : sous 48 heures.";
            Biz.MHist(c, id, de, "Retard", txt);
            if (em == Me)
            {
                Biz.Message(c, de, em, $"Retard — {r[3]}", txt, id, categorie: "Mission");
                Biz.Notify(c, "Mission en retard", "Haute", $"Mission en retard — {r[3]}", txt, "mission", id);
            }
            else c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,mission_id,categorie) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Mission')", U.D(t), de, em, $"Retard — {r[3]}", txt, id);
            Biz.Event(c, "Retard signalé", Biz.Name(c, de), de, txt, "mission", id);
        }
    }

    // ------------------------------------------------------------------ réponses aux messages de l'utilisateur
    static void Replies(DbCtx c, DateTime t)
    {
        foreach (var r in c.Rows("SELECT m.id, m.dest_id, m.objet, m.corps, m.mission_id, m.dossier_id FROM messages m JOIN collaborateurs c ON c.id=m.dest_id WHERE m.attente_reponse=1 AND m.exp_id=@p1 AND m.date<=@p0 AND c.dispo IN ('Disponible','Hors horaires') LIMIT 5", U.D(t.AddMinutes(-Rnd.Next(12, 40))), Me))
        {
            long id = U.L(r[0]), de = U.L(r[1]), mis = U.L(r[4]);
            c.Exec("UPDATE messages SET attente_reponse=0, lu=CASE WHEN dest_id=@p1 THEN lu ELSE 1 END WHERE id=@p0", id, Me);
            string corps = U.S(r[3]);
            var sb = new StringBuilder("Monsieur le Procureur,\n\n");
            if (mis != 0)
            {
                var mm = c.Rows("SELECT numero, statut, avancement, echeance FROM missions WHERE id=@p0", mis);
                if (mm.Count > 0) sb.AppendLine($"Concernant la mission {mm[0][0]} : statut « {mm[0][1]} », avancement {U.Dv(mm[0][2]):0} %, échéance le {U.Aff(mm[0][3])}.");
            }
            if (corps.Contains('?')) sb.AppendLine("Je rassemble les éléments demandés et vous les transmets dans les meilleurs délais.");
            else sb.AppendLine(Rnd.Pick(new[] { "Bien reçu, je m'en occupe immédiatement.", "C'est noté, je prends les dispositions nécessaires.", "Bien reçu. J'informe mon équipe et je reviens vers vous.", "Instruction prise en compte." }));
            long charge = c.Long("SELECT charge FROM collaborateurs WHERE id=@p0", de);
            if (charge > 70) sb.AppendLine($"Je vous signale que ma charge de travail est actuellement élevée ({charge} %).");
            sb.AppendLine("\n" + Biz.NameF(c, de));
            Biz.Message(c, de, Me, "RE: " + U.S(r[2]), sb.ToString(), mis, U.L(r[5]));
        }
    }

    // ------------------------------------------------------------------ tâches planifiées
    static bool Once(DbCtx c, string key, DateTime t, int h, int min)
    {
        if (t.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        if (t.Hour < h || (t.Hour == h && t.Minute < min)) return false;
        string d = t.ToString("yyyy-MM-dd");
        if (c.Get("done_" + key, "") == d) return false;
        c.Set("done_" + key, d);
        return true;
    }

    static void Scheduled(DbCtx c, DateTime t)
    {
        long secp = long.Parse(c.Get("secp_id", "0")), adj = long.Parse(c.Get("adjoint_id", "0")), sg = long.Parse(c.Get("sg_id", "0"));
        string jour = t.ToString("yyyy-MM-dd");
        if (Once(c, "agenda", t, 8, 30))
        {
            var reus = c.Rows("SELECT r.debut, r.titre, r.lieu FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND substr(r.debut,1,10)=@p1 AND r.statut IN ('Planifiée','Proposée') ORDER BY r.debut", Me, jour);
            var appels = c.Long("SELECT COUNT(*) FROM appels WHERE rappel=1");
            var sb = new StringBuilder($"Monsieur le Procureur,\n\nVotre agenda du {U.AffJ(t)} :\n");
            if (reus.Count == 0) sb.AppendLine(" - Aucune réunion programmée.");
            foreach (var r in reus) sb.AppendLine($" - {U.P(r[0]):HH:mm} : {r[1]} ({r[2]})");
            sb.AppendLine($"\nAppels à rappeler : {appels}.\nDemandes en attente de décision : {c.Long("SELECT COUNT(*) FROM demandes WHERE statut='En attente'")}.\n\nLe secrétaire particulier");
            Biz.Message(c, secp, Me, $"Votre agenda du {t:dd/MM}", sb.ToString(), categorie: "Agenda");
        }
        if (Once(c, "synthese_adj", t, 9, 15))
        {
            var sb = new StringBuilder("Monsieur le Procureur,\n\nSynthèse quotidienne de coordination.\n\n");
            sb.AppendLine(Biz.Synthese(c));
            var dels = c.Rows($"SELECT numero, objet, statut, avancement FROM missions WHERE emetteur_id=@p0 AND parent_id IS NULL AND statut IN {Biz.SqlActifs} ORDER BY echeance LIMIT 10", Me);
            if (dels.Count > 0) { sb.AppendLine("\nVos délégations en cours :"); foreach (var d in dels) sb.AppendLine($" - {d[0]} {d[1]} : {d[2]} ({U.Dv(d[3]):0} %)"); }
            sb.AppendLine("\nLe procureur adjoint");
            Biz.Message(c, adj, Me, $"Synthèse de coordination du {t:dd/MM}", sb.ToString(), categorie: "Synthèse");
            Biz.Event(c, "Synthèse transmise", Biz.Name(c, adj), adj, "Synthèse quotidienne du procureur adjoint", "moteur", 0);
        }
        if (Once(c, "bilan_sg", t, 18, 0))
        {
            var sb = new StringBuilder($"BILAN QUOTIDIEN DU {U.AffJ(t).ToUpper(U.Fr)}\n{U.Mention}\n\n");
            sb.AppendLine("Activité de la journée (données enregistrées) :");
            foreach (var e in c.Rows("SELECT type, COUNT(*) FROM evenements WHERE substr(date,1,10)=@p0 GROUP BY type ORDER BY COUNT(*) DESC", jour)) sb.AppendLine($" - {e[0]} : {e[1]}");
            sb.AppendLine($"\nMessages échangés : {c.Long("SELECT COUNT(*) FROM messages WHERE substr(date,1,10)=@p0", jour)}");
            sb.AppendLine($"Missions terminées : {c.Long("SELECT COUNT(*) FROM missions WHERE substr(date_rapport,1,10)=@p0", jour)}");
            sb.AppendLine($"Réunions tenues : {c.Long("SELECT COUNT(*) FROM reunions WHERE substr(debut,1,10)=@p0 AND statut='Terminée'", jour)}");
            sb.AppendLine("\nCharge moyenne par service :");
            foreach (var s in c.Rows("SELECT s.nom, ROUND(AVG(c.charge)), SUM(c.dispo='Absent') FROM collaborateurs c JOIN services s ON s.id=c.service_id GROUP BY s.id ORDER BY s.id")) sb.AppendLine($" - {s[0]} : {s[1]} % (absents : {s[2]})");
            sb.AppendLine("\n" + Biz.Synthese(c));
            long doc = c.Insert("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif) VALUES(@p0,@p1,'Bilans',@p2,@p3,@p4,@p4)", Biz.Seq(c, "DOC"), $"Bilan quotidien du {t:dd/MM/yyyy}", sb.ToString(), sg, U.D(t));
            Biz.Message(c, sg, Me, $"Bilan quotidien du {t:dd/MM}", sb.ToString(), categorie: "Bilan", docId: doc);
            Biz.Notify(c, "Rapport de service disponible", "Normale", "Bilan quotidien du secrétariat général", "Le bilan de la journée est disponible.", "document", doc);
        }
        if (t.DayOfWeek == DayOfWeek.Friday && Once(c, "hebdo_stat", t, 16, 0))
        {
            long stat = c.Long("SELECT responsable_id FROM services WHERE code='STAT'");
            var sb = new StringBuilder($"RAPPORT HEBDOMADAIRE DE PILOTAGE — semaine du {t.AddDays(-4):dd/MM/yyyy}\n{U.Mention}\n\n");
            string d0 = U.D(t.Date.AddDays(-4));
            sb.AppendLine($"Missions créées : {c.Long("SELECT COUNT(*) FROM missions WHERE date_creation>=@p0", d0)}");
            sb.AppendLine($"Missions terminées : {c.Long("SELECT COUNT(*) FROM missions WHERE date_rapport>=@p0", d0)}");
            sb.AppendLine($"Dossiers ouverts : {c.Long("SELECT COUNT(*) FROM dossiers WHERE date_ouverture>=@p0", d0)}");
            sb.AppendLine($"Dossiers clôturés : {c.Long("SELECT COUNT(*) FROM dossiers WHERE date_cloture>=@p0", d0)}");
            sb.AppendLine($"Événements : {c.Long("SELECT COUNT(*) FROM evenements WHERE date>=@p0", d0)}");
            sb.AppendLine("\nMissions terminées par service :");
            foreach (var s in c.Rows("SELECT s.nom, COUNT(m.id) FROM services s LEFT JOIN missions m ON m.service_id=s.id AND m.date_rapport>=@p0 GROUP BY s.id ORDER BY COUNT(m.id) DESC", d0)) sb.AppendLine($" - {s[0]} : {s[1]}");
            long doc = c.Insert("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif) VALUES(@p0,@p1,'Rapports d''activité',@p2,@p3,@p4,@p4)", Biz.Seq(c, "DOC"), $"Rapport hebdomadaire de pilotage — {t:dd/MM/yyyy}", sb.ToString(), stat, U.D(t));
            Biz.Notify(c, "Rapport de service disponible", "Normale", "Rapport hebdomadaire de pilotage", "Le service des statistiques a publié son rapport hebdomadaire.", "document", doc);
        }
    }

    // ------------------------------------------------------------------ réunions
    static void Meetings(DbCtx c, DateTime t)
    {
        string T = U.D(t);
        foreach (var r in c.Rows("SELECT id FROM reunions WHERE statut='Planifiée' AND debut<=@p0 AND fin>@p0", T))
        {
            c.Exec("UPDATE reunions SET statut='En cours' WHERE id=@p0", r[0]);
            c.Exec("UPDATE collaborateurs SET dispo='En réunion' WHERE dispo<>'Absent' AND id IN (SELECT collab_id FROM reunion_participants WHERE reunion_id=@p0 AND reponse<>'Décliné')", r[0]);
        }
        foreach (var r in c.Rows("SELECT id, organisateur_id, titre FROM reunions WHERE statut IN ('Planifiée','En cours') AND fin<=@p0", T))
        {
            long id = U.L(r[0]), org = U.L(r[1]);
            c.Exec("UPDATE reunions SET statut='Terminée' WHERE id=@p0", id);
            c.Exec("UPDATE collaborateurs SET dispo='Disponible' WHERE dispo='En réunion' AND id IN (SELECT collab_id FROM reunion_participants WHERE reunion_id=@p0)", id);
            Biz.Event(c, "Réunion terminée", Biz.Name(c, org), org, $"Fin de la réunion « {r[2]} »", "reunion", id);
            if (org != Me)
            {
                Biz.GenererCompteRendu(c, id, org);
                if (c.Long("SELECT COUNT(*) FROM reunion_participants WHERE reunion_id=@p0 AND collab_id=@p1", id, Me) > 0)
                    Biz.Notify(c, "Rapport de service disponible", "Normale", "Compte rendu de réunion", $"Le compte rendu de « {r[2]} » est disponible.", "reunion", id);
            }
        }
        c.Exec("UPDATE reunions SET statut='Annulée' WHERE statut='Proposée' AND debut<=@p0", T);
        foreach (var r in c.Rows("SELECT r.id, r.titre, r.debut FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p1 AND r.statut='Planifiée' AND r.rappel_envoye=0 AND datetime(r.debut, '-'||r.rappel_min||' minutes')<=@p0", T, Me))
        {
            c.Exec("UPDATE reunions SET rappel_envoye=1 WHERE id=@p0", r[0]);
            Biz.Notify(c, "Réunion imminente", "Haute", "Réunion imminente : " + r[1], $"Début à {U.P(r[2]):HH:mm}.", "reunion", U.L(r[0]));
        }
    }

    // ------------------------------------------------------------------ événements aléatoires (liés à l'état réel)
    static void RandomEvents(DbCtx c, DateTime t, double dt)
    {
        if (Rnd.NextDouble() < P(2.5, dt) && c.Long($"SELECT COUNT(*) FROM missions WHERE statut IN {Biz.SqlActifs}") < 380) EvtMissionInterne(c, t);
        if (Rnd.NextDouble() < P(6, dt)) EvtEchange(c, t);
        if (Rnd.NextDouble() < P(0.5, dt)) EvtDifficulte(c, t);
        if (Rnd.NextDouble() < P(0.6, dt)) EvtNouveauDossier(c, t);
        if (Rnd.NextDouble() < P(5, dt)) EvtAvancementDossier(c, t);
        if (Rnd.NextDouble() < P(0.35, dt)) EvtReunion(c, t);
        if (Rnd.NextDouble() < P(0.6, dt)) EvtAppel(c, t);
        if (Rnd.NextDouble() < P(0.4, dt)) EvtPartenaire(c, t);
        if (Rnd.NextDouble() < P(0.3, dt)) EvtDemande(c, t);
        if (Rnd.NextDouble() < P(0.4, dt)) EvtSurcharge(c, t);
        if (Rnd.NextDouble() < P(0.12, dt)) EvtAbsence(c, t);
        if (Rnd.NextDouble() < P(0.08, dt)) EvtReunionDeplacee(c, t);
        if (Rnd.NextDouble() < P(1.5, dt)) EvtInstitutionnel(c, t);
    }

    static void EvtMissionInterne(DbCtx c, DateTime t)
    {
        var chefs = c.Rows("SELECT c.id, c.service_id FROM collaborateurs c WHERE c.niveau BETWEEN 3 AND 6 AND c.autonomie>=2 AND c.dispo='Disponible' AND EXISTS(SELECT 1 FROM collaborateurs s WHERE s.superieur_id=c.id AND s.dispo<>'Absent') ORDER BY RANDOM() LIMIT 1");
        if (chefs.Count == 0) return;
        long chef = U.L(chefs[0][0]), svc = U.L(chefs[0][1]);
        long dest = c.Long("SELECT id FROM collaborateurs WHERE superieur_id=@p0 AND dispo<>'Absent' ORDER BY charge ASC, RANDOM() LIMIT 1", chef);
        long dos = Rnd.NextDouble() < 0.5 ? c.Long("SELECT id FROM dossiers WHERE service_id=@p0 AND statut<>'Clôturé' AND archive=0 ORDER BY RANDOM() LIMIT 1", svc) : 0;
        string objet = Rnd.Pick(ObjetsInternes);
        if (dos != 0) objet += " — " + c.Str("SELECT numero FROM dossiers WHERE id=@p0", dos);
        try
        {
            Biz.CreerMission(c, chef, dest, objet, "Mission interne décidée par le responsable dans le cadre de l'activité courante du service.", Rnd.Pick(new[] { "Normale", "Normale", "Basse", "Haute" }),
                t.AddDays(Rnd.Next(2, 7)).AddHours(Rnd.Next(0, 6)), "Me rendre compte à l'issue.", "Interne", svc, dos, 0, null, "Initiative interne");
        }
        catch (InvalidOperationException) { }
        catch (UnauthorizedAccessException) { }
    }

    static void EvtEchange(DbCtx c, DateTime t)
    {
        var m = c.Rows("SELECT m.id, m.numero, m.objet, m.destinataire_id, m.emetteur_id, m.avancement, c.profil FROM missions m JOIN collaborateurs c ON c.id=m.destinataire_id WHERE m.statut IN ('En cours','À modifier') AND c.dispo='Disponible' ORDER BY RANDOM() LIMIT 1");
        if (m.Count == 0) return;
        var x = m[0];
        long id = U.L(x[0]), de = U.L(x[3]), em = U.L(x[4]);
        string profil = U.S(x[6]);
        long autre = c.Long("SELECT assigne_id FROM sous_taches WHERE mission_id=@p0 AND assigne_id<>@p1 ORDER BY RANDOM() LIMIT 1", id, de);
        int kind = Rnd.Next(4);
        long exp, dst; string obj, corps;
        if (autre != 0 && kind < 2)
        {
            exp = kind == 0 ? de : autre; dst = kind == 0 ? autre : de;
            obj = kind == 0 ? $"Point d'étape — {x[1]}" : $"Transmission d'éléments — {x[1]}";
            corps = kind == 0 ? $"Peux-tu me faire un point sur ta partie de « {x[2]} » ? Nous sommes à {U.Dv(x[5]):0} % au global." : $"Je te transmets mes éléments pour « {x[2]} ». Je reste disponible pour toute précision.";
        }
        else
        {
            exp = de; dst = em;
            obj = kind == 2 ? $"Demande de précision — {x[1]}" : $"Point d'avancement — {x[1]}";
            corps = kind == 2 ? $"Afin de finaliser « {x[2]} », pourriez-vous préciser le périmètre attendu et le format du rendu ?" : $"La mission « {x[2]} » avance : {U.Dv(x[5]):0} % réalisés. Pas de difficulté particulière à ce stade.";
        }
        if (dst == Me)
        {
            if (profil == "Sollicite fréquemment son responsable" || Rnd.NextDouble() < 0.25) Biz.Message(c, exp, dst, obj, corps, id, categorie: "Mission");
            return;
        }
        c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,mission_id,categorie,lu) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Interne',1)", U.D(t), exp, dst, obj, corps, id);
        Biz.MHist(c, id, exp, "Échange", $"{Biz.Name(c, exp)} → {Biz.Name(c, dst)} : {obj}");
    }

    static void EvtDifficulte(DbCtx c, DateTime t)
    {
        var m = c.Rows(@"SELECT m.id, m.numero, m.objet, m.destinataire_id, m.emetteur_id FROM missions m JOIN collaborateurs c ON c.id=m.destinataire_id
            WHERE m.statut IN ('En cours','À modifier') ORDER BY (c.profil='Signale rapidement les difficultés') DESC, RANDOM() LIMIT 1");
        if (m.Count == 0) return;
        long id = U.L(m[0][0]), de = U.L(m[0][3]), em = U.L(m[0][4]);
        if (c.Long("SELECT COUNT(*) FROM mission_hist WHERE mission_id=@p0 AND type='Difficulté'", id) >= 2) return;
        string diff = Rnd.Pick(Difficultes);
        c.Exec("UPDATE missions SET vitesse=vitesse*0.85 WHERE id=@p0", id);
        Biz.MHist(c, id, de, "Difficulté", diff);
        string txt = $"Je vous signale une difficulté sur la mission {m[0][1]} « {m[0][2]} » : {diff.ToLowerInvariant()}. Je propose : {Rnd.Pick(Propositions).ToLowerInvariant()}";
        if (em == Me)
        {
            Biz.Message(c, de, em, $"Difficulté — {m[0][1]}", txt, id, important: true, categorie: "Mission");
            Biz.Notify(c, "Difficulté signalée", "Haute", $"Difficulté signalée — {m[0][1]}", txt, "mission", id);
        }
        else c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,mission_id,categorie) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Mission')", U.D(t), de, em, $"Difficulté — {m[0][1]}", txt, id);
        Biz.Event(c, "Difficulté signalée", Biz.Name(c, de), de, txt, "mission", id);
    }

    static void EvtNouveauDossier(DbCtx c, DateTime t)
    {
        var org = c.Rows("SELECT id, nom FROM organismes WHERE categorie IN ('Service d''enquête','Tribunal','Administration partenaire','Institution') ORDER BY RANDOM() LIMIT 1");
        if (org.Count == 0) return;
        string cat = Rnd.Pick(Seed.Categories);
        long svc = c.Long("SELECT id FROM services WHERE code=@p0", CatService.TryGetValue(cat, out var code) ? code : "GEST");
        long referent = c.Long("SELECT id FROM collaborateurs WHERE service_id=@p0 AND niveau>=6 AND dispo<>'Absent' ORDER BY charge ASC LIMIT 1", svc);
        string prio = Rnd.NextDouble() < 0.15 ? "Haute" : Rnd.NextDouble() < 0.75 ? "Normale" : "Basse";
        string num = Biz.Seq(c, "DOS");
        string intitule = Rnd.Pick(Seed.SujetsDossiers) + " (simulation)";
        long id = c.Insert(@"INSERT INTO dossiers(numero,intitule,categorie,resume,service_id,referent_id,organisme_id,personnes,statut,priorite,avancement,date_ouverture,echeance)
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,'Personnes fictives (simulation)','Ouvert',@p7,0,@p8,@p9)", num, intitule, cat,
            $"Dossier transmis par {org[0][1]}. Contenu fictif généré par le moteur de simulation.", svc, referent, org[0][0], prio, U.D(t), U.D(t.AddDays(Rnd.Next(15, 60))));
        Biz.DHist(c, id, referent, $"Ouverture du dossier sur transmission de {org[0][1]}");
        c.Exec("UPDATE organismes SET activite=activite+1 WHERE id=@p0", org[0][0]);
        Biz.Event(c, "Nouveau dossier", U.S(org[0][1]), 0, $"Dossier {num} « {intitule} » transmis au {c.Str("SELECT nom FROM services WHERE id=@p0", svc)}", "dossier", id, orgId: U.L(org[0][0]), svc: svc);
        Biz.Notify(c, "Nouveau dossier", prio == "Haute" ? "Haute" : "Basse", $"Nouveau dossier {num}", $"{intitule} — {cat} — référent : {Biz.Name(c, referent)}", "dossier", id);
        if (prio == "Haute")
        {
            long head = c.Long("SELECT responsable_id FROM services WHERE id=@p0", svc);
            Biz.Message(c, head, Me, $"Dossier prioritaire {num}", $"Monsieur le Procureur,\n\nLe dossier {num} « {intitule} » vient d'être ouvert dans mon service sur transmission de {org[0][1]}. Je l'ai confié à {Biz.NameF(c, referent)}.\n\n{Biz.NameF(c, head)}", 0, id, true, categorie: "Dossier");
        }
    }

    static void EvtAvancementDossier(DbCtx c, DateTime t)
    {
        var d = c.Rows("SELECT d.id, d.numero, d.avancement, d.referent_id, d.statut FROM dossiers d JOIN collaborateurs c ON c.id=d.referent_id WHERE d.statut<>'Clôturé' AND d.archive=0 AND c.dispo='Disponible' ORDER BY RANDOM() LIMIT 1");
        if (d.Count == 0) return;
        long id = U.L(d[0][0]), refe = U.L(d[0][3]);
        int av = Math.Min(100, (int)U.L(d[0][2]) + Rnd.Next(4, 13));
        string statut = av >= 100 ? "Clôturé" : av > 40 ? "En cours d'instruction" : "Ouvert";
        c.Exec("UPDATE dossiers SET avancement=@p1, statut=@p2, date_cloture=CASE WHEN @p2='Clôturé' THEN @p3 ELSE date_cloture END WHERE id=@p0", id, av, statut, U.D(t));
        Biz.DHist(c, id, refe, statut == "Clôturé" ? "Dossier clôturé par le référent" : $"Mise à jour du dossier — avancement {av} % ({Rnd.Pick(new[] { "pièces analysées", "échange avec le partenaire", "note de suivi rédigée", "vérification des délais", "mise à jour de la fiche" })})");
        if (statut == "Clôturé") Biz.Event(c, "Dossier clôturé", Biz.Name(c, refe), refe, $"Clôture du dossier {d[0][1]}", "dossier", id);
        else if (U.S(d[0][4]) != statut) Biz.Event(c, "Dossier mis à jour", Biz.Name(c, refe), refe, $"Dossier {d[0][1]} : {statut}", "dossier", id);
    }

    static void EvtReunion(DbCtx c, DateTime t)
    {
        long adj = long.Parse(c.Get("adjoint_id", "0"));
        long org = Rnd.NextDouble() < 0.4 ? adj : c.Long("SELECT responsable_id FROM services WHERE code NOT IN ('CABP') ORDER BY RANDOM() LIMIT 1");
        if (org == 0 || org == Me) return;
        var parts = c.Rows("SELECT id FROM collaborateurs WHERE superieur_id=@p0 AND dispo<>'Absent' ORDER BY RANDOM() LIMIT @p1", org, Rnd.Next(2, 5)).Select(x => U.L(x[0])).ToList();
        parts.Add(org);
        bool avecMoi = Rnd.NextDouble() < (org == adj ? 0.7 : 0.35);
        if (avecMoi) parts.Add(Me);
        var day = t.Date.AddDays(1);
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) day = day.AddDays(1);
        int dur = Rnd.Pick(new[] { 30, 60, 60, 90 });
        for (int essai = 0; essai < 4; essai++)
        {
            var deb = day.AddHours(Rnd.Next(9, 17)).AddMinutes(Rnd.Next(2) * 30);
            if (Biz.Conflits(c, deb, deb.AddMinutes(dur), parts, 0).Count > 0) continue;
            string titre = Rnd.Pick(TitresReunions) + " — " + c.Str("SELECT s.code FROM collaborateurs c JOIN services s ON s.id=c.service_id WHERE c.id=@p0", org);
            var mis = c.Rows($"SELECT objet FROM missions WHERE (destinataire_id=@p0 OR emetteur_id=@p0) AND statut IN {Biz.SqlActifs} LIMIT 3", org).Select(x => U.S(x[0])).ToList();
            string odj = "1. Tour de table\n" + string.Join("\n", mis.Select((m, i) => $"{i + 2}. {m}")) + $"\n{mis.Count + 2}. Questions diverses";
            string statut = avecMoi ? "Proposée" : "Planifiée";
            long id = c.Insert("INSERT INTO reunions(titre,debut,fin,lieu,organisateur_id,ordre_du_jour,priorite,statut) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Normale',@p6)",
                titre, U.D(deb), U.D(deb.AddMinutes(dur)), Rnd.Pick(new[] { "Salle de réunion B", "Salle de coordination", "Bureau du procureur adjoint", "Visioconférence (simulée)" }), org, odj, statut);
            foreach (var p in parts.Distinct()) c.Exec("INSERT OR IGNORE INTO reunion_participants(reunion_id,collab_id,reponse) VALUES(@p0,@p1,@p2)", id, p, p == Me ? "Invité" : "Accepté");
            Biz.Event(c, "Réunion proposée", Biz.Name(c, org), org, $"« {titre} » le {U.Aff(U.D(deb))}", "reunion", id);
            if (avecMoi)
            {
                Biz.Notify(c, "Réunion proposée", "Normale", "Réunion proposée : " + titre, $"{Biz.Name(c, org)} propose une réunion le {U.Aff(U.D(deb))}. Acceptez-la depuis l'agenda.", "reunion", id);
                Biz.Message(c, org, Me, "Proposition de réunion : " + titre, $"Monsieur le Procureur,\n\nJe vous propose une réunion le {U.Aff(U.D(deb))} ({dur} min).\n\nOrdre du jour :\n{odj}\n\n{Biz.NameF(c, org)}", categorie: "Agenda");
            }
            return;
        }
    }

    static void EvtAppel(DbCtx c, DateTime t)
    {
        bool externe = Rnd.NextDouble() < 0.7;
        string nom; long interId = 0, org = 0, collab = 0;
        if (externe)
        {
            var i = c.Rows("SELECT i.id, i.prenom||' '||i.nom||' ('||i.fonction||', '||o.nom||')', o.id FROM interlocuteurs i JOIN organismes o ON o.id=i.organisme_id ORDER BY RANDOM() LIMIT 1");
            if (i.Count == 0) return;
            interId = U.L(i[0][0]); nom = U.S(i[0][1]); org = U.L(i[0][2]);
        }
        else
        {
            collab = c.Long("SELECT responsable_id FROM services WHERE responsable_id<>@p0 ORDER BY RANDOM() LIMIT 1", Me);
            nom = Biz.NameF(c, collab);
        }
        bool enReunion = c.Long("SELECT COUNT(*) FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND r.statut='En cours'", Me) > 0;
        double x = Rnd.NextDouble();
        string statut = enReunion ? "Manqué" : x < 0.5 ? "Pris par le secrétariat" : x < 0.75 ? "À rappeler" : "Manqué";
        string motif = Rnd.Pick(MotifsAppel);
        long dos = org != 0 ? c.Long("SELECT id FROM dossiers WHERE organisme_id=@p0 AND statut<>'Clôturé' ORDER BY RANDOM() LIMIT 1", org) : 0;
        string notes = statut == "Pris par le secrétariat" ? $"Appel pris par le secrétariat particulier. {nom} souhaite : {motif.ToLowerInvariant()}." : enReunion ? "Appel reçu pendant une réunion." : "";
        long id = c.Insert("INSERT INTO appels(date,sens,statut,interlocuteur,interlocuteur_id,organisme_id,collab_id,duree,motif,notes,dossier_id,rappel) VALUES(@p0,'Entrant',@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)",
            U.D(t), statut, nom, interId == 0 ? null : interId, org == 0 ? null : org, collab == 0 ? null : collab, statut == "Pris par le secrétariat" ? Rnd.Next(60, 400) : 0, motif, notes, dos == 0 ? null : dos, statut != "Pris par le secrétariat");
        if (org != 0) c.Exec("UPDATE organismes SET activite=activite+1 WHERE id=@p0", org);
        Biz.Event(c, "Appel entrant", nom, collab, $"Appel de {nom} — {motif} — {statut}", "appel", id, orgId: org);
        Biz.Notify(c, "Appel", statut == "Manqué" ? "Normale" : "Basse", $"Appel {statut.ToLowerInvariant()} — {nom}", motif, "appel", id);
    }

    static void EvtPartenaire(DbCtx c, DateTime t)
    {
        var i = c.Rows("SELECT i.prenom||' '||i.nom||' — '||i.fonction, o.id, o.nom FROM interlocuteurs i JOIN organismes o ON o.id=i.organisme_id ORDER BY RANDOM() LIMIT 1");
        if (i.Count == 0) return;
        long org = U.L(i[0][1]);
        string info = Rnd.Pick(InfosPartenaires);
        long dos = c.Long("SELECT id FROM dossiers WHERE organisme_id=@p0 AND statut<>'Clôturé' ORDER BY RANDOM() LIMIT 1", org);
        string corps = $"Monsieur le Procureur,\n\n{info}" + (dos != 0 ? $" concernant le dossier {c.Str("SELECT numero FROM dossiers WHERE id=@p0", dos)}" : "") + ".\nNous restons à votre disposition pour tout échange.\n\n" + i[0][0] + "\n" + i[0][2] + "\n\n(Communication simulée — sans valeur officielle)";
        Biz.Message(c, 0, Me, $"{info} — {i[0][2]}", corps, 0, dos, false, $"{i[0][0]} ({i[0][2]})", org, "Externe");
        c.Exec("UPDATE organismes SET activite=activite+1 WHERE id=@p0", org);
        Biz.Event(c, "Événement institutionnel", U.S(i[0][2]), 0, info, "organisme", org, orgId: org);
        if (dos != 0) Biz.DHist(c, dos, 0, $"Information reçue de {i[0][2]} : {info}");
    }

    static void EvtDemande(DbCtx c, DateTime t)
    {
        if (c.Long("SELECT COUNT(*) FROM demandes WHERE statut='En attente'") > 12) return;
        var h = c.Rows("SELECT s.responsable_id, s.id, s.nom FROM services s WHERE s.responsable_id<>@p0 ORDER BY RANDOM() LIMIT 1", Me);
        if (h.Count == 0) return;
        long dem = U.L(h[0][0]), svc = U.L(h[0][1]);
        string type = Rnd.Pick(new[] { "Demande de validation", "Demande d'arbitrage", "Demande de renseignement", "Proposition d'organisation" });
        string dosNum = c.Str("SELECT numero FROM dossiers WHERE service_id=@p0 AND statut<>'Clôturé' ORDER BY RANDOM() LIMIT 1", svc) ?? "";
        string misNum = c.Str($"SELECT numero FROM missions WHERE service_id=@p0 AND statut IN {Biz.SqlActifs} ORDER BY RANDOM() LIMIT 1", svc) ?? "";
        string objet = type switch
        {
            "Demande de validation" => dosNum != "" ? $"Validation de l'orientation du dossier {dosNum}" : "Validation d'une note de service",
            "Demande d'arbitrage" => misNum != "" ? $"Arbitrage sur la priorité de la mission {misNum}" : "Arbitrage sur la répartition des permanences",
            "Demande de renseignement" => "Précisions sur les orientations de politique interne (simulation)",
            _ => Rnd.Pick(Propositions).TrimEnd('.')
        };
        string texte = $"Monsieur le Procureur,\n\n{type} : {objet}.\nCharge moyenne actuelle du service : {c.Long("SELECT ROUND(AVG(charge)) FROM collaborateurs WHERE service_id=@p0", svc)} %.\n\n{Biz.NameF(c, dem)}";
        long id = c.Insert("INSERT INTO demandes(date,demandeur_id,type,objet,texte,service_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", U.D(t), dem, type, objet, texte, svc);
        Biz.Notify(c, "Demande de validation", type is "Demande de validation" or "Demande d'arbitrage" ? "Haute" : "Normale", $"{type} — {h[0][2]}", objet, "demande", id);
        Biz.Event(c, type, Biz.Name(c, dem), dem, objet, "demande", id, svc: svc);
    }

    static void EvtSurcharge(DbCtx c, DateTime t)
    {
        var s = c.Rows("SELECT s.id, s.nom, s.responsable_id, AVG(c.charge) FROM services s JOIN collaborateurs c ON c.service_id=s.id GROUP BY s.id HAVING AVG(c.charge)>55 ORDER BY AVG(c.charge) DESC LIMIT 1");
        if (s.Count == 0) return;
        long svc = U.L(s[0][0]), head = U.L(s[0][2]);
        string key = "surcharge_" + svc;
        if (c.Get(key, "") == t.ToString("yyyy-MM-dd") || head == Me) return;
        c.Set(key, t.ToString("yyyy-MM-dd"));
        string txt = $"Monsieur le Procureur,\n\nLe {s[0][1]} connaît une charge moyenne de {U.Dv(s[0][3]):0} %. Je sollicite un renfort temporaire d'un collaborateur afin de tenir les échéances.\n\n{Biz.NameF(c, head)}";
        long id = c.Insert("INSERT INTO demandes(date,demandeur_id,type,objet,texte,service_id) VALUES(@p0,@p1,'Demande de renfort',@p2,@p3,@p4)", U.D(t), head, $"Renfort pour le {s[0][1]}", txt, svc);
        Biz.Message(c, head, Me, "Surcharge — demande de renfort", txt, important: true, categorie: "Demande");
        Biz.Notify(c, "Difficulté signalée", "Haute", "Surcharge signalée — " + s[0][1], $"Charge moyenne {U.Dv(s[0][3]):0} %. Demande de renfort en attente.", "demande", id);
        Biz.Event(c, "Surcharge signalée", Biz.Name(c, head), head, $"Surcharge du {s[0][1]} ({U.Dv(s[0][3]):0} %)", "demande", id, svc: svc);
    }

    static void EvtAbsence(DbCtx c, DateTime t)
    {
        var p = c.Rows("SELECT id, superieur_id FROM collaborateurs WHERE id<>@p0 AND dispo='Disponible' AND niveau>=4 ORDER BY RANDOM() LIMIT 1", Me);
        if (p.Count == 0) return;
        long id = U.L(p[0][0]), sup = U.L(p[0][1]);
        c.Exec("UPDATE collaborateurs SET dispo='Absent' WHERE id=@p0", id);
        string motif = Rnd.Pick(new[] { "congé", "formation", "absence pour raison personnelle", "déplacement extérieur (simulation)" });
        Biz.Event(c, "Absence", Biz.Name(c, id), id, $"{Biz.NameF(c, id)} absent(e) pour la journée ({motif})", "collaborateur", id);
        long nb = c.Long($"SELECT COUNT(*) FROM missions WHERE destinataire_id=@p0 AND statut IN {Biz.SqlActifs}", id);
        if (nb > 0 && sup != 0)
        {
            string txt = $"{Biz.Name(c, id)} est absent(e) aujourd'hui ({motif}). {nb} mission(s) en cours sont suspendues jusqu'à son retour.";
            if (sup == Me) Biz.Message(c, id, sup, "Absence", txt, categorie: "Personnel");
            else c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,categorie) VALUES(@p0,@p1,@p2,'Absence',@p3,'Personnel')", U.D(t), id, sup, txt);
        }
    }

    static void EvtReunionDeplacee(DbCtx c, DateTime t)
    {
        var r = c.Rows("SELECT id, titre, debut, fin, organisateur_id FROM reunions WHERE statut='Planifiée' AND debut>@p0 AND organisateur_id<>@p1 ORDER BY RANDOM() LIMIT 1", U.D(t.AddHours(3)), Me);
        if (r.Count == 0) return;
        long id = U.L(r[0][0]);
        var deb = U.P(r[0][2]).AddHours(1); var fin = U.P(r[0][3]).AddHours(1);
        if (fin.Hour >= 19) return;
        var parts = c.Rows("SELECT collab_id FROM reunion_participants WHERE reunion_id=@p0", id).Select(x => U.L(x[0])).ToList();
        if (Biz.Conflits(c, deb, fin, parts, id).Count > 0) return;
        c.Exec("UPDATE reunions SET debut=@p1, fin=@p2, rappel_envoye=0 WHERE id=@p0", id, U.D(deb), U.D(fin));
        long org = U.L(r[0][4]);
        Biz.Event(c, "Réunion déplacée", Biz.Name(c, org), org, $"« {r[0][1]} » déplacée au {U.Aff(U.D(deb))}", "reunion", id);
        if (parts.Contains(Me)) Biz.Notify(c, "Réunion proposée", "Normale", "Réunion déplacée : " + r[0][1], $"Nouvel horaire : {U.Aff(U.D(deb))}", "reunion", id);
    }

    static void EvtInstitutionnel(DbCtx c, DateTime t)
    {
        var o = c.Rows("SELECT id, nom, categorie FROM organismes ORDER BY RANDOM() LIMIT 1");
        if (o.Count == 0) return;
        var s = c.Rows("SELECT id, nom, responsable_id FROM services WHERE code IN ('ENQ','GEST','COORD','AUD','PCT','EMF','EDC','UTC','COM','SG') ORDER BY RANDOM() LIMIT 1");
        string action = Rnd.Pick(new[] { "transmet des pièces complémentaires (simulation) au", "sollicite un point de situation auprès du", "adresse un compte rendu d'activité au", "propose une réunion de coordination au", "échange sur un calendrier commun avec le" });
        c.Exec("UPDATE organismes SET activite=activite+1 WHERE id=@p0", o[0][0]);
        Biz.Event(c, "Échange institutionnel", U.S(o[0][1]), 0, $"{o[0][1]} {action} {s[0][1]}", "organisme", U.L(o[0][0]), orgId: U.L(o[0][0]), svc: U.L(s[0][0]));
        if (Rnd.NextDouble() < 0.3)
            c.Exec("INSERT INTO messages(date,exp_ext,dest_id,objet,corps,organisme_id,categorie) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Externe')", U.D(t), U.S(o[0][1]), s[0][2],
                "Échange — " + o[0][1], $"{o[0][1]} {action} service.\n\n(Communication simulée — sans valeur officielle)", o[0][0]);
    }
}
