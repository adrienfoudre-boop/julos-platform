import Link from "next/link";
import { signup } from "./actions";

export default async function SignupPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const { error } = await searchParams;

  return (
    <main className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center gap-6 px-6 py-16">
      <h1 className="text-2xl font-semibold">Créer un compte</h1>

      {error ? (
        <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
          {error}
        </p>
      ) : null}

      <form action={signup} className="flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm">
          Adresse e-mail
          <input
            type="email"
            name="email"
            required
            autoComplete="email"
            className="rounded-md border border-neutral-300 px-3 py-2"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          Mot de passe
          <input
            type="password"
            name="password"
            required
            minLength={8}
            autoComplete="new-password"
            className="rounded-md border border-neutral-300 px-3 py-2"
          />
          <span className="text-xs text-neutral-500">8 caractères minimum.</span>
        </label>

        <button
          type="submit"
          className="rounded-md bg-neutral-900 px-4 py-2 text-sm font-medium text-white"
        >
          Créer mon compte
        </button>
      </form>

      <p className="text-sm text-neutral-600">
        Déjà un compte ? <Link href="/login">Se connecter</Link>
      </p>
    </main>
  );
}
