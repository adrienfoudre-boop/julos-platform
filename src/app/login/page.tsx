import Link from "next/link";
import { login } from "./actions";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; next?: string }>;
}) {
  const { error, next } = await searchParams;

  return (
    <main className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center gap-6 px-6 py-16">
      <h1 className="text-2xl font-semibold">Connexion</h1>

      {error ? (
        <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
          {error}
        </p>
      ) : null}

      <form action={login} className="flex flex-col gap-4">
        <input type="hidden" name="next" value={next ?? "/dashboard"} />

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
            autoComplete="current-password"
            className="rounded-md border border-neutral-300 px-3 py-2"
          />
        </label>

        <button
          type="submit"
          className="rounded-md bg-neutral-900 px-4 py-2 text-sm font-medium text-white"
        >
          Se connecter
        </button>
      </form>

      <div className="flex justify-between text-sm text-neutral-600">
        <Link href="/forgot-password">Mot de passe oublié ?</Link>
        <Link href="/signup">Créer un compte</Link>
      </div>
    </main>
  );
}
