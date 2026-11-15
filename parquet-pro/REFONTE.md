# PARQUET PRO — Refonte v2 : audit, concept et architecture

> DOCUMENT DE SIMULATION — SANS VALEUR JURIDIQUE

## PHASE 1 — Audit de la version 1

### Architecture technique (v1)

| Élément | Constat |
|---|---|
| Technologie | C# .NET 8 WinForms, exécutable autonome, SQLite local (WAL), 5 400 lignes |
| Fichiers | `Db` (accès SQLite), `Schema` (migration v1), `Seed` (300 collaborateurs), `Biz` (règles métier), `Engine` (moteur), `Controls` (grilles, graphiques, agenda), `Dialogs`, `MainForm`, `Modules1-3` |
| Points solides | Transactions + verrou d'écriture, migrations par `user_version`, sauvegardes automatiques, journal technique, contrôle des droits dans la logique métier, moteur hors thread UI |
| Points faibles | Un seul composant générique (`GridPanel`) réutilisé partout → tous les modules se ressemblent ; double-clic = formulaire d'édition ; pas d'icônes (caractères Unicode) ; pas de modèle « personne » ; documents = texte brut |

### Base de données (v1)

Tables : `services, collaborateurs, affectations_hist, organismes, interlocuteurs, dossiers, dossier_notes, dossier_hist, missions, sous_taches, mission_hist, messages, conversations, documents, reunions, reunion_participants, appels, evenements, notifications, demandes, journal_admin, journal_tech, utilisateurs, autorisations, vues, parametres`.

**Manques structurels** : aucune table pour les personnes de l'univers, les véhicules, les pièces d'enquête (PV, auditions, rapports), les scellés, les décisions, le casier, la détention. Le champ `dossiers.personnes` est un **texte libre** (« Personnes fictives ») → impossible de relier une personne à plusieurs dossiers. Les messages ont un `conversation_id` jamais renseigné → pas de fils de discussion.

### Diagnostic par module

