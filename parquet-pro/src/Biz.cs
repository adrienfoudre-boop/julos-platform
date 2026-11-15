using System.Text;

namespace ParquetPro;

public static class Biz
{
    public static readonly string[] StatutsActifs = { "Nouvelle", "En analyse", "En cours", "À modifier" };
    public const string SqlActifs = "('Nouvelle','En analyse','En cours','À modifier')";
    public static readonly string[] Priorites = { "Haute", "Normale", "Basse" };

    public static long ProcId => 1;

    public static string Name(DbCtx c, long id) => id == 0 ? "" : c.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", id) ?? "";
    public static string NameF(DbCtx c, long id) => id == 0 ? "" : c.Str("SELECT prenom||' '||nom||' ('||fonction||')' FROM collaborateurs WHERE id=@p0", id) ?? "";

    public static string Seq(DbCtx c, string prefix)
    {
        long n = c.Long("SELECT CAST(IFNULL((SELECT valeur FROM parametres WHERE cle='seq_'||@p0),'0') AS INTEGER)", prefix) + 1;
        c.Set("seq_" + prefix, n);
        return $"{prefix}-{Engine.Now:yyyy}-{n:00000}";
    }

    // ------------------------------------------------------------ journal / événements / notifications
    public static void Admin(string action, string details)
    {
        try { Db.Exec("INSERT INTO journal_admin(date,date_reelle,auteur,action,details) VALUES(@p0,@p1,@p2,@p3,@p4)", U.D(Engine.Now), U.D(DateTime.Now), Auth.Login == "" ? "Système" : Auth.Login, action, details); } catch { }
    }
    public static void Admin(DbCtx c, string action, string details)
        => c.Exec("INSERT INTO journal_admin(date,date_reelle,auteur,action,details) VALUES(@p0,@p1,@p2,@p3,@p4)", U.D(Engine.Now), U.D(DateTime.Now), Auth.Login == "" ? "Système" : Auth.Login, action, details);

    public static long Event(DbCtx c, string type, string origine, long origineId, string desc, string refType, long refId, DateTime? date = null, long orgId = 0, long svc = 0)
        => c.Insert("INSERT INTO evenements(uid,date,type,origine,origine_id,description,ref_type,ref_id,organisme_id,service_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)",
            Guid.NewGuid().ToString("N"), U.D(date ?? Engine.Now), type, origine, origineId == 0 ? null : origineId, desc, refType, refId == 0 ? null : refId, orgId == 0 ? null : orgId, svc == 0 ? null : svc);

    public static void Notify(DbCtx c, string type, string importance, string titre, string texte, string refType, long refId)
    {
        if (c.Get("notif_off_" + type, "0") == "1") return;
        // anti-doublon : même type, même élément, non lue, dans l'heure simulée
        if (refId != 0 && c.Long("SELECT COUNT(*) FROM notifications WHERE type=@p0 AND ref_type=@p1 AND ref_id=@p2 AND lu=0 AND date>=@p3", type, refType, refId, U.D(Engine.Now.AddHours(-1))) > 0) return;
        c.Exec("INSERT INTO notifications(date,type,importance,titre,texte,ref_type,ref_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6)", U.D(Engine.Now), type, importance, titre, texte, refType, refId == 0 ? null : refId);
    }

