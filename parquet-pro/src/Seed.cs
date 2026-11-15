namespace ParquetPro;

public static class Seed
{
    public static readonly string[] Prenoms = { "Claire", "Julien", "Sophie", "Thomas", "Camille", "Nicolas", "Isabelle", "Antoine", "Nathalie", "Mathieu", "Céline", "Pierre", "Aurélie", "Sébastien", "Émilie", "Laurent", "Hélène", "François", "Caroline", "Olivier",
        "Valérie", "Guillaume", "Sandrine", "Vincent", "Marion", "Romain", "Élodie", "Alexandre", "Delphine", "Maxime", "Stéphanie", "Benoît", "Anne", "Christophe", "Laure", "Frédéric", "Agnès", "Damien", "Pauline", "Jérôme",
        "Virginie", "Arnaud", "Mélanie", "Cédric", "Charlotte", "Fabien", "Juliette", "Yann", "Manon", "Patrick", "Sarah", "David", "Lucie", "Éric", "Chloé", "Bruno", "Alice", "Philippe", "Margaux", "Hugo",
        "Léa", "Xavier", "Inès", "Gilles", "Audrey", "Raphaël", "Marine", "Thierry", "Justine", "Adrien", "Florence", "Lucas", "Mathilde", "Paul", "Elsa", "Jean", "Nadia", "Karim", "Sonia", "Samuel",
        "Amandine", "Grégoire", "Bérénice", "Quentin", "Coralie", "Florian", "Estelle", "Bastien", "Gaëlle", "Rémi", "Myriam", "Loïc", "Ophélie", "Tristan", "Anaïs", "Victor", "Noémie", "Édouard", "Clémence", "Martin" };
    public static readonly string[] Noms = { "Martin", "Bernard", "Dubois", "Durand", "Lefebvre", "Moreau", "Laurent", "Simon", "Michel", "Garcia", "David", "Bertrand", "Roux", "Vincent", "Fournier", "Morel", "Girard", "André", "Mercier", "Dupont",
        "Lambert", "Bonnet", "François", "Martinez", "Legrand", "Garnier", "Faure", "Rousseau", "Blanc", "Guérin", "Muller", "Henry", "Roussel", "Nicolas", "Perrin", "Morin", "Mathieu", "Clément", "Gauthier", "Dumont",
        "Lopez", "Fontaine", "Chevalier", "Robin", "Masson", "Sanchez", "Gérard", "Nguyen", "Boyer", "Denis", "Lemaire", "Duval", "Joly", "Gautier", "Roger", "Roche", "Roy", "Noël", "Meyer", "Lucas",
        "Meunier", "Jean", "Perez", "Marchand", "Dufour", "Blanchard", "Marie", "Barbier", "Brun", "Dumas", "Brunet", "Schmitt", "Leroux", "Colin", "Fernandez", "Pierre", "Renard", "Arnaud", "Rolland", "Caron",
        "Aubert", "Giraud", "Leclerc", "Vidal", "Bourgeois", "Renaud", "Lemoine", "Picard", "Gaillard", "Philippe", "Leclercq", "Lacroix", "Fabre", "Dupuis", "Olivier", "Rodriguez", "Da Silva", "Hubert", "Louis", "Charles",
        "Guillot", "Rivière", "Le Gall", "Guillaume", "Adam", "Rey", "Moulin", "Gonzalez", "Berger", "Lecomte", "Menard", "Fleury", "Deschamps", "Carpentier", "Julien", "Benoît", "Paris", "Maillard", "Marchal", "Aubry",
        "Vasseur", "Le Roux", "Renault", "Jacquet", "Collet", "Prévost", "Poirier", "Charpentier", "Royer", "Huet", "Baron", "Dupuy", "Pons", "Paul", "Laine", "Carré", "Breton", "Rémy", "Schneider", "Perrot",
        "Guyot", "Barre", "Marty", "Cousin", "Tessier", "Lebrun", "Hamel", "Besson", "Etienne", "Langlois" };

    record Svc(string Code, string Nom, string Cat, int Eff, string Rep, string Head, string[] Titles, string[] Comp);

