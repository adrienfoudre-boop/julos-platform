using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ParquetPro;

public static class U
{
    public const string Mention = "DOCUMENT DE SIMULATION — SANS VALEUR JURIDIQUE";
    public static readonly CultureInfo Fr = new("fr-FR");
    public static string D(DateTime d) => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    public static DateTime P(object s)
    {
        if (s == null || s == DBNull.Value) return DateTime.MinValue;
        return DateTime.TryParse(s.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.MinValue;
    }
    public static string Aff(object s) { var d = P(s); return d == DateTime.MinValue ? "" : d.ToString("dd/MM/yyyy HH:mm", Fr); }
    public static string AffJ(DateTime d) => d.ToString("dddd d MMMM yyyy", Fr);
    public static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("ParquetPro$" + s)));
    public static T Pick<T>(this Random r, IList<T> l) => l[r.Next(l.Count)];
    public static long L(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
    public static double Dv(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
    public static string S(object o) => o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
    public static string Like(string s) => "%" + (s ?? "").Trim() + "%";
}

public static class Log
{
    public static void Write(string niveau, string msg)
    {
        try { File.AppendAllText(Db.LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{niveau}] {msg}{Environment.NewLine}"); } catch { }
        try
        {
            using var c = new DbCtx();
            c.Exec("INSERT INTO journal_tech(date_reelle,niveau,message) VALUES(@p0,@p1,@p2)", U.D(DateTime.Now), niveau, msg.Length > 4000 ? msg[..4000] : msg);
        }
        catch { }
    }
    public static void Info(string m) => Write("INFO", m);
    public static void Warn(string m) => Write("AVERT", m);
    public static void Error(Exception e) => Write("ERREUR", e.ToString());
}

public static class Auth
{
    public static long UserId, CollabId = 1;
    public static string Login = "", Role = "Procureur";
    static HashSet<string> droits = new();
    public static readonly string[] Droits = { "missions.creer", "missions.valider", "dossiers.gerer", "messages.envoyer", "agenda.gerer", "documents.gerer", "personnel.modifier", "telephonie", "admin", "moteur.piloter" };
    public static readonly string[] Roles = { "Procureur", "Procureur adjoint", "Administrateur", "Consultation" };

    public static bool Connect(string login, string mdp)
    {
        var r = Db.Rows("SELECT id, role, collab_id, mdp FROM utilisateurs WHERE login=@p0 AND actif=1", login.Trim());
        if (r.Count == 0 || U.S(r[0][3]) != U.Hash(mdp)) return false;
        UserId = U.L(r[0][0]); Role = U.S(r[0][1]); CollabId = U.L(r[0][2]); if (CollabId == 0) CollabId = 1; Login = login.Trim();
        Reload();
        Db.Exec("UPDATE utilisateurs SET derniere_connexion=@p0 WHERE id=@p1", U.D(DateTime.Now), UserId);
        Biz.Admin("Connexion", $"Connexion de l'utilisateur {Login} ({Role})");
        return true;
    }
    public static void Reload() => droits = Db.Rows("SELECT droit FROM autorisations WHERE role=@p0", Role).Select(x => U.S(x[0])).ToHashSet();
    public static bool Can(string d) => droits.Contains(d);
    public static void Check(string d)
    {
        if (!Can(d)) throw new UnauthorizedAccessException($"Votre profil « {Role} » ne dispose pas du droit « {d} ». Opération refusée par les règles métier.");
    }
}

public static class Theme
{
    public static readonly Color Navy = Color.FromArgb(0, 40, 85);
    public static readonly Color Blue = Color.FromArgb(0, 70, 140);
    public static readonly Color Accent = Color.FromArgb(0, 102, 204);
    public static readonly Color Light = Color.FromArgb(229, 237, 247);
    public static readonly Color Back = Color.FromArgb(244, 246, 249);
    public static readonly Color Gray = Color.FromArgb(222, 226, 232);
    public static readonly Color Anthracite = Color.FromArgb(55, 63, 75);
    public static readonly Color Red = Color.FromArgb(200, 40, 40);
    public static readonly Color Orange = Color.FromArgb(220, 130, 0);
    public static readonly Color Green = Color.FromArgb(30, 140, 70);
    public static readonly Font F = new("Segoe UI", 9f);
    public static readonly Font FB = new("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font FT = new("Segoe UI Semibold", 13f);
    public static readonly Font FH = new("Segoe UI Semibold", 10.5f);
    public static readonly Font FBig = new("Segoe UI", 20f, FontStyle.Bold);
    public static int RowHeight = 24;
    public static readonly Color[] Palette = { Color.FromArgb(0, 102, 204), Color.FromArgb(0, 153, 153), Color.FromArgb(230, 140, 20), Color.FromArgb(120, 80, 170), Color.FromArgb(200, 60, 60), Color.FromArgb(60, 150, 60), Color.FromArgb(100, 110, 125), Color.FromArgb(0, 60, 120) };
}

public static class Ui
{
    public static bool Try(Action a)
    {
        try { a(); return true; }
        catch (UnauthorizedAccessException e) { MessageBox.Show(e.Message, "Autorisation refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        catch (InvalidOperationException e) { MessageBox.Show(e.Message, "Opération impossible", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception e) { Log.Error(e); MessageBox.Show("Une erreur est survenue et a été journalisée :\n" + e.Message, "PARQUET PRO", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        return false;
    }

    public static void Info(string m) => MessageBox.Show(m, "PARQUET PRO", MessageBoxButtons.OK, MessageBoxIcon.Information);
    public static bool Confirm(string m) => MessageBox.Show(m, "PARQUET PRO — Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public static Button Btn(string text, EventHandler click, int w = 0)
    {
        var b = new Button { Text = text, AutoSize = w == 0, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Navy, Font = Theme.F, Margin = new Padding(3), Cursor = Cursors.Hand };
        if (w > 0) b.Width = w;
        b.FlatAppearance.BorderColor = Theme.Gray;
        b.FlatAppearance.MouseOverBackColor = Theme.Light;
        b.Click += click;
        return b;
    }
    public static Button PrimaryBtn(string text, EventHandler click, int w = 0)
    {
        var b = Btn(text, click, w); b.BackColor = Theme.Blue; b.ForeColor = Color.White; b.FlatAppearance.BorderColor = Theme.Navy; b.FlatAppearance.MouseOverBackColor = Theme.Accent; return b;
    }

    public static Label Title(string t) => new() { Text = t, Font = Theme.FT, ForeColor = Theme.Navy, Dock = DockStyle.Top, Height = 34, Padding = new Padding(6, 6, 0, 0) };

    public static FlowLayoutPanel Bar(params Control[] cs)
    {
        var f = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4, 3, 4, 0), BackColor = Theme.Back, WrapContents = false, AutoScroll = true };
        f.Controls.AddRange(cs);
        return f;
    }

    public static string Prompt(string title, string label, string def = "", bool multi = false)
    {
        using var f = new Form { Text = title, StartPosition = FormStartPosition.CenterParent, Width = 520, Height = multi ? 360 : 170, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Theme.F };
        var l = new Label { Text = label, Dock = DockStyle.Top, Height = 26, Padding = new Padding(8, 6, 0, 0) };
        var t = new TextBox { Text = def, Dock = DockStyle.Fill, Multiline = multi, ScrollBars = multi ? ScrollBars.Vertical : ScrollBars.None };
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) }; p.Controls.Add(t);
        var ok = new Button { Text = "Valider", DialogResult = DialogResult.OK, Width = 90 };
        var ca = new Button { Text = "Annuler", DialogResult = DialogResult.Cancel, Width = 90 };
        var bp = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
        bp.Controls.Add(ca); bp.Controls.Add(ok);
        f.Controls.Add(p); f.Controls.Add(l); f.Controls.Add(bp);
        f.AcceptButton = multi ? null : ok; f.CancelButton = ca;
        return f.ShowDialog() == DialogResult.OK ? t.Text : null;
    }

    public static void ShowText(string title, string text, Form owner = null)
    {
        var f = new Form { Text = title, Width = 820, Height = 640, StartPosition = FormStartPosition.CenterParent, Font = Theme.F, ShowIcon = false };
        var t = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9.5f), Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n"), BackColor = Color.White };
        var bar = Bar(Btn("Exporter PDF", (s, e) => Ui.Try(() => Export.PdfOpen(title, text))), Btn("Imprimer", (s, e) => Ui.Try(() => Export.Print(title, text))), Btn("Fermer", (s, e) => f.Close()));
        bar.Dock = DockStyle.Bottom;
        f.Controls.Add(t); f.Controls.Add(bar);
        f.Show(owner);
    }

    public static ComboBox Combo(IEnumerable<object> items, int w = 160, bool editable = false)
    {
        var c = new ComboBox { Width = w, DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 3) };
        foreach (var i in items) c.Items.Add(i);
        if (c.Items.Count > 0) c.SelectedIndex = 0;
        if (editable) { c.AutoCompleteMode = AutoCompleteMode.SuggestAppend; c.AutoCompleteSource = AutoCompleteSource.ListItems; }
        return c;
    }
}

