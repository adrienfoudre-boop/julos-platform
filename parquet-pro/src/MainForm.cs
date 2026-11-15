namespace ParquetPro;

public abstract class Module : UserControl
{
    public abstract string Titre { get; }
    public virtual bool AutoRefresh => true;
    public virtual void RefreshData() { }
    public virtual void Nouveau() { }
    protected Module() { Dock = DockStyle.Fill; BackColor = Theme.Back; Font = Theme.F; }
    protected static Panel Card(Control inner, string title = null)
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = Theme.Gray, Margin = new Padding(5) };
        var w = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        inner.Dock = DockStyle.Fill;
        w.Controls.Add(inner);
        if (title != null) w.Controls.Add(new Label { Text = "  " + title, Dock = DockStyle.Top, Height = 26, BackColor = Theme.Light, ForeColor = Theme.Navy, Font = Theme.FB, TextAlign = ContentAlignment.MiddleLeft });
        p.Controls.Add(w);
        return p;
    }
}

public class RefItem
{
    public string Text, Type; public long Id;
    public RefItem(string t, string type, long id) { Text = t; Type = type; Id = id; }
    public override string ToString() => Text;
}

public class MainForm : Form
{
    public static MainForm Instance;
    readonly AppCtx app;
    bool detached;
    readonly Panel host = new() { Dock = DockStyle.Fill, BackColor = Theme.Back };
    readonly Dictionary<string, Module> modules = new();
    readonly Dictionary<string, Button> navButtons = new();
    readonly FlowLayoutPanel navFav = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Navy };
    readonly FlowLayoutPanel navAll = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Navy };
    readonly Panel right = new() { Dock = DockStyle.Right, Width = 300, BackColor = Color.White, Padding = new Padding(6) };
    readonly ListBox lAlertes = new(), lDecisions = new(), lActivite = new();
    readonly Label lSim = new(), lReel = new(), lUser = new();
    readonly Button bNotif;
    readonly ToolStripStatusLabel sMoteur = new(), sVitesse = new(), sEvts = new(), sSave = new(), sDb = new(), sUser = new();
    readonly ToolStripComboBox cbVitesse = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    readonly ToolStripButton bPlay, bPause;
    readonly System.Windows.Forms.Timer uiTimer = new() { Interval = 1000 };
    long lastVersion = -1; DateTime lastRefresh = DateTime.MinValue;
    string current;
    Splitter rightSplit;

    public record ModDef(string Name, string Icon, Func<Module> Make);
    public static readonly ModDef[] Defs =
    {
        new("Mon cabinet", "⌂", () => new CabinetModule()),
        new("Délégations", "✎", () => new DelegationsModule()),
        new("Dossiers", "▤", () => new DossiersModule()),
        new("Messagerie", "✉", () => new MessagerieModule()),
        new("Téléphonie", "☎", () => new TelephonieModule()),
        new("Agenda", "▦", () => new AgendaModule()),
        new("Organigramme", "☷", () => new OrganisationModule()),
        new("Personnel", "☺", () => new PersonnelModule()),
        new("Univers institutionnel", "⚖", () => new UniversModule()),
        new("Documents", "❐", () => new DocumentsModule()),
        new("Statistiques", "∑", () => new StatsModule()),
        new("Notifications", "⚑", () => new NotificationsModule()),
        new("Archives", "▣", () => new ArchivesModule()),
        new("Journal", "≡", () => new JournalModule()),
        new("Assistant", "✦", () => new AssistantModule()),
        new("Administration", "⚙", () => new AdminModule()),
    };

    // ------------------------------------------------------------ raccourcis personnalisables
    public static readonly (string name, Keys def, Action<MainForm> act)[] Actions =
    {
        ("Mon cabinet", Keys.Control | Keys.D1, f => f.ShowModule("Mon cabinet")),
        ("Délégations", Keys.Control | Keys.D2, f => f.ShowModule("Délégations")),
        ("Dossiers", Keys.Control | Keys.D3, f => f.ShowModule("Dossiers")),
        ("Messagerie", Keys.Control | Keys.D4, f => f.ShowModule("Messagerie")),
        ("Téléphonie", Keys.Control | Keys.D5, f => f.ShowModule("Téléphonie")),
        ("Agenda", Keys.Control | Keys.D6, f => f.ShowModule("Agenda")),
        ("Organigramme", Keys.Control | Keys.D7, f => f.ShowModule("Organigramme")),
        ("Personnel", Keys.Control | Keys.D8, f => f.ShowModule("Personnel")),
        ("Statistiques", Keys.Control | Keys.D9, f => f.ShowModule("Statistiques")),
        ("Nouvelle délégation", Keys.Control | Keys.D, f => f.NewMission()),
        ("Nouveau message", Keys.Control | Keys.M, f => f.NewMessage()),
        ("Nouvelle réunion", Keys.Control | Keys.R, f => f.NewReunion()),
        ("Nouveau (module actif)", Keys.Control | Keys.N, f => f.NewInModule()),
        ("Actualiser", Keys.F5, f => f.RefreshAll(true)),
        ("Pause / reprise du moteur", Keys.Control | Keys.P, f => f.TogglePause()),
        ("Journée simulée suivante", Keys.Control | Keys.J, f => f.NextDay()),
        ("Recherche dans les archives", Keys.Control | Keys.F, f => f.ShowModule("Archives")),
        ("Notifications", Keys.Control | Keys.Shift | Keys.N, f => f.ShowModule("Notifications")),
        ("Assistant", Keys.F2, f => f.ShowModule("Assistant")),
        ("Aide — raccourcis", Keys.F1, f => f.ShowShortcuts()),
    };
    static Dictionary<Keys, Action<MainForm>> keyMap = new();
    public static Keys KeyOf(string name)
    {
        var a = Actions.First(x => x.name == name);
        return int.TryParse(Db.Get("key_" + name, ""), out var k) ? (Keys)k : a.def;
    }
    public static void LoadKeys()
    {
        keyMap = new();
        foreach (var a in Actions) { var k = KeyOf(a.name); if (k != Keys.None) keyMap[k] = a.act; }
    }

    public MainForm(AppCtx app)
    {
        Instance = this; this.app = app;
        Text = "PARQUET PRO"; WindowState = FormWindowState.Maximized; MinimumSize = new Size(1100, 700); Font = Theme.F; BackColor = Theme.Back; KeyPreview = true;
        LoadKeys();

        // ---- en-tête
        var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Theme.Navy };
        var logo = new Label { Text = "PARQUET PRO", Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(14, 10) };
        var sub = new Label { Text = "Cabinet du procureur de la République — simulation", ForeColor = Color.FromArgb(160, 185, 215), AutoSize = true, Location = new Point(190, 22) };
        lSim.ForeColor = Color.White; lSim.Font = new Font("Segoe UI Semibold", 12f); lSim.AutoSize = true; lSim.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lReel.ForeColor = Color.FromArgb(160, 185, 215); lReel.AutoSize = true; lReel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lUser.ForeColor = Color.White; lUser.AutoSize = true; lUser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bNotif = new Button { Text = "⚑ 0", ForeColor = Color.White, BackColor = Theme.Blue, FlatStyle = FlatStyle.Flat, Width = 70, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
        bNotif.FlatAppearance.BorderSize = 0;
        bNotif.Click += (s, e) => ShowModule("Notifications");
        var bSet = new Button { Text = "⚙", ForeColor = Color.White, BackColor = Theme.Blue, FlatStyle = FlatStyle.Flat, Width = 36, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
        bSet.FlatAppearance.BorderSize = 0;
        bSet.Click += (s, e) => ShowModule("Administration");
        header.Controls.AddRange(new Control[] { logo, sub, lSim, lReel, lUser, bNotif, bSet });
        header.Resize += (s, e) =>
        {
            bSet.Location = new Point(header.Width - 50, 11); bNotif.Location = new Point(header.Width - 128, 11);
            lUser.Location = new Point(header.Width - 140 - lUser.Width, 19);
            lSim.Location = new Point(header.Width - 170 - lUser.Width - lSim.Width, 8);
            lReel.Location = new Point(header.Width - 170 - lUser.Width - lReel.Width, 32);
        };

        // ---- menus
        var menu = new MenuStrip { BackColor = Color.White, Font = Theme.F };
        ToolStripMenuItem M(string t, params ToolStripItem[] items) { var m = new ToolStripMenuItem(t); m.DropDownItems.AddRange(items); return m; }
        ToolStripMenuItem I(string t, Action a, Keys k = Keys.None) { var i = new ToolStripMenuItem(t, null, (s, e) => Ui.Try(a)); if (k != Keys.None) i.ShortcutKeyDisplayString = new KeysConverter().ConvertToString(k); return i; }
        menu.Items.Add(M("&Fichier", I("Sauvegarder maintenant", () => { var f = Db.Backup("manuelle"); sSave.Text = "Sauvegarde : " + DateTime.Now.ToString("HH:mm"); Ui.Info("Sauvegarde créée :\n" + f); }),
            I("Ouvrir le dossier des exports", () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Db.ExportDir) { UseShellExecute = true })),
            new ToolStripSeparator(), I("Changer d'utilisateur…", () => app.Relogin()), I("Réduire en tâche de fond", () => { Hide(); app.Balloon("PARQUET PRO", "Le moteur continue de fonctionner."); }),
            new ToolStripSeparator(), I("Quitter", () => { if (Ui.Confirm("Quitter PARQUET PRO ? Le moteur de simulation sera arrêté et l'état sauvegardé.")) app.ExitApp(); })));
        menu.Items.Add(M("&Cabinet", I("Mon cabinet", () => ShowModule("Mon cabinet")), I("Mes délégations", () => ShowModule("Délégations")), I("Dossiers", () => ShowModule("Dossiers")),
            new ToolStripSeparator(), I("Nouvelle délégation…", NewMission, KeyOf("Nouvelle délégation")), I("Nouveau dossier…", () => { using var d = new DossierDialog(); d.ShowDialog(this); RefreshAll(true); })));
        menu.Items.Add(M("C&ommunication", I("Messagerie", () => ShowModule("Messagerie")), I("Téléphonie", () => ShowModule("Téléphonie")), I("Agenda et réunions", () => ShowModule("Agenda")), I("Notifications", () => ShowModule("Notifications")),
            new ToolStripSeparator(), I("Nouveau message…", NewMessage, KeyOf("Nouveau message")), I("Nouvelle réunion…", NewReunion, KeyOf("Nouvelle réunion")), I("Appel sortant…", () => { using var d = new AppelDialog(); d.ShowDialog(this); })));
        menu.Items.Add(M("&Organisation", I("Organigramme", () => ShowModule("Organigramme")), I("Personnel et ressources humaines", () => ShowModule("Personnel")), I("Univers institutionnel", () => ShowModule("Univers institutionnel"))));
        menu.Items.Add(M("&Documents", I("Bibliothèque documentaire", () => ShowModule("Documents")), I("Archives", () => ShowModule("Archives")), I("Journal chronologique", () => ShowModule("Journal")),
            I("Nouveau document…", () => { using var d = new DocumentDialog(); d.ShowDialog(this); })));
        menu.Items.Add(M("&Pilotage", I("Statistiques et tableaux de bord", () => ShowModule("Statistiques")), I("Synthèse de situation", () => Ui.ShowText("Synthèse de situation", Db.Q(c => Biz.Synthese(c)), this))));
        var mSim = M("&Simulation", I("Lecture", () => SetRun(true)), I("Pause", () => SetRun(false)), I("Pause / reprise", TogglePause, KeyOf("Pause / reprise du moteur")), new ToolStripSeparator());
        foreach (var v in Engine.Vitesses) { int vv = v; mSim.DropDownItems.Add(I("Vitesse " + Engine.VitesseLabel(v), () => SetSpeed(vv))); }
        mSim.DropDownItems.Add(new ToolStripSeparator());
        mSim.DropDownItems.Add(I("Avancer d'une heure", () => Advance(Engine.Now.AddHours(1))));
        mSim.DropDownItems.Add(I("Journée simulée suivante (8h00)", NextDay, KeyOf("Journée simulée suivante")));
        menu.Items.Add(mSim);
        menu.Items.Add(M("&Affichage", I("Afficher / masquer le panneau droit", ToggleRight), I("Densité compacte", () => SetDensity("Compacte")), I("Densité normale", () => SetDensity("Normale")), I("Densité confortable", () => SetDensity("Confortable")),
            new ToolStripSeparator(), I("Actualiser", () => RefreshAll(true), Keys.F5)));
        menu.Items.Add(M("O&utils", I("Assistant", () => ShowModule("Assistant")), I("Administration", () => ShowModule("Administration")), I("Personnaliser les raccourcis", () => { ShowModule("Administration"); ((AdminModule)modules["Administration"]).ShowTab("Raccourcis"); })));
        menu.Items.Add(M("&?", I("Raccourcis clavier", ShowShortcuts, Keys.F1), I("À propos de PARQUET PRO", () => Ui.Info($"PARQUET PRO 1.0\nSimulation institutionnelle interactive.\n\n{U.Mention}\n\nDonnées : {Db.Dir}"))));

        // ---- ruban
        var ribbon = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Theme.Light, Padding = new Padding(6, 4, 6, 4), ImageScalingSize = new Size(20, 20), Font = Theme.F, RenderMode = ToolStripRenderMode.System };
        ToolStripLabel G(string t) => new(t) { ForeColor = Color.Gray, Font = new Font("Segoe UI", 7.5f) };
        ToolStripButton B(string t, Action a, string tip = null) => new(t, null, (s, e) => Ui.Try(a)) { ToolTipText = tip ?? t, ForeColor = Theme.Navy, Margin = new Padding(2, 1, 2, 1) };
        ribbon.Items.Add(G("CABINET"));
        ribbon.Items.Add(B("✎ Délégation", NewMission, "Nouvelle délégation (Ctrl+D)"));
        ribbon.Items.Add(B("▤ Dossier", () => { using var d = new DossierDialog(); d.ShowDialog(this); RefreshAll(true); }));
        ribbon.Items.Add(B("✉ Message", NewMessage, "Nouveau message (Ctrl+M)"));
        ribbon.Items.Add(B("▦ Réunion", NewReunion, "Nouvelle réunion (Ctrl+R)"));
        ribbon.Items.Add(B("☎ Appel", () => { using var d = new AppelDialog(); d.ShowDialog(this); RefreshAll(true); }));
        ribbon.Items.Add(B("❐ Document", () => { using var d = new DocumentDialog(); d.ShowDialog(this); }));
        ribbon.Items.Add(new ToolStripSeparator());
        ribbon.Items.Add(G("SIMULATION"));
        bPlay = B("▶ Lecture", () => SetRun(true)); bPause = B("❚❚ Pause", () => SetRun(false));
        ribbon.Items.Add(bPlay); ribbon.Items.Add(bPause);
        foreach (var v in Engine.Vitesses) cbVitesse.Items.Add(Engine.VitesseLabel(v));
        cbVitesse.SelectedIndex = Math.Max(0, Array.IndexOf(Engine.Vitesses, Engine.Speed));
        cbVitesse.SelectedIndexChanged += (s, e) => Ui.Try(() => SetSpeed(Engine.Vitesses[cbVitesse.SelectedIndex]));
        ribbon.Items.Add(cbVitesse);
        ribbon.Items.Add(B("+1 h", () => Advance(Engine.Now.AddHours(1)), "Avancer d'une heure simulée"));
        ribbon.Items.Add(B("⏭ Journée suivante", NextDay, "Avance rapide jusqu'à la journée suivante (Ctrl+J)"));
        ribbon.Items.Add(new ToolStripSeparator());
        ribbon.Items.Add(G("AFFICHAGE"));
        ribbon.Items.Add(B("⟳ Actualiser", () => RefreshAll(true), "F5"));
        ribbon.Items.Add(B("◧ Panneau droit", ToggleRight));
        ribbon.Items.Add(B("✦ Assistant", () => ShowModule("Assistant")));
        ribbon.Items.Add(new ToolStripSeparator());
        ribbon.Items.Add(G("PILOTAGE"));
        ribbon.Items.Add(B("∑ Statistiques", () => ShowModule("Statistiques")));
        ribbon.Items.Add(B("≡ Journal", () => ShowModule("Journal")));

        // ---- navigation latérale
        var nav = new Panel { Dock = DockStyle.Left, Width = 215, BackColor = Theme.Navy, AutoScroll = true };
        nav.Controls.Add(navAll);
        nav.Controls.Add(new Label { Text = "  ESPACES DE TRAVAIL", Dock = DockStyle.Top, Height = 28, ForeColor = Color.FromArgb(140, 165, 200), Font = new Font("Segoe UI", 8f, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft });
        nav.Controls.Add(navFav);
        nav.Controls.Add(new Label { Text = "  FAVORIS (clic droit pour épingler)", Dock = DockStyle.Top, Height = 28, ForeColor = Color.FromArgb(140, 165, 200), Font = new Font("Segoe UI", 8f, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft });
        BuildNav();

        // ---- panneau droit
        Label RT(string t) => new() { Text = t, Dock = DockStyle.Top, Height = 24, Font = Theme.FB, ForeColor = Theme.Navy, TextAlign = ContentAlignment.BottomLeft };
        foreach (var lb in new[] { lAlertes, lDecisions, lActivite })
        {
            lb.Dock = DockStyle.Top; lb.Height = 170; lb.BorderStyle = BorderStyle.None; lb.BackColor = Theme.Back; lb.IntegralHeight = false; lb.HorizontalScrollbar = true;
            var l = lb;
            l.DoubleClick += (s, e) => { if (l.SelectedItem is RefItem r) Ui.Try(() => OpenRef(r.Type, r.Id)); };
        }
        lActivite.Dock = DockStyle.Fill;
        right.Controls.Add(lActivite); right.Controls.Add(RT("Activité récente"));
        right.Controls.Add(lDecisions); right.Controls.Add(RT("Décisions attendues"));
        right.Controls.Add(lAlertes); right.Controls.Add(RT("Alertes et notifications"));
        rightSplit = new Splitter { Dock = DockStyle.Right, Width = 4, BackColor = Theme.Gray };
        if (Db.Get("panneau_droit", "1") != "1") { right.Visible = false; rightSplit.Visible = false; }

        // ---- barre d'état
        var status = new StatusStrip { BackColor = Theme.Anthracite, ForeColor = Color.White, SizingGrip = false };
        foreach (var s in new[] { sMoteur, sVitesse, sEvts, sSave, sUser }) { s.BorderSides = ToolStripStatusLabelBorderSides.Right; s.BorderStyle = Border3DStyle.Etched; s.ForeColor = Color.White; status.Items.Add(s); }
        sDb.Spring = true; sDb.TextAlign = ContentAlignment.MiddleRight; sDb.ForeColor = Color.Gainsboro; status.Items.Add(sDb);
        sDb.Text = "Base locale : " + Db.PathDb + " — composants locaux : SQLite OK";
        sSave.Text = "Sauvegarde auto : " + Db.Get("backup_min", "30") + " min";

        Controls.Add(host); Controls.Add(rightSplit); Controls.Add(right); Controls.Add(nav); Controls.Add(ribbon); Controls.Add(menu); Controls.Add(header); Controls.Add(status);
        MainMenuStrip = menu;
        lUser.Text = $"{Db.Str("SELECT prenom||' '||nom FROM collaborateurs WHERE id=@p0", Auth.CollabId)} — {Auth.Role}";
        sUser.Text = "Utilisateur : " + Auth.Login;

        uiTimer.Tick += (s, e) => OnTimer();
        uiTimer.Start();
        Shown += (s, e) => { ShowModule("Mon cabinet"); RefreshAll(true); header.PerformLayout(); OnResizeHeader(header); };
        FormClosing += OnClosing;
    }

    void OnResizeHeader(Panel h) { h.Width += 1; h.Width -= 1; }

    public void Detach() { detached = true; uiTimer.Stop(); }

    void OnClosing(object s, FormClosingEventArgs e)
    {
        if (detached) { uiTimer.Stop(); return; }
        if (e.CloseReason == CloseReason.UserClosing && Db.Get("fond", "0") == "1")
        {
            e.Cancel = true; Hide();
            app.Balloon("PARQUET PRO", "L'application continue en tâche de fond. Double-cliquez sur l'icône pour la rouvrir.");
            return;
        }
        if (e.CloseReason == CloseReason.UserClosing && !Ui.Confirm("Quitter PARQUET PRO ?\nLe moteur de simulation sera arrêté et l'état sera sauvegardé."))
        {
            e.Cancel = true; return;
        }
        detached = true;
        uiTimer.Stop();
        BeginInvoke(() => app.ExitApp());
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyMap.TryGetValue(keyData, out var a)) { Ui.Try(() => a(this)); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ------------------------------------------------------------ navigation
    List<string> Favoris() => (Db.Get("favoris", "") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Where(f => Defs.Any(d => d.Name == f)).ToList();

    void BuildNav()
    {
        navFav.Controls.Clear(); navAll.Controls.Clear(); navButtons.Clear();
        var fav = Favoris();
        foreach (var f in fav) navFav.Controls.Add(NavBtn(Defs.First(d => d.Name == f), true));
        foreach (var d in Defs) navAll.Controls.Add(NavBtn(d, false));
    }

    Button NavBtn(ModDef d, bool fav)
    {
        var b = new Button
        {
            Text = $"  {d.Icon}   {d.Name}", Width = 212, Height = 32, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Theme.Navy, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f), Margin = new Padding(0), Cursor = Cursors.Hand, Tag = d.Name
        };
        b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Theme.Blue;
        b.Click += (s, e) => ShowModule(d.Name);
        var cm = new ContextMenuStrip();
        cm.Items.Add(fav ? "Retirer des favoris" : "Épingler dans les favoris", null, (s, e) =>
        {
            var l = Favoris();
            if (fav) l.Remove(d.Name); else if (!l.Contains(d.Name)) l.Add(d.Name);
            Db.Set("favoris", string.Join(",", l)); BuildNav(); HighlightNav(); UpdateBadges();
        });
        cm.Items.Add("Ouvrir", null, (s, e) => ShowModule(d.Name));
        b.ContextMenuStrip = cm;
        if (!fav) navButtons[d.Name] = b; else navButtons["★" + d.Name] = b;
        return b;
    }

    void HighlightNav()
    {
        foreach (var kv in navButtons)
        {
            bool on = (string)kv.Value.Tag == current;
            kv.Value.BackColor = on ? Theme.Accent : Theme.Navy;
            kv.Value.Font = on ? new Font("Segoe UI Semibold", 9.5f) : new Font("Segoe UI", 9.5f);
        }
    }

    public Module ShowModule(string name)
    {
        try
        {
            if (!modules.TryGetValue(name, out var m))
            {
                m = Defs.First(d => d.Name == name).Make();
                modules[name] = m;
            }
            current = name;
            host.SuspendLayout();
            host.Controls.Clear();
            host.Controls.Add(m);
            host.ResumeLayout();
            Text = "PARQUET PRO — " + m.Titre;
            HighlightNav();
            m.RefreshData();
            return m;
        }
        catch (Exception e) { Log.Error(e); MessageBox.Show("Impossible d'ouvrir le module : " + e.Message); return null; }
    }

    public T Get<T>(string name) where T : Module => ShowModule(name) as T;

    // ------------------------------------------------------------ actions
    public void NewMission() { using var d = new MissionDialog(); if (d.ShowDialog(this) == DialogResult.OK) RefreshAll(true); }
    public void NewMessage() { using var d = new MessageDialog(); if (d.ShowDialog(this) == DialogResult.OK) RefreshAll(true); }
    public void NewReunion() { using var d = new ReunionDialog(); if (d.ShowDialog(this) == DialogResult.OK) RefreshAll(true); }
    void NewInModule() { if (current != null && modules.TryGetValue(current, out var m)) { m.Nouveau(); RefreshAll(true); } }

    void SetRun(bool r) { Auth.Check("moteur.piloter"); Engine.SetRunning(r); Biz.Admin("Moteur", r ? "Reprise du moteur" : "Mise en pause du moteur"); OnTimer(); }
    void TogglePause() => SetRun(!Engine.Running);
    void SetSpeed(int v)
    {
        Auth.Check("moteur.piloter");
        Engine.SetSpeed(v);
        int i = Array.IndexOf(Engine.Vitesses, v);
        if (i >= 0 && cbVitesse.SelectedIndex != i) cbVitesse.SelectedIndex = i;
    }
    void Advance(DateTime target)
    {
        Auth.Check("moteur.piloter");
        Cursor = Cursors.WaitCursor;
        sMoteur.Text = "Moteur : avance rapide en cours…";
        Engine.AvancerJusquA(target, () => BeginInvoke(() => { Cursor = Cursors.Default; RefreshAll(true); }));
    }
    void NextDay() => Advance(Engine.ProchaineJournee());

    void ToggleRight()
    {
        right.Visible = !right.Visible; rightSplit.Visible = right.Visible;
        Db.Set("panneau_droit", right.Visible ? 1 : 0);
    }

    void SetDensity(string d)
    {
        Db.Set("densite", d);
        Theme.RowHeight = d switch { "Compacte" => 20, "Confortable" => 30, _ => 24 };
        var cur = current;
        foreach (var m in modules.Values) m.Dispose();
        modules.Clear();
        if (cur != null) ShowModule(cur);
    }

    void ShowShortcuts()
    {
        var sb = new System.Text.StringBuilder("RACCOURCIS CLAVIER (personnalisables dans Administration > Raccourcis)\n\n");
        var kc = new KeysConverter();
        foreach (var a in Actions) sb.AppendLine($"{kc.ConvertToString(KeyOf(a.name)),-22} {a.name}");
        Ui.ShowText("Raccourcis clavier", sb.ToString(), this);
    }

    // ------------------------------------------------------------ ouverture d'un élément
    public void OpenRef(string type, long id)
    {
        switch (type)
        {
            case "mission": new MissionView(id).Show(this); break;
            case "dossier": Get<DossiersModule>("Dossiers")?.Focus(id); break;
            case "message": MessageViewer.Open(this, id); break;
            case "reunion": { using var d = new ReunionDialog(id); d.ShowDialog(this); } break;
            case "appel": Get<TelephonieModule>("Téléphonie"); break;
            case "document": { using var d = new DocumentDialog(id); d.ShowDialog(this); } break;
            case "demande": { using var d = new DemandeDialog(id); d.ShowDialog(this); } break;
            case "collaborateur": new FicheCollab(id).Show(this); break;
            case "organisme": Get<UniversModule>("Univers institutionnel")?.Focus(id); break;
            case "module": ShowModule("Mon cabinet"); break;
            default: ShowModule("Journal"); break;
        }
        RefreshAll(true);
    }

    // ------------------------------------------------------------ rafraîchissement
    void OnTimer()
    {
        try
        {
            var n = Engine.Now;
            lSim.Text = "Temps simulé : " + n.ToString("ddd dd/MM/yyyy HH:mm", U.Fr);
            lReel.Text = "Heure réelle : " + DateTime.Now.ToString("HH:mm:ss");
            sMoteur.Text = "Moteur : " + Engine.Etat;
            sMoteur.ForeColor = Engine.Running ? Color.LightGreen : Color.Orange;
            sVitesse.Text = "Vitesse : " + (Engine.Running ? Engine.VitesseLabel(Engine.Speed) : "pause");
            sEvts.Text = "Événements (dernier cycle) : " + Engine.EvtsDerniereMinute;
            bPlay.Checked = Engine.Running; bPause.Checked = !Engine.Running;
            var v = Interlocked.Read(ref Engine.Version);
            if (v != lastVersion && (DateTime.Now - lastRefresh).TotalSeconds >= 3) RefreshAll(false);
        }
        catch (Exception e) { Log.Error(e); }
    }

    public void RefreshAll(bool force)
    {
        if (!Visible && !force) return;
        lastVersion = Interlocked.Read(ref Engine.Version);
        lastRefresh = DateTime.Now;
        try
        {
            UpdateBadges();
            if (right.Visible) UpdateRight();
            if (current != null && modules.TryGetValue(current, out var m) && (force || m.AutoRefresh)) m.RefreshData();
            Toasts();
        }
        catch (Exception e) { Log.Error(e); }
    }

    void UpdateBadges()
    {
        long me = Auth.CollabId;
        var counts = Db.Rows($@"SELECT
            (SELECT COUNT(*) FROM notifications WHERE lu=0 AND archive=0),
            (SELECT COUNT(*) FROM messages WHERE dest_id=@p0 AND lu=0 AND etat_dest='actif' AND brouillon=0),
            (SELECT COUNT(*) FROM missions WHERE emetteur_id=@p0 AND statut='Rapport transmis'),
            (SELECT COUNT(*) FROM appels WHERE rappel=1),
            (SELECT COUNT(*) FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND r.statut='Proposée'),
            (SELECT COUNT(*) FROM demandes WHERE statut='En attente')", me)[0];
        bNotif.Text = "⚑ " + counts[0];
        bNotif.BackColor = U.L(counts[0]) > 0 ? Theme.Accent : Theme.Blue;
        var badge = new Dictionary<string, long> { ["Notifications"] = U.L(counts[0]), ["Messagerie"] = U.L(counts[1]), ["Délégations"] = U.L(counts[2]), ["Téléphonie"] = U.L(counts[3]), ["Agenda"] = U.L(counts[4]), ["Mon cabinet"] = U.L(counts[5]) + U.L(counts[2]) };
        foreach (var kv in navButtons)
        {
            var name = (string)kv.Value.Tag;
            var d = Defs.First(x => x.Name == name);
            kv.Value.Text = $"  {d.Icon}   {d.Name}" + (badge.TryGetValue(name, out var c) && c > 0 ? $"  ({c})" : "");
        }
    }

    void UpdateRight()
    {
        void Fill(ListBox lb, List<RefItem> items)
        {
            int sel = lb.SelectedIndex, top = lb.TopIndex;
            lb.BeginUpdate(); lb.Items.Clear(); foreach (var i in items) lb.Items.Add(i); lb.EndUpdate();
            if (sel >= 0 && sel < lb.Items.Count) lb.SelectedIndex = sel;
            if (top < lb.Items.Count) lb.TopIndex = Math.Max(0, top);
        }
        Fill(lAlertes, Db.Rows("SELECT titre, ref_type, ref_id, importance, substr(date,12,5) FROM notifications WHERE lu=0 AND archive=0 ORDER BY CASE importance WHEN 'Haute' THEN 0 ELSE 1 END, id DESC LIMIT 40")
            .Select(r => new RefItem($"{(U.S(r[3]) == "Haute" ? "⚠" : "•")} {r[4]} {r[0]}", U.S(r[1]), U.L(r[2]))).ToList());
        var dec = Db.Rows("SELECT '✔ Rapport : '||numero||' '||objet, 'mission', id FROM missions WHERE emetteur_id=@p0 AND statut='Rapport transmis' ORDER BY date_rapport DESC LIMIT 20", Auth.CollabId)
            .Select(r => new RefItem(U.S(r[0]), "mission", U.L(r[2]))).ToList();
        dec.AddRange(Db.Rows("SELECT '? '||type||' : '||objet, id FROM demandes WHERE statut='En attente' ORDER BY id DESC LIMIT 20").Select(r => new RefItem(U.S(r[0]), "demande", U.L(r[1]))));
        dec.AddRange(Db.Rows("SELECT '▦ Réunion proposée : '||r.titre, r.id FROM reunions r JOIN reunion_participants p ON p.reunion_id=r.id WHERE p.collab_id=@p0 AND r.statut='Proposée' LIMIT 10", Auth.CollabId).Select(r => new RefItem(U.S(r[0]), "reunion", U.L(r[1]))));
        Fill(lDecisions, dec);
        Fill(lActivite, Db.Rows("SELECT substr(date,12,5)||'  '||description, ref_type, ref_id FROM evenements ORDER BY id DESC LIMIT 60").Select(r => new RefItem(U.S(r[0]), U.S(r[1]), U.L(r[2]))).ToList());
    }

    readonly List<Form> toasts = new();
    void Toasts()
    {
        var rows = Db.Rows("SELECT id, titre, texte, importance, ref_type, ref_id FROM notifications WHERE affichee=0 ORDER BY id LIMIT 30");
        if (rows.Count == 0) return;
        Db.Exec("UPDATE notifications SET affichee=1 WHERE affichee=0 AND id<=@p0", rows[^1][0]);
        if (Db.Get("popup_notifs", "1") != "1") return;
        var show = rows.Where(r => U.S(r[3]) == "Haute").TakeLast(3).ToList();
        if (show.Count == 0) show = rows.TakeLast(1).ToList();
        if (!Visible) { app.Balloon(U.S(show[^1][1]), U.S(show[^1][2])); return; }
        foreach (var r in show) Toast(U.S(r[1]), U.S(r[2]), U.S(r[4]), U.L(r[5]), U.S(r[3]) == "Haute");
    }

    void Toast(string title, string text, string type, long id, bool high)
    {
        toasts.RemoveAll(t => t.IsDisposed);
        if (toasts.Count >= 4) { toasts[0].Close(); toasts.RemoveAt(0); }
        var f = new ToastForm(title, text, high);
        f.Click2 += () => Ui.Try(() => OpenRef(type, id));
        var wa = Screen.FromControl(this).WorkingArea;
        f.Location = new Point(wa.Right - f.Width - 12, wa.Bottom - (f.Height + 8) * (toasts.Count + 1) - 8);
        toasts.Add(f);
        f.Show(this);
    }
}

public class ToastForm : Form
{
    public event Action Click2;
    readonly System.Windows.Forms.Timer t = new() { Interval = 7000 };
    protected override bool ShowWithoutActivation => true;
    public ToastForm(string title, string text, bool high)
    {
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual; Width = 340; Height = 86; TopMost = true; ShowInTaskbar = false; BackColor = Color.White;
        var bar = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = high ? Theme.Red : Theme.Accent };
        var l1 = new Label { Text = title, Font = Theme.FB, ForeColor = Theme.Navy, Location = new Point(14, 8), Width = 316, Height = 20, AutoEllipsis = true };
        var l2 = new Label { Text = text, Location = new Point(14, 30), Width = 316, Height = 48, ForeColor = Theme.Anthracite, AutoEllipsis = true };
        Controls.AddRange(new Control[] { bar, l1, l2 });
        foreach (Control c in Controls) c.Click += (s, e) => { Click2?.Invoke(); Close(); };
        Click += (s, e) => { Click2?.Invoke(); Close(); };
        Paint += (s, e) => e.Graphics.DrawRectangle(Pens.Silver, 0, 0, Width - 1, Height - 1);
        t.Tick += (s, e) => { t.Stop(); Close(); };
        t.Start();
    }
}

public static class MessageViewer
{
    public static void Open(IWin32Window owner, long id)
    {
        var m = Db.Rows(@"SELECT date, IFNULL((SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=exp_id), exp_ext), (SELECT prenom||' '||nom FROM collaborateurs WHERE id=dest_id), objet, corps, mission_id, dossier_id, exp_id, document_id FROM messages WHERE id=@p0", id);
        if (m.Count == 0) return;
        var x = m[0];
        if (U.L(Db.Scalar("SELECT dest_id FROM messages WHERE id=@p0", id)) == Auth.CollabId) Db.Exec("UPDATE messages SET lu=1 WHERE id=@p0", id);
        var f = new Form { Text = U.S(x[3]), Width = 820, Height = 600, StartPosition = FormStartPosition.CenterParent, Font = Theme.F, ShowIcon = false };
        var head = new Label { Dock = DockStyle.Top, Height = 64, Padding = new Padding(10, 6, 0, 0), BackColor = Theme.Light, Text = $"De : {x[1]}\nÀ : {x[2]}\nDate : {U.Aff(x[0])}     Objet : {x[3]}" };
        var body = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Text = U.S(x[4]).Replace("\r\n", "\n").Replace("\n", "\r\n"), Font = new Font("Segoe UI", 10f) };
        var bar = Ui.Bar();
        bar.Dock = DockStyle.Bottom;
        long exp = U.L(x[7]);
        if (exp != 0 && exp != Auth.CollabId) bar.Controls.Add(Ui.PrimaryBtn("Répondre", (s, e) => { using var d = new MessageDialog(exp, "RE: " + x[3], "\r\n\r\n----- Message d'origine -----\r\n" + U.S(x[4]).Replace("\n", "\r\n"), U.L(x[5]), U.L(x[6])); d.ShowDialog(f); }));
        if (U.L(x[5]) != 0) bar.Controls.Add(Ui.Btn("Ouvrir la mission", (s, e) => new MissionView(U.L(x[5])).Show(f)));
        if (U.L(x[6]) != 0) bar.Controls.Add(Ui.Btn("Ouvrir le dossier", (s, e) => { f.Close(); MainForm.Instance?.OpenRef("dossier", U.L(x[6])); }));
        if (U.L(x[8]) != 0) bar.Controls.Add(Ui.Btn("Ouvrir le document", (s, e) => { using var d = new DocumentDialog(U.L(x[8])); d.ShowDialog(f); }));
        bar.Controls.Add(Ui.Btn("Exporter PDF", (s, e) => Ui.Try(() => Export.PdfOpen(U.S(x[3]), $"De : {x[1]}\nÀ : {x[2]}\nDate : {U.Aff(x[0])}\nObjet : {x[3]}\n\n{x[4]}"))));
        bar.Controls.Add(Ui.Btn("Fermer", (s, e) => f.Close()));
        f.Controls.Add(body); f.Controls.Add(head); f.Controls.Add(bar);
        f.Show(owner);
    }
}