| Module | Existe / fonctionne | Insuffisant | À refaire | Manque | Relations à créer |
|---|---|---|---|---|---|
| **Mon cabinet** | KPI + 6 grilles réelles | Présentation « tableur », pas de notion de journée ni de nouvelles affaires | Mise en page « centre de commandement » | Nouvelles affaires, dossiers en attente de décision, décisions en cours | Dossiers, Décisions, Enquêtes |
| **Délégations** | Cascade réelle adjoint → chefs → équipes, rapports consolidés, validation | Correct | Conservé | Lien mission ↔ décision | Décisions (complément d'enquête → mission) |
| **Dossiers** | Liste, fiche texte, notes, historique, missions liées | Double-clic = formulaire de modification ; aucun contenu d'enquête ; personnes en texte libre | **Entièrement refait** : fenêtre de consultation à 13 sections | Faits, chronologie, personnes, auditions, PV, rapports, scellés, décisions, PDF complet | Personnes, Véhicules, Pièces, Décisions, Messagerie, Casier, Détention |
| **Enquêtes** | — | — | — | **Module absent** | Dossiers, Organismes, Décisions |
| **Décisions** | Seulement « accepter/refuser un rapport » | — | — | **Module absent** : aucun parcours de décision, aucune conséquence | Tous les modules métier |
| **Messagerie** | Dossiers de boîte, lecture, réponse, brouillons | Pas de fil de discussion, pas d'aperçu, pas de dossiers personnels, références non cliquables | **Refaite** en boîte mail 3 volets | Fils, dossiers personnels, déplacer, liens vers dossiers/décisions | Dossiers, Décisions, Personnes |
| **Téléphonie** | Appels entrants/sortants simulés, notes, rappel | Pas d'accès direct au dossier | Ajouts | Ouvrir le dossier depuis l'appel | Dossiers |
| **Agenda** | Vues jour/semaine/mois/liste, conflits, propositions | Correct | Conservé | Réunion créée depuis une décision | Décisions |
| **Personnes** | — | — | — | **Module absent** | Dossiers, Casier, Détention, Véhicules |
| **Fichiers de recherche** | — | — | — | **Module absent** (recherche nom, immatriculation…) | Personnes, Véhicules, Dossiers, Organismes |
| **Casier judiciaire** | — | — | — | **Module absent** | Décisions, Personnes |
| **Personnes détenues** | — | — | — | **Module absent** | Décisions, Personnes, Moteur |
| **Organismes** | Fiche, interlocuteurs, échanges, appels | Dossiers « transmis » non détaillés | Ajouts | Pièces transmises | Pièces, Dossiers |
| **Organigramme / RH** | Arbre, glisser-déposer, annuaire, charge | Correct | Regroupés sous « Services et collaborateurs » | — | — |
| **Documents** | Bibliothèque, PDF texte, impression | PDF sans mise en page, sans sommaire, sans pagination de dossier | PDF professionnel | Dossier complet PDF, sélection de pièces | Dossiers, Pièces |
| **Statistiques** | 8 graphiques réels | Aucune donnée judiciaire | Ajouts | Décisions, détention, situations | Décisions, Détention |
| **Notifications / Archives / Journal** | Fonctionnels | Ne connaissent pas les nouveaux objets | Extension | Ouverture des personnes, décisions, pièces | Tous |
| **Administration** | Droits, utilisateurs, sauvegardes, diagnostic | Correct | Conservé | Droit `decisions.prendre` | — |
| **Moteur** | Cascade de missions, retards, réunions, appels, bilans | Générateur de messages ; dossiers = intitulé + % d'avancement | **Moteur judiciaire** | Cycle de vie des affaires, réponses des services et juridictions aux décisions, évolution des détentions | Tout |

## PHASE 2 — Nouveau concept

**Principe** : le procureur arrive dans son cabinet, découvre les affaires reçues, ouvre un dossier comme on ouvre une chemise papier, lit les pièces, recherche les personnes et véhicules, prend une décision par un parcours guidé, et voit les services, juridictions et établissements simulés réagir.

### Navigation (4 espaces, icônes Windows)

- **CABINET** : Cabinet du procureur · Décisions · Messagerie · Téléphonie · Agenda
- **AFFAIRES** : Dossiers · Enquêtes · Fichiers de recherche · Personnes · Casier judiciaire · Personnes détenues
- **ORGANISATION** : Services et collaborateurs · Délégations · Organismes
- **PILOTAGE** : Documents et impressions · Statistiques · Archives · Notifications · Journal · Assistant · Administration

Une barre de recherche globale (en-tête) cherche à la fois dans les personnes, les immatriculations et les dossiers.

### Parcours principal

1. Le moteur crée une **affaire** : un organisme simulé transmet un dossier avec des faits, des personnes (victimes, mis en cause, témoins), parfois des véhicules et des scellés, et des premières pièces.
2. L'affaire arrive dans le **Cabinet** (« Nouvelles affaires ») et dans la **Messagerie** (message de l'organisme avec lien vers le dossier).
3. **Ouvrir le dossier** : fenêtre de consultation à 13 sections. Chaque pièce s'ouvre dans un lecteur de document mis en page.
4. Pendant l'**enquête**, le service enquêteur ajoute des pièces : auditions, PV, rapport de synthèse. À la fin, le dossier passe « En attente de décision » et le référent envoie son avis.
5. **Préparer une décision** : parcours guidé en 7 étapes (contexte, informations, éléments à examiner, prérequis, orientation, motivation, validation). Les décisions proposées dépendent de la situation du dossier.
6. **Conséquences** : la décision met à jour le dossier, les personnes, l'historique, les documents (acte de décision simulé), la messagerie, les notifications et les statistiques. Les demandes qui partent vers l'extérieur (complément d'enquête, transmission à une juridiction, saisine du juge des libertés) reçoivent **une réponse du moteur** quelques heures ou jours simulés plus tard : nouvelles pièces, jugement simulé avec inscription au casier, placement en détention ou contrôle judiciaire, refus motivé…
7. Les **personnes détenues** évoluent (demandes de mise en liberté, fins de détention) et redemandent des décisions.

## PHASE 3 — Architecture des données

Migration **v2** (sans réinitialisation, sauvegarde automatique préalable) :

```
personnes(id, ref, nom, prenom, sexe, date_naissance, lieu_naissance, adresse, ville, profession, statut, notes)
dossier_personnes(dossier_id, personne_id, role, date_ajout, source)        ← une personne, N dossiers
vehicules(id, immatriculation UNIQUE, marque, modele, couleur, proprietaire_id)
dossier_vehicules(dossier_id, vehicule_id, role, source)
pieces(id, dossier_id, cote, type, titre, date, auteur, organisme_id, personne_id, contenu, origine)
scelles(id, dossier_id, numero, description, lieu, date, statut, piece_id)
dossier_chrono(id, dossier_id, date, libelle, source)
decisions(id, ref, dossier_id, personne_id, type, orientation, contexte, verifications, motivation,
          statut, etat_execution, echeance_effet, resultat, date, auteur_id, version, remplace_id, piece_id)
casier(id, personne_id, dossier_id, decision_id, date, nature, juridiction, mesure, statut, source)
detentions(id, personne_id, dossier_id, decision_id, etablissement, regime, date_entree, date_sortie, statut, motif)
detention_hist(id, detention_id, date, ancien_statut, nouveau_statut, decision_id, commentaire)
mail_dossiers(id, owner_id, nom)
+ dossiers : faits, lieu_faits, date_faits, qualification, gravite, situation, service_enqueteur_id, date_transmission
+ messages : conversation_id renseigné, mail_dossier_id, ref_type/ref_id (lien générique)
+ dossier_notes : type, auteur_ext (observations des services)
```

**Règle d'identité unique** : une personne est créée une seule fois (`personnes`) et rattachée aux dossiers par `dossier_personnes` (avec son rôle et la source). Les véhicules sont rattachés de la même façon. Le casier et la détention ne sont alimentés **que** par des décisions (`decision_id`) ou par l'historique simulé initial (`source = 'Historique simulé'`). Les rapprochements affichés (personnes ↔ dossiers ↔ véhicules) proviennent uniquement de ces liaisons enregistrées et indiquent leur origine.

**Reprise des données existantes** : les dossiers existants reçoivent des faits, des personnes, des pièces et, pour les dossiers clos, une décision historique cohérente (classement ou jugement simulé → casier, éventuelle détention).

## PHASES 4 à 6 — Réalisation

| Élément | Fichier | Contenu |
|---|---|---|
| Migration v2 | `Schema.cs` | 12 nouvelles tables, colonnes ajoutées, sauvegarde automatique `avant_migration_v2` avant toute modification |
| Noyau judiciaire | `Jud.cs` | 16 modèles d'affaires, génération des personnes (identité unique, réutilisation → rapprochements réels), véhicules, pièces (plainte, constatations, auditions, PV, scellés, rapport de synthèse), 9 natures de décision, prérequis, effets, réponses du moteur (tribunal, juge des libertés, services), casier, détention, reprise des données v1 |
| Interface | `JudUi.cs`, `JudModules.cs`, `Icons.cs`, `MainForm.cs` | Fenêtre dossier (13 sections), lecteur de pièces, fiche personne (8 onglets), parcours de décision (7 étapes), Cabinet (centre de commandement), Décisions, Enquêtes, Fichiers de recherche, Personnes, Casier, Personnes détenues, Services et collaborateurs, messagerie 3 volets, icônes Windows (Segoe Fluent/MDL2), recherche globale |
| Impression | `Pdf.cs` | Dossier PDF : couverture, sommaire paginé, en-têtes, pieds de page, filigrane, sélection de pièces ; relevé de casier ; fiche personne |
| Moteur | `Engine.cs` | Nouvelles affaires contextualisées, progression des enquêtes pièce par pièce, avis du référent, exécution des décisions, jugements, ordonnances, fins de peine, demandes de mise en liberté |

## PHASE 7 — Tests réalisés

- Exécution réelle de l'exe Windows sous Wine 9 (Xvfb) avec captures d'écran : connexion, cabinet, fenêtre dossier, parcours de décision, lecteur de pièces, messagerie, décisions.
- **Autotest intégré** (`ParquetPro.exe --selftest`) : 58 vérifications, **58 réussies** :
  - ouverture des 21 modules et de tous leurs onglets ;
  - fenêtres de dossier dans chaque situation (13 sections chacune) ;
  - fiches personnes (tous onglets) ;
  - parcours de décision des 9 natures (7 étapes chacune) ;
  - enregistrement d'une décision de comparution immédiate et de réquisitions de détention, puis exécution par le moteur (jugement, détention, libération) ;
  - PDF complet et PDF de sélection ;
  - recherche par nom, immatriculation et numéro ;
  - contrôles de cohérence (pas de doublon de personne, casier ↔ décisions, statut des détenus).
- Scénario de **migration** : base créée par la v1, puis ouverte par la v2 → données conservées, sauvegarde préalable créée, autotest 58/58.
- Scénario d'**installation neuve** : autotest 58/58.
- Bugs trouvés et corrigés pendant les tests : boucle infinie du PDF sur un mot très long, plantage du module Statistiques au chargement (présent en v1), origine incohérente des dossiers repris, chevauchement de l'en-tête, alertes en double, rythme d'arrivée des affaires trop élevé.

## PHASE 8 — Livraison

- `parquet-pro/ParquetPro.exe` : exécutable Windows x64 autonome, version 2.0.0.
- `parquet-pro/src/` : code source complet.

**Limites connues** : les icônes n'ont pas pu être vues sous Windows (environnement de test Linux/Wine : remplacement par des lettres si la police d'icônes est absente) ; l'assistant reste un assistant à règles hors ligne ; pas d'installateur (exécutable unique).