public class Item
{
    public long Id; public string Text;
    public Item(long id, string text) { Id = id; Text = text; }
    public override string ToString() => Text;
    public static List<Item> Collabs(string where = "1=1", params object[] a) =>
        Db.Rows($"SELECT c.id, c.nom||' '||c.prenom||' — '||c.fonction||' ('||IFNULL(s.code,'')||')' FROM collaborateurs c LEFT JOIN services s ON s.id=c.service_id WHERE c.actif=1 AND {where} ORDER BY c.nom, c.prenom", a)
          .Select(r => new Item(U.L(r[0]), U.S(r[1]))).ToList();
    public static List<Item> Services(bool all = false)
    {
        var l = Db.Rows("SELECT id, nom FROM services WHERE actif=1 ORDER BY id").Select(r => new Item(U.L(r[0]), U.S(r[1]))).ToList();
        if (all) l.Insert(0, new Item(0, "(Tous les services)"));
        return l;
    }
    public static List<Item> Dossiers(bool none = true)
    {
        var l = Db.Rows("SELECT id, numero||' — '||intitule FROM dossiers WHERE archive=0 ORDER BY id DESC").Select(r => new Item(U.L(r[0]), U.S(r[1]))).ToList();
        if (none) l.Insert(0, new Item(0, "(Aucun dossier)"));
        return l;
    }
    public static void Select(ComboBox c, long id)
    {
        for (int i = 0; i < c.Items.Count; i++) if (c.Items[i] is Item it && it.Id == id) { c.SelectedIndex = i; return; }
    }
    public static long Of(ComboBox c) => c.SelectedItem is Item it ? it.Id : 0;
}
