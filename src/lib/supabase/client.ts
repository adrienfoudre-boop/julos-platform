// Client Supabase utilisé côté navigateur (composants clients).
// Utilise uniquement la clé publique (anon) : jamais la clé de service ici.
import { createBrowserClient } from "@supabase/ssr";

export function createClient() {
  return createBrowserClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
  );
}
