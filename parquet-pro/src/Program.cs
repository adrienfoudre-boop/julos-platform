using Microsoft.Win32;

namespace ParquetPro;

static class Program
{
    static Mutex mutex;

    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => { Log.Error(e.Exception); MessageBox.Show("Erreur inattendue (journalisée) :\n" + e.Exception.Message, "PARQUET PRO", MessageBoxButtons.OK, MessageBoxIcon.Error); };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => { if (e.ExceptionObject is Exception ex) Log.Error(ex); };
        TaskScheduler.UnobservedTaskException += (s, e) => { Log.Error(e.Exception); e.SetObserved(); };
        ApplicationConfiguration.Initialize();
        mutex = new Mutex(true, "ParquetPro_Instance_Unique", out bool first);
        if (!first) { MessageBox.Show("PARQUET PRO est déjà en cours d'exécution (voir la zone de notification).", "PARQUET PRO", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try
        {
            Db.Init();
            Schema.Migrate();
            if (!Schema.IsSeeded())
            {
                using var init = new InitForm();
                init.ShowDialog();
                if (init.Error != null)
                {
                    Log.Error(init.Error);
                    MessageBox.Show("Échec de l'initialisation : " + init.Error.Message, "PARQUET PRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Log.Info("Base initialisée : " + Db.Long("SELECT COUNT(*) FROM collaborateurs") + " collaborateurs.");
            }
            Theme.RowHeight = Db.Get("densite", "Normale") switch { "Compacte" => 20, "Confortable" => 30, _ => 24 };
            Engine.Load();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            MessageBox.Show("Impossible d'ouvrir la base de données :\n" + ex.Message + "\n\nDossier : " + Db.Dir, "PARQUET PRO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Application.Run(new AppCtx(args.Contains("--tray")));
        GC.KeepAlive(mutex);
    }

    public static Icon AppIcon = MakeIcon();
    static Icon MakeIcon()
    {
        try
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using var b = new SolidBrush(Theme.Blue);
                g.FillRectangle(b, 1, 1, 30, 30);
                g.FillRectangle(Brushes.White, 1, 24, 30, 3);
                using var f = new Font("Segoe UI", 15f, FontStyle.Bold);
                g.DrawString("P", f, Brushes.White, 6, 0);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
        catch { return SystemIcons.Application; }
    }

    public static void SetAutostart(bool on)
    {
        using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (k == null) return;
        if (on) k.SetValue("ParquetPro", $"\"{Environment.ProcessPath}\" --tray");
        else if (k.GetValue("ParquetPro") != null) k.DeleteValue("ParquetPro");
    }
}

public class AppCtx : ApplicationContext
{
    readonly NotifyIcon tray;
    MainForm main;
    bool exiting;

    public AppCtx(bool trayOnly)
    {
        tray = new NotifyIcon { Icon = Program.AppIcon, Text = "PARQUET PRO", Visible = true };
        var m = new ContextMenuStrip();
        m.Items.Add("Ouvrir PARQUET PRO", null, (s, e) => ShowMain());
        m.Items.Add("Pause / reprise du moteur", null, (s, e) => Ui.Try(() => { Auth.Check("moteur.piloter"); Engine.SetRunning(!Engine.Running); }));
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Quitter", null, (s, e) => ExitApp());
        tray.ContextMenuStrip = m;
        tray.DoubleClick += (s, e) => ShowMain();
        Engine.Start();
        if (trayOnly)
        {
            tray.ShowBalloonTip(4000, "PARQUET PRO", "Le moteur de simulation fonctionne en tâche de fond.", ToolTipIcon.Info);
        }
        else ShowMain();
    }

    public void Balloon(string title, string text) { try { tray.ShowBalloonTip(4000, title, text, ToolTipIcon.Info); } catch { } }

    public void ShowMain()
    {
        if (main != null && !main.IsDisposed)
        {
            main.Show();
            if (main.WindowState == FormWindowState.Minimized) main.WindowState = FormWindowState.Maximized;
            main.Activate();
            return;
        }
        using var login = new LoginForm { Icon = Program.AppIcon };
        if (login.ShowDialog() == DialogResult.OK)
        {
            main = new MainForm(this) { Icon = Program.AppIcon };
            main.Show();
        }
        else if (Db.Get("fond", "0") != "1") ExitApp();
    }

    public void Relogin()
    {
        var old = main; main = null;
        old.Detach();
        old.Close();
        ShowMain();
    }

    public void ExitApp()
    {
        if (exiting) return;
        exiting = true;
        try { Engine.Stop(); } catch (Exception e) { Log.Error(e); }
        try { Db.Backup("fermeture"); } catch (Exception e) { Log.Error(e); }
        Log.Info("Fermeture de l'application.");
        tray.Visible = false;
        tray.Dispose();
        if (main != null && !main.IsDisposed) { main.Detach(); main.Close(); }
        ExitThread();
    }
}