    static readonly Svc[] Services =
    {
        new("CABP", "Cabinet du procureur", "Direction", 12, "-", "Directeur de cabinet", new[]{"Conseiller technique","Chargé de mission","Rédacteur","Attaché de cabinet"}, new[]{"Synthèse","Rédaction","Coordination","Analyse juridique"}),
        new("CABA", "Cabinet du procureur adjoint", "Direction", 14, "-", "Chef de cabinet adjoint", new[]{"Conseiller technique","Chargé de mission","Analyste","Rédacteur"}, new[]{"Coordination","Synthèse","Suivi des délais","Analyse juridique"}),
        new("SECP", "Secrétariat particulier", "Direction", 12, "P", "Secrétaire particulier", new[]{"Assistant de direction","Gestionnaire d'agenda","Chargé du courrier","Standardiste"}, new[]{"Gestion d'agenda","Accueil","Courrier","Organisation"}),
        new("SG", "Secrétariat général", "Administration", 18, "P", "Secrétaire général", new[]{"Secrétaire général adjoint","Chargé de coordination","Gestionnaire administratif","Rédacteur"}, new[]{"Gestion administrative","Coordination","Bilans","Pilotage"}),
        new("GEST", "Service de gestion des dossiers", "Opérationnel", 35, "A", "Chef du service de gestion des dossiers", new[]{"Gestionnaire de dossiers","Greffier (simulation)","Rédacteur","Analyste"}, new[]{"Procédure (simulation)","Suivi des délais","Rédaction","Analyse juridique"}),
        new("AUD", "Service des audiences et du calendrier", "Opérationnel", 20, "A", "Chef du service des audiences", new[]{"Gestionnaire du calendrier","Audiencier","Planificateur","Agent d'audience"}, new[]{"Planification","Calendrier","Logistique","Coordination"}),
        new("ENQ", "Service des relations avec les enquêteurs", "Opérationnel", 30, "A", "Chef du service des relations avec les enquêteurs", new[]{"Chargé de liaison","Coordinateur d'enquêtes (simulation)","Analyste","Rédacteur"}, new[]{"Relations partenaires","Coordination","Analyse","Permanence"}),
        new("COORD", "Service de coordination et de suivi", "Opérationnel", 25, "A", "Chef du service de coordination", new[]{"Coordinateur","Chargé de suivi","Analyste","Gestionnaire de projet"}, new[]{"Coordination","Gestion de projet","Suivi des délais","Synthèse"}),
        new("AFFG", "Service des affaires générales", "Administration", 20, "SG", "Chef du service des affaires générales", new[]{"Gestionnaire logistique","Agent administratif","Chargé des moyens","Gestionnaire budgétaire"}, new[]{"Logistique","Budget","Gestion administrative","Achats"}),
        new("STAT", "Service des statistiques et du pilotage", "Pilotage", 12, "SG", "Chef du service des statistiques", new[]{"Statisticien","Contrôleur de gestion","Analyste de données","Chargé de tableaux de bord"}, new[]{"Statistiques","Pilotage","Analyse de données","Tableaux de bord"}),
        new("DOC", "Service documentaire et des archives", "Administration", 18, "SG", "Chef du service documentaire", new[]{"Archiviste","Documentaliste","Agent d'archives","Gestionnaire documentaire"}, new[]{"Archivage","Documentation","Classement","Numérisation"}),
        new("COM", "Service de communication institutionnelle", "Direction", 12, "P", "Responsable de la communication", new[]{"Chargé de communication","Rédacteur","Chargé des relations presse (simulation)","Webmestre"}, new[]{"Communication","Rédaction","Relations publiques","Veille"}),
        new("RH", "Service des ressources humaines", "Administration", 12, "SG", "Responsable des ressources humaines", new[]{"Gestionnaire RH","Chargé de formation","Chargé du recrutement","Gestionnaire paie"}, new[]{"Ressources humaines","Formation","Gestion des effectifs","Droit social"}),
        new("INFO", "Service informatique et administration", "Support", 10, "SG", "Responsable informatique", new[]{"Administrateur systèmes","Technicien support","Développeur","Chargé de sécurité informatique"}, new[]{"Informatique","Réseaux","Sécurité","Support"}),
        new("CI", "Service de contrôle interne", "Pilotage", 10, "P", "Responsable du contrôle interne", new[]{"Auditeur interne","Contrôleur","Chargé de qualité","Analyste des risques"}, new[]{"Audit","Contrôle","Qualité","Analyse des risques"}),
        new("EDC", "Équipe spécialisée — Dossiers complexes", "Transversal", 10, "A", "Chef de l'équipe dossiers complexes", new[]{"Analyste dossiers complexes","Chargé d'études","Rédacteur spécialisé"}, new[]{"Dossiers complexes","Analyse financière (simulation)","Synthèse"}),
        new("UTC", "Unité transversale de coordination interservices", "Transversal", 8, "A", "Chef de l'unité de coordination", new[]{"Coordinateur interservices","Chargé de liaison"}, new[]{"Coordination","Médiation","Gestion de projet"}),
        new("CAV", "Cellule d'appui et de veille", "Transversal", 8, "A", "Chef de la cellule d'appui et de veille", new[]{"Veilleur","Analyste","Chargé d'appui"}, new[]{"Veille","Analyse","Appui opérationnel"}),
        new("EMF", "Équipe spécialisée mineurs et familles", "Transversal", 7, "A", "Chef de l'équipe mineurs et familles", new[]{"Chargé de dossiers mineurs (simulation)","Coordinateur partenarial"}, new[]{"Mineurs (simulation)","Partenariats","Rédaction"}),
        new("PCT", "Pôle de coordination territoriale", "Transversal", 7, "A", "Chef du pôle territorial", new[]{"Chargé de coordination territoriale","Correspondant local"}, new[]{"Territoire","Partenariats","Coordination"}),
    };

