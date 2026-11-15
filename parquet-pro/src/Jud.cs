using System.Text;

namespace ParquetPro;

/// Noyau judiciaire : personnes, véhicules, pièces, décisions, casier, détention (univers 100 % fictif).
public static class Jud
{
    public static long Me => Auth.CollabId == 0 ? 1 : Auth.CollabId;
    static Random R => Random.Shared;

    // ======================================================================= référentiels
    public static readonly string[] PrenomsM = { "Julien", "Thomas", "Nicolas", "Antoine", "Mathieu", "Pierre", "Sébastien", "Laurent", "François", "Olivier", "Guillaume", "Vincent", "Romain", "Alexandre", "Maxime", "Benoît", "Christophe", "Frédéric", "Damien", "Jérôme", "Arnaud", "Cédric", "Fabien", "Yann", "Patrick", "David", "Éric", "Bruno", "Philippe", "Hugo", "Xavier", "Gilles", "Raphaël", "Thierry", "Adrien", "Lucas", "Paul", "Jean", "Karim", "Samuel", "Kevin", "Dylan", "Mehdi", "Yanis", "Enzo", "Théo", "Nathan", "Bilal", "Loïc", "Steven" };
    public static readonly string[] PrenomsF = { "Claire", "Sophie", "Camille", "Isabelle", "Nathalie", "Céline", "Aurélie", "Émilie", "Hélène", "Caroline", "Valérie", "Sandrine", "Marion", "Élodie", "Delphine", "Stéphanie", "Anne", "Laure", "Agnès", "Pauline", "Virginie", "Mélanie", "Charlotte", "Juliette", "Manon", "Sarah", "Lucie", "Chloé", "Alice", "Margaux", "Léa", "Inès", "Audrey", "Marine", "Justine", "Florence", "Mathilde", "Elsa", "Nadia", "Sonia", "Jessica", "Laura", "Océane", "Yasmine", "Emma", "Jade", "Amandine", "Coralie", "Estelle", "Gaëlle" };
    static readonly string[] Rues = { "rue des Tilleuls", "avenue Jean-Moulin", "rue de la Gare", "boulevard Victor-Hugo", "impasse des Lilas", "rue du Moulin", "place du Marché", "rue Pasteur", "chemin des Vignes", "allée des Peupliers", "rue de la République", "rue des Écoles", "quai de la Loire", "rue Voltaire", "avenue de la Libération" };
    public static readonly string[] Villes = { "Valmont", "Valmont", "Valmont", "Saint-Aubin-sur-Loire", "Montclair", "Bréval", "Les Ormes", "Vaucelles", "Fontenay-le-Bois" };
    static readonly string[] Professions = { "employé(e) de commerce", "ouvrier(ère) du bâtiment", "chauffeur-livreur", "étudiant(e)", "sans emploi", "artisan", "aide-soignant(e)", "comptable", "agent d'entretien", "intérimaire", "cadre commercial", "enseignant(e)", "retraité(e)", "agriculteur(trice)", "gérant(e) de société", "infirmier(ère)", "serveur(se)", "mécanicien(ne)" };
    static readonly (string marque, string[] modeles)[] Marques = { ("Peugeot", new[] { "208", "308", "3008", "Partner" }), ("Renault", new[] { "Clio", "Mégane", "Kangoo", "Captur" }), ("Citroën", new[] { "C3", "C4", "Berlingo" }), ("Volkswagen", new[] { "Golf", "Polo", "Transporter" }), ("Dacia", new[] { "Sandero", "Duster" }), ("Toyota", new[] { "Yaris", "Corolla" }), ("Ford", new[] { "Fiesta", "Transit" }), ("BMW", new[] { "Série 1", "X3" }) };
    static readonly string[] Couleurs = { "gris", "noir", "blanc", "bleu", "rouge", "argent", "vert foncé" };
    public static readonly string[] Etablissements = { "Centre pénitentiaire de Valmont (fictif)", "Maison d'arrêt de Montclair (fictive)", "Centre de détention de Bréval (fictif)" };
    public const string Tribunal = "Tribunal correctionnel de Valmont (simulé)";
    public const string JLD = "Juge des libertés et de la détention — TJ de Valmont (simulé)";

    public static readonly Dictionary<string, string> CatService = new() { ["Atteintes aux biens"] = "GEST", ["Atteintes aux personnes"] = "ENQ", ["Économique et financier"] = "EDC", ["Circulation routière"] = "GEST", ["Stupéfiants"] = "ENQ", ["Environnement"] = "PCT", ["Mineurs"] = "EMF", ["Exécution des peines"] = "COORD", ["Parquet civil"] = "GEST", ["Coopération"] = "UTC" };

    public record Modele(string Cat, string Titre, string Qualif, int Grav, bool Veh, int NV, int NM, int NT, string Faits, string[] Scelles);

    public static readonly Modele[] Modeles =
    {
        new("Atteintes aux biens", "Vol par effraction dans un commerce", "Vol aggravé par effraction (qualification simulée)", 2, false, 1, 1, 1,
            "Le {date}, vers {heure}, les enquêteurs de l'unité « {org} » ont été requis pour un cambriolage au sein d'un commerce de proximité situé {lieu} à {ville}. La gérante, {V}, a constaté le bris de la porte arrière et la disparition du fonds de caisse (environ {montant} euros) ainsi que de matériel informatique. Les images de vidéoprotection (simulées) montrent un individu cagoulé quittant les lieux. Les premières vérifications ont orienté les soupçons vers {M}.",
            new[] { "Pied-de-biche retrouvé à proximité du commerce", "Clé USB contenant les images de vidéoprotection", "Paire de gants en tissu noir" }),
        new("Atteintes aux biens", "Vol d'un véhicule stationné", "Vol simple (qualification simulée)", 1, true, 1, 1, 0,
            "Le {date}, {V} a déposé plainte pour le vol de son véhicule {veh}, stationné {lieu} à {ville}. Le véhicule a été retrouvé deux jours plus tard, endommagé, en périphérie de la commune. Un relevé d'empreintes (simulé) et un témoignage ont permis de mettre en cause {M}.",
            new[] { "Clé de contact non d'origine retrouvée dans l'habitacle", "Relevé d'empreintes papillaires (simulation)" }),
        new("Atteintes aux personnes", "Violences volontaires sur la voie publique", "Violences volontaires ayant entraîné une ITT de {itt} jours (qualification simulée)", 2, false, 1, 1, 2,
            "Dans la nuit du {date}, vers {heure}, une altercation a éclaté {lieu} à {ville}, à la sortie d'un établissement de nuit. {V} a été frappé(e) à plusieurs reprises et a été pris(e) en charge par les secours. Le certificat médical (simulé) fait état d'une incapacité totale de travail de {itt} jours. Deux témoins ont désigné {M} comme l'auteur des coups.",
            new[] { "Certificat médical initial (simulation)", "Vêtements de la victime" }),
        new("Atteintes aux personnes", "Menaces réitérées entre voisins", "Menaces réitérées (qualification simulée)", 1, false, 1, 1, 1,
            "Depuis plusieurs semaines, {V}, domicilié(e) {lieu} à {ville}, signale recevoir des menaces verbales et écrites de la part de son voisin, {M}. Le {date}, un nouvel incident a conduit la victime à déposer plainte auprès de l'unité « {org} ». Des messages écrits ont été remis aux enquêteurs.",
            new[] { "Copie des messages reçus par la victime" }),
        new("Économique et financier", "Abus de confiance au préjudice d'une association", "Abus de confiance (qualification simulée)", 2, false, 1, 1, 1,
            "Le trésorier d'une association sportive de {ville}, {V}, a signalé le {date} des irrégularités comptables portant sur environ {montant} euros. Les relevés bancaires (simulés) font apparaître des virements répétés vers un compte personnel de {M}, ancien membre du bureau.",
            new[] { "Relevés bancaires de l'association (simulation)", "Registre comptable 2025-2026 (simulation)" }),
        new("Économique et financier", "Escroquerie au faux virement", "Escroquerie (qualification simulée)", 2, false, 1, 1, 0,
            "Le {date}, la société dirigée par {V}, établie {lieu} à {ville}, a procédé à un virement de {montant} euros à la suite d'un courriel frauduleux imitant un fournisseur. L'analyse des flux financiers (simulée) a permis de remonter jusqu'à un compte ouvert au nom de {M}.",
            new[] { "Copie du courriel frauduleux", "Ordre de virement (simulation)" }),
        new("Circulation routière", "Conduite sous l'empire d'un état alcoolique", "Conduite sous l'empire d'un état alcoolique (qualification simulée)", 1, true, 0, 1, 0,
            "Le {date}, vers {heure}, lors d'un contrôle routier {lieu} à {ville}, les agents de l'unité « {org} » ont procédé au contrôle du véhicule {veh} conduit par {M}. Le dépistage (simulé) s'est révélé positif et la mesure a établi un taux de {taux} mg par litre d'air expiré.",
            new[] { "Ticket de l'éthylomètre (simulation)" }),
        new("Circulation routière", "Délit de fuite après accident matériel", "Délit de fuite (qualification simulée)", 1, true, 1, 1, 1,
            "Le {date}, vers {heure}, un véhicule a percuté le véhicule de {V} stationné {lieu} à {ville}, avant de quitter les lieux sans s'arrêter. Un témoin a relevé l'immatriculation du véhicule {veh}, dont le conducteur a été identifié comme étant {M}.",
            new[] { "Éclats de peinture prélevés sur le véhicule endommagé", "Photographies des dégâts (simulation)" }),
        new("Stupéfiants", "Trafic de produits stupéfiants en bas d'immeuble", "Détention et cession de produits stupéfiants (qualification simulée)", 3, true, 0, 2, 1,
            "À la suite de signalements répétés d'habitants du quartier, les enquêteurs de l'unité « {org} » ont mis en place une surveillance (simulée) {lieu} à {ville}. Le {date}, ils ont interpellé {M} et un second individu à proximité du véhicule {veh}. Des produits stupéfiants conditionnés et une somme de {montant} euros en numéraire ont été découverts.",
            new[] { "Sachets de résine de cannabis conditionnés (simulation)", "Somme en numéraire", "Balance de précision", "Téléphone portable" }),
        new("Stupéfiants", "Usage de produits stupéfiants", "Usage de produits stupéfiants (qualification simulée)", 1, false, 0, 1, 0,
            "Le {date}, lors d'une patrouille {lieu} à {ville}, les agents de l'unité « {org} » ont contrôlé {M}, trouvé(e) en possession d'une petite quantité de produits stupéfiants destinés, selon ses déclarations, à sa consommation personnelle.",
            new[] { "Petite quantité de résine de cannabis (simulation)" }),
        new("Environnement", "Dépôt illégal de déchets de chantier", "Dépôt illégal de déchets (qualification simulée)", 1, true, 0, 1, 1,
            "Le {date}, un agent communal de {ville} a constaté un important dépôt de déchets de chantier {lieu}. Un riverain a relevé l'immatriculation d'une camionnette {veh} déchargeant des gravats. Le véhicule appartient à l'entreprise de {M}.",
            new[] { "Photographies du dépôt (simulation)", "Bordereaux de chantier retrouvés parmi les déchets" }),
        new("Environnement", "Pollution d'un cours d'eau", "Pollution des eaux (qualification simulée)", 2, false, 0, 1, 1,
            "Le {date}, une mortalité piscicole a été constatée sur la rivière traversant {ville}. Les prélèvements (simulés) ont mis en évidence un rejet d'hydrocarbures provenant du site exploité par la société gérée par {M}, {lieu}.",
            new[] { "Échantillons d'eau prélevés (simulation)", "Rapport de l'agent environnemental (simulation)" }),
        new("Mineurs", "Dégradations commises par des mineurs", "Dégradations volontaires (qualification simulée)", 1, false, 1, 1, 1,
            "Le {date}, des dégradations ont été commises sur les locaux d'un gymnase {lieu} à {ville} (vitres brisées, inscriptions). Le gardien, {V}, a reconnu {M}, mineur(e), parmi les auteurs présumés.",
            new[] { "Bombe de peinture retrouvée sur place" }),
        new("Exécution des peines", "Non-respect d'une obligation de soins", "Non-respect des obligations d'une mesure (qualification simulée)", 2, false, 0, 1, 0,
            "Le service pénitentiaire d'insertion et de probation (simulé) signale que {M}, soumis(e) à une obligation de soins, ne s'est pas présenté(e) aux convocations depuis le {date} et n'a fourni aucun justificatif.",
            new[] { "Rapport d'incident du service de probation (simulation)" }),
        new("Parquet civil", "Requête en rectification d'état civil", "Rectification d'acte d'état civil (procédure civile simulée)", 1, false, 1, 0, 0,
            "Le {date}, {V}, demeurant {lieu} à {ville}, a saisi le parquet d'une demande de rectification d'une erreur matérielle affectant son acte de naissance (orthographe du nom).",
            new string[0]),
        new("Coopération", "Demande d'entraide pénale internationale", "Entraide pénale internationale (procédure simulée)", 2, false, 1, 1, 0,
            "Le {date}, une autorité étrangère (simulée) a transmis une demande d'entraide visant à entendre {M}, résidant {lieu} à {ville}, dans le cadre d'une enquête portant sur une escroquerie au préjudice de {V}.",
            new[] { "Demande d'entraide traduite (simulation)" }),
    };