public class DemandeDialog : Dlg
{
    public DemandeDialog(long id) : base("Demande adressée au procureur", 720, 480)
    {
        var d = Db.Rows("SELECT date, (SELECT prenom||' '||nom||' — '||fonction FROM collaborateurs WHERE id=demandeur_id), type, objet, texte, statut, reponse FROM demandes WHERE id=@p0", id)[0];
        Row("Date", new Label { Text = U.Aff(d[0]), AutoSize = true });
        Row("Demandeur", new Label { Text = U.S(d[1]), AutoSize = true });
        Row("Type", new Label { Text = U.S(d[2]), Font = Theme.FB, AutoSize = true });
        Row("Objet", new Label { Text = U.S(d[3]), AutoSize = true, MaximumSize = new Size(500, 0) });
        Row("Texte", new TextBox { Multiline = true, ReadOnly = true, Text = U.S(d[4]).Replace("\n", "\r\n"), ScrollBars = ScrollBars.Vertical }, 120);
        Row("Statut", new Label { Text = U.S(d[5]), AutoSize = true });
        var tRep = Row("Votre réponse", new TextBox { Multiline = true, Text = U.S(d[6]) }, 70);
        Cancel();
        if (U.S(d[5]) == "En attente")
        {
            Btn("Refuser", () => Db.Tx(c => Biz.TraiterDemande(c, id, false, tRep.Text)));
            Btn("Accorder", () => Db.Tx(c => Biz.TraiterDemande(c, id, true, tRep.Text)), true);
        }
    }
}