    static readonly string[] Profils = { "Très réactif", "Méthodique", "Spécialiste des dossiers complexes", "Très organisé", "Signale rapidement les difficultés", "Sollicite fréquemment son responsable" };
    static readonly string[] Assist = { "Assistant administratif", "Agent administratif", "Secrétaire" };

    public static readonly string[] Categories = { "Atteintes aux biens", "Atteintes aux personnes", "Économique et financier", "Circulation routière", "Stupéfiants", "Environnement", "Mineurs", "Exécution des peines", "Parquet civil", "Coopération" };

    public static void Run(Action<int, string> progress)
    {
        var r = new Random(20261116);
        using var c = new DbCtx(true);
        progress(10, "Initialisation des paramètres du moteur…");
        var start = DateTime.Today.AddDays(1);
        while (start.DayOfWeek != DayOfWeek.Monday) start = start.AddDays(1);
        start = start.AddHours(8);
        var defaults = new Dictionary<string, object>
        {
            ["sim_time"] = U.D(start), ["vitesse"] = 60, ["moteur_actif"] = 1, ["autonomie_defaut"] = 3, ["frequence"] = "1.0",
            ["backup_min"] = 30, ["reprise"] = "auto", ["fond"] = 0, ["demarrage_windows"] = 0, ["densite"] = "Normale",
            ["panneau_droit"] = 1, ["favoris"] = "Cabinet du procureur,Décisions,Dossiers,Messagerie", ["derniere_fermeture"] = "", ["seed_version"] = 1,
            ["popup_notifs"] = 1, ["heure_debut_journee"] = 8, ["sequence"] = 0
        };
        foreach (var kv in defaults) c.Set(kv.Key, kv.Value);
        Engine.SetNow(start);

        progress(20, "Création des services…");
        var svcIds = new Dictionary<string, long>();
        foreach (var s in Services)
            svcIds[s.Code] = c.Insert("INSERT INTO services(code,nom,categorie,effectif_cible,description) VALUES(@p0,@p1,@p2,@p3,@p4)", s.Code, s.Nom, s.Cat, s.Eff, $"{s.Nom} — unité de l'organisation simulée PARQUET PRO.");

        progress(35, "Génération des 300 collaborateurs…");
        var used = new HashSet<string>();
        int mat = 0;
        long Add(string fonction, int niveau, long svc, long sup, string[] comp)
        {
            string p, n;
            do { p = r.Pick(Prenoms); n = r.Pick(Noms); } while (!used.Add(p + "|" + n));
            mat++;
            var comps = string.Join(", ", comp.OrderBy(_ => r.Next()).Take(2).Concat(new[] { r.Pick(new[] { "Rédaction", "Synthèse", "Organisation", "Communication", "Gestion de projet", "Suivi des délais" }) }).Distinct());
            int deb = r.Next(0, 3) == 0 ? 8 : 9; int fin = deb + 8 + r.Next(0, 3);
            string mail = (Strip(p) + "." + Strip(n)).ToLowerInvariant() + "@parquetpro.sim";
            return c.Insert(@"INSERT INTO collaborateurs(matricule,prenom,nom,fonction,niveau,service_id,superieur_id,competences,responsabilites,profil,autonomie,horaire_debut,horaire_fin,charge_base,telephone,email,date_entree)
                VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,3,@p10,@p11,@p12,@p13,@p14,@p15)",
                $"PP-{mat:0000}", p, n.ToUpperInvariant(), fonction, niveau, svc, sup == 0 ? null : sup, comps, Resp(niveau, fonction), r.Pick(Profils), deb, fin, r.Next(8, 30), $"Poste {4000 + mat}", mail,
                U.D(start.AddDays(-r.Next(60, 6000)).Date));
        }

