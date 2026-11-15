namespace ParquetPro;

public static class Schema
{
    public const int Version = 2;

    const string V1 = @"
CREATE TABLE IF NOT EXISTS parametres(cle TEXT PRIMARY KEY, valeur TEXT);
CREATE TABLE IF NOT EXISTS services(id INTEGER PRIMARY KEY AUTOINCREMENT, code TEXT UNIQUE, nom TEXT NOT NULL, categorie TEXT, effectif_cible INTEGER DEFAULT 0, responsable_id INTEGER, rattachement_id INTEGER, description TEXT, actif INTEGER DEFAULT 1);
CREATE TABLE IF NOT EXISTS collaborateurs(id INTEGER PRIMARY KEY AUTOINCREMENT, matricule TEXT UNIQUE, prenom TEXT, nom TEXT, fonction TEXT, niveau INTEGER, service_id INTEGER, superieur_id INTEGER,
  competences TEXT, responsabilites TEXT, profil TEXT, autonomie INTEGER DEFAULT 3, horaire_debut INTEGER DEFAULT 9, horaire_fin INTEGER DEFAULT 18, charge INTEGER DEFAULT 0, charge_base INTEGER DEFAULT 15,
  dispo TEXT DEFAULT 'Disponible', telephone TEXT, email TEXT, date_entree TEXT, actif INTEGER DEFAULT 1, notes TEXT);
CREATE INDEX IF NOT EXISTS ix_col_service ON collaborateurs(service_id);
CREATE INDEX IF NOT EXISTS ix_col_sup ON collaborateurs(superieur_id);
CREATE TABLE IF NOT EXISTS affectations_hist(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, collab_id INTEGER, ancien_service INTEGER, nouveau_service INTEGER, ancien_sup INTEGER, nouveau_sup INTEGER, motif TEXT, auteur TEXT);
CREATE TABLE IF NOT EXISTS organismes(id INTEGER PRIMARY KEY AUTOINCREMENT, nom TEXT, categorie TEXT, description TEXT, adresse TEXT, telephone TEXT, activite INTEGER DEFAULT 0);
CREATE TABLE IF NOT EXISTS interlocuteurs(id INTEGER PRIMARY KEY AUTOINCREMENT, organisme_id INTEGER, prenom TEXT, nom TEXT, fonction TEXT, telephone TEXT, email TEXT);
CREATE TABLE IF NOT EXISTS dossiers(id INTEGER PRIMARY KEY AUTOINCREMENT, numero TEXT UNIQUE, intitule TEXT, categorie TEXT, resume TEXT, service_id INTEGER, referent_id INTEGER, organisme_id INTEGER, personnes TEXT,
  statut TEXT DEFAULT 'Ouvert', priorite TEXT DEFAULT 'Normale', avancement INTEGER DEFAULT 0, date_ouverture TEXT, echeance TEXT, date_cloture TEXT, archive INTEGER DEFAULT 0);
CREATE INDEX IF NOT EXISTS ix_dos_statut ON dossiers(statut, archive);
CREATE TABLE IF NOT EXISTS dossier_notes(id INTEGER PRIMARY KEY AUTOINCREMENT, dossier_id INTEGER, date TEXT, auteur_id INTEGER, texte TEXT);
CREATE TABLE IF NOT EXISTS dossier_hist(id INTEGER PRIMARY KEY AUTOINCREMENT, dossier_id INTEGER, date TEXT, auteur_id INTEGER, texte TEXT);
CREATE INDEX IF NOT EXISTS ix_doshist ON dossier_hist(dossier_id);
CREATE TABLE IF NOT EXISTS missions(id INTEGER PRIMARY KEY AUTOINCREMENT, numero TEXT UNIQUE, parent_id INTEGER, racine_id INTEGER, profondeur INTEGER DEFAULT 0, objet TEXT, description TEXT, emetteur_id INTEGER, destinataire_id INTEGER,
  service_id INTEGER, priorite TEXT DEFAULT 'Normale', statut TEXT DEFAULT 'Nouvelle', avancement REAL DEFAULT 0, date_creation TEXT, echeance TEXT, instructions TEXT, confidentialite TEXT DEFAULT 'Interne',
  dossier_id INTEGER, document_id INTEGER, rapport TEXT, date_rapport TEXT, date_cloture TEXT, vitesse REAL DEFAULT 0.1, analyse_min INTEGER DEFAULT 45, retard_signale INTEGER DEFAULT 0, origine TEXT DEFAULT 'Utilisateur');
CREATE INDEX IF NOT EXISTS ix_mis_statut ON missions(statut);
CREATE INDEX IF NOT EXISTS ix_mis_dest ON missions(destinataire_id);
CREATE INDEX IF NOT EXISTS ix_mis_parent ON missions(parent_id);
CREATE INDEX IF NOT EXISTS ix_mis_emet ON missions(emetteur_id);
CREATE TABLE IF NOT EXISTS sous_taches(id INTEGER PRIMARY KEY AUTOINCREMENT, mission_id INTEGER, libelle TEXT, assigne_id INTEGER, statut TEXT DEFAULT 'À faire', avancement REAL DEFAULT 0, date_fin TEXT);
CREATE INDEX IF NOT EXISTS ix_st_mis ON sous_taches(mission_id);
CREATE INDEX IF NOT EXISTS ix_st_ass ON sous_taches(assigne_id);
CREATE TABLE IF NOT EXISTS mission_hist(id INTEGER PRIMARY KEY AUTOINCREMENT, mission_id INTEGER, date TEXT, auteur_id INTEGER, type TEXT, texte TEXT);
CREATE INDEX IF NOT EXISTS ix_mh ON mission_hist(mission_id);
CREATE TABLE IF NOT EXISTS messages(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, exp_id INTEGER, exp_ext TEXT, dest_id INTEGER, cc TEXT, objet TEXT, corps TEXT, lu INTEGER DEFAULT 0, important INTEGER DEFAULT 0, suivi INTEGER DEFAULT 0,
  etat_exp TEXT DEFAULT 'actif', etat_dest TEXT DEFAULT 'actif', brouillon INTEGER DEFAULT 0, mission_id INTEGER, dossier_id INTEGER, organisme_id INTEGER, document_id INTEGER, conversation_id INTEGER, attente_reponse INTEGER DEFAULT 0, categorie TEXT);
CREATE INDEX IF NOT EXISTS ix_msg_dest ON messages(dest_id, etat_dest);
CREATE INDEX IF NOT EXISTS ix_msg_exp ON messages(exp_id, etat_exp);
CREATE INDEX IF NOT EXISTS ix_msg_date ON messages(date);
CREATE TABLE IF NOT EXISTS conversations(id INTEGER PRIMARY KEY AUTOINCREMENT, sujet TEXT, date TEXT);
CREATE TABLE IF NOT EXISTS documents(id INTEGER PRIMARY KEY AUTOINCREMENT, numero TEXT, titre TEXT, categorie TEXT, contenu TEXT, auteur_id INTEGER, date TEXT, date_modif TEXT, dossier_id INTEGER, mission_id INTEGER, reunion_id INTEGER, archive INTEGER DEFAULT 0, version INTEGER DEFAULT 1);
CREATE INDEX IF NOT EXISTS ix_doc_cat ON documents(categorie, archive);
CREATE TABLE IF NOT EXISTS reunions(id INTEGER PRIMARY KEY AUTOINCREMENT, titre TEXT, debut TEXT, fin TEXT, lieu TEXT, organisateur_id INTEGER, ordre_du_jour TEXT, priorite TEXT DEFAULT 'Normale', statut TEXT DEFAULT 'Planifiée',
  compte_rendu TEXT, rappel_min INTEGER DEFAULT 15, rappel_envoye INTEGER DEFAULT 0, dossier_id INTEGER, mission_id INTEGER, document_id INTEGER, service_id INTEGER);
CREATE INDEX IF NOT EXISTS ix_reu_debut ON reunions(debut);
CREATE TABLE IF NOT EXISTS reunion_participants(reunion_id INTEGER, collab_id INTEGER, reponse TEXT DEFAULT 'Invité', PRIMARY KEY(reunion_id, collab_id));
CREATE INDEX IF NOT EXISTS ix_rp_col ON reunion_participants(collab_id);
CREATE TABLE IF NOT EXISTS appels(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, sens TEXT, statut TEXT, interlocuteur TEXT, interlocuteur_id INTEGER, organisme_id INTEGER, collab_id INTEGER, duree INTEGER DEFAULT 0, motif TEXT, notes TEXT, compte_rendu TEXT, dossier_id INTEGER, rappel INTEGER DEFAULT 0);
CREATE INDEX IF NOT EXISTS ix_app_date ON appels(date);
CREATE TABLE IF NOT EXISTS evenements(id INTEGER PRIMARY KEY AUTOINCREMENT, uid TEXT UNIQUE, date TEXT, type TEXT, origine TEXT, origine_id INTEGER, description TEXT, etat TEXT DEFAULT 'Enregistré', ref_type TEXT, ref_id INTEGER, organisme_id INTEGER, service_id INTEGER);
CREATE INDEX IF NOT EXISTS ix_evt_date ON evenements(date);
CREATE INDEX IF NOT EXISTS ix_evt_ref ON evenements(ref_type, ref_id);
CREATE TABLE IF NOT EXISTS notifications(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, type TEXT, importance TEXT, titre TEXT, texte TEXT, lu INTEGER DEFAULT 0, archive INTEGER DEFAULT 0, ref_type TEXT, ref_id INTEGER, affichee INTEGER DEFAULT 0);
CREATE INDEX IF NOT EXISTS ix_notif ON notifications(lu, archive);
CREATE TABLE IF NOT EXISTS demandes(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, demandeur_id INTEGER, type TEXT, objet TEXT, texte TEXT, statut TEXT DEFAULT 'En attente', reponse TEXT, date_reponse TEXT, ref_type TEXT, ref_id INTEGER, service_id INTEGER);
CREATE TABLE IF NOT EXISTS journal_admin(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, date_reelle TEXT, auteur TEXT, action TEXT, details TEXT);
CREATE TABLE IF NOT EXISTS journal_tech(id INTEGER PRIMARY KEY AUTOINCREMENT, date_reelle TEXT, niveau TEXT, message TEXT);
CREATE TABLE IF NOT EXISTS utilisateurs(id INTEGER PRIMARY KEY AUTOINCREMENT, login TEXT UNIQUE, mdp TEXT, role TEXT, collab_id INTEGER, actif INTEGER DEFAULT 1, derniere_connexion TEXT);
CREATE TABLE IF NOT EXISTS autorisations(role TEXT, droit TEXT, PRIMARY KEY(role, droit));
CREATE TABLE IF NOT EXISTS vues(id INTEGER PRIMARY KEY AUTOINCREMENT, module TEXT, nom TEXT, filtre TEXT);
";

