import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";

export default async function DashboardPage() {
  const supabase = await createClient();
  const {
    data: { user },
  } = await supabase.auth.getUser();

  // Deuxième vérification, indépendante du middleware : une page protégée
  // ne doit jamais supposer qu'elle a été atteinte légitimement.
  if (!user) {
    redirect("/login");
  }

  return (
    <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-6 px-6 py-16">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Tableau de bord</h1>
        <form action="/auth/signout" method="post">
          <button
            type="submit"
            className="rounded-md border border-neutral-300 px-3 py-1.5 text-sm"
          >
            Se déconnecter
          </button>
        </form>
      </div>
      <p className="text-neutral-600">Connecté en tant que {user.email}.</p>
      <p className="rounded-full self-start bg-neutral-100 px-4 py-1 text-sm text-neutral-500">
        Phase B — authentification en place. Le reste du tableau de bord
        arrive en Phase D.
      </p>
    </main>
  );
}