    // ======================================================================= personnes et véhicules
    public static long NouvellePersonne(DbCtx c, int amin, int amax, string sexe = null)
    {
        var r = R;
        sexe ??= r.Next(2) == 0 ? "M" : "F";
        for (int essai = 0; essai < 30; essai++)
        {
            string prenom = sexe == "M" ? r.Pick(PrenomsM) : r.Pick(PrenomsF);
            string nom = r.Pick(Seed.Noms).ToUpperInvariant();
            var dn = Engine.Now.Date.AddYears(-r.Next(amin, amax + 1)).AddDays(-r.Next(0, 365));
            if (c.Long("SELECT COUNT(*) FROM personnes WHERE nom=@p0 AND prenom=@p1 AND date_naissance=@p2", nom, prenom, dn.ToString("yyyy-MM-dd")) > 0) continue;
            int age = Engine.Now.Year - dn.Year;
            string prof = age < 18 ? "collégien(ne) / lycéen(ne)" : age > 66 ? "retraité(e)" : r.Pick(Professions);
            long id = c.Insert("INSERT INTO personnes(nom,prenom,sexe,date_naissance,lieu_naissance,adresse,ville,profession,statut,date_creation) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,'Sans antécédent',@p8)",
                nom, prenom, sexe, dn.ToString("yyyy-MM-dd"), r.Pick(Villes), $"{r.Next(1, 140)} {r.Pick(Rues)}", r.Pick(Villes), prof, U.D(Engine.Now));
            c.Exec("UPDATE personnes SET ref=@p1 WHERE id=@p0", id, $"P-{id:000000}");
            return id;
        }
        throw new InvalidOperationException("Impossible de générer une identité unique.");
    }

    public static string NomP(DbCtx c, long pid) => pid == 0 ? "" : c.Str("SELECT prenom||' '||nom FROM personnes WHERE id=@p0", pid) ?? "";
    public static string IdentiteP(DbCtx c, long pid)
    {
        var p = c.Rows("SELECT prenom, nom, date_naissance, lieu_naissance, adresse, ville, profession, sexe FROM personnes WHERE id=@p0", pid);
        if (p.Count == 0) return "";
        var x = p[0];
        return $"{x[1]} {x[0]}, {(U.S(x[7]) == "F" ? "née" : "né")} le {U.P(x[2]):dd/MM/yyyy} à {x[3]}, {x[6]}, demeurant {x[4]} à {x[5]} (identité fictive)";
    }

    static string Immat()
    {
        const string L = "ABCDEFGHJKLMNPQRSTVWXYZ";
        var r = R;
        return $"{L[r.Next(L.Length)]}{L[r.Next(L.Length)]}-{r.Next(100, 999)}-{L[r.Next(L.Length)]}{L[r.Next(L.Length)]}";
    }

    public static long NouveauVehicule(DbCtx c, long proprietaire)
    {
        var r = R; var m = r.Pick(Marques);
        string im; do im = Immat(); while (c.Long("SELECT COUNT(*) FROM vehicules WHERE immatriculation=@p0", im) > 0);
        return c.Insert("INSERT INTO vehicules(immatriculation,marque,modele,couleur,proprietaire_id,notes) VALUES(@p0,@p1,@p2,@p3,@p4,'Véhicule fictif')", im, m.marque, r.Pick(m.modeles), r.Pick(Couleurs), proprietaire == 0 ? null : proprietaire);
    }

    public static string DescVeh(DbCtx c, long vid)
    {
        var v = c.Rows("SELECT marque, modele, couleur, immatriculation FROM vehicules WHERE id=@p0", vid);
        return v.Count == 0 ? "" : $"{v[0][0]} {v[0][1]} de couleur {v[0][2]} immatriculé {v[0][3]}";
    }

