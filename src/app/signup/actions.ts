"use server";

import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";

export async function signup(formData: FormData) {
  const email = String(formData.get("email") ?? "");
  const password = String(formData.get("password") ?? "");

  if (!email || !password) {
    redirect(`/signup?error=${encodeURIComponent("Email et mot de passe requis.")}`);
  }
  if (password.length < 8) {
    redirect(
      `/signup?error=${encodeURIComponent("Le mot de passe doit contenir au moins 8 caractères.")}`,
    );
  }

  const supabase = await createClient();
  const { error } = await supabase.auth.signUp({
    email,
    password,
    options: {
      // Supabase enverra un e-mail de confirmation vers cette URL, qui
      // échange le lien contre une session (voir /auth/callback).
      emailRedirectTo: `${process.env.NEXT_PUBLIC_SITE_URL}/auth/callback`,
    },
  });

  if (error) {
    redirect(`/signup?error=${encodeURIComponent(error.message)}`);
  }

  redirect("/signup/check-email");
}
