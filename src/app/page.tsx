export default function HomePage() {
  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-6 px-6 text-center">
      <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">Julos</h1>
      <p className="max-w-xl text-lg text-neutral-600">
        Décrivez l&apos;application dont vous avez besoin, laissez l&apos;IA la
        construire, et publiez-la en un clic sur votre propre sous-domaine.
      </p>
      <div className="flex gap-3">
        <a
          href="/signup"
          className="rounded-md bg-neutral-900 px-4 py-2 text-sm font-medium text-white"
        >
          Créer un compte
        </a>
        <a
          href="/login"
          className="rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium"
        >
          Se connecter
        </a>
      </div>
      <p className="rounded-full bg-neutral-100 px-4 py-1 text-sm text-neutral-500">
        Phase B — authentification en cours de construction
      </p>
    </main>
  );
}