    const string V2 = @"
CREATE TABLE IF NOT EXISTS personnes(id INTEGER PRIMARY KEY AUTOINCREMENT, ref TEXT UNIQUE, nom TEXT, prenom TEXT, sexe TEXT, date_naissance TEXT, lieu_naissance TEXT, adresse TEXT, ville TEXT, profession TEXT,
  statut TEXT DEFAULT 'Sans antécédent', mesure TEXT, notes TEXT, date_creation TEXT);
CREATE INDEX IF NOT EXISTS ix_pers_nom ON personnes(nom, prenom);
CREATE TABLE IF NOT EXISTS dossier_personnes(dossier_id INTEGER, personne_id INTEGER, role TEXT, declaration TEXT, date_ajout TEXT, source TEXT, PRIMARY KEY(dossier_id, personne_id, role));
CREATE INDEX IF NOT EXISTS ix_dp_pers ON dossier_personnes(personne_id);
CREATE TABLE IF NOT EXISTS vehicules(id INTEGER PRIMARY KEY AUTOINCREMENT, immatriculation TEXT UNIQUE, marque TEXT, modele TEXT, couleur TEXT, proprietaire_id INTEGER, notes TEXT);
CREATE TABLE IF NOT EXISTS dossier_vehicules(dossier_id INTEGER, vehicule_id INTEGER, role TEXT, source TEXT, PRIMARY KEY(dossier_id, vehicule_id));
CREATE INDEX IF NOT EXISTS ix_dv_veh ON dossier_vehicules(vehicule_id);
CREATE TABLE IF NOT EXISTS pieces(id INTEGER PRIMARY KEY AUTOINCREMENT, dossier_id INTEGER, cote TEXT, type TEXT, titre TEXT, date TEXT, auteur TEXT, organisme_id INTEGER, personne_id INTEGER, contenu TEXT, origine TEXT, decision_id INTEGER, lue INTEGER DEFAULT 0);
CREATE INDEX IF NOT EXISTS ix_pieces_dos ON pieces(dossier_id);
CREATE INDEX IF NOT EXISTS ix_pieces_pers ON pieces(personne_id);
CREATE TABLE IF NOT EXISTS scelles(id INTEGER PRIMARY KEY AUTOINCREMENT, dossier_id INTEGER, numero TEXT, description TEXT, lieu TEXT, date TEXT, statut TEXT DEFAULT 'Placé sous scellé (sim.)', piece_id INTEGER);
CREATE TABLE IF NOT EXISTS dossier_chrono(id INTEGER PRIMARY KEY AUTOINCREMENT, dossier_id INTEGER, date TEXT, libelle TEXT, source TEXT);
CREATE INDEX IF NOT EXISTS ix_chrono ON dossier_chrono(dossier_id);
CREATE TABLE IF NOT EXISTS decisions(id INTEGER PRIMARY KEY AUTOINCREMENT, ref TEXT, dossier_id INTEGER, personne_id INTEGER, type TEXT, libelle TEXT, orientation TEXT, contexte TEXT, verifications TEXT, motivation TEXT,
  statut TEXT DEFAULT 'Validée', etat_execution TEXT DEFAULT 'Exécutée', echeance_effet TEXT, resultat TEXT, date TEXT, auteur_id INTEGER, version INTEGER DEFAULT 1, remplace_id INTEGER, piece_id INTEGER, situation_avant TEXT, source TEXT DEFAULT 'Utilisateur', demande_id INTEGER);
CREATE INDEX IF NOT EXISTS ix_dec_dos ON decisions(dossier_id);
CREATE INDEX IF NOT EXISTS ix_dec_exec ON decisions(etat_execution);
CREATE TABLE IF NOT EXISTS casier(id INTEGER PRIMARY KEY AUTOINCREMENT, personne_id INTEGER, dossier_id INTEGER, decision_id INTEGER, ref_externe TEXT, date TEXT, nature TEXT, juridiction TEXT, mesure TEXT, statut TEXT, source TEXT);
CREATE INDEX IF NOT EXISTS ix_casier_pers ON casier(personne_id);
CREATE TABLE IF NOT EXISTS detentions(id INTEGER PRIMARY KEY AUTOINCREMENT, personne_id INTEGER, dossier_id INTEGER, decision_id INTEGER, ref_externe TEXT, etablissement TEXT, regime TEXT, date_entree TEXT, date_sortie TEXT, duree_mois INTEGER, statut TEXT, motif TEXT);
CREATE INDEX IF NOT EXISTS ix_det_pers ON detentions(personne_id);
CREATE TABLE IF NOT EXISTS detention_hist(id INTEGER PRIMARY KEY AUTOINCREMENT, detention_id INTEGER, date TEXT, ancien_statut TEXT, nouveau_statut TEXT, decision_id INTEGER, commentaire TEXT);
CREATE TABLE IF NOT EXISTS mail_dossiers(id INTEGER PRIMARY KEY AUTOINCREMENT, owner_id INTEGER, nom TEXT);
ALTER TABLE dossiers ADD COLUMN faits TEXT;
ALTER TABLE dossiers ADD COLUMN lieu_faits TEXT;
ALTER TABLE dossiers ADD COLUMN date_faits TEXT;
ALTER TABLE dossiers ADD COLUMN qualification TEXT;
ALTER TABLE dossiers ADD COLUMN gravite INTEGER DEFAULT 1;
ALTER TABLE dossiers ADD COLUMN situation TEXT;
ALTER TABLE dossiers ADD COLUMN service_enqueteur_id INTEGER;
ALTER TABLE dossiers ADD COLUMN date_transmission TEXT;
ALTER TABLE messages ADD COLUMN mail_dossier_id INTEGER;
ALTER TABLE messages ADD COLUMN ref_type TEXT;
ALTER TABLE messages ADD COLUMN ref_id INTEGER;
ALTER TABLE dossier_notes ADD COLUMN type TEXT DEFAULT 'Observation du procureur';
ALTER TABLE dossier_notes ADD COLUMN auteur_ext TEXT;
CREATE INDEX IF NOT EXISTS ix_msg_conv ON messages(conversation_id);
INSERT OR IGNORE INTO autorisations(role,droit) VALUES('Procureur','decisions.prendre');
INSERT OR IGNORE INTO autorisations(role,droit) VALUES('Procureur adjoint','decisions.prendre');
";

    public static void Migrate()
    {
        using (var w = new DbCtx()) { try { w.Scalar("PRAGMA journal_mode=WAL;"); } catch { } }
        long v0 = Db.Long("PRAGMA user_version");
        if (v0 >= 1 && v0 < 2) { try { Db.Backup("avant_migration_v2"); } catch (Exception e) { Log.Error(e); } }
        using var c = new DbCtx(true);
        long v = c.Long("PRAGMA user_version");
        if (v < 1) { c.Exec(V1); c.Exec("PRAGMA user_version=1"); }
        if (v < 2) { c.Exec(V2); c.Exec("PRAGMA user_version=2"); }
        c.Commit();
    }

    public static bool IsSeeded() => Db.Long("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='collaborateurs'") > 0
                                     && Db.Long("SELECT COUNT(*) FROM collaborateurs") > 0;
}
