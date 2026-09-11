export default function ForgotPasswordCheckEmailPage() {
  return (
    <main className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center gap-4 px-6 py-16 text-center">
      <h1 className="text-2xl font-semibold">Vérifiez votre boîte mail</h1>
      <p className="text-neutral-600">
        Si un compte existe avec cette adresse, un lien de réinitialisation
        vient de vous être envoyé.
      </p>
    </main>
  );
}