        long proc = Add("Procureur de la République", 1, svcIds["CABP"], 0, new[] { "Direction", "Arbitrage", "Représentation" });
        long adj = Add("Procureur adjoint", 2, svcIds["CABA"], proc, new[] { "Direction", "Coordination", "Arbitrage" });
        var heads = new Dictionary<string, long>();
        long sg = 0;
        // ordre : services rattachés au procureur, puis SG, puis le reste
        foreach (var s in Services.OrderBy(s => s.Code == "CABP" ? 0 : s.Code == "CABA" ? 1 : s.Code == "SG" ? 2 : 3))
        {
            int eff = s.Eff - (s.Code == "CABP" || s.Code == "CABA" ? 1 : 0);
            long svc = svcIds[s.Code];
            long headSup = s.Code == "CABP" ? proc : s.Code == "CABA" ? adj : s.Rep == "P" ? proc : s.Rep == "SG" ? sg : adj;
            int lvl = s.Code == "SG" ? 3 : 4;
            long head = Add(s.Head, lvl, svc, headSup, s.Comp);
            heads[s.Code] = head;
            if (s.Code == "SG") sg = head;
            int rest = eff - 1;
            var divs = new List<long>();
            int nd = rest >= 29 ? 3 : rest >= 17 ? 2 : 0;
            for (int i = 0; i < nd; i++) divs.Add(Add("Chef de division " + (char)('A' + i), 5, svc, head, s.Comp));
            rest -= nd;
            int team = 0;
            while (rest > 0)
            {
                long lsup = divs.Count > 0 ? divs[team % divs.Count] : head;
                int size = Math.Min(rest, 5);
                long lead = Add("Responsable d'équipe", 6, svc, lsup, s.Comp);
                rest--;
                for (int k = 1; k < size; k++)
                {
                    bool assist = k == size - 1 && size >= 4;
                    Add(assist ? r.Pick(Assist) : r.Pick(s.Titles), assist ? 8 : 7, svc, lead, s.Comp);
                    rest--;
                }
                team++;
            }
        }
        long total = c.Long("SELECT COUNT(*) FROM collaborateurs");
        if (total != 300) throw new InvalidOperationException($"Contrôle d'effectif : {total} collaborateurs générés au lieu de 300.");

        progress(50, "Construction de l'organigramme et affectation des responsables…");
        foreach (var s in Services) c.Exec("UPDATE services SET responsable_id=@p0 WHERE code=@p1", s.Code == "CABP" ? proc : s.Code == "CABA" ? adj : heads[s.Code], s.Code);
        c.Exec("UPDATE services SET rattachement_id=(SELECT service_id FROM collaborateurs WHERE id=(SELECT superieur_id FROM collaborateurs WHERE id=services.responsable_id))");
        c.Set("procureur_id", proc); c.Set("adjoint_id", adj); c.Set("sg_id", sg); c.Set("secp_id", heads["SECP"]); c.Set("dircab_id", heads["CABP"]);
        c.Exec("UPDATE collaborateurs SET dispo='Disponible'");

        progress(60, "Création de l'univers institutionnel…");
        SeedOrganismes(c, r);