    public static long Message(DbCtx c, long exp, long dest, string objet, string corps, long missionId = 0, long dossierId = 0, bool important = false, string expExt = null, long orgId = 0, string categorie = "Interne", long docId = 0, bool attente = false, string cc = null)
    {
        long id = c.Insert(@"INSERT INTO messages(date,exp_id,exp_ext,dest_id,cc,objet,corps,important,mission_id,dossier_id,organisme_id,categorie,document_id,attente_reponse)
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13)",
            U.D(Engine.Now), exp == 0 ? null : exp, expExt, dest, cc, objet, corps, important, missionId == 0 ? null : missionId, dossierId == 0 ? null : dossierId, orgId == 0 ? null : orgId, categorie, docId == 0 ? null : docId, attente);
        if (dest == Auth.CollabId && exp != Auth.CollabId)
            Notify(c, "Nouveau message", important ? "Haute" : "Normale", "Message de " + (exp != 0 ? Name(c, exp) : expExt), objet, "message", id);
        return id;
    }

    public static void MHist(DbCtx c, long mid, long auteur, string type, string texte)
        => c.Exec("INSERT INTO mission_hist(mission_id,date,auteur_id,type,texte) VALUES(@p0,@p1,@p2,@p3,@p4)", mid, U.D(Engine.Now), auteur == 0 ? null : auteur, type, texte);

    public static void DHist(DbCtx c, long did, long auteur, string texte)
        => c.Exec("INSERT INTO dossier_hist(dossier_id,date,auteur_id,texte) VALUES(@p0,@p1,@p2,@p3)", did, U.D(Engine.Now), auteur == 0 ? null : auteur, texte);

    // ------------------------------------------------------------ hiérarchie / droits de délégation
    public static bool EstSubordonne(DbCtx c, long collab, long chef)
    {
        long cur = c.Long("SELECT IFNULL(superieur_id,0) FROM collaborateurs WHERE id=@p0", collab);
        int guard = 0;
        while (cur != 0 && guard++ < 15)
        {
            if (cur == chef) return true;
            cur = c.Long("SELECT IFNULL(superieur_id,0) FROM collaborateurs WHERE id=@p0", cur);
        }
        return false;
    }

    public static void VerifierDelegation(DbCtx c, long emet, long dest)
    {
        if (emet == dest) throw new InvalidOperationException("Un collaborateur ne peut pas se déléguer une mission à lui-même.");
        if (c.Long("SELECT COUNT(*) FROM collaborateurs WHERE id=@p0 AND actif=1", dest) == 0) throw new InvalidOperationException("Destinataire inconnu ou inactif.");
        if (emet == ProcId) return;
        int ne = (int)c.Long("SELECT niveau FROM collaborateurs WHERE id=@p0", emet);
        if (ne <= 2 && dest != ProcId) return; // le procureur adjoint peut solliciter tout collaborateur (sauf le procureur)
        if (!EstSubordonne(c, dest, emet)) throw new UnauthorizedAccessException("Délégation refusée : le destinataire n'est pas placé sous l'autorité de l'émetteur.");
    }

    // ------------------------------------------------------------ missions
    public static long CreerMission(DbCtx c, long emet, long dest, string objet, string desc, string prio, DateTime ech, string instr, string conf,
        long serviceId, long dossierId, long parentId, long? docId, string origine)
    {
        if (string.IsNullOrWhiteSpace(objet)) throw new InvalidOperationException("L'objet de la mission est obligatoire.");
        VerifierDelegation(c, emet, dest);
        if (ech <= Engine.Now) throw new InvalidOperationException("L'échéance doit être postérieure à la date simulée actuelle.");
        // anti-doublon : même objet actif pour le même destinataire
        if (c.Long($"SELECT COUNT(*) FROM missions WHERE destinataire_id=@p0 AND objet=@p1 AND statut IN {SqlActifs} AND IFNULL(parent_id,0)=@p2", dest, objet, parentId) > 0)
            throw new InvalidOperationException("Une mission active portant le même objet existe déjà pour ce destinataire.");
        var prof = c.Rows("SELECT profil, IFNULL(charge,0) FROM collaborateurs WHERE id=@p0", dest)[0];
        double mult = Engine.ProfilMult(U.S(prof[0]));
        var rnd = Engine.Rnd;
        double heures = prio == "Haute" ? rnd.Next(5, 14) : prio == "Basse" ? rnd.Next(14, 34) : rnd.Next(8, 24);
        double vitesse = 100.0 / (heures * 60.0) * mult;
        int profondeur = parentId == 0 ? 0 : (int)c.Long("SELECT profondeur FROM missions WHERE id=@p0", parentId) + 1;
        long racine = parentId == 0 ? 0 : c.Long("SELECT racine_id FROM missions WHERE id=@p0", parentId);
        if (serviceId == 0) serviceId = c.Long("SELECT service_id FROM collaborateurs WHERE id=@p0", dest);
        string num = Seq(c, "M");
        long id = c.Insert(@"INSERT INTO missions(numero,parent_id,racine_id,profondeur,objet,description,emetteur_id,destinataire_id,service_id,priorite,statut,avancement,date_creation,echeance,instructions,confidentialite,dossier_id,document_id,vitesse,analyse_min,origine)
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,'Nouvelle',0,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17,@p18)",
            num, parentId == 0 ? null : parentId, racine == 0 ? null : racine, profondeur, objet, desc, emet, dest, serviceId, prio, U.D(Engine.Now), U.D(ech), instr, conf,
            dossierId == 0 ? null : dossierId, docId is null or 0 ? null : docId, vitesse, (int)(rnd.Next(20, 70) / Math.Max(0.6, mult)), origine);
        if (racine == 0) c.Exec("UPDATE missions SET racine_id=id WHERE id=@p0", id);
        MHist(c, id, emet, "Création", $"Mission créée par {Name(c, emet)} et confiée à {NameF(c, dest)}. Échéance : {U.Aff(U.D(ech))}.");
        if (dossierId != 0) DHist(c, dossierId, emet, $"Mission {num} créée : {objet}");
        Message(c, emet, dest, $"[{num}] {objet}", $"{desc}\n\nInstructions : {instr}\nPriorité : {prio} — Échéance : {U.Aff(U.D(ech))}", id, dossierId, prio == "Haute", categorie: "Mission");
        Event(c, "Nouvelle mission", Name(c, emet), emet, $"Mission {num} « {objet} » confiée à {Name(c, dest)}", "mission", id);
        c.Exec("UPDATE collaborateurs SET charge=MIN(100,charge+12) WHERE id=@p0", dest);
        return id;
    }

    static string StatutMission(DbCtx c, long id) => c.Str("SELECT statut FROM missions WHERE id=@p0", id);

    public static void ValiderMission(DbCtx c, long id, string commentaire)
    {
        Auth.Check("missions.valider");
        if (StatutMission(c, id) != "Rapport transmis") throw new InvalidOperationException("Seule une mission dont le rapport a été transmis peut être acceptée.");
        long dest = c.Long("SELECT destinataire_id FROM missions WHERE id=@p0", id);
        c.Exec("UPDATE missions SET statut='Validée', date_cloture=@p1 WHERE id=@p0", id, U.D(Engine.Now));
        MHist(c, id, Auth.CollabId, "Validation", "Rapport accepté. " + commentaire);
        Message(c, Auth.CollabId, dest, "Rapport accepté — " + c.Str("SELECT numero FROM missions WHERE id=@p0", id), "Votre rapport est accepté. " + commentaire, id, categorie: "Mission");
        Event(c, "Mission validée", Name(c, Auth.CollabId), Auth.CollabId, $"Mission {c.Str("SELECT numero FROM missions WHERE id=@p0", id)} validée", "mission", id);
    }

    public static void RefuserMission(DbCtx c, long id, string motif)
    {
        Auth.Check("missions.valider");
        var st = StatutMission(c, id);
        if (st is "Clôturée" or "Refusée" or "Validée") throw new InvalidOperationException("Cette mission est déjà terminée.");
        long dest = c.Long("SELECT destinataire_id FROM missions WHERE id=@p0", id);
        c.Exec("UPDATE missions SET statut='Refusée', date_cloture=@p1 WHERE id=@p0", id, U.D(Engine.Now));
        c.Exec($"UPDATE missions SET statut='Clôturée', date_cloture=@p1 WHERE racine_id=(SELECT racine_id FROM missions WHERE id=@p0) AND id<>@p0 AND statut IN {SqlActifs}", id, U.D(Engine.Now));
        MHist(c, id, Auth.CollabId, "Refus", "Rapport refusé. Motif : " + motif);
        Message(c, Auth.CollabId, dest, "Rapport refusé — " + c.Str("SELECT numero FROM missions WHERE id=@p0", id), "Le rapport est refusé. Motif : " + motif, id, categorie: "Mission");
        Event(c, "Mission refusée", Name(c, Auth.CollabId), Auth.CollabId, "Mission refusée : " + motif, "mission", id);
    }

    public static void DemanderModification(DbCtx c, long id, string texte)
    {
        Auth.Check("missions.valider");
        if (StatutMission(c, id) != "Rapport transmis") throw new InvalidOperationException("Une demande de modification n'est possible que sur un rapport transmis.");
        long dest = c.Long("SELECT destinataire_id FROM missions WHERE id=@p0", id);
        c.Exec("UPDATE missions SET statut='À modifier', avancement=80, retard_signale=0 WHERE id=@p0", id);
        c.Exec("INSERT INTO sous_taches(mission_id,libelle,assigne_id,statut,avancement) VALUES(@p0,@p1,@p2,'À faire',0)", id, "Intégrer les modifications demandées : " + texte, dest);
        MHist(c, id, Auth.CollabId, "Modification demandée", texte);
        Message(c, Auth.CollabId, dest, "Modifications demandées — " + c.Str("SELECT numero FROM missions WHERE id=@p0", id), texte, id, categorie: "Mission");
    }

    public static void CloturerMission(DbCtx c, long id)
    {
        Auth.Check("missions.valider");
        var st = StatutMission(c, id);
        if (st is "Clôturée" or "Refusée") throw new InvalidOperationException("Mission déjà clôturée.");
        c.Exec("UPDATE missions SET statut='Clôturée', date_cloture=@p1 WHERE id=@p0", id, U.D(Engine.Now));
        c.Exec($"UPDATE missions SET statut='Clôturée', date_cloture=@p1 WHERE parent_id=@p0 AND statut IN {SqlActifs}", id, U.D(Engine.Now));
        MHist(c, id, Auth.CollabId, "Clôture", "Mission clôturée par " + Name(c, Auth.CollabId));
        Event(c, "Mission clôturée", Name(c, Auth.CollabId), Auth.CollabId, "Clôture de la mission " + c.Str("SELECT numero FROM missions WHERE id=@p0", id), "mission", id);
    }

    public static void AjouterSousTache(DbCtx c, long mid, string libelle, long assigne)
    {
        Auth.Check("missions.creer");
        var st = StatutMission(c, mid);
        if (!StatutsActifs.Contains(st)) throw new InvalidOperationException("Impossible d'ajouter une sous-tâche à une mission terminée.");
        c.Exec("INSERT INTO sous_taches(mission_id,libelle,assigne_id) VALUES(@p0,@p1,@p2)", mid, libelle, assigne == 0 ? c.Long("SELECT destinataire_id FROM missions WHERE id=@p0", mid) : assigne);
        MHist(c, mid, Auth.CollabId, "Sous-tâche", "Ajout de la sous-tâche : " + libelle);
    }

    public static string Chaine(DbCtx c, long mid)
    {
        long racine = c.Long("SELECT racine_id FROM missions WHERE id=@p0", mid);
        var sb = new StringBuilder();
        void Rec(long id, int lvl)
        {
            var m = c.Rows("SELECT numero, objet, statut, avancement, emetteur_id, destinataire_id FROM missions WHERE id=@p0", id);
            if (m.Count == 0) return;
            var x = m[0];
            sb.AppendLine($"{new string(' ', lvl * 4)}{(lvl > 0 ? "└─ " : "")}{x[0]} — {x[1]} | {Name(c, U.L(x[4]))} → {Name(c, U.L(x[5]))} | {x[2]} | {U.Dv(x[3]):0}%");
            foreach (var st in c.Rows("SELECT libelle, assigne_id, statut, avancement FROM sous_taches WHERE mission_id=@p0", id))
                sb.AppendLine($"{new string(' ', lvl * 4 + 6)}• {st[0]} ({Name(c, U.L(st[1]))}) — {st[2]} {U.Dv(st[3]):0}%");
            foreach (var ch in c.Rows("SELECT id FROM missions WHERE parent_id=@p0 ORDER BY id", id)) Rec(U.L(ch[0]), lvl + 1);
        }
        Rec(racine == 0 ? mid : racine, 0);
        return sb.ToString();
    }

    // ------------------------------------------------------------ dossiers
    public static long CreerDossier(DbCtx c, string intitule, string cat, string resume, long svc, long referent, string prio, DateTime ech, string personnes, long org)
    {
        Auth.Check("dossiers.gerer");
        if (string.IsNullOrWhiteSpace(intitule)) throw new InvalidOperationException("L'intitulé est obligatoire.");
        string num = Seq(c, "DOS");
        long id = c.Insert(@"INSERT INTO dossiers(numero,intitule,categorie,resume,service_id,referent_id,organisme_id,personnes,statut,priorite,avancement,date_ouverture,echeance)
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,'Ouvert',@p8,0,@p9,@p10)", num, intitule, cat, resume, svc, referent, org == 0 ? null : org, personnes, prio, U.D(Engine.Now), U.D(ech));
        long enq = org != 0 && c.Str("SELECT categorie FROM organismes WHERE id=@p0", org) == "Service d'enquête" ? org : c.Long("SELECT id FROM organismes WHERE categorie='Service d''enquête' ORDER BY RANDOM() LIMIT 1");
        c.Exec("UPDATE dossiers SET faits=@p1, qualification=@p2, gravite=@p3, situation='Enquête en cours', service_enqueteur_id=@p4, date_transmission=@p5, date_faits=@p5 WHERE id=@p0",
            id, string.IsNullOrWhiteSpace(resume) ? "Faits à préciser par le service enquêteur." : resume, intitule + " (qualification simulée)", prio == "Haute" ? 3 : prio == "Normale" ? 2 : 1, enq, U.D(Engine.Now));
        c.Exec("INSERT INTO dossier_chrono(dossier_id,date,libelle,source) VALUES(@p0,@p1,'Ouverture de la procédure par le parquet','Parquet')", id, U.D(Engine.Now));
        DHist(c, id, Auth.CollabId, "Ouverture du dossier par " + Name(c, Auth.CollabId));
        Event(c, "Nouveau dossier", Name(c, Auth.CollabId), Auth.CollabId, $"Création du dossier {num} — {intitule}", "dossier", id, svc: svc);
        if (referent != 0) Message(c, Auth.CollabId, referent, $"Dossier {num} — vous êtes désigné référent", $"Vous êtes désigné référent du dossier {num} : {intitule}.", 0, id, categorie: "Dossier");
        return id;
    }

    public static void ModifierDossier(DbCtx c, long id, string intitule, string cat, string resume, long svc, long referent, string prio, DateTime ech, string statut, string personnes, long org)
    {
        Auth.Check("dossiers.gerer");
        var old = c.Rows("SELECT referent_id, statut FROM dossiers WHERE id=@p0", id)[0];
        c.Exec("UPDATE dossiers SET intitule=@p1,categorie=@p2,resume=@p3,service_id=@p4,referent_id=@p5,priorite=@p6,echeance=@p7,statut=@p8,personnes=@p9,organisme_id=@p10 WHERE id=@p0",
            id, intitule, cat, resume, svc, referent, prio, U.D(ech), statut, personnes, org == 0 ? null : org);
        if (statut == "Clôturé" && U.S(old[1]) != "Clôturé") c.Exec("UPDATE dossiers SET date_cloture=@p1, avancement=100 WHERE id=@p0", id, U.D(Engine.Now));
        DHist(c, id, Auth.CollabId, "Modification de la fiche du dossier");
        if (U.L(old[0]) != referent && referent != 0)
        {
            DHist(c, id, Auth.CollabId, "Nouveau référent : " + NameF(c, referent));
            Message(c, Auth.CollabId, referent, "Affectation d'un dossier", $"Vous êtes désormais référent du dossier {c.Str("SELECT numero FROM dossiers WHERE id=@p0", id)}.", 0, id, categorie: "Dossier");
        }
    }

    public static void ArchiverDossier(DbCtx c, long id, bool archive)
    {
        Auth.Check("dossiers.gerer");
        c.Exec("UPDATE dossiers SET archive=@p1 WHERE id=@p0", id, archive);
        DHist(c, id, Auth.CollabId, archive ? "Dossier archivé" : "Dossier désarchivé");
        Event(c, archive ? "Dossier archivé" : "Dossier désarchivé", Name(c, Auth.CollabId), Auth.CollabId, "Dossier " + c.Str("SELECT numero FROM dossiers WHERE id=@p0", id), "dossier", id);
    }

    public static void NoteDossier(DbCtx c, long id, string texte)
    {
        Auth.Check("dossiers.gerer");
        c.Exec("INSERT INTO dossier_notes(dossier_id,date,auteur_id,texte) VALUES(@p0,@p1,@p2,@p3)", id, U.D(Engine.Now), Auth.CollabId, texte);
        DHist(c, id, Auth.CollabId, "Ajout d'une note");
    }

    public static string FicheDossier(DbCtx c, long id)
    {
        var d = c.Rows(@"SELECT d.numero,d.intitule,d.categorie,d.resume,s.nom,(SELECT prenom||' '||nom FROM collaborateurs WHERE id=d.referent_id),d.statut,d.priorite,d.avancement,d.date_ouverture,d.echeance,d.personnes,(SELECT nom FROM organismes WHERE id=d.organisme_id)
            FROM dossiers d LEFT JOIN services s ON s.id=d.service_id WHERE d.id=@p0", id)[0];
        var sb = new StringBuilder();
        sb.AppendLine("FICHE RÉCAPITULATIVE DE DOSSIER"); sb.AppendLine(U.Mention); sb.AppendLine();
        sb.AppendLine($"Numéro interne : {d[0]}\nIntitulé : {d[1]}\nCatégorie : {d[2]}\nService responsable : {d[4]}\nRéférent : {d[5]}\nOrganisme à l'origine : {d[12]}\nStatut : {d[6]} — Priorité : {d[7]} — Avancement : {d[8]}%\nOuverture : {U.Aff(d[9])} — Échéance : {U.Aff(d[10])}\nPersonnes associées : {d[11]}\n");
        sb.AppendLine("Résumé :\n" + d[3] + "\n");
        sb.AppendLine("Notes :");
        foreach (var n in c.Rows("SELECT date, (SELECT prenom||' '||nom FROM collaborateurs WHERE id=auteur_id), texte FROM dossier_notes WHERE dossier_id=@p0 ORDER BY date", id)) sb.AppendLine($" - {U.Aff(n[0])} ({n[1]}) : {n[2]}");
        sb.AppendLine("\nMissions liées :");
        foreach (var m in c.Rows("SELECT numero, objet, statut, avancement FROM missions WHERE dossier_id=@p0", id)) sb.AppendLine($" - {m[0]} {m[1]} — {m[2]} ({U.Dv(m[3]):0}%)");
        sb.AppendLine("\nHistorique :");
        foreach (var h in c.Rows("SELECT date, texte FROM dossier_hist WHERE dossier_id=@p0 ORDER BY date", id)) sb.AppendLine($" - {U.Aff(h[0])} : {h[1]}");
        return sb.ToString();
    }

    // ------------------------------------------------------------ messagerie
    public static void Thread(DbCtx c, long msgId, long parentId)
    {
        if (parentId == 0) return;
        long conv = c.Long("SELECT IFNULL(conversation_id, id) FROM messages WHERE id=@p0", parentId);
        if (conv == 0) return;
        c.Exec("UPDATE messages SET conversation_id=@p1 WHERE id IN (@p0, @p2) OR conversation_id IS NULL AND id=@p1", msgId, conv, parentId);
    }

    public static long EnvoyerMessage(DbCtx c, long dest, string cc, string objet, string corps, bool important, long missionId, long dossierId, long docId, bool brouillon, long parentId = 0)
    {
        Auth.Check("messages.envoyer");
        if (!brouillon && dest == 0) throw new InvalidOperationException("Veuillez choisir un destinataire.");
        if (string.IsNullOrWhiteSpace(objet)) objet = "(sans objet)";
        long id = c.Insert(@"INSERT INTO messages(date,exp_id,dest_id,cc,objet,corps,important,brouillon,mission_id,dossier_id,document_id,attente_reponse,categorie,lu)
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,'Interne',0)", U.D(Engine.Now), Auth.CollabId, dest == 0 ? null : dest, cc, objet, corps, important, brouillon,
            missionId == 0 ? null : missionId, dossierId == 0 ? null : dossierId, docId == 0 ? null : docId, !brouillon && dest != Auth.CollabId);
        if (!brouillon && !string.IsNullOrWhiteSpace(cc))
        {
            foreach (var part in cc.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (long.TryParse(part, out long ccId) && ccId != dest)
                    c.Exec(@"INSERT INTO messages(date,exp_id,dest_id,cc,objet,corps,important,mission_id,dossier_id,categorie,etat_exp) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,'Copie','masque')",
                        U.D(Engine.Now), Auth.CollabId, ccId, cc, objet, corps, important, missionId == 0 ? null : missionId, dossierId == 0 ? null : dossierId);
        }
        if (!brouillon) Thread(c, id, parentId);
        if (!brouillon) Event(c, "Message envoyé", Name(c, Auth.CollabId), Auth.CollabId, $"Message « {objet} » à {Name(c, dest)}", "message", id);
        return id;
    }

    // ------------------------------------------------------------ réunions
    public static List<string> Conflits(DbCtx c, DateTime deb, DateTime fin, IEnumerable<long> parts, long exclure)
    {
        var res = new List<string>();
        foreach (var p in parts.Distinct())
        {
            var r = c.Rows(@"SELECT r.titre, r.debut FROM reunions r JOIN reunion_participants rp ON rp.reunion_id=r.id
                WHERE rp.collab_id=@p0 AND r.id<>@p1 AND r.statut IN ('Planifiée','Proposée','En cours') AND r.debut<@p3 AND r.fin>@p2 LIMIT 1", p, exclure, U.D(deb), U.D(fin));
            if (r.Count > 0) res.Add($"{Name(c, p)} : déjà convoqué(e) à « {r[0][0]} » ({U.Aff(r[0][1])})");
        }
        return res;
    }

    public static long EnregistrerReunion(DbCtx c, long id, string titre, DateTime deb, DateTime fin, string lieu, long org, string odj, string prio, int rappel, List<long> parts, long dossierId, string statut = "Planifiée")
    {
        Auth.Check("agenda.gerer");
        if (fin <= deb) throw new InvalidOperationException("L'heure de fin doit être postérieure à l'heure de début.");
        if (!parts.Contains(org)) parts.Add(org);
        var conf = Conflits(c, deb, fin, parts, id);
        if (conf.Count > 0) throw new InvalidOperationException("Conflits d'agenda détectés :\n" + string.Join("\n", conf));
        if (id == 0)
        {
            id = c.Insert("INSERT INTO reunions(titre,debut,fin,lieu,organisateur_id,ordre_du_jour,priorite,statut,rappel_min,dossier_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)",
                titre, U.D(deb), U.D(fin), lieu, org, odj, prio, statut, rappel, dossierId == 0 ? null : dossierId);
            Event(c, "Réunion planifiée", Name(c, org), org, $"Réunion « {titre} » le {U.Aff(U.D(deb))}", "reunion", id);
        }
        else
        {
            c.Exec("UPDATE reunions SET titre=@p1,debut=@p2,fin=@p3,lieu=@p4,organisateur_id=@p5,ordre_du_jour=@p6,priorite=@p7,rappel_min=@p8,dossier_id=@p9,rappel_envoye=0 WHERE id=@p0",
                id, titre, U.D(deb), U.D(fin), lieu, org, odj, prio, rappel, dossierId == 0 ? null : dossierId);
            c.Exec("DELETE FROM reunion_participants WHERE reunion_id=@p0", id);
            Event(c, "Réunion modifiée", Name(c, Auth.CollabId), Auth.CollabId, $"Réunion « {titre} » modifiée", "reunion", id);
        }
        foreach (var p in parts.Distinct()) c.Exec("INSERT OR IGNORE INTO reunion_participants(reunion_id,collab_id,reponse) VALUES(@p0,@p1,'Invité')", id, p);
        foreach (var p in parts.Distinct().Where(p => p != Auth.CollabId).Take(40))
            c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,categorie,etat_exp) VALUES(@p0,@p1,@p2,@p3,@p4,'Convocation','masque')", U.D(Engine.Now), Auth.CollabId, p, "Convocation : " + titre, $"Réunion le {U.Aff(U.D(deb))} — {lieu}\n\nOrdre du jour :\n{odj}");
        return id;
    }

    public static string GenererCompteRendu(DbCtx c, long rid, long auteur)
    {
        var r = c.Rows("SELECT titre, debut, fin, lieu, ordre_du_jour, organisateur_id FROM reunions WHERE id=@p0", rid)[0];
        var parts = c.Rows("SELECT c.prenom||' '||c.nom||' — '||c.fonction FROM reunion_participants rp JOIN collaborateurs c ON c.id=rp.collab_id WHERE rp.reunion_id=@p0", rid).Select(x => U.S(x[0])).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("COMPTE RENDU DE RÉUNION"); sb.AppendLine(U.Mention); sb.AppendLine();
        sb.AppendLine($"Objet : {r[0]}\nDate : {U.Aff(r[1])} – {U.P(r[2]):HH:mm}\nLieu : {r[3]}\nOrganisateur : {NameF(c, U.L(r[5]))}\n");
        sb.AppendLine("Participants :"); foreach (var p in parts) sb.AppendLine(" - " + p);
        sb.AppendLine("\nOrdre du jour et relevé de décisions :");
        int i = 1;
        foreach (var l in U.S(r[4]).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            sb.AppendLine($" {l.Trim()}");
            sb.AppendLine($"   → {Engine.Rnd.Pick(Engine.Decisions)}");
            i++;
        }
        long actives = c.Long($"SELECT COUNT(*) FROM missions WHERE statut IN {SqlActifs}");
        long retards = c.Long($"SELECT COUNT(*) FROM missions WHERE statut IN {SqlActifs} AND echeance<@p0", U.D(Engine.Now));
        sb.AppendLine($"\nÉléments de contexte (données du système à la date de la réunion) : {actives} missions actives, dont {retards} en retard.");
        string txt = sb.ToString();
        c.Exec("UPDATE reunions SET compte_rendu=@p1 WHERE id=@p0", rid, txt);
        long did = c.Insert("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif,reunion_id) VALUES(@p0,@p1,'Comptes rendus',@p2,@p3,@p4,@p4,@p5)",
            Seq(c, "DOC"), "Compte rendu — " + r[0], txt, auteur, U.D(Engine.Now), rid);
        c.Exec("UPDATE reunions SET document_id=@p1 WHERE id=@p0", rid, did);
        return txt;
    }

    // ------------------------------------------------------------ documents
    public static long EnregistrerDocument(DbCtx c, long id, string titre, string cat, string contenu, long dossierId, long missionId)
    {
        Auth.Check("documents.gerer");
        if (string.IsNullOrWhiteSpace(titre)) throw new InvalidOperationException("Le titre est obligatoire.");
        if (id == 0)
        {
            id = c.Insert("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif,dossier_id,mission_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p5,@p6,@p7)",
                Seq(c, "DOC"), titre, cat, contenu, Auth.CollabId, U.D(Engine.Now), dossierId == 0 ? null : dossierId, missionId == 0 ? null : missionId);
            Event(c, "Document créé", Name(c, Auth.CollabId), Auth.CollabId, "Document « " + titre + " »", "document", id);
        }
        else
        {
            c.Exec("UPDATE documents SET titre=@p1,categorie=@p2,contenu=@p3,dossier_id=@p4,mission_id=@p5,date_modif=@p6,version=version+1 WHERE id=@p0",
                id, titre, cat, contenu, dossierId == 0 ? null : dossierId, missionId == 0 ? null : missionId, U.D(Engine.Now));
        }
        if (dossierId != 0) DHist(c, dossierId, Auth.CollabId, "Document associé : " + titre);
        return id;
    }

    // ------------------------------------------------------------ personnel
    public static void Affecter(DbCtx c, long collab, long svc, long sup, string motif)
    {
        Auth.Check("personnel.modifier");
        if (collab == ProcId) throw new InvalidOperationException("Le procureur ne peut pas être réaffecté.");
        if (sup == collab) throw new InvalidOperationException("Un collaborateur ne peut pas être son propre supérieur.");
        if (sup != 0 && EstSubordonne(c, sup, collab)) throw new InvalidOperationException("Affectation impossible : le supérieur choisi est placé sous l'autorité de ce collaborateur (boucle hiérarchique).");
        var old = c.Rows("SELECT service_id, superieur_id FROM collaborateurs WHERE id=@p0", collab)[0];
        c.Exec("UPDATE collaborateurs SET service_id=@p1, superieur_id=@p2 WHERE id=@p0", collab, svc, sup == 0 ? null : sup);
        c.Exec("INSERT INTO affectations_hist(date,collab_id,ancien_service,nouveau_service,ancien_sup,nouveau_sup,motif,auteur) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)",
            U.D(Engine.Now), collab, old[0], svc, old[1], sup == 0 ? null : sup, motif, Auth.Login == "" ? "Système" : Auth.Login);
        string txt = $"{NameF(c, collab)} : {c.Str("SELECT nom FROM services WHERE id=@p0", U.L(old[0]))} → {c.Str("SELECT nom FROM services WHERE id=@p0", svc)}. Motif : {motif}";
        Admin(c, "Changement d'affectation", txt);
        Event(c, "Changement d'organisation", "Ressources humaines", 0, txt, "collaborateur", collab, svc: svc);
    }

    public static void ModifierFiche(DbCtx c, long id, string fonction, string competences, string resp, int autonomie, int hd, int hf, string dispo, string notes)
    {
        Auth.Check("personnel.modifier");
        c.Exec("UPDATE collaborateurs SET fonction=@p1, competences=@p2, responsabilites=@p3, autonomie=@p4, horaire_debut=@p5, horaire_fin=@p6, dispo=@p7, notes=@p8 WHERE id=@p0", id, fonction, competences, resp, autonomie, hd, hf, dispo, notes);
        Admin(c, "Modification de fiche", "Fiche modifiée : " + NameF(c, id));
    }

    // ------------------------------------------------------------ demandes
    public static void TraiterDemande(DbCtx c, long id, bool accord, string commentaire)
    {
        Auth.Check("missions.valider");
        var d = c.Rows("SELECT demandeur_id, type, objet, statut, service_id FROM demandes WHERE id=@p0", id)[0];
        if (U.S(d[3]) != "En attente") throw new InvalidOperationException("Cette demande a déjà été traitée.");
        c.Exec("UPDATE demandes SET statut=@p1, reponse=@p2, date_reponse=@p3 WHERE id=@p0", id, accord ? "Accordée" : "Refusée", commentaire, U.D(Engine.Now));
        long dem = U.L(d[0]);
        string effet = "";
        if (accord && U.S(d[1]) == "Demande de renfort")
        {
            long svc = U.L(d[4]);
            var donneur = c.Rows(@"SELECT c.id, c.service_id FROM collaborateurs c WHERE c.niveau>=7 AND c.service_id<>@p0 AND c.actif=1 AND c.service_id NOT IN (SELECT id FROM services WHERE code IN ('CABP','CABA','SECP'))
                AND NOT EXISTS(SELECT 1 FROM collaborateurs s WHERE s.superieur_id=c.id)
                ORDER BY (SELECT AVG(charge) FROM collaborateurs x WHERE x.service_id=c.service_id) ASC, c.charge ASC LIMIT 1", svc);
            if (donneur.Count > 0)
            {
                long lead = c.Long("SELECT id FROM collaborateurs WHERE service_id=@p0 AND niveau=6 ORDER BY charge LIMIT 1", svc);
                if (lead == 0) lead = c.Long("SELECT responsable_id FROM services WHERE id=@p0", svc);
                long cid = U.L(donneur[0][0]);
                var old = donneur[0][1];
                c.Exec("UPDATE collaborateurs SET service_id=@p1, superieur_id=@p2 WHERE id=@p0", cid, svc, lead);
                c.Exec("INSERT INTO affectations_hist(date,collab_id,ancien_service,nouveau_service,nouveau_sup,motif,auteur) VALUES(@p0,@p1,@p2,@p3,@p4,'Renfort accordé par le procureur',@p5)", U.D(Engine.Now), cid, old, svc, lead, Auth.Login);
                effet = $"\nRenfort mis en œuvre : {NameF(c, cid)} rejoint le service.";
                Admin(c, "Renfort", effet.Trim());
                Event(c, "Changement d'organisation", "Ressources humaines", 0, effet.Trim(), "collaborateur", cid, svc: svc);
            }
        }
        Message(c, Auth.CollabId, dem, $"Réponse à votre demande : {d[2]}", (accord ? "Votre demande est accordée." : "Votre demande n'est pas accordée.") + "\n" + commentaire + effet, categorie: "Décision");
        Event(c, "Décision", Name(c, Auth.CollabId), Auth.CollabId, $"{d[1]} « {d[2]} » : {(accord ? "accordée" : "refusée")}", "demande", id);
    }

    // ------------------------------------------------------------ téléphonie
    public static long AppelSortant(DbCtx c, string interlocuteur, long collab, long interId, long org, string motif, long dossierId)
    {
        Auth.Check("telephonie");
        string statut = "Terminé"; int duree = Engine.Rnd.Next(60, 900);
        string notes = "";
        if (collab != 0)
        {
            var dispo = c.Str("SELECT dispo FROM collaborateurs WHERE id=@p0", collab);
            if (dispo is "Absent" or "Hors horaires") { statut = "Sans réponse"; duree = 0; notes = $"Interlocuteur indisponible ({dispo}). Message laissé."; c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,categorie,etat_exp,attente_reponse) VALUES(@p0,@p1,@p2,'Appel du procureur',@p3,'Téléphone','masque',1)", U.D(Engine.Now), Auth.CollabId, collab, "Le procureur a tenté de vous joindre. Motif : " + motif); }
            else if (dispo == "En réunion") { statut = "Rappel prévu"; duree = 0; notes = "Interlocuteur en réunion — rappel prévu."; }
            else notes = $"Échange avec {NameF(c, collab)} : {Engine.Rnd.Pick(Engine.ReponsesTel)}";
        }
        else notes = $"Échange avec {interlocuteur} : {Engine.Rnd.Pick(Engine.ReponsesTel)}";
        long id = c.Insert("INSERT INTO appels(date,sens,statut,interlocuteur,interlocuteur_id,organisme_id,collab_id,duree,motif,notes,dossier_id) VALUES(@p0,'Sortant',@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)",
            U.D(Engine.Now), statut, interlocuteur, interId == 0 ? null : interId, org == 0 ? null : org, collab == 0 ? null : collab, duree, motif, notes, dossierId == 0 ? null : dossierId);
        if (dossierId != 0) DHist(c, dossierId, Auth.CollabId, "Appel sortant : " + motif);
        Event(c, "Appel sortant", Name(c, Auth.CollabId), Auth.CollabId, $"Appel vers {interlocuteur} — {statut}", "appel", id, orgId: org);
        return id;
    }

    // ------------------------------------------------------------ synthèses (données réelles)
    public static string Synthese(DbCtx c)
    {
        var now = U.D(Engine.Now);
        var sb = new StringBuilder();
        sb.AppendLine($"Situation au {U.Aff(now)} :");
        sb.AppendLine($" - Missions actives : {c.Long($"SELECT COUNT(*) FROM missions WHERE statut IN {SqlActifs}")} (dont {c.Long($"SELECT COUNT(*) FROM missions WHERE statut IN {SqlActifs} AND echeance<@p0", now)} en retard)");
        sb.AppendLine($" - Rapports en attente de votre décision : {c.Long("SELECT COUNT(*) FROM missions WHERE statut='Rapport transmis' AND emetteur_id=1")}");
        sb.AppendLine($" - Dossiers ouverts : {c.Long("SELECT COUNT(*) FROM dossiers WHERE statut<>'Clôturé' AND archive=0")}");
        sb.AppendLine($" - Demandes en attente : {c.Long("SELECT COUNT(*) FROM demandes WHERE statut='En attente'")}");
        sb.AppendLine($" - Collaborateurs absents : {c.Long("SELECT COUNT(*) FROM collaborateurs WHERE dispo='Absent'")}");
        var top = c.Rows("SELECT s.nom, ROUND(AVG(c.charge)) FROM collaborateurs c JOIN services s ON s.id=c.service_id GROUP BY s.id ORDER BY AVG(c.charge) DESC LIMIT 3");
        sb.AppendLine(" - Services les plus chargés : " + string.Join(", ", top.Select(t => $"{t[0]} ({t[1]}%)")));
        return sb.ToString();
    }
}
