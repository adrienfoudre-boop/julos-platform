"use server";

import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";

export async function requestPasswordReset(formData: FormData) {
  const email = String(formData.get("email") ?? "");

  if (!email) {
    redirect(`/forgot-password?error=${encodeURIComponent("Adresse e-mail requise.")}`);
  }

  const supabase = await createClient();

  // On ignore volontairement l'erreur retournée par Supabase pour ne pas
  // révéler si une adresse e-mail existe ou non dans la base.
  await supabase.auth.resetPasswordForEmail(email, {
    redirectTo: `${process.env.NEXT_PUBLIC_SITE_URL}/auth/callback?next=/update-password`,
  });

  redirect("/forgot-password/check-email");
}