        progress(70, "Création des comptes et des autorisations…");
        c.Exec("INSERT INTO utilisateurs(login,mdp,role,collab_id) VALUES('procureur',@p0,'Procureur',@p1)", U.Hash("parquet"), proc);
        c.Exec("INSERT INTO utilisateurs(login,mdp,role,collab_id) VALUES('adjoint',@p0,'Procureur adjoint',@p1)", U.Hash("adjoint"), adj);
        c.Exec("INSERT INTO utilisateurs(login,mdp,role,collab_id) VALUES('admin',@p0,'Administrateur',@p1)", U.Hash("admin"), heads["INFO"]);
        c.Exec("INSERT INTO utilisateurs(login,mdp,role,collab_id) VALUES('consultation',@p0,'Consultation',@p1)", U.Hash("consultation"), heads["SG"]);
        foreach (var d in Auth.Droits)
        {
            c.Exec("INSERT OR IGNORE INTO autorisations VALUES('Procureur',@p0)", d);
            if (d != "admin") c.Exec("INSERT OR IGNORE INTO autorisations VALUES('Procureur adjoint',@p0)", d);
            if (d is "admin" or "personnel.modifier" or "documents.gerer" or "moteur.piloter") c.Exec("INSERT OR IGNORE INTO autorisations VALUES('Administrateur',@p0)", d);
        }

        progress(80, "Préparation des dossiers et des premières activités…");
        SeedActivites(c, r, start, proc, adj, sg, heads, svcIds);

