# Julos — plateforme

Plateforme permettant de créer, modifier et publier des applications web par IA.
Domaine technique : societe-aq.fr.

## État du projet

- **Phase A (socle)** : terminée. Next.js + TypeScript + Tailwind, structure de base.
- Phase B (authentification Supabase) : à venir.
- Phase C (base de données + RLS) : à venir.

## Démarrage local (Windows 11 / PowerShell)

```powershell
npm install
npm run dev
```

Puis ouvrir http://localhost:3000

## Variables d'environnement

Copier `.env.local.example` en `.env.local` et renseigner les valeurs au fur
et à mesure des phases (voir les commentaires dans le fichier). Ce fichier
n'est jamais commité.

## Structure

```
src/
  app/          routes Next.js (App Router)
  components/   composants partagés (vide pour l'instant)
  lib/          logique partagée, clients externes (vide pour l'instant)
```
