"use server";

import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";

export async function updatePassword(formData: FormData) {
  const password = String(formData.get("password") ?? "");

  if (password.length < 8) {
    redirect(
      `/update-password?error=${encodeURIComponent("Le mot de passe doit contenir au moins 8 caractères.")}`,
    );
  }

  const supabase = await createClient();
  const { error } = await supabase.auth.updateUser({ password });

  if (error) {
    redirect(`/update-password?error=${encodeURIComponent(error.message)}`);
  }

  redirect("/dashboard");
}