        c.Exec("INSERT INTO journal_admin(date,date_reelle,auteur,action,details) VALUES(@p0,@p1,'Système','Initialisation','Base initialisée : 20 services, 300 collaborateurs, organigramme et univers institutionnel créés.')", U.D(start), U.D(DateTime.Now));
        c.Commit();
        progress(100, "Initialisation terminée.");
    }

    static string Strip(string s)
    {
        var n = s.Normalize(System.Text.NormalizationForm.FormD);
        return new string(n.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark && ch != ' ' && ch != '\'').ToArray());
    }

    static string Resp(int niveau, string f) => niveau switch
    {
        1 => "Direction générale de l'organisation, arbitrages, représentation institutionnelle.",
        2 => "Coordination des services opérationnels, suppléance du procureur, suivi des délégations.",
        3 => "Coordination administrative, centralisation des comptes rendus, bilans.",
        4 => "Direction du service, répartition du travail, comptes rendus à la hiérarchie.",
        5 => "Encadrement d'une division, suivi des équipes, consolidation des résultats.",
        6 => "Animation d'une équipe, répartition des tâches, points d'étape.",
        7 => $"Missions de {f.ToLowerInvariant()} dans le périmètre du service.",
        _ => "Appui administratif, secrétariat, classement et suivi."
    };

    static void SeedOrganismes(DbCtx c, Random r)
    {
        var orgs = new (string nom, string cat, string desc)[]
        {
            ("Commissariat central de Valmont (simulé)", "Service d'enquête", "Service d'enquête urbain simulé — transmissions de procédures et comptes rendus."),
            ("Compagnie de gendarmerie de Valmont-Nord (simulée)", "Service d'enquête", "Unité de gendarmerie simulée couvrant le secteur rural nord."),
            ("Brigade de recherches départementale (simulée)", "Service d'enquête", "Unité d'enquête spécialisée simulée."),
            ("Service régional de police judiciaire (simulé)", "Service d'enquête", "Service d'enquête régional simulé pour les dossiers complexes."),
            ("Tribunal judiciaire de Valmont (simulé)", "Tribunal", "Juridiction simulée de premier degré."),
            ("Cour d'appel de Valmont (simulée)", "Tribunal", "Juridiction d'appel simulée."),
            ("Tribunal de commerce de Valmont (simulé)", "Tribunal", "Juridiction consulaire simulée."),
            ("Préfecture de Valmont (simulée)", "Institution", "Représentation de l'État simulée dans le département."),
            ("Service pénitentiaire d'insertion et de probation (simulé)", "Administration partenaire", "Service partenaire simulé — suivi des mesures."),
            ("Protection de la jeunesse — Direction territoriale (simulée)", "Administration partenaire", "Partenaire simulé pour les questions relatives aux mineurs."),
            ("Conseil départemental — Aide sociale (simulé)", "Administration partenaire", "Collectivité partenaire simulée."),
            ("Direction départementale des finances publiques (simulée)", "Administration partenaire", "Administration financière partenaire simulée."),
            ("Direction régionale des douanes (simulée)", "Service d'enquête", "Administration douanière simulée."),
            ("Barreau de Valmont (simulé)", "Interlocuteur extérieur", "Ordre professionnel simulé."),
            ("Association d'aide aux victimes de Valmont (simulée)", "Interlocuteur extérieur", "Association partenaire simulée."),
            ("Conseil local de sécurité et de prévention (simulé)", "Structure de coordination", "Instance de coordination territoriale simulée."),
            ("Comité départemental de coordination (simulé)", "Structure de coordination", "Instance de coordination simulée réunissant les partenaires."),
        };
        var fonctions = new Dictionary<string, string[]>
        {
            ["Service d'enquête"] = new[] { "Commandant", "Capitaine", "Officier de liaison", "Chef d'unité" },
            ["Tribunal"] = new[] { "Président (simulation)", "Directeur de greffe", "Greffier en chef", "Juge (simulation)" },
            ["Institution"] = new[] { "Directeur de cabinet", "Chargé de mission sécurité", "Secrétaire général" },
            ["Administration partenaire"] = new[] { "Directeur", "Chef de service", "Référent" },
            ["Interlocuteur extérieur"] = new[] { "Président", "Directeur", "Coordinateur" },
            ["Structure de coordination"] = new[] { "Coordinateur", "Secrétaire permanent", "Animateur" },
        };
        int k = 0;
        foreach (var o in orgs)
        {
            k++;
            long id = c.Insert("INSERT INTO organismes(nom,categorie,description,adresse,telephone) VALUES(@p0,@p1,@p2,@p3,@p4)", o.nom, o.cat, o.desc, $"{r.Next(1, 120)} avenue de la République, Valmont (fictif)", $"01 00 {k:00} {r.Next(10, 99)} {r.Next(10, 99)}");
            int n = r.Next(2, 5);
            for (int i = 0; i < n; i++)
            {
                var p = r.Pick(Prenoms); var nm = r.Pick(Noms);
                c.Exec("INSERT INTO interlocuteurs(organisme_id,prenom,nom,fonction,telephone,email) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", id, p, nm.ToUpperInvariant(), r.Pick(fonctions[o.cat]), $"01 00 {k:00} {r.Next(10, 99)} {r.Next(10, 99)}", (Strip(p) + "." + Strip(nm)).ToLowerInvariant() + "@partenaire.sim");
            }
        }
    }

    public static readonly string[] SujetsDossiers =
    {
        "Vols en série — secteur Nord", "Escroqueries en ligne signalées par plusieurs plaignants", "Dégradations de mobilier urbain", "Infraction routière grave — RN 12", "Trafic présumé — quartier des Tilleuls",
        "Pollution d'un cours d'eau", "Signalement concernant un mineur", "Suivi de mesures d'exécution", "Requête en rectification d'état civil", "Demande d'entraide internationale",
        "Abus de biens sociaux présumés — société fictive Alpha", "Violences signalées — commune de Saint-Aubin (fictive)", "Cambriolages de commerces — centre-ville", "Fraude documentaire présumée", "Dépôt sauvage de déchets industriels",
        "Contentieux de la circulation — radars", "Rodéos urbains signalés", "Usurpation d'identité", "Recel présumé — marché hebdomadaire", "Travail dissimulé présumé — chantier fictif"
    };

    static void SeedActivites(DbCtx c, Random r, DateTime start, long proc, long adj, long sg, Dictionary<string, long> heads, Dictionary<string, long> svcIds)
    {
        var orgIds = c.Rows("SELECT id FROM organismes").Select(x => U.L(x[0])).ToList();
        var opSvcs = new[] { "GEST", "ENQ", "COORD", "EDC", "EMF", "AUD", "PCT" };
        // dossiers historiques + actifs
        for (int i = 0; i < 60; i++)
        {
            var code = r.Pick(opSvcs);
            long svc = svcIds[code];
            var refs = c.Rows("SELECT id FROM collaborateurs WHERE service_id=@p0 AND niveau>=6", svc);
            long referent = U.L(r.Pick(refs)[0]);
            DateTime ouv = start.AddDays(-r.Next(1, 120)).AddHours(r.Next(0, 9));
            bool clos = i < 22;
            int av = clos ? 100 : r.Next(0, 85);
            string cat = r.Pick(Categories);
            string prio = r.Next(10) < 2 ? "Haute" : r.Next(10) < 7 ? "Normale" : "Basse";
            string num = $"DOS-{ouv:yyyy}-H{(i + 1):0000}";
            long id = c.Insert(@"INSERT INTO dossiers(numero,intitule,categorie,resume,service_id,referent_id,organisme_id,personnes,statut,priorite,avancement,date_ouverture,echeance,date_cloture,archive)
                VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14)",
                num, r.Pick(SujetsDossiers) + " (simulation)", cat, "Dossier fictif généré pour la simulation. " + "Catégorie : " + cat + ". Aucune donnée réelle.", svc, referent, r.Pick(orgIds),
                "Personnes fictives (simulation)", clos ? "Clôturé" : (av > 60 ? "En cours d'instruction" : "Ouvert"), prio, av, U.D(ouv), U.D(ouv.AddDays(r.Next(20, 90))), clos ? U.D(ouv.AddDays(r.Next(5, 30))) : null, clos && i < 12 ? 1 : 0);
            c.Exec("INSERT INTO dossier_hist(dossier_id,date,auteur_id,texte) VALUES(@p0,@p1,@p2,'Ouverture du dossier')", id, U.D(ouv), referent);
            Biz.Event(c, "Nouveau dossier", "Service " + code, referent, $"Ouverture du dossier {num}", "dossier", id, ouv);
            if (clos) Biz.Event(c, "Dossier clôturé", "Service " + code, referent, $"Clôture du dossier {num}", "dossier", id, ouv.AddDays(5));
        }
        // missions historiques clôturées (pour statistiques et archives)
        for (int i = 0; i < 25; i++)
        {
            var code = r.Pick(svcIds.Keys.ToList());
            long head = code == "CABP" ? heads["CABP"] : code == "CABA" ? heads["CABA"] : heads[code];
            var subs = c.Rows("SELECT id FROM collaborateurs WHERE superieur_id=@p0", head);
            if (subs.Count == 0) continue;
            long dest = U.L(r.Pick(subs)[0]);
            DateTime d0 = start.AddDays(-r.Next(5, 60));
            c.Insert(@"INSERT INTO missions(numero,racine_id,objet,description,emetteur_id,destinataire_id,service_id,priorite,statut,avancement,date_creation,echeance,rapport,date_rapport,date_cloture,origine)
               VALUES(@p0,NULL,@p1,'Mission antérieure à la prise de fonction (historique).',@p2,@p3,@p4,'Normale','Clôturée',100,@p5,@p6,'Mission réalisée conformément aux instructions.',@p7,@p7,'Historique')",
                $"M-{d0:yyyy}-H{i + 1:000}", r.Pick(Engine.ObjetsInternes), head, dest, svcIds[code], U.D(d0), U.D(d0.AddDays(7)), U.D(d0.AddDays(r.Next(2, 7))));
        }
        c.Exec("UPDATE missions SET racine_id=id WHERE racine_id IS NULL");
        c.Set("sequence", 0);

        // réunions de la semaine
        var headIds = heads.Values.ToList();
        long R(string titre, DateTime d, int min, long org, IEnumerable<long> parts, string odj, string statut = "Planifiée", string prio = "Normale")
        {
            long id = c.Insert("INSERT INTO reunions(titre,debut,fin,lieu,organisateur_id,ordre_du_jour,priorite,statut) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)",
                titre, U.D(d), U.D(d.AddMinutes(min)), r.Pick(new[] { "Salle du conseil", "Bureau du procureur", "Salle de réunion B", "Salle de coordination", "Visioconférence (simulée)" }), org, odj, prio, statut);
            foreach (var p in parts.Distinct()) c.Exec("INSERT OR IGNORE INTO reunion_participants(reunion_id,collab_id,reponse) VALUES(@p0,@p1,'Accepté')", id, p);
            return id;
        }
        R("Réunion de direction hebdomadaire", start.AddHours(2), 90, proc, new[] { proc, adj, sg, heads["CABP"], heads["SECP"] }, "1. Point de situation générale\n2. Délégations en cours\n3. Échéances de la semaine\n4. Questions diverses", prio: "Haute");
        R("Conférence des chefs de service", start.AddDays(1).AddHours(6), 120, adj, new[] { proc, adj, sg }.Concat(headIds.Take(10)), "1. Activité des services\n2. Charge de travail\n3. Coordination interservices");
        R("Point budgétaire et moyens", start.AddDays(2).AddHours(3), 60, sg, new[] { sg, heads["AFFG"], heads["RH"], heads["INFO"] }, "1. Moyens\n2. Effectifs\n3. Informatique");
        R("Comité de pilotage statistique", start.AddDays(3).AddHours(7), 60, heads["STAT"], new[] { proc, heads["STAT"], sg, heads["CI"] }, "1. Indicateurs mensuels\n2. Tableau de bord");
        R("Réunion partenariale (simulation)", start.AddDays(4).AddHours(2), 90, adj, new[] { adj, heads["ENQ"], heads["PCT"], heads["COORD"] }, "1. Échanges avec les partenaires simulés\n2. Calendrier commun");

        // messages d'accueil
        void M(long exp, string obj, string corps, int imp = 0)
            => c.Exec("INSERT INTO messages(date,exp_id,dest_id,objet,corps,important,categorie) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Interne')", U.D(start), exp, proc, obj, corps, imp);
        M(adj, "Prise de fonctions — point de situation", "Monsieur le Procureur,\n\nBienvenue. L'organisation compte 300 collaborateurs répartis dans 20 services et unités. Je me tiens à votre disposition pour toute délégation ; je répartirai le travail entre les chefs de service et vous transmettrai les synthèses.\n\nLe procureur adjoint", 1);
        M(heads["SECP"], "Votre agenda de la semaine", "Monsieur le Procureur,\n\nVotre agenda comporte la réunion de direction ce matin à 10h00 et la conférence des chefs de service demain. Je vous signalerai les appels et les demandes de rendez-vous au fil de l'eau.\n\nLe secrétaire particulier");
        M(sg, "Organisation administrative", "Monsieur le Procureur,\n\nLe secrétariat général centralise les comptes rendus des services et vous transmettra un bilan quotidien en fin de journée.\n\nLe secrétaire général");

        // documents d'organisation
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ORGANISATION ET EFFECTIFS — ÉTAT INITIAL\n");
        foreach (var s in c.Rows("SELECT s.nom, COUNT(c.id), (SELECT prenom||' '||nom FROM collaborateurs WHERE id=s.responsable_id) FROM services s LEFT JOIN collaborateurs c ON c.service_id=s.id GROUP BY s.id ORDER BY s.id"))
            sb.AppendLine($"- {s[0]} : {s[1]} collaborateurs — responsable : {s[2]}");
        sb.AppendLine($"\nTotal : {c.Long("SELECT COUNT(*) FROM collaborateurs")} collaborateurs.");
        c.Exec("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif) VALUES('DOC-INIT-001','Organisation et effectifs — état initial','Documents d''organisation',@p0,@p1,@p2,@p2)", sb.ToString(), sg, U.D(start));
        c.Exec("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif) VALUES('DOC-INIT-002','Note d''organisation du cabinet','Notes internes',@p0,@p1,@p2,@p2)",
            "NOTE D'ORGANISATION DU CABINET\n\n1. Le procureur adjoint coordonne les services opérationnels et les unités transversales.\n2. Le secrétariat général coordonne les services administratifs et support.\n3. Le secrétariat particulier gère l'agenda, les appels et le courrier du procureur.\n4. Les délégations sont suivies dans le module « Mes délégations ».", heads["CABP"], U.D(start));

        // délégation initiale en cours
        Biz.CreerMission(c, proc, adj, "Établir un état des lieux de l'activité des services", "Dresser un état des lieux complet de l'activité, des effectifs et des difficultés de chaque service.",
            "Haute", start.AddDays(4).AddHours(9), "Consolider les contributions des chefs de service et me transmettre une synthèse.", "Interne", 0, 0, 0, null, "Utilisateur");
        Biz.Notify(c, "Nouveau message", "Normale", "Bienvenue dans PARQUET PRO", "Votre organisation est opérationnelle. Consultez votre cabinet.", "module", 0);
    }
}