    /// Choisit une personne pour un rôle : réutilise parfois une personne existante (rapprochements réels), sinon en crée une.
    static long PersonnePourRole(DbCtx c, string role, bool mineur)
    {
        var r = R;
        if (!mineur && role == "Mis en cause" && r.NextDouble() < 0.28)
        {
            long ex = c.Long(@"SELECT p.id FROM personnes p WHERE (EXISTS(SELECT 1 FROM casier WHERE personne_id=p.id) OR EXISTS(SELECT 1 FROM dossier_personnes WHERE personne_id=p.id AND role='Mis en cause'))
                AND NOT EXISTS(SELECT 1 FROM detentions d WHERE d.personne_id=p.id AND d.date_sortie IS NULL) AND p.date_naissance<=@p0 ORDER BY RANDOM() LIMIT 1", Engine.Now.AddYears(-18).ToString("yyyy-MM-dd"));
            if (ex != 0) return ex;
        }
        if (role != "Mis en cause" && r.NextDouble() < 0.06)
        {
            long ex = c.Long("SELECT id FROM personnes WHERE date_naissance<=@p0 ORDER BY RANDOM() LIMIT 1", Engine.Now.AddYears(-18).ToString("yyyy-MM-dd"));
            if (ex != 0) return ex;
        }
        return mineur ? NouvellePersonne(c, 13, 17) : NouvellePersonne(c, 19, 72);
    }

    public static void Lier(DbCtx c, long dos, long pid, string role, string source, string declaration = null)
        => c.Exec("INSERT OR IGNORE INTO dossier_personnes(dossier_id,personne_id,role,declaration,date_ajout,source) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", dos, pid, role, declaration, U.D(Engine.Now), source);

    // ======================================================================= pièces
    public static long Piece(DbCtx c, long dos, string type, string titre, DateTime date, string auteur, long org, long pers, string contenu, string origine, long decision = 0)
    {
        int n = (int)c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0", dos) + 1;
        string cote = "D" + n;
        long id = c.Insert("INSERT INTO pieces(dossier_id,cote,type,titre,date,auteur,organisme_id,personne_id,contenu,origine,decision_id) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)",
            dos, cote, type, titre, U.D(date), auteur, org == 0 ? null : org, pers == 0 ? null : pers, contenu, origine, decision == 0 ? null : decision);
        c.Exec("INSERT INTO dossier_chrono(dossier_id,date,libelle,source) VALUES(@p0,@p1,@p2,@p3)", dos, U.D(date), $"{cote} — {titre}", auteur);
        return id;
    }

    public static string EnTete(DbCtx c, long pieceId)
    {
        var p = c.Rows("SELECT p.cote, p.type, p.titre, p.date, p.auteur, d.numero, d.intitule FROM pieces p JOIN dossiers d ON d.id=p.dossier_id WHERE p.id=@p0", pieceId);
        if (p.Count == 0) return "";
        var x = p[0];
        return $"{U.Mention}\n\n{U.S(x[2]).ToUpper(U.Fr)}\n\nProcédure : {x[5]} — {x[6]}\nCote : {x[0]}    Nature : {x[1]}\nDate : {U.Aff(x[3])}\nRédacteur : {x[4]}\n" + new string('─', 56) + "\n\n";
    }

    static Dictionary<string, string> Vars(DbCtx c, long dos)
    {
        var d = c.Rows("SELECT date_faits, lieu_faits, (SELECT nom FROM organismes WHERE id=service_enqueteur_id), qualification FROM dossiers WHERE id=@p0", dos)[0];
        var v = new Dictionary<string, string>
        {
            ["date"] = U.P(d[0]).ToString("dd/MM/yyyy"), ["heure"] = U.P(d[0]).ToString("HH'h'mm"), ["lieu"] = U.S(d[1]).Split(" à ")[0], ["ville"] = U.S(d[1]).Contains(" à ") ? U.S(d[1]).Split(" à ")[^1] : "Valmont",
            ["org"] = U.S(d[2]), ["qualif"] = U.S(d[3])
        };
        var vic = c.Long("SELECT personne_id FROM dossier_personnes WHERE dossier_id=@p0 AND role IN ('Victime','Plaignant','Requérant') LIMIT 1", dos);
        var mec = c.Long("SELECT personne_id FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause' LIMIT 1", dos);
        v["V"] = vic == 0 ? "la victime" : NomP(c, vic);
        v["M"] = mec == 0 ? "un individu non identifié" : NomP(c, mec);
        return v;
    }

    static string Fill(string tpl, Dictionary<string, string> v)
    {
        foreach (var kv in v) tpl = tpl.Replace("{" + kv.Key + "}", kv.Value);
        return tpl;
    }

    public static Modele ModeleDe(DbCtx c, long dos)
    {
        var x = c.Rows("SELECT categorie, intitule FROM dossiers WHERE id=@p0", dos)[0];
        string cat = U.S(x[0]), tit = U.S(x[1]);
        return Modeles.FirstOrDefault(m => tit.StartsWith(m.Titre)) ?? Modeles.FirstOrDefault(m => m.Cat == cat) ?? Modeles[0];
    }

    /// Génère le contenu complet d'une affaire (faits, personnes, véhicules, scellés, pièces initiales).
    public static void Generer(DbCtx c, long dos, string cat, long org, DateTime trans, bool complete, Modele forced = null)
    {
        var r = R;
        var cands = Modeles.Where(m => m.Cat == cat).ToList();
        var m = forced ?? (cands.Count > 0 ? r.Pick(cands) : r.Pick(Modeles));
        if (org == 0) org = c.Long("SELECT id FROM organismes WHERE categorie='Service d''enquête' ORDER BY RANDOM() LIMIT 1");
        string orgNom = c.Str("SELECT nom FROM organismes WHERE id=@p0", org) ?? "service enquêteur (simulé)";
        var dateFaits = trans.Date.AddDays(-r.Next(1, 12)).AddHours(r.Next(7, 23)).AddMinutes(r.Next(0, 4) * 15);
        string ville = r.Pick(Villes);
        string lieu = $"{r.Next(1, 140)} {r.Pick(Rues)} à {ville}";
        int itt = r.Next(2, 15);
        string qualif = m.Qualif.Replace("{itt}", itt.ToString());
        bool mineur = m.Cat == "Mineurs";
        // personnes
        var vics = new List<long>(); var mecs = new List<long>(); var tems = new List<long>();
        string roleV = m.Cat == "Parquet civil" ? "Requérant" : "Victime";
        for (int i = 0; i < m.NV; i++) { long p = PersonnePourRole(c, roleV, false); vics.Add(p); Lier(c, dos, p, roleV, "Procédure transmise par " + orgNom); }
        bool inconnu = m.NM > 0 && r.NextDouble() < 0.08;
        if (!inconnu)
            for (int i = 0; i < m.NM; i++)
            {
                long p = PersonnePourRole(c, "Mis en cause", mineur);
                if (mecs.Contains(p) || vics.Contains(p)) continue;
                mecs.Add(p);
                double x = r.NextDouble();
                Lier(c, dos, p, "Mis en cause", "Procédure transmise par " + orgNom, x < 0.4 ? "Reconnaît les faits" : x < 0.6 ? "Reconnaît partiellement les faits" : "Conteste les faits");
            }
        int nt = m.NT == 0 ? 0 : r.Next(Math.Max(1, m.NT - 1), m.NT + 1);
        for (int i = 0; i < nt; i++) { long p = PersonnePourRole(c, "Témoin", false); if (vics.Contains(p) || mecs.Contains(p)) continue; tems.Add(p); Lier(c, dos, p, "Témoin", "Procédure transmise par " + orgNom); }
        // véhicule
        string vehDesc = "";
        if (m.Veh)
        {
            long owner = m.Titre.StartsWith("Vol d'un véhicule") && vics.Count > 0 ? vics[0] : mecs.Count > 0 ? mecs[0] : 0;
            long vid = owner != 0 && r.NextDouble() < 0.35 ? c.Long("SELECT id FROM vehicules WHERE proprietaire_id=@p0 LIMIT 1", owner) : 0;
            if (vid == 0) vid = NouveauVehicule(c, owner);
            c.Exec("INSERT OR IGNORE INTO dossier_vehicules(dossier_id,vehicule_id,role,source) VALUES(@p0,@p1,@p2,@p3)", dos, vid, m.Titre.StartsWith("Vol d'un véhicule") ? "Véhicule volé" : "Véhicule impliqué", "Procédure transmise par " + orgNom);
            vehDesc = DescVeh(c, vid);
        }
        var v = new Dictionary<string, string>
        {
            ["date"] = dateFaits.ToString("dd/MM/yyyy"), ["heure"] = dateFaits.ToString("HH'h'mm"), ["lieu"] = lieu.Split(" à ")[0], ["ville"] = ville, ["org"] = orgNom,
            ["V"] = vics.Count > 0 ? NomP(c, vics[0]) : "la victime", ["M"] = mecs.Count > 0 ? NomP(c, mecs[0]) : "un individu non identifié", ["veh"] = vehDesc,
            ["montant"] = (r.Next(4, 120) * 100).ToString("N0", U.Fr), ["taux"] = (0.45 + r.NextDouble() * 0.9).ToString("0.00", U.Fr), ["itt"] = itt.ToString()
        };
        string faits = Fill(m.Faits, v);
        c.Exec(@"UPDATE dossiers SET faits=@p1, lieu_faits=@p2, date_faits=@p3, qualification=@p4, gravite=@p5, service_enqueteur_id=@p6, date_transmission=@p7, organisme_id=@p6,
            personnes=@p8, resume=@p9, situation='Enquête en cours' WHERE id=@p0",
            dos, faits, lieu, U.D(dateFaits), qualif, m.Grav, org, U.D(trans),
            $"{vics.Count} {roleV.ToLower()}(s), {mecs.Count} mis en cause, {tems.Count} témoin(s)" + (inconnu ? " — auteur non identifié" : ""),
            $"{qualif}. {faits.Split(". ")[0]}.");
        c.Exec("INSERT INTO dossier_chrono(dossier_id,date,libelle,source) VALUES(@p0,@p1,@p2,'Faits')", dos, U.D(dateFaits), "Commission des faits (date retenue par l'enquête)");
        // pièces initiales
        var dt = dateFaits.AddHours(r.Next(1, 30));
        if (vics.Count > 0)
            Piece(c, dos, "Procès-verbal", m.Cat == "Parquet civil" ? "Requête et pièces justificatives" : "Procès-verbal de plainte", dt, orgNom, org, vics[0], TextePlainte(c, vics[0], faits, m), "Service enquêteur");
        else
            Piece(c, dos, "Procès-verbal", "Procès-verbal de saisine et d'interpellation", dt, orgNom, org, 0, $"Les enquêteurs de l'unité « {orgNom} » se saisissent des faits suivants :\n\n{faits}\n\nLes premières diligences sont engagées sous le contrôle du parquet (simulation).", "Service enquêteur");
        if (m.Cat != "Parquet civil")
            Piece(c, dos, "Procès-verbal", "Procès-verbal de constatations", dt.AddHours(2), orgNom, org, 0,
                $"Nous, enquêteurs de l'unité « {orgNom} », nous transportons {lieu}.\n\nConstatations :\n- Configuration des lieux relevée et photographiée (planche photographique simulée).\n- {(m.Scelles.Length > 0 ? "Éléments matériels relevés : " + string.Join(", ", m.Scelles.Take(2)).ToLower() + "." : "Aucun élément matériel particulier.")}\n- {(vehDesc != "" ? "Véhicule concerné : " + vehDesc + "." : "Aucun véhicule concerné.")}\n\nDont procès-verbal (mention simulée).", "Service enquêteur");
        foreach (var vic in vics.Skip(1)) Piece(c, dos, "Audition", "Audition de " + NomP(c, vic) + " (" + roleV.ToLower() + ")", dt.AddHours(4), orgNom, org, vic, TexteAudition(c, vic, roleV, v, m), "Service enquêteur");
        int k = 1;
        foreach (var s in m.Scelles.Take(r.Next(1, Math.Max(2, m.Scelles.Length + 1))))
            c.Exec("INSERT INTO scelles(dossier_id,numero,description,lieu,date,statut) VALUES(@p0,@p1,@p2,@p3,@p4,'Placé sous scellé (sim.)')", dos, $"Scellé n°{k++}", s, lieu, U.D(dt.AddHours(3)));
        c.Exec("UPDATE dossiers SET avancement=20 WHERE id=@p0", dos);
        if (complete) { var t = dt.AddHours(6); while (!PieceSuivante(c, dos, t, false)) t = t.AddHours(r.Next(3, 30)); }
    }

    static string TextePlainte(DbCtx c, long pid, string faits, Modele m)
        => $"Se présente devant nous : {IdentiteP(c, pid)}.\n\nLaquelle/lequel déclare vouloir déposer plainte et expose :\n« {faits} »\n\nQ : Souhaitez-vous ajouter quelque chose ?\nR : Je souhaite que l'auteur soit identifié et je me constitue {(m.Cat == "Parquet civil" ? "requérant(e)" : "partie plaignante")} (mention simulée).\n\nLecture faite par elle/lui-même, persiste et signe (mention simulée).";

    static string TexteAudition(DbCtx c, long pid, string role, Dictionary<string, string> v, Modele m)
    {
        var r = R;
        var sb = new StringBuilder();
        sb.AppendLine($"Entendu(e) en qualité de {role.ToLower()} : {IdentiteP(c, pid)}.\n");
        if (role == "Témoin")
        {
            sb.AppendLine($"Q : Pouvez-vous nous indiquer ce que vous avez vu le {v["date"]} ?");
            sb.AppendLine($"R : {r.Pick(new[] { "J'étais à proximité et j'ai assisté à la scène.", "Je rentrais chez moi lorsque j'ai remarqué des allées et venues inhabituelles.", "J'ai été alerté(e) par du bruit et je suis sorti(e)." })} {(v["M"].StartsWith("un individu") ? "Je n'ai pas pu identifier la personne." : $"Je reconnais formellement {v["M"]} sur la planche photographique qui m'est présentée (simulation).")}");
            sb.AppendLine("Q : Avez-vous des liens avec les personnes concernées ?");
            sb.AppendLine("R : " + r.Pick(new[] { "Aucun, je ne les connais pas.", "Je les connais de vue, nous habitons le même quartier.", "Je connais la victime depuis plusieurs années." }));
        }
        else
        {
            sb.AppendLine("Q : Confirmez-vous les termes de votre plainte ?");
            sb.AppendLine("R : Oui, je les confirme intégralement.");
            sb.AppendLine("Q : Quelles sont les conséquences des faits pour vous ?");
            sb.AppendLine("R : " + r.Pick(new[] { "Un préjudice matériel important, que je chiffrerai ultérieurement.", "Je suis très affecté(e) et j'ai dû interrompre mon activité.", "Un préjudice moral et financier." }));
        }
        sb.AppendLine("Q : Avez-vous autre chose à déclarer ?\nR : Non.\n\nLecture faite, persiste et signe (mention simulée).");
        return sb.ToString();
    }

    static string TexteInterrogatoire(DbCtx c, long dos, long pid, string decl, Dictionary<string, string> v, Modele m)
    {
        var r = R;
        long ant = c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", pid);
        var sb = new StringBuilder();
        sb.AppendLine($"Audition de personne mise en cause (garde à vue simulée) : {IdentiteP(c, pid)}.");
        sb.AppendLine("Notification préalable des droits (formalité simulée). La personne déclare ne pas souhaiter garder le silence.\n");
        sb.AppendLine($"Q : Les faits suivants vous sont reprochés : {v["qualif"]}. Qu'avez-vous à déclarer ?");
        sb.AppendLine("R : " + decl switch
        {
            "Reconnaît les faits" => r.Pick(new[] { "Je reconnais les faits. Je regrette ce que j'ai fait.", "Oui, c'est bien moi. J'avais des difficultés à ce moment-là.", "Je reconnais les faits et je suis prêt(e) à indemniser." }),
            "Reconnaît partiellement les faits" => r.Pick(new[] { "J'étais présent(e), mais je n'ai pas fait tout ce qu'on me reproche.", "Je reconnais une partie des faits seulement." }),
            _ => r.Pick(new[] { "Je conteste. Je n'ai rien à voir avec cette affaire.", "Ce n'est pas moi, il y a une erreur.", "Je conteste formellement, j'étais ailleurs ce jour-là." })
        });
        sb.AppendLine($"Q : Où vous trouviez-vous le {v["date"]} vers {v["heure"]} ?");
        sb.AppendLine("R : " + (decl == "Conteste les faits" ? r.Pick(new[] { "Chez moi, avec ma compagne/mon compagnon.", "Je travaillais, mon employeur peut le confirmer." }) : $"J'étais sur place, {v["lieu"]}."));
        sb.AppendLine($"Q : Connaissez-vous {v["V"]} ?");
        sb.AppendLine("R : " + r.Pick(new[] { "Non, jamais vu(e).", "De vue seulement.", "Oui, nous nous connaissons." }));
        sb.AppendLine("Q : Avez-vous déjà fait l'objet de condamnations ?");
        sb.AppendLine("R : " + (ant > 0 ? $"Oui, j'ai déjà été condamné(e) ({ant} mention(s) au casier simulé)." : "Non, jamais."));
        sb.AppendLine("Q : Avez-vous quelque chose à ajouter ?\nR : " + r.Pick(new[] { "Non.", "Je souhaite pouvoir rentrer chez moi.", "Je demande à être assisté(e) d'un avocat pour la suite (mention simulée)." }));
        sb.AppendLine("\nLecture faite, persiste et signe (mention simulée). Fin de la mesure de garde à vue (simulée).");
        return sb.ToString();
    }

    /// Ajoute la prochaine pièce logique de l'enquête. Retourne true si le rapport de synthèse vient d'être versé.
    public static bool PieceSuivante(DbCtx c, long dos, DateTime t, bool notifier)
    {
        var r = R;
        var m = ModeleDe(c, dos);
        long org = c.Long("SELECT IFNULL(service_enqueteur_id,0) FROM dossiers WHERE id=@p0", dos);
        string orgNom = c.Str("SELECT nom FROM organismes WHERE id=@p0", org) ?? "Service enquêteur (simulé)";
        var v = Vars(c, dos);
        bool Has(string titreLike, long pid = 0) => c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND titre LIKE @p1 AND (@p2=0 OR personne_id=@p2)", dos, titreLike, pid) > 0;
        foreach (var w in c.Rows("SELECT personne_id FROM dossier_personnes WHERE dossier_id=@p0 AND role IN ('Victime','Témoin','Requérant')", dos).Select(x => U.L(x[0])))
        {
            if (!Has("Audition de%", w) && !Has("Procès-verbal de plainte", w) && !Has("Requête%", w))
            {
                string role = c.Str("SELECT role FROM dossier_personnes WHERE dossier_id=@p0 AND personne_id=@p1", dos, w);
                Piece(c, dos, "Audition", $"Audition de {NomP(c, w)} ({role.ToLower()})", t, orgNom, org, w, TexteAudition(c, w, role, v, m), "Service enquêteur");
                Avance(c, dos); return false;
            }
        }
        if (m.Cat != "Parquet civil" && !Has("Procès-verbal de vérifications%"))
        {
            Piece(c, dos, "Procès-verbal", "Procès-verbal de vérifications et d'investigations", t, orgNom, org, 0,
                $"Diligences réalisées (simulation) :\n- Consultation des fichiers de police (simulés) concernant les personnes mises en cause.\n- {r.Pick(new[] { "Exploitation des images de vidéoprotection.", "Réquisitions auprès d'un opérateur de téléphonie (simulées).", "Vérification de l'emploi du temps déclaré.", "Enquête de voisinage." })}\n- {r.Pick(new[] { "Les vérifications confirment la présence du mis en cause sur les lieux.", "Les vérifications ne permettent pas de confirmer l'alibi avancé.", "Les vérifications apportent des éléments partiels." })}\n\nDont procès-verbal (mention simulée).", "Service enquêteur");
            Avance(c, dos); return false;
        }
        foreach (var x in c.Rows("SELECT personne_id, declaration FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos))
        {
            long p = U.L(x[0]);
            if (!Has("Audition de mis en cause%", p))
            {
                Piece(c, dos, "Interrogatoire", $"Audition de mis en cause — {NomP(c, p)}", t, orgNom, org, p, TexteInterrogatoire(c, dos, p, U.S(x[1]), v, m), "Service enquêteur");
                Avance(c, dos); return false;
            }
        }
        if (c.Long("SELECT COUNT(*) FROM scelles WHERE dossier_id=@p0", dos) > 0 && !Has("Procès-verbal de placement sous scellés%"))
        {
            var sc = c.Rows("SELECT numero, description FROM scelles WHERE dossier_id=@p0", dos);
            long pid = Piece(c, dos, "Procès-verbal", "Procès-verbal de placement sous scellés", t, orgNom, org, 0,
                "Inventaire des éléments matériels placés sous scellés (simulation) :\n" + string.Join("\n", sc.Select(s => $"- {s[0]} : {s[1]}")) + "\n\nLes scellés sont déposés au greffe (simulé).", "Service enquêteur");
            c.Exec("UPDATE scelles SET piece_id=@p1 WHERE dossier_id=@p0", dos, pid);
            Avance(c, dos); return false;
        }
        if (!Has("Rapport de synthèse%"))
        {
            var mecs = c.Rows("SELECT personne_id, declaration FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos);
            var sb = new StringBuilder();
            sb.AppendLine($"RAPPORT DE SYNTHÈSE — {v["qualif"]}\n");
            sb.AppendLine("1. Rappel des faits\n" + c.Str("SELECT faits FROM dossiers WHERE id=@p0", dos) + "\n");
            sb.AppendLine("2. Personnes mises en cause");
            if (mecs.Count == 0) sb.AppendLine("   Auteur non identifié à ce stade.");
            foreach (var x in mecs) sb.AppendLine($"   - {IdentiteP(c, U.L(x[0]))} : {U.S(x[1]).ToLower()} ; antécédents simulés : {c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", U.L(x[0]))}.");
            sb.AppendLine("\n3. Investigations réalisées");
            foreach (var p in c.Rows("SELECT cote, titre FROM pieces WHERE dossier_id=@p0 ORDER BY id", dos)) sb.AppendLine($"   - {p[0]} {p[1]}");
            sb.AppendLine("\n4. Éléments matériels");
            var sc = c.Rows("SELECT numero, description FROM scelles WHERE dossier_id=@p0", dos);
            sb.AppendLine(sc.Count == 0 ? "   Aucun." : string.Join("\n", sc.Select(s => $"   - {s[0]} : {s[1]}")));
            sb.AppendLine($"\n5. Conclusion\n   Les investigations sont terminées. La procédure est soumise à l'appréciation du parquet (simulation).");
            Piece(c, dos, "Rapport", "Rapport de synthèse d'enquête", t, orgNom, org, 0, sb.ToString(), "Service enquêteur");
            c.Exec("UPDATE dossiers SET avancement=MAX(avancement,90), situation='En attente de décision', statut='En cours d''instruction' WHERE id=@p0 AND statut<>'Clôturé'", dos);
            if (notifier) AvisReferent(c, dos, t);
            return true;
        }
        return true;
    }

    static void Avance(DbCtx c, long dos) => c.Exec("UPDATE dossiers SET avancement=MIN(85, avancement+12), statut=CASE WHEN statut='Ouvert' THEN 'En cours d''instruction' ELSE statut END WHERE id=@p0 AND statut<>'Clôturé'", dos);

    public static int Score(DbCtx c, long dos, long pid)
    {
        int s = 0;
        s += (int)Math.Min(4, c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0", dos) / 2);
        s += (int)Math.Min(2, c.Long("SELECT COUNT(*) FROM scelles WHERE dossier_id=@p0", dos));
        s += (int)Math.Min(2, c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0 AND role='Témoin'", dos));
        if (c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND titre LIKE 'Rapport de synthèse%'", dos) > 0) s += 1;
        if (pid != 0)
        {
            var d = c.Str("SELECT declaration FROM dossier_personnes WHERE dossier_id=@p0 AND personne_id=@p1 AND role='Mis en cause'", dos, pid);
            s += d == "Reconnaît les faits" ? 3 : d == "Reconnaît partiellement les faits" ? 1 : 0;
        }
        return s;
    }

    static string Proposition(DbCtx c, long dos)
    {
        int g = (int)c.Long("SELECT gravite FROM dossiers WHERE id=@p0", dos);
        var mec = c.Rows("SELECT personne_id, declaration FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos);
        if (ModeleDe(c, dos).Cat == "Parquet civil") return "examen de la requête et décision d'orientation civile (simulation)";
        if (mec.Count == 0) return "classement (auteur non identifié) ou complément d'enquête";
        int s = Score(c, dos, U.L(mec[0][0]));
        if (s <= 4) return "complément d'enquête, les charges paraissant insuffisantes en l'état";
        if (g >= 3) return "ouverture d'une information judiciaire ou comparution immédiate (simulation)";
        if (g == 1 && U.S(mec[0][1]) == "Reconnaît les faits") return "alternative aux poursuites (composition pénale simulée)";
        return "convocation devant le tribunal correctionnel (simulation)";
    }

    public static void AvisReferent(DbCtx c, long dos, DateTime t)
    {
        var d = c.Rows("SELECT numero, intitule, referent_id FROM dossiers WHERE id=@p0", dos)[0];
        long refe = U.L(d[2]);
        string txt = $"Monsieur le Procureur,\n\nLe service enquêteur a versé son rapport de synthèse dans le dossier {d[0]} « {d[1]} ». L'enquête est terminée.\n\nAprès examen des pièces, je vous propose : {Proposition(c, dos)}.\n\nLe dossier est en attente de votre décision (module Décisions).\n\n{Biz.NameF(c, refe)}";
        long mid = Biz.Message(c, refe == 0 ? long.Parse(c.Get("adjoint_id", "2")) : refe, Me, $"Dossier {d[0]} — en attente de décision", txt, 0, dos, false, categorie: "Dossier");
        c.Exec("UPDATE messages SET ref_type='dossier', ref_id=@p1 WHERE id=@p0", mid, dos);
        Biz.Notify(c, "Décision requise", "Haute", $"Décision attendue — {d[0]}", $"{d[1]} : l'enquête est terminée.", "dossier", dos);
        Biz.Event(c, "Enquête terminée", Biz.Name(c, refe), refe, $"Rapport de synthèse versé au dossier {d[0]} — décision attendue", "dossier", dos);
    }

    // ======================================================================= statut des personnes
    public static void MajStatut(DbCtx c, long pid)
    {
        string s;
        var det = c.Str("SELECT regime FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL ORDER BY id DESC LIMIT 1", pid);
        var mesure = c.Str("SELECT mesure FROM personnes WHERE id=@p0", pid);
        if (det != null) s = "Détenu(e) — " + det;
        else if (!string.IsNullOrEmpty(mesure)) s = mesure;
        else if (c.Long("SELECT COUNT(*) FROM dossier_personnes dp JOIN dossiers d ON d.id=dp.dossier_id WHERE dp.personne_id=@p0 AND dp.role='Mis en cause' AND d.statut<>'Clôturé'", pid) > 0) s = "Mis en cause (sim.)";
        else if (c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", pid) > 0) s = "Antécédents enregistrés (sim.)";
        else s = "Sans antécédent";
        c.Exec("UPDATE personnes SET statut=@p1 WHERE id=@p0", pid, s);
    }

    static void MajStatutsDossier(DbCtx c, long dos)
    {
        foreach (var p in c.Rows("SELECT personne_id FROM dossier_personnes WHERE dossier_id=@p0", dos)) MajStatut(c, U.L(p[0]));
    }

    public static void DetHist(DbCtx c, long detId, string ancien, string nouveau, long dec, string com)
        => c.Exec("INSERT INTO detention_hist(detention_id,date,ancien_statut,nouveau_statut,decision_id,commentaire) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", detId, U.D(Engine.Now), ancien, nouveau, dec == 0 ? null : dec, com);

    // ======================================================================= catalogue des décisions
    public record TypeDec(string Code, string Libelle, string Description, bool AvecPersonne);
    public static readonly TypeDec[] Types =
    {
        new("COMPLEMENT", "Demander un complément d'enquête", "Le service enquêteur réalise de nouvelles investigations et verse des pièces complémentaires.", false),
        new("RAPPORT", "Demander un nouveau rapport", "Le service enquêteur ou le service référent produit un rapport actualisé.", false),
        new("OBSERVATIONS", "Solliciter des observations", "Un service ou un partenaire simulé transmet ses observations écrites.", false),
        new("REUNION", "Organiser une réunion sur le dossier", "Une réunion est planifiée avec le référent et le chef de service.", false),
        new("CLASSEMENT", "Classer le dossier (simulation)", "Le dossier est clos sans poursuite ; les personnes concernées sont informées (simulation).", false),
        new("ALTERNATIVE", "Alternative aux poursuites (simulation)", "Une mesure alternative est proposée au mis en cause ; son exécution est suivie par le moteur.", true),
        new("ORIENTATION", "Orienter vers une juridiction (simulation)", "Transmission simulée à une juridiction : information judiciaire, convocation ou comparution immédiate. Le tribunal simulé rendra une décision.", false),
        new("PERSONNE", "Décision concernant la situation d'une personne", "Contrôle judiciaire, détention provisoire, maintien ou mise en liberté : parcours soumis au juge des libertés simulé.", true),
        new("DML", "Avis sur une demande de mise en liberté", "Répondre à une demande de mise en liberté présentée pour une personne détenue (simulation).", true),
    };
    public static TypeDec TypeOf(string code) => Types.First(t => t.Code == code);

    static readonly string[] Fermes = { "Classé sans suite (sim.)", "Jugé (sim.)", "Alternative exécutée (sim.)", "Clos" };
    public static bool Clos(DbCtx c, long dos) => c.Str("SELECT statut FROM dossiers WHERE id=@p0", dos) == "Clôturé";
    static bool EnAttente(DbCtx c, long dos, string code) => c.Long("SELECT COUNT(*) FROM decisions WHERE dossier_id=@p0 AND type=@p1 AND etat_execution='En attente' AND statut='Validée'", dos, code) > 0;

    /// Décisions proposées selon le contexte du dossier (disponible / raison).
    public static List<(TypeDec type, bool ok, string raison)> Disponibles(DbCtx c, long dos)
    {
        var res = new List<(TypeDec, bool, string)>();
        bool clos = Clos(c, dos);
        string sit = c.Str("SELECT IFNULL(situation,'') FROM dossiers WHERE id=@p0", dos);
        bool mec = c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos) > 0;
        bool juridiction = sit is "Transmis à juridiction (sim.)" or "Information judiciaire (sim.)";
        bool detenu = c.Long("SELECT COUNT(*) FROM detentions WHERE dossier_id=@p0 AND date_sortie IS NULL", dos) > 0;
        bool dml = c.Long("SELECT COUNT(*) FROM demandes WHERE statut='En attente' AND type='Demande de mise en liberté' AND ref_type='detention' AND ref_id IN (SELECT id FROM detentions WHERE dossier_id=@p0)", dos) > 0;
        foreach (var t in Types)
        {
            (bool ok, string why) = t.Code switch
            {
                "COMPLEMENT" => clos ? (false, "Dossier clos") : juridiction ? (false, "Dossier déjà transmis à une juridiction") : EnAttente(c, dos, "COMPLEMENT") ? (false, "Un complément est déjà en cours") : (true, ""),
                "RAPPORT" => clos ? (false, "Dossier clos") : EnAttente(c, dos, "RAPPORT") ? (false, "Un rapport est déjà demandé") : (true, ""),
                "OBSERVATIONS" => clos ? (false, "Dossier clos") : EnAttente(c, dos, "OBSERVATIONS") ? (false, "Des observations sont déjà sollicitées") : (true, ""),
                "REUNION" => (true, ""),
                "CLASSEMENT" => clos ? (false, "Dossier déjà clos") : juridiction ? (false, "Juridiction saisie : classement impossible") : (true, ""),
                "ALTERNATIVE" => clos ? (false, "Dossier clos") : !mec ? (false, "Aucun mis en cause identifié") : juridiction ? (false, "Juridiction saisie") : EnAttente(c, dos, "ALTERNATIVE") ? (false, "Une alternative est en cours") : (true, ""),
                "ORIENTATION" => clos ? (false, "Dossier clos") : !mec ? (false, "Aucun mis en cause identifié") : juridiction ? (false, "Juridiction déjà saisie") : sit == "Enquête en cours" ? (false, "Enquête en cours : attendre le rapport de synthèse ou demander un complément") : (true, ""),
                "PERSONNE" => clos ? (false, "Dossier clos") : !mec ? (false, "Aucun mis en cause identifié") : EnAttente(c, dos, "PERSONNE") ? (false, "Une demande au juge des libertés est en cours") : (true, ""),
                "DML" => !detenu ? (false, "Aucune personne détenue dans ce dossier") : !dml ? (false, "Aucune demande de mise en liberté en attente") : (true, ""),
                _ => (false, "")
            };
            res.Add((t, ok, why));
        }
        return res;
    }

    public static bool Detenu(DbCtx c, long pid) => c.Long("SELECT COUNT(*) FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL", pid) > 0;

    public static string[] Orientations(DbCtx c, string code, long dos, long pid) => code switch
    {
        "COMPLEMENT" => new[] { "Audition complémentaire des personnes concernées", "Vérifications techniques (téléphonie, vidéoprotection — simulation)", "Recherche et audition de témoins", "Expertise (simulation)" },
        "RAPPORT" => new[] { "Rapport de synthèse actualisé", "Rapport sur la personnalité du mis en cause (simulation)", "Rapport de situation du service référent" },
        "OBSERVATIONS" => new[] { "Observations du service référent", "Observations du service enquêteur", "Observations de l'association d'aide aux victimes (simulée)" },
        "REUNION" => new[] { "Réunion le prochain jour ouvré à 10h00", "Réunion dans deux jours ouvrés à 14h30", "Réunion dans trois jours ouvrés à 11h00" },
        "CLASSEMENT" => new[] { "Absence d'infraction (simulation)", "Infraction insuffisamment caractérisée (simulation)", "Auteur non identifié (simulation)", "Rappel à la loi / avertissement (simulation)", "Désintéressement de la victime (simulation)" },
        "ALTERNATIVE" => new[] { "Composition pénale (simulation)", "Stage de sensibilisation (simulation)", "Médiation (simulation)" },
        "ORIENTATION" => new[] { "Convocation devant le tribunal correctionnel (simulation)", "Comparution immédiate (simulation)", "Ouverture d'une information judiciaire (simulation)" },
        "PERSONNE" => pid != 0 && Detenu(c, pid)
            ? new[] { "Réquisitions de maintien en détention (simulation)", "Mise en liberté sous contrôle judiciaire (simulation)", "Avis favorable à une mise en liberté (simulation)" }
            : new[] { "Demande de placement sous contrôle judiciaire (simulation)", "Réquisitions de placement en détention provisoire (simulation)", "Laisser la personne libre sans mesure (simulation)" },
        "DML" => new[] { "Avis défavorable — maintien en détention (simulation)", "Avis favorable — mise en liberté sous contrôle judiciaire (simulation)" },
        _ => Array.Empty<string>()
    };

    public record Check(string Libelle, bool Ok, bool Bloquant);

    public static List<Check> Prerequis(DbCtx c, string code, string orient, long dos, long pid, string motivation, bool examine)
    {
        var l = new List<Check>();
        int g = (int)c.Long("SELECT gravite FROM dossiers WHERE id=@p0", dos);
        string sit = c.Str("SELECT IFNULL(situation,'') FROM dossiers WHERE id=@p0", dos);
        long nbPieces = c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0", dos);
        bool synthese = c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND titre LIKE 'Rapport de synthèse%'", dos) > 0;
        bool audMec = pid != 0 ? c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND type='Interrogatoire' AND personne_id=@p1", dos, pid) > 0
                               : c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0 AND type='Interrogatoire'", dos) > 0;
        string decl = pid != 0 ? c.Str("SELECT declaration FROM dossier_personnes WHERE dossier_id=@p0 AND personne_id=@p1 AND role='Mis en cause'", dos, pid) : null;
        int lm = (motivation ?? "").Trim().Length;
        l.Add(new("Les pièces essentielles du dossier ont été examinées", examine, code is "CLASSEMENT" or "ORIENTATION" or "PERSONNE" or "ALTERNATIVE" or "DML"));
        l.Add(new($"Le dossier comporte des pièces ({nbPieces})", nbPieces > 0, true));
        switch (code)
        {
            case "CLASSEMENT":
                l.Add(new("Motivation rédigée (20 caractères minimum)", lm >= 20, true));
                if (g >= 3) l.Add(new("Gravité élevée des faits : un classement est inhabituel (avertissement)", false, false));
                if (orient.StartsWith("Auteur non identifié")) l.Add(new("Aucun mis en cause identifié dans le dossier", c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos) == 0, false));
                break;
            case "ALTERNATIVE":
                l.Add(new("Personne mise en cause sélectionnée", pid != 0, true));
                l.Add(new("La personne a été entendue (audition de mis en cause)", audMec, true));
                l.Add(new("La personne reconnaît les faits", decl == "Reconnaît les faits", true));
                l.Add(new("Gravité des faits compatible (niveau 1 ou 2)", g <= 2, true));
                l.Add(new("Motivation rédigée (40 caractères minimum)", lm >= 40, true));
                break;
            case "ORIENTATION":
                l.Add(new("Rapport de synthèse versé au dossier", synthese, !orient.StartsWith("Ouverture")));
                l.Add(new("Au moins un mis en cause a été entendu", audMec, orient.StartsWith("Comparution")));
                if (orient.StartsWith("Comparution")) l.Add(new("Gravité suffisante pour une comparution immédiate (niveau ≥ 2)", g >= 2, true));
                if (orient.StartsWith("Ouverture")) l.Add(new("Affaire complexe ou grave (niveau 3 recommandé)", g >= 3, false));
                l.Add(new("Motivation rédigée (60 caractères minimum)", lm >= 60, true));
                break;
            case "PERSONNE":
                bool det = pid != 0 && Detenu(c, pid);
                l.Add(new("Personne mise en cause sélectionnée", pid != 0, true));
                l.Add(new("La personne a été entendue (audition de mis en cause)", audMec, !orient.StartsWith("Laisser")));
                if (orient.StartsWith("Réquisitions de placement en détention"))
                {
                    l.Add(new("Juridiction saisie (information judiciaire ou comparution immédiate en cours)", sit is "Information judiciaire (sim.)" or "Transmis à juridiction (sim.)", true));
                    l.Add(new("Gravité suffisante des faits (niveau ≥ 2)", g >= 2, true));
                    l.Add(new("La personne n'est pas déjà détenue", !det, true));
                    l.Add(new("Motivation circonstanciée (120 caractères minimum)", lm >= 120, true));
                }
                else if (orient.StartsWith("Réquisitions de maintien")) { l.Add(new("La personne est détenue dans la simulation", det, true)); l.Add(new("Motivation circonstanciée (100 caractères minimum)", lm >= 100, true)); }
                else if (orient.StartsWith("Demande de placement sous contrôle")) { l.Add(new("Motivation rédigée (60 caractères minimum)", lm >= 60, true)); }
                else l.Add(new("Motivation rédigée (30 caractères minimum)", lm >= 30, true));
                break;
            case "DML":
                l.Add(new("Personne détenue sélectionnée", pid != 0 && Detenu(c, pid), true));
                l.Add(new("Motivation rédigée (60 caractères minimum)", lm >= 60, true));
                break;
            default:
                l.Add(new("Motivation ou précisions rédigées (10 caractères minimum)", lm >= 10, true));
                break;
        }
        return l;
    }

    public static string Effets(string code, string orient) => code switch
    {
        "COMPLEMENT" => "• Le dossier passe en « Complément demandé ».\n• Un soit-transmis (simulé) est versé au dossier et une mission de suivi est confiée au référent.\n• Le service enquêteur versera de nouvelles pièces sous 1 à 3 jours simulés, puis le dossier reviendra « En attente de décision ».",
        "RAPPORT" => "• Un acte de demande est versé au dossier.\n• Un nouveau rapport sera versé sous 1 à 2 jours simulés et vous serez notifié.",
        "OBSERVATIONS" => "• Les observations seront enregistrées dans la section « Observations » du dossier sous quelques heures simulées.",
        "REUNION" => "• Une réunion est immédiatement inscrite à votre agenda avec le référent et son chef de service (vérification des conflits).",
        "CLASSEMENT" => "• Le dossier est clos (« Classé sans suite (sim.) »).\n• Le statut des personnes concernées est recalculé.\n• Un avis de classement simulé est versé au dossier et le référent est informé.\n• Aucune inscription au casier simulé.",
        "ALTERNATIVE" => "• Le dossier passe en « Alternative en cours (sim.) ».\n• L'exécution de la mesure est suivie par le moteur (3 à 10 jours simulés).\n• En cas d'exécution d'une composition pénale, une mention est inscrite au casier simulé.",
        "ORIENTATION" => "• Un bordereau de transmission simulé est versé au dossier.\n• La juridiction simulée rendra une décision (jugement simulé) après un délai fonction de l'orientation.\n• Le jugement mettra à jour le casier, le statut des personnes et, le cas échéant, le module des personnes détenues.",
        "PERSONNE" => orient.StartsWith("Laisser") ? "• La personne reste libre ; aucune mesure n'est sollicitée. Son statut est recalculé." : "• Des réquisitions simulées sont adressées au juge des libertés et de la détention (simulé).\n• Le juge rendra une ordonnance sous quelques heures simulées, qui mettra à jour la fiche de la personne et, le cas échéant, le module des personnes détenues.",
        "DML" => "• Votre avis est transmis au juge des libertés et de la détention (simulé), qui statuera sous quelques heures simulées.",
        _ => ""
    };

    // ======================================================================= enregistrement d'une décision (utilisateur)
    public static long Enregistrer(DbCtx c, string code, string orient, long dos, long pid, string contexte, string verifs, string motivation, long remplaceId = 0)
    {
        Auth.Check("decisions.prendre");
        var checks = Prerequis(c, code, orient, dos, pid, motivation, true);
        var bloc = checks.Where(x => x.Bloquant && !x.Ok && !x.Libelle.StartsWith("Les pièces essentielles")).ToList();
        if (bloc.Count > 0) throw new InvalidOperationException("Prérequis non remplis :\n" + string.Join("\n", bloc.Select(b => "✖ " + b.Libelle)));
        var t = TypeOf(code);
        var now = Engine.Now; var r = R;
        var d = c.Rows("SELECT numero, intitule, situation, referent_id, service_id, service_enqueteur_id FROM dossiers WHERE id=@p0", dos)[0];
        string num = U.S(d[0]); long refe = U.L(d[3]); long org = U.L(d[5]);
        string reff = Biz.Seq(c, "DEC");
        DateTime? due = null; bool immediat = false; string nouvelleSit = null;
        switch (code)
        {
            case "COMPLEMENT": due = now.AddHours(r.Next(20, 70)); nouvelleSit = "Complément demandé"; break;
            case "RAPPORT": due = now.AddHours(r.Next(18, 48)); break;
            case "OBSERVATIONS": due = now.AddHours(r.Next(3, 20)); break;
            case "REUNION": immediat = true; break;
            case "CLASSEMENT": immediat = true; nouvelleSit = "Classé sans suite (sim.)"; break;
            case "ALTERNATIVE": due = now.AddDays(r.Next(3, 11)); nouvelleSit = "Alternative en cours (sim.)"; break;
            case "ORIENTATION":
                if (orient.StartsWith("Comparution")) { due = now.AddHours(r.Next(20, 40)); nouvelleSit = "Transmis à juridiction (sim.)"; }
                else if (orient.StartsWith("Convocation")) { due = now.AddDays(r.Next(5, 16)); nouvelleSit = "Transmis à juridiction (sim.)"; }
                else { due = now.AddDays(r.Next(15, 41)); nouvelleSit = "Information judiciaire (sim.)"; }
                break;
            case "PERSONNE": if (orient.StartsWith("Laisser")) immediat = true; else due = now.AddHours(r.Next(2, 9)); break;
            case "DML": due = now.AddHours(r.Next(3, 24)); break;
        }
        long id = c.Insert(@"INSERT INTO decisions(ref,dossier_id,personne_id,type,libelle,orientation,contexte,verifications,motivation,statut,etat_execution,echeance_effet,date,auteur_id,version,remplace_id,situation_avant,source)
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,'Validée',@p9,@p10,@p11,@p12,@p13,@p14,@p15,'Utilisateur')",
            reff, dos, pid == 0 ? null : pid, code, t.Libelle, orient, contexte, verifs, motivation, immediat ? "Exécutée" : "En attente", due.HasValue ? U.D(due.Value) : null, U.D(now), Auth.CollabId,
            remplaceId == 0 ? 1 : c.Long("SELECT version FROM decisions WHERE id=@p0", remplaceId) + 1, remplaceId == 0 ? null : remplaceId, U.S(d[2]));
        if (remplaceId != 0) c.Exec("UPDATE decisions SET statut='Remplacée', etat_execution='Sans effet' WHERE id=@p0", remplaceId);
        // acte de décision versé au dossier + bibliothèque
        string acte = $"DÉCISION DU PARQUET (SIMULATION) — {reff}\n\nDossier : {num} — {d[1]}\nNature : {t.Libelle}\nOrientation : {orient}\n" + (pid != 0 ? $"Personne concernée : {IdentiteP(c, pid)}\n" : "") +
            $"Date : {U.Aff(U.D(now))}\nAuteur : {Biz.NameF(c, Auth.CollabId)}\n\nCONTEXTE\n{contexte}\n\nVÉRIFICATIONS\n{verifs}\n\nMOTIVATION\n{motivation}\n\n{U.Mention}\nCet acte simulé n'est pas un acte juridique réel.";
        long pieceId = Piece(c, dos, "Acte du parquet", $"{t.Libelle} — {reff}", now, Biz.NameF(c, Auth.CollabId), 0, pid, acte, "Parquet", id);
        c.Exec("UPDATE decisions SET piece_id=@p1 WHERE id=@p0", id, pieceId);
        c.Exec("INSERT INTO documents(numero,titre,categorie,contenu,auteur_id,date,date_modif,dossier_id) VALUES(@p0,@p1,'Décisions du parquet',@p2,@p3,@p4,@p4,@p5)", reff, $"{t.Libelle} — {num}", acte, Auth.CollabId, U.D(now), dos);
        if (nouvelleSit != null) c.Exec("UPDATE dossiers SET situation=@p1 WHERE id=@p0", dos, nouvelleSit);
        Biz.DHist(c, dos, Auth.CollabId, $"Décision {reff} : {t.Libelle} — {orient}");
        Biz.Event(c, "Décision du parquet", Biz.Name(c, Auth.CollabId), Auth.CollabId, $"{reff} — {t.Libelle} ({orient}) — dossier {num}", "decision", id);
        // effets immédiats
        switch (code)
        {
            case "COMPLEMENT":
                Piece(c, dos, "Acte du parquet", "Soit-transmis au service enquêteur (simulation)", now, Biz.NameF(c, Auth.CollabId), org, 0, $"Retour de la procédure {num} au service enquêteur pour : {orient.ToLower()}.\n\nPrécisions : {motivation}\n\n{U.Mention}", "Parquet", id);
                if (refe != 0) try { Biz.CreerMission(c, Auth.CollabId, refe, $"Suivi du complément d'enquête — {num}", $"Suivre l'exécution du complément d'enquête ({orient}).", "Normale", due.Value.AddDays(1), "Me rendre compte du retour du service enquêteur.", "Interne", 0, dos, 0, null, "Décision du parquet"); } catch (InvalidOperationException) { }
                break;
            case "REUNION":
                var day = now.Date.AddDays(orient.Contains("deux") ? 2 : orient.Contains("trois") ? 3 : 1);
                while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) day = day.AddDays(1);
                var deb = day.AddHours(orient.Contains("14h30") ? 14.5 : orient.Contains("11h00") ? 11 : 10);
                var parts = new List<long> { Auth.CollabId }; if (refe != 0) parts.Add(refe);
                long head = c.Long("SELECT responsable_id FROM services WHERE id=@p0", U.L(d[4])); if (head != 0) parts.Add(head);
                for (int i = 0; i < 6 && Biz.Conflits(c, deb, deb.AddHours(1), parts, 0).Count > 0; i++) deb = deb.AddHours(1);
                long rid = c.Insert("INSERT INTO reunions(titre,debut,fin,lieu,organisateur_id,ordre_du_jour,priorite,statut,dossier_id) VALUES(@p0,@p1,@p2,'Bureau du procureur',@p3,@p4,'Haute','Planifiée',@p5)",
                    $"Réunion sur le dossier {num}", U.D(deb), U.D(deb.AddHours(1)), Auth.CollabId, $"1. Point sur le dossier {num} — {d[1]}\n2. {motivation}\n3. Orientation", dos);
                foreach (var p in parts.Distinct()) c.Exec("INSERT OR IGNORE INTO reunion_participants(reunion_id,collab_id,reponse) VALUES(@p0,@p1,'Accepté')", rid, p);
                c.Exec("UPDATE decisions SET resultat=@p1 WHERE id=@p0", id, $"Réunion planifiée le {U.Aff(U.D(deb))}.");
                break;
            case "CLASSEMENT":
                c.Exec("UPDATE dossiers SET statut='Clôturé', avancement=100, date_cloture=@p1 WHERE id=@p0", dos, U.D(now));
                foreach (var p in c.Rows("SELECT personne_id FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos)) c.Exec("UPDATE personnes SET mesure=NULL WHERE id=@p0 AND mesure IS NOT NULL AND NOT EXISTS(SELECT 1 FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL)", p[0]);
                MajStatutsDossier(c, dos);
                Piece(c, dos, "Acte du parquet", "Avis de classement adressé aux parties (simulation)", now, Biz.NameF(c, Auth.CollabId), 0, 0, $"Les parties sont avisées du classement de la procédure {num}.\nMotif : {orient}.\n\n{U.Mention}", "Parquet", id);
                c.Exec("UPDATE decisions SET resultat='Dossier clos.' WHERE id=@p0", id);
                break;
            case "PERSONNE":
                if (orient.StartsWith("Laisser")) { c.Exec("UPDATE personnes SET mesure=NULL WHERE id=@p0", pid); MajStatut(c, pid); c.Exec("UPDATE decisions SET resultat='Aucune mesure sollicitée.' WHERE id=@p0", id); }
                break;
            case "DML":
                c.Exec("UPDATE demandes SET statut='Traitée', reponse=@p1, date_reponse=@p2 WHERE statut='En attente' AND type='Demande de mise en liberté' AND ref_type='detention' AND ref_id IN (SELECT id FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL)", pid, orient, U.D(now));
                break;
        }
        if (refe != 0 && refe != Auth.CollabId)
        {
            long mid = Biz.Message(c, Auth.CollabId, refe, $"Décision {reff} — dossier {num}", $"Pour information et suites à donner :\n\n{t.Libelle}\nOrientation : {orient}\n\nMotivation :\n{motivation}", 0, dos, categorie: "Décision");
            c.Exec("UPDATE messages SET ref_type='decision', ref_id=@p1 WHERE id=@p0", mid, id);
        }
        return id;
    }

    public static bool Modifiable(DbCtx c, long decId)
        => c.Long("SELECT COUNT(*) FROM decisions WHERE id=@p0 AND statut='Validée' AND etat_execution='En attente' AND source='Utilisateur'", decId) > 0;

    public static void Retirer(DbCtx c, long decId, string motif)
    {
        Auth.Check("decisions.prendre");
        if (!Modifiable(c, decId)) throw new InvalidOperationException("Seule une décision dont l'effet est encore en attente peut être retirée.");
        var d = c.Rows("SELECT dossier_id, situation_avant, ref, type FROM decisions WHERE id=@p0", decId)[0];
        c.Exec("UPDATE decisions SET statut='Retirée', etat_execution='Sans effet', resultat=@p1 WHERE id=@p0", decId, "Retirée : " + motif);
        if (U.S(d[3]) is "COMPLEMENT" or "ALTERNATIVE" or "ORIENTATION") c.Exec("UPDATE dossiers SET situation=@p1 WHERE id=@p0", d[0], U.S(d[1]) == "" ? "En attente de décision" : d[1]);
        Biz.DHist(c, U.L(d[0]), Auth.CollabId, $"Décision {d[2]} retirée : {motif}");
        Biz.Event(c, "Décision retirée", Biz.Name(c, Auth.CollabId), Auth.CollabId, $"{d[2]} retirée — {motif}", "decision", decId);
    }

    // ======================================================================= réponses du moteur (juridictions, services)
    public static void Resolutions(DbCtx c, DateTime t)
    {
        foreach (var row in c.Rows("SELECT id FROM decisions WHERE etat_execution='En attente' AND statut='Validée' AND echeance_effet<=@p0 LIMIT 10", U.D(t)))
        {
            long id = U.L(row[0]);
            var d = c.Rows("SELECT type, orientation, dossier_id, personne_id, ref, motivation FROM decisions WHERE id=@p0", id)[0];
            string code = U.S(d[0]), orient = U.S(d[1]), reff = U.S(d[4]); long dos = U.L(d[2]), pid = U.L(d[3]);
            var dd = c.Rows("SELECT numero, intitule, service_enqueteur_id, referent_id FROM dossiers WHERE id=@p0", dos)[0];
            string num = U.S(dd[0]); long org = U.L(dd[2]); long refe = U.L(dd[3]);
            string orgNom = c.Str("SELECT nom FROM organismes WHERE id=@p0", org) ?? "Service enquêteur (simulé)";
            long trib = c.Long("SELECT id FROM organismes WHERE nom LIKE 'Tribunal judiciaire%' LIMIT 1");
            string resultat = "";
            switch (code)
            {
                case "COMPLEMENT":
                {
                    var v = Vars(c, dos); var m = ModeleDe(c, dos);
                    if (orient.StartsWith("Audition"))
                        foreach (var p in c.Rows("SELECT personne_id, role, declaration FROM dossier_personnes WHERE dossier_id=@p0 AND role IN ('Mis en cause','Victime') LIMIT 2", dos))
                            Piece(c, dos, U.S(p[1]) == "Mis en cause" ? "Interrogatoire" : "Audition", $"Audition complémentaire de {NomP(c, U.L(p[0]))}", t, orgNom, org, U.L(p[0]),
                                U.S(p[1]) == "Mis en cause" ? TexteInterrogatoire(c, dos, U.L(p[0]), U.S(p[2]), v, m) : TexteAudition(c, U.L(p[0]), U.S(p[1]), v, m), "Complément d'enquête", id);
                    else if (orient.StartsWith("Recherche"))
                    {
                        long w = NouvellePersonne(c, 20, 70); Lier(c, dos, w, "Témoin", "Complément d'enquête " + reff);
                        Piece(c, dos, "Audition", $"Audition de {NomP(c, w)} (témoin)", t, orgNom, org, w, TexteAudition(c, w, "Témoin", Vars(c, dos), m), "Complément d'enquête", id);
                    }
                    else
                        Piece(c, dos, orient.StartsWith("Expertise") ? "Rapport" : "Procès-verbal", orient.StartsWith("Expertise") ? "Rapport d'expertise (simulation)" : "Procès-verbal de vérifications techniques complémentaires", t, orient.StartsWith("Expertise") ? "Expert inscrit (simulé)" : orgNom, org, 0,
                            $"En exécution du soit-transmis {reff} :\n\n- {R.Pick(new[] { "Les investigations confirment les éléments initiaux.", "Les investigations apportent des éléments nouveaux défavorables au mis en cause.", "Les investigations ne permettent pas de lever les incertitudes.", "Les conclusions techniques corroborent les déclarations de la victime." })}\n- Les pièces techniques (simulées) sont annexées.\n\n{U.Mention}", "Complément d'enquête", id);
                    c.Exec("UPDATE dossiers SET situation='En attente de décision' WHERE id=@p0 AND situation='Complément demandé'", dos);
                    resultat = "Complément d'enquête exécuté ; nouvelles pièces versées.";
                    Externe(c, org, orgNom, dos, $"Retour de complément d'enquête — {num}", $"Monsieur le Procureur,\n\nEn exécution de votre demande {reff}, nous vous retournons la procédure {num} complétée ({orient.ToLower()}).\n\n{orgNom}\n\n(Communication simulée)");
                    Biz.Notify(c, "Décision requise", "Haute", $"Complément reçu — {num}", "Le dossier est de nouveau en attente de votre décision.", "dossier", dos);
                    break;
                }
                case "RAPPORT":
                    Piece(c, dos, "Rapport", orient, t, orient.Contains("référent") ? Biz.NameF(c, refe) : orgNom, org, pid, $"{orient.ToUpper(U.Fr)}\n\n{Biz.Synthese(c).Split('\n')[0]}\nÉtat de la procédure : {c.Long("SELECT COUNT(*) FROM pieces WHERE dossier_id=@p0", dos)} pièces versées.\nAppréciation : {Proposition(c, dos)}.\n\n{U.Mention}", "Rapport demandé", id);
                    resultat = "Rapport versé au dossier.";
                    Biz.Notify(c, "Rapport terminé", "Normale", $"Nouveau rapport — {num}", orient, "dossier", dos);
                    break;
                case "OBSERVATIONS":
                    string auteur = orient.Contains("référent") ? Biz.NameF(c, refe) : orient.Contains("enquêteur") ? orgNom : "Association d'aide aux victimes de Valmont (simulée)";
                    c.Exec("INSERT INTO dossier_notes(dossier_id,date,auteur_id,texte,type,auteur_ext) VALUES(@p0,@p1,NULL,@p2,'Observation du service',@p3)", dos, U.D(t),
                        R.Pick(new[] { "Le service estime que les éléments réunis sont suffisants pour une décision d'orientation.", "Le service attire l'attention sur la situation de la victime et sollicite une réponse rapide.", "Le service suggère une mesure adaptée à la personnalité du mis en cause.", "Le service recommande des investigations complémentaires avant toute décision." }) + " (observations simulées)", auteur);
                    resultat = "Observations reçues.";
                    Biz.Notify(c, "Rapport de service disponible", "Normale", $"Observations reçues — {num}", auteur, "dossier", dos);
                    break;
                case "ALTERNATIVE":
                    if (R.NextDouble() < 0.82)
                    {
                        c.Exec("UPDATE dossiers SET situation='Alternative exécutée (sim.)', statut='Clôturé', avancement=100, date_cloture=@p1 WHERE id=@p0", dos, U.D(t));
                        if (orient.StartsWith("Composition"))
                            c.Exec("INSERT INTO casier(personne_id,dossier_id,decision_id,date,nature,juridiction,mesure,statut,source) VALUES(@p0,@p1,@p2,@p3,@p4,'Parquet de Valmont (simulé)',@p5,'Exécutée (sim.)','Décision PARQUET PRO')",
                                pid, dos, id, U.D(t), c.Str("SELECT qualification FROM dossiers WHERE id=@p0", dos), "Composition pénale exécutée (simulation)");
                        resultat = "Mesure exécutée ; dossier clos.";
                    }
                    else
                    {
                        c.Exec("UPDATE dossiers SET situation='En attente de décision' WHERE id=@p0", dos);
                        resultat = "Mesure non exécutée par le mis en cause ; nouvelle décision nécessaire.";
                        Biz.Notify(c, "Décision requise", "Haute", $"Alternative non exécutée — {num}", "Une nouvelle orientation est nécessaire.", "dossier", dos);
                    }
                    Piece(c, dos, "Courrier", "Compte rendu d'exécution de la mesure alternative (simulation)", t, "Délégué du procureur (simulé)", 0, pid, resultat + "\n\n" + U.Mention, "Exécution", id);
                    MajStatutsDossier(c, dos);
                    break;
                case "ORIENTATION":
                    resultat = Jugement(c, dos, id, orient, t);
                    Externe(c, trib, "Greffe du " + Tribunal, dos, $"Décision rendue — {num}", $"Monsieur le Procureur,\n\nLa juridiction (simulée) a statué dans la procédure {num} :\n\n{resultat}\n\nLe greffe (simulé)\n\n{U.Mention}");
                    Biz.Notify(c, "Décision juridictionnelle", "Haute", $"Jugement simulé rendu — {num}", resultat.Split('\n')[0], "dossier", dos);
                    break;
                case "PERSONNE":
                case "DML":
                    resultat = Jld(c, dos, pid, id, orient, U.S(d[5]), t);
                    Externe(c, trib, JLD, dos, $"Ordonnance du juge des libertés — {num}", $"Monsieur le Procureur,\n\nSuite à vos réquisitions {reff} concernant {NomP(c, pid)} :\n\n{resultat}\n\n{U.Mention}");
                    Biz.Notify(c, "Décision juridictionnelle", "Haute", $"Ordonnance du JLD (simulé) — {NomP(c, pid)}", resultat, "personne", pid);
                    break;
            }
            c.Exec("UPDATE decisions SET etat_execution='Exécutée', resultat=@p1 WHERE id=@p0", id, resultat);
            Biz.DHist(c, dos, 0, $"Effet de la décision {reff} : {resultat.Split('\n')[0]}");
            Biz.Event(c, "Effet d'une décision", "Moteur judiciaire", 0, $"{reff} ({num}) : {resultat.Split('\n')[0]}", "decision", id);
        }
    }

    static void Externe(DbCtx c, long org, string exp, long dos, string objet, string corps)
    {
        long mid = Biz.Message(c, 0, Me, objet, corps, 0, dos, false, exp, org, "Externe");
        c.Exec("UPDATE messages SET ref_type='dossier', ref_id=@p1 WHERE id=@p0", mid, dos);
    }

    static long NouvelleDetention(DbCtx c, long pid, long dos, long dec, string regime, int mois, DateTime t, string motif, string refExt = null)
    {
        long id = c.Insert("INSERT INTO detentions(personne_id,dossier_id,decision_id,ref_externe,etablissement,regime,date_entree,duree_mois,statut,motif) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,'En cours',@p8)",
            pid, dos == 0 ? null : dos, dec == 0 ? null : dec, refExt, R.Pick(Etablissements), regime, U.D(t), mois == 0 ? null : mois, motif);
        c.Exec("INSERT INTO detention_hist(detention_id,date,ancien_statut,nouveau_statut,decision_id,commentaire) VALUES(@p0,@p1,'Libre',@p2,@p3,@p4)", id, U.D(t), regime, dec == 0 ? null : dec, motif);
        c.Exec("UPDATE personnes SET mesure=NULL WHERE id=@p0", pid);
        return id;
    }

    static void FinDetention(DbCtx c, long pid, long dec, string statut, string com, DateTime t)
    {
        foreach (var det in c.Rows("SELECT id, regime FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL", pid))
        {
            c.Exec("UPDATE detentions SET date_sortie=@p1, statut=@p2 WHERE id=@p0", det[0], U.D(t), statut);
            c.Exec("INSERT INTO detention_hist(detention_id,date,ancien_statut,nouveau_statut,decision_id,commentaire) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", det[0], U.D(t), det[1], statut, dec == 0 ? null : dec, com);
        }
    }

    /// Jugement simulé : met à jour casier, détention, statuts et dossier.
    public static string Jugement(DbCtx c, long dos, long dec, string orient, DateTime t, bool historique = false)
    {
        var r = R;
        int g = (int)c.Long("SELECT gravite FROM dossiers WHERE id=@p0", dos);
        string qualif = c.Str("SELECT qualification FROM dossiers WHERE id=@p0", dos);
        long org = c.Long("SELECT id FROM organismes WHERE nom LIKE 'Tribunal judiciaire%' LIMIT 1");
        var sb = new StringBuilder();
        foreach (var p in c.Rows("SELECT personne_id FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos).Select(x => U.L(x[0])))
        {
            int s = Score(c, dos, p);
            long ant = c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", p);
            bool detenu = Detenu(c, p);
            double pRelaxe = s <= 3 ? 0.55 : s <= 6 ? 0.18 : 0.05;
            string mesure; int mois = 0; bool ferme = false;
            if (r.NextDouble() < pRelaxe) mesure = "Relaxe (simulation)";
            else
            {
                int sev = g + (ant > 0 ? 1 : 0);
                if (sev <= 1) mesure = $"Amende de {r.Next(2, 16) * 100} euros (simulation)";
                else if (sev == 2) mesure = $"Emprisonnement de {r.Next(2, 9)} mois avec sursis (simulation)";
                else if (sev == 3) { mois = r.Next(4, 13); int sursis = r.Next(1, mois); mesure = $"Emprisonnement de {mois} mois dont {sursis} mois avec sursis (simulation)"; mois -= sursis; ferme = orient.StartsWith("Comparution") || detenu; }
                else { mois = r.Next(12, 49); mesure = $"Emprisonnement ferme de {mois} mois avec maintien ou mandat de dépôt (simulation)"; ferme = true; }
            }
            string ligne = $"{NomP(c, p)} : {mesure}.";
            if (!mesure.StartsWith("Relaxe"))
                c.Exec("INSERT INTO casier(personne_id,dossier_id,decision_id,date,nature,juridiction,mesure,statut,source) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,'Définitive (sim.)',@p7)",
                    p, dos, dec == 0 ? null : dec, U.D(t), qualif, Tribunal, mesure, historique ? "Historique simulé" : "Décision PARQUET PRO");
            if (ferme && mois > 0)
            {
                var cur = c.Rows("SELECT id, regime FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL", p);
                if (cur.Count > 0)
                {
                    c.Exec("UPDATE detentions SET regime='Exécution de peine (sim.)', duree_mois=@p1, decision_id=@p2 WHERE id=@p0", cur[0][0], mois, dec == 0 ? null : dec);
                    c.Exec("INSERT INTO detention_hist(detention_id,date,ancien_statut,nouveau_statut,decision_id,commentaire) VALUES(@p0,@p1,@p2,'Exécution de peine (sim.)',@p3,'Condamnation (simulation)')", cur[0][0], U.D(t), cur[0][1], dec == 0 ? null : dec);
                }
                else NouvelleDetention(c, p, dos, dec, "Exécution de peine (sim.)", mois, t, "Mandat de dépôt à l'audience (simulation)");
                ligne += $" Incarcération : {mois} mois ferme(s) (simulation).";
            }
            else if (detenu) { FinDetention(c, p, dec, "Libéré (sim.)", "Libération à l'issue du jugement (simulation)", t); ligne += " Remise en liberté (simulation)."; }
            c.Exec("UPDATE personnes SET mesure=NULL WHERE id=@p0", p);
            sb.AppendLine(ligne);
        }
        if (sb.Length == 0) sb.AppendLine("Aucune personne poursuivie.");
        c.Exec("UPDATE dossiers SET situation='Jugé (sim.)', statut='Clôturé', avancement=100, date_cloture=@p1 WHERE id=@p0", dos, U.D(t));
        string txt = $"Jugement simulé — {Tribunal}\n{(orient.StartsWith("Ouverture") ? "À l'issue de l'information judiciaire simulée et du renvoi devant le tribunal.\n" : "")}Qualification : {qualif}\n\n{sb}";
        Piece(c, dos, "Jugement", "Jugement du tribunal correctionnel (simulation)", t, "Greffe du " + Tribunal, org, 0, txt + "\n" + U.Mention, "Juridiction", dec);
        MajStatutsDossier(c, dos);
        return sb.ToString().Trim();
    }

    static string Jld(DbCtx c, long dos, long pid, long dec, string orient, string motivation, DateTime t)
    {
        var r = R;
        int s = Score(c, dos, pid);
        int g = (int)c.Long("SELECT gravite FROM dossiers WHERE id=@p0", dos);
        long ant = c.Long("SELECT COUNT(*) FROM casier WHERE personne_id=@p0", pid);
        string nom = NomP(c, pid);
        string res;
        if (orient.StartsWith("Réquisitions de placement en détention"))
        {
            double p = Math.Min(0.92, 0.3 + 0.06 * s + (g >= 3 ? 0.15 : 0) + (ant > 0 ? 0.1 : 0) + ((motivation ?? "").Length > 250 ? 0.1 : 0));
            if (r.NextDouble() < p)
            {
                long det = NouvelleDetention(c, pid, dos, dec, "Détention provisoire (sim.)", 0, t, "Ordonnance de placement en détention provisoire (simulation)");
                res = $"Placement de {nom} en détention provisoire (simulation) — établissement : {c.Str("SELECT etablissement FROM detentions WHERE id=@p0", det)}.";
            }
            else if (r.NextDouble() < 0.65) { c.Exec("UPDATE personnes SET mesure='Sous contrôle judiciaire (sim.)' WHERE id=@p0", pid); res = $"Réquisitions rejetées : {nom} est placé(e) sous contrôle judiciaire (simulation). Motif : garanties de représentation jugées suffisantes."; }
            else res = $"Réquisitions rejetées : {nom} reste libre (simulation). Motif : éléments insuffisants pour justifier une mesure de sûreté.";
        }
        else if (orient.StartsWith("Demande de placement sous contrôle"))
        {
            if (r.NextDouble() < 0.85) { c.Exec("UPDATE personnes SET mesure='Sous contrôle judiciaire (sim.)' WHERE id=@p0", pid); res = $"{nom} est placé(e) sous contrôle judiciaire (simulation) : obligation de pointage hebdomadaire, interdiction d'entrer en contact avec la victime."; }
            else res = $"Demande rejetée : {nom} reste libre sans mesure (simulation).";
        }
        else if (orient.StartsWith("Réquisitions de maintien") || orient.StartsWith("Avis défavorable"))
        {
            if (r.NextDouble() < 0.72) { res = $"Maintien en détention de {nom} (simulation)."; foreach (var d in c.Rows("SELECT id, regime FROM detentions WHERE personne_id=@p0 AND date_sortie IS NULL", pid)) DetHist(c, U.L(d[0]), U.S(d[1]), U.S(d[1]), dec, "Maintien en détention ordonné (simulation)"); }
            else { FinDetention(c, pid, dec, "Libéré (sim.)", "Mise en liberté sous contrôle judiciaire (simulation)", t); c.Exec("UPDATE personnes SET mesure='Sous contrôle judiciaire (sim.)' WHERE id=@p0", pid); res = $"Mise en liberté de {nom} sous contrôle judiciaire, contrairement aux réquisitions (simulation)."; }
        }
        else
        {
            if (r.NextDouble() < 0.9)
            {
                FinDetention(c, pid, dec, "Libéré (sim.)", "Mise en liberté (simulation)", t);
                bool cj = orient.Contains("contrôle judiciaire");
                c.Exec("UPDATE personnes SET mesure=@p1 WHERE id=@p0", pid, cj ? "Sous contrôle judiciaire (sim.)" : null);
                res = $"Mise en liberté de {nom}{(cj ? " sous contrôle judiciaire" : "")} (simulation).";
            }
            else res = $"Le juge (simulé) ordonne le maintien en détention de {nom} malgré l'avis favorable.";
        }
        long trib = c.Long("SELECT id FROM organismes WHERE nom LIKE 'Tribunal judiciaire%' LIMIT 1");
        Piece(c, dos, "Jugement", "Ordonnance du juge des libertés et de la détention (simulation)", t, JLD, trib, pid, res + "\n\n" + U.Mention, "Juridiction", dec);
        MajStatut(c, pid);
        return res;
    }

    // ======================================================================= événements du moteur
    public static void EvtNouvelleAffaire(DbCtx c, DateTime t)
    {
        if (c.Long("SELECT COUNT(*) FROM dossiers WHERE situation='Enquête en cours'") > 22) return;
        if (c.Long("SELECT COUNT(*) FROM dossiers WHERE situation='En attente de décision'") > 18) return;
        var r = R;
        var m = r.Pick(Modeles);
        string catOrg = m.Cat is "Parquet civil" ? "Institution" : m.Cat is "Coopération" ? "Institution" : m.Cat is "Exécution des peines" ? "Administration partenaire" : "Service d'enquête";
        long org = c.Long("SELECT id FROM organismes WHERE categorie=@p0 ORDER BY RANDOM() LIMIT 1", catOrg);
        if (org == 0) org = c.Long("SELECT id FROM organismes ORDER BY RANDOM() LIMIT 1");
        long svc = c.Long("SELECT id FROM services WHERE code=@p0", CatService.TryGetValue(m.Cat, out var code) ? code : "GEST");
        long referent = c.Long("SELECT id FROM collaborateurs WHERE service_id=@p0 AND niveau>=6 AND dispo<>'Absent' AND actif=1 ORDER BY charge ASC LIMIT 1", svc);
        string num = Biz.Seq(c, "DOS");
        string prio = m.Grav >= 3 ? "Haute" : m.Grav == 2 ? "Normale" : "Basse";
        long dos = c.Insert("INSERT INTO dossiers(numero,intitule,categorie,service_id,referent_id,organisme_id,statut,priorite,avancement,date_ouverture,echeance) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Ouvert',@p6,0,@p7,@p8)",
            num, m.Titre + " (simulation)", m.Cat, svc, referent, org, prio, U.D(t), U.D(t.AddDays(r.Next(20, 60))));
        Generer(c, dos, m.Cat, org, t, false, m);
        Biz.DHist(c, dos, referent, $"Réception de la procédure transmise par {c.Str("SELECT nom FROM organismes WHERE id=@p0", org)}");
        c.Exec("UPDATE organismes SET activite=activite+1 WHERE id=@p0", org);
        var pers = c.Rows("SELECT p.prenom||' '||p.nom, dp.role FROM dossier_personnes dp JOIN personnes p ON p.id=dp.personne_id WHERE dp.dossier_id=@p0", dos);
        var pcs = c.Rows("SELECT cote, titre FROM pieces WHERE dossier_id=@p0", dos);
        string orgNom = c.Str("SELECT nom FROM organismes WHERE id=@p0", org);
        Externe(c, org, orgNom, dos, $"Transmission d'une procédure — {num} {m.Titre}",
            $"Monsieur le Procureur,\n\nNous vous transmettons la procédure {num} relative à : {c.Str("SELECT qualification FROM dossiers WHERE id=@p0", dos)}.\n\nFaits :\n{c.Str("SELECT faits FROM dossiers WHERE id=@p0", dos)}\n\nPersonnes concernées :\n{string.Join("\n", pers.Select(p => $" - {p[0]} ({p[1]})"))}\n\nPièces transmises :\n{string.Join("\n", pcs.Select(p => $" - {p[0]} {p[1]}"))}\n\nL'enquête se poursuit sous votre contrôle.\n\n{orgNom}\n(Communication simulée)");
        Biz.Event(c, "Nouvelle affaire", orgNom, 0, $"Procédure {num} « {m.Titre} » transmise — référent : {Biz.Name(c, referent)}", "dossier", dos, orgId: org, svc: svc);
        Biz.Notify(c, "Nouvelle affaire", m.Grav >= 3 ? "Haute" : "Normale", $"Nouvelle affaire {num}", $"{m.Titre} — {m.Cat}", "dossier", dos);
    }

    public static void EvtEnquete(DbCtx c, DateTime t)
    {
        var d = c.Rows("SELECT d.id FROM dossiers d LEFT JOIN collaborateurs c ON c.id=d.referent_id WHERE d.situation='Enquête en cours' AND d.statut<>'Clôturé' AND IFNULL(c.dispo,'Disponible')<>'Absent' AND d.date_transmission<=@p0 ORDER BY RANDOM() LIMIT 1", U.D(t.AddHours(-4)));
        if (d.Count == 0) return;
        long dos = U.L(d[0][0]);
        PieceSuivante(c, dos, t, true);
    }

    public static void EvtDetentions(DbCtx c, DateTime t)
    {
        foreach (var d in c.Rows("SELECT id, personne_id, regime, date_entree, duree_mois FROM detentions WHERE date_sortie IS NULL AND regime='Exécution de peine (sim.)' AND duree_mois IS NOT NULL"))
        {
            if (U.P(d[3]).AddMonths((int)U.L(d[4])) > t) continue;
            long pid = U.L(d[1]);
            FinDetention(c, pid, 0, "Fin de peine (sim.)", "Libération en fin de peine (simulation)", t);
            MajStatut(c, pid);
            Biz.Event(c, "Détention", "Établissement pénitentiaire (simulé)", 0, $"Libération en fin de peine de {NomP(c, pid)}", "personne", pid);
        }
    }

    public static void EvtDemandeMiseEnLiberte(DbCtx c, DateTime t)
    {
        var d = c.Rows(@"SELECT d.id, d.personne_id, d.dossier_id FROM detentions d WHERE d.date_sortie IS NULL AND d.regime='Détention provisoire (sim.)' AND d.dossier_id IS NOT NULL
            AND NOT EXISTS(SELECT 1 FROM demandes x WHERE x.ref_type='detention' AND x.ref_id=d.id AND x.statut='En attente') ORDER BY RANDOM() LIMIT 1");
        if (d.Count == 0) return;
        long det = U.L(d[0][0]), pid = U.L(d[0][1]), dos = U.L(d[0][2]);
        string num = c.Str("SELECT numero FROM dossiers WHERE id=@p0", dos);
        long avocatOrg = c.Long("SELECT id FROM organismes WHERE nom LIKE 'Barreau%' LIMIT 1");
        string avocat = "Me " + c.Str("SELECT prenom||' '||nom FROM interlocuteurs WHERE organisme_id=@p0 ORDER BY RANDOM() LIMIT 1", avocatOrg) + " (avocat simulé)";
        long id = c.Insert("INSERT INTO demandes(date,demandeur_id,type,objet,texte,ref_type,ref_id) VALUES(@p0,NULL,'Demande de mise en liberté',@p1,@p2,'detention',@p3)", U.D(t),
            $"Mise en liberté de {NomP(c, pid)} — {num}", $"{avocat} présente une demande de mise en liberté (simulation) pour {IdentiteP(c, pid)}, détenu(e) dans le cadre du dossier {num}. Votre avis est requis.", det);
        Piece(c, dos, "Courrier", $"Demande de mise en liberté présentée pour {NomP(c, pid)} (simulation)", t, avocat, avocatOrg, pid, $"{avocat} sollicite la mise en liberté de son client, invoquant des garanties de représentation (domicile, emploi).\n\n{U.Mention}", "Défense");
        Externe(c, avocatOrg, avocat, dos, $"Demande de mise en liberté — {NomP(c, pid)}", $"Monsieur le Procureur,\n\nJ'ai l'honneur de présenter une demande de mise en liberté pour {NomP(c, pid)} (dossier {num}).\n\n{avocat}\n(Communication simulée)");
        Biz.Notify(c, "Décision requise", "Haute", $"Demande de mise en liberté — {NomP(c, pid)}", $"Dossier {num} : avis du parquet requis (module Décisions).", "dossier", dos);
        Biz.Event(c, "Demande de mise en liberté", avocat, 0, $"Pour {NomP(c, pid)} — dossier {num}", "demande", id);
    }

    // ======================================================================= reprise des données existantes (migration v2)
    public static void Backfill(Action<string> progress = null)
    {
        if (Db.Get("jud_v2", "0") == "1") return;
        using var c = new DbCtx(true);
        var r = R;
        progress?.Invoke("Création de la population fictive et des antécédents…");
        var natures = Modeles.Where(m => m.Cat != "Parquet civil" && m.Cat != "Coopération").Select(m => m.Qualif.Replace("{itt}", "8")).ToArray();
        for (int i = 0; i < 70; i++)
        {
            long p = NouvellePersonne(c, 20, 60);
            int n = r.Next(1, 4);
            for (int k = 0; k < n; k++)
            {
                var dt = Engine.Now.AddDays(-r.Next(200, 3000));
                c.Exec("INSERT INTO casier(personne_id,ref_externe,date,nature,juridiction,mesure,statut,source) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,'Définitive (sim.)','Historique simulé')",
                    p, $"DOS-{dt.Year}-A{r.Next(1000, 9999)} (archive simulée)", U.D(dt), r.Pick(natures), Tribunal,
                    r.Pick(new[] { $"Amende de {r.Next(2, 10) * 100} euros (simulation)", $"Emprisonnement de {r.Next(2, 8)} mois avec sursis (simulation)", "Travail d'intérêt général de 70 heures (simulation)", $"Emprisonnement ferme de {r.Next(6, 30)} mois (simulation)" }));
            }
            if (i < 12)
            {
                var entree = Engine.Now.AddDays(-r.Next(30, 500));
                NouvelleDetention(c, p, 0, 0, "Exécution de peine (sim.)", r.Next(18, 40), entree, "Exécution d'une condamnation antérieure (simulation)", $"DOS-{entree.Year - 1}-A{r.Next(1000, 9999)} (archive simulée)");
            }
            MajStatut(c, p);
        }
        progress?.Invoke("Enrichissement des dossiers existants…");
        foreach (var d in c.Rows("SELECT id, categorie, organisme_id, date_ouverture, statut, avancement, date_cloture FROM dossiers WHERE situation IS NULL ORDER BY id"))
        {
            long dos = U.L(d[0]); string cat = U.S(d[1]); long org = U.L(d[2]);
            if (c.Str("SELECT categorie FROM organismes WHERE id=@p0", org) != "Service d'enquête" && cat != "Parquet civil" && cat != "Coopération") org = 0;
            bool clos = U.S(d[4]) == "Clôturé";
            bool complet = clos || U.L(d[5]) > 55;
            var ouv = U.P(d[3]); if (ouv == DateTime.MinValue) ouv = Engine.Now.AddDays(-10);
            var cands = Modeles.Where(m => m.Cat == cat).ToList();
            var mod = cands.Count > 0 ? r.Pick(cands) : r.Pick(Modeles);
            Generer(c, dos, cat, org, ouv, complet, mod);
            c.Exec("UPDATE dossiers SET intitule=@p1, priorite=@p2 WHERE id=@p0", dos, mod.Titre + " (simulation)", mod.Grav >= 3 ? "Haute" : mod.Grav == 2 ? "Normale" : "Basse");
            if (clos)
            {
                var dc = U.P(d[6]); if (dc == DateTime.MinValue || dc < ouv) dc = ouv.AddDays(r.Next(5, 30));
                bool mec = c.Long("SELECT COUNT(*) FROM dossier_personnes WHERE dossier_id=@p0 AND role='Mis en cause'", dos) > 0;
                bool juge = mec && r.NextDouble() < 0.55;
                string reff = Biz.Seq(c, "DEC");
                string orient = juge ? "Convocation devant le tribunal correctionnel (simulation)" : mec ? "Infraction insuffisamment caractérisée (simulation)" : "Auteur non identifié (simulation)";
                long id = c.Insert(@"INSERT INTO decisions(ref,dossier_id,type,libelle,orientation,contexte,verifications,motivation,statut,etat_execution,date,auteur_id,source)
                    VALUES(@p0,@p1,@p2,@p3,@p4,'Décision antérieure à la prise de fonction (historique simulé).','—','Décision reprise de l''historique simulé.','Validée','Exécutée',@p5,1,'Historique simulé')",
                    reff, dos, juge ? "ORIENTATION" : "CLASSEMENT", juge ? TypeOf("ORIENTATION").Libelle : TypeOf("CLASSEMENT").Libelle, orient, U.D(dc.AddDays(-3)));
                string res;
                if (juge) res = Jugement(c, dos, id, orient, dc, true);
                else { c.Exec("UPDATE dossiers SET situation='Classé sans suite (sim.)', date_cloture=@p1 WHERE id=@p0", dos, U.D(dc)); res = "Dossier classé (historique)."; MajStatutsDossier(c, dos); }
                c.Exec("UPDATE decisions SET resultat=@p1 WHERE id=@p0", id, res);
                c.Exec("UPDATE dossiers SET statut='Clôturé', avancement=100 WHERE id=@p0", dos);
            }
            else
            {
                c.Exec("UPDATE dossiers SET situation=@p1 WHERE id=@p0", dos, complet ? "En attente de décision" : "Enquête en cours");
                MajStatutsDossier(c, dos);
            }
        }
        c.Set("jud_v2", 1);
        c.Commit();
    }
}
