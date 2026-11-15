using System.ComponentModel;
using System.Data;
using System.Drawing.Drawing2D;

namespace ParquetPro;

public class GridPanel : Panel
{
    public readonly DataGridView Grid;
    public readonly ToolStrip Bar;
    public readonly ToolStripTextBox Search;
    readonly ToolStripLabel info;
    readonly ToolStripButton bPrev, bNext;
    readonly ToolStripDropDownButton vues;
    public Func<string, int, int, DataTable> Loader;
    public readonly string Key;
    public int PageSize = 300;
    int page;
    bool loading;
    public event Action<long> Activated;
    public event Action SelChanged;
    HashSet<string> hidden = new();

    public GridPanel(string key, string titre = null)
    {
        Key = key;
        Dock = DockStyle.Fill;
        Grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AllowUserToOrderColumns = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, Font = Theme.F, GridColor = Theme.Gray, EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = 28, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };
        Grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Light;
        Grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Navy;
        Grid.ColumnHeadersDefaultCellStyle.Font = Theme.FB;
        Grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 224, 245);
        Grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        Grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
        Grid.RowTemplate.Height = Theme.RowHeight;
        typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(Grid, true);
        Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && SelectedId != 0) Activated?.Invoke(SelectedId); };
        Grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && SelectedId != 0) { Activated?.Invoke(SelectedId); e.Handled = true; } };
        Grid.SelectionChanged += (s, e) => { if (!loading) SelChanged?.Invoke(); };
        Grid.CellFormatting += Format;
        Grid.DataBindingComplete += (s, e) => ApplyCols();
        Grid.ColumnHeaderMouseClick += (s, e) => { if (e.Button == MouseButtons.Right) ColMenu(); };

        Bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Theme.Back, RenderMode = ToolStripRenderMode.System, Padding = new Padding(4, 2, 4, 2) };
        if (titre != null) Bar.Items.Add(new ToolStripLabel(titre) { Font = Theme.FH, ForeColor = Theme.Navy });
        Bar.Items.Add(new ToolStripLabel("Rechercher :"));
        Search = new ToolStripTextBox { Width = 200, ToolTipText = "Recherche (Entrée)" };
        Search.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { page = 0; Reload(); e.SuppressKeyPress = true; } };
        Bar.Items.Add(Search);
        Bar.Items.Add(new ToolStripButton("🔍", null, (s, e) => { page = 0; Reload(); }) { ToolTipText = "Lancer la recherche" });
        bPrev = new ToolStripButton("◀", null, (s, e) => { if (page > 0) { page--; Reload(); } }) { ToolTipText = "Page précédente", Alignment = ToolStripItemAlignment.Right };
        bNext = new ToolStripButton("▶", null, (s, e) => { page++; Reload(); }) { ToolTipText = "Page suivante", Alignment = ToolStripItemAlignment.Right };
        info = new ToolStripLabel("") { Alignment = ToolStripItemAlignment.Right, ForeColor = Theme.Anthracite };
        var csv = new ToolStripButton("Exporter", null, (s, e) => Ui.Try(() => { if (Grid.DataSource is DataTable dt) Export.Csv(dt, Key); })) { Alignment = ToolStripItemAlignment.Right, ToolTipText = "Exporter le tableau (CSV)" };
        var cols = new ToolStripButton("Colonnes", null, (s, e) => ColMenu()) { Alignment = ToolStripItemAlignment.Right, ToolTipText = "Choisir les colonnes affichées" };
        vues = new ToolStripDropDownButton("Vues") { Alignment = ToolStripItemAlignment.Right, ToolTipText = "Vues enregistrées" };
        vues.DropDownOpening += (s, e) => FillVues();
        vues.DropDownItems.Add("…");
        Bar.Items.Add(bNext); Bar.Items.Add(bPrev); Bar.Items.Add(info); Bar.Items.Add(csv); Bar.Items.Add(cols); Bar.Items.Add(vues);
        Controls.Add(Grid); Controls.Add(Bar);
        try { hidden = (Db.Get("cols_" + Key, "") ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet(); } catch { }
    }

    void FillVues()
    {
        vues.DropDownItems.Clear();
        vues.DropDownItems.Add("Enregistrer la vue actuelle…", null, (s, e) => Ui.Try(() =>
        {
            var n = Ui.Prompt("Vue enregistrée", "Nom de la vue :", Search.Text);
            if (!string.IsNullOrWhiteSpace(n)) Db.Exec("INSERT INTO vues(module,nom,filtre) VALUES(@p0,@p1,@p2)", Key, n, Search.Text);
        }));
        vues.DropDownItems.Add("Vue par défaut (aucun filtre)", null, (s, e) => { Search.Text = ""; page = 0; Reload(); });
        var rows = Db.Rows("SELECT id, nom, filtre FROM vues WHERE module=@p0 ORDER BY nom", Key);
        if (rows.Count > 0) vues.DropDownItems.Add(new ToolStripSeparator());
        foreach (var r in rows)
        {
            var f = U.S(r[2]); long id = U.L(r[0]);
            var it = new ToolStripMenuItem(U.S(r[1]), null, (s, e) => { Search.Text = f; page = 0; Reload(); });
            it.DropDownItems.Add("Supprimer cette vue", null, (s, e) => Db.Exec("DELETE FROM vues WHERE id=@p0", id));
            vues.DropDownItems.Add(it);
        }
    }

    void ColMenu()
    {
        var m = new ContextMenuStrip();
        foreach (DataGridViewColumn c in Grid.Columns)
        {
            if (c.Name == "id" || c.Name.StartsWith("_")) continue;
            var name = c.Name;
            var it = new ToolStripMenuItem(c.HeaderText) { Checked = !hidden.Contains(name), CheckOnClick = true };
            it.CheckedChanged += (s, e) => { if (it.Checked) hidden.Remove(name); else hidden.Add(name); SaveCols(); ApplyCols(); };
            m.Items.Add(it);
        }
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Afficher toutes les colonnes", null, (s, e) => { hidden.Clear(); SaveCols(); ApplyCols(); });
        m.Show(Cursor.Position);
    }

    void SaveCols() { try { Db.Set("cols_" + Key, string.Join("|", hidden)); } catch { } }

    void ApplyCols()
    {
        foreach (DataGridViewColumn c in Grid.Columns)
        {
            c.Visible = !(c.Name == "id" || c.Name.StartsWith("_") || hidden.Contains(c.Name));
            c.SortMode = DataGridViewColumnSortMode.Automatic;
            if (c.Width == 100)
            {
                int w = c.Name.Length * 8 + 30;
                if (c.ValueType == typeof(string)) w = Math.Max(w, 110);
                c.Width = Math.Min(Math.Max(60, w), 260);
                if (c.Name is "Objet" or "Intitulé" or "Titre" or "Description" or "Texte" or "Détail") c.Width = 340;
            }
        }
    }

    void Format(object s, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.Value == null || e.Value == DBNull.Value) return;
        var col = Grid.Columns[e.ColumnIndex].Name;
        var v = e.Value.ToString();
        if (col.StartsWith("Priorit") || col == "Importance")
        {
            if (v == "Haute") { e.CellStyle.ForeColor = Theme.Red; e.CellStyle.Font = Theme.FB; }
            else if (v == "Basse") e.CellStyle.ForeColor = Color.Gray;
        }
        else if (col == "Statut" || col == "Dispo" || col == "Disponibilité")
        {
            if (v is "Rapport transmis" or "En attente" or "Proposée" or "À rappeler" or "Manqué") e.CellStyle.ForeColor = Theme.Orange;
            else if (v is "Validée" or "Clôturée" or "Clôturé" or "Terminée" or "Accordée" or "Disponible" or "Terminé") e.CellStyle.ForeColor = Theme.Green;
            else if (v is "Refusée" or "Absent" or "Annulée" or "En retard") e.CellStyle.ForeColor = Theme.Red;
            else if (v is "En cours" or "En réunion" or "En analyse") e.CellStyle.ForeColor = Theme.Accent;
        }
        else if (col.StartsWith("Avancement") || col == "Charge" || col == "Charge %")
        {
            if (e.Value is double d) { e.Value = d.ToString("0") + " %"; e.FormattingApplied = true; }
            else if (e.Value is long l) { e.Value = l + " %"; e.FormattingApplied = true; }
        }
        else if (col == "Lu" && e.Value is long lu) { e.Value = lu == 1 ? "" : "●"; e.CellStyle.ForeColor = Theme.Accent; e.FormattingApplied = true; }
        else if (col == "!" && e.Value is long imp) { e.Value = imp == 1 ? "★" : ""; e.CellStyle.ForeColor = Theme.Orange; e.FormattingApplied = true; }
    }

    public long SelectedId
    {
        get
        {
            if (Grid.CurrentRow?.DataBoundItem is not DataRowView dv) return 0;
            return dv.Row.Table.Columns.Contains("id") ? U.L(dv["id"]) : 0;
        }
    }
    public object Cell(string col)
    {
        if (Grid.CurrentRow?.DataBoundItem is not DataRowView dv || !dv.Row.Table.Columns.Contains(col)) return null;
        return dv[col];
    }

    public void ResetPage() => page = 0;

    public void Reload()
    {
        if (Loader == null) return;
        try
        {
            loading = true;
            long sel = SelectedId;
            int first = Grid.FirstDisplayedScrollingRowIndex;
            var sortCol = Grid.SortedColumn?.Name; var sortDir = Grid.SortOrder;
            var dt = Loader(Search.Text ?? "", page * PageSize, PageSize + 1);
            bool more = dt.Rows.Count > PageSize;
            if (more) dt.Rows.RemoveAt(dt.Rows.Count - 1);
            Grid.DataSource = dt;
            ApplyCols();
            if (sortCol != null && Grid.Columns.Contains(sortCol) && sortDir != SortOrder.None)
                Grid.Sort(Grid.Columns[sortCol], sortDir == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);
            var firstVis = Grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
            if (sel != 0 && firstVis != null && dt.Columns.Contains("id"))
            {
                foreach (DataGridViewRow r in Grid.Rows)
                    if (r.DataBoundItem is DataRowView dv && U.L(dv["id"]) == sel) { Grid.CurrentCell = r.Cells[firstVis.Index]; break; }
            }
            else Grid.ClearSelection();
            if (first >= 0 && first < Grid.Rows.Count) try { Grid.FirstDisplayedScrollingRowIndex = first; } catch { }
            info.Text = $"{dt.Rows.Count} élément(s){(page > 0 || more ? $" — page {page + 1}" : "")}";
            bNext.Enabled = more; bPrev.Enabled = page > 0;
        }
        catch (Exception ex) { Log.Error(ex); info.Text = "Erreur de chargement"; }
        finally { loading = false; }
    }
}

public class Kpi : Panel
{
    readonly Label val, lab;
    public Kpi(string label, Color c)
    {
        Width = 170; Height = 74; BackColor = Color.White; Margin = new Padding(5); Cursor = Cursors.Hand; Padding = new Padding(8, 4, 4, 4);
        var bar = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = c };
        val = new Label { Dock = DockStyle.Top, Height = 38, Font = Theme.FBig, ForeColor = c, Text = "—", Padding = new Padding(6, 0, 0, 0) };
        lab = new Label { Dock = DockStyle.Fill, Font = Theme.F, ForeColor = Theme.Anthracite, Text = label, Padding = new Padding(8, 0, 0, 0) };
        Controls.Add(lab); Controls.Add(val); Controls.Add(bar);
        foreach (Control x in Controls) x.Click += (s, e) => OnClick(e);
        Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Theme.Gray), 0, 0, Width - 1, Height - 1);
    }
    public void Set(object v) => val.Text = Convert.ToString(v);
}

public class Chart : Panel
{
    public enum Kind { Bars, HBars, Line, Pie }
    public Kind Type = Kind.Bars;
    public string Title = "";
    public string[] Labels = Array.Empty<string>();
    public List<(string name, double[] values)> Series = new();

    public Chart() { DoubleBuffered = true; BackColor = Color.White; Dock = DockStyle.Fill; ResizeRedraw = true; Margin = new Padding(6); }

    public void Set(string[] labels, params (string, double[])[] series) { Labels = labels; Series = series.ToList(); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawRectangle(new Pen(Theme.Gray), 0, 0, Width - 1, Height - 1);
        g.DrawString(Title, Theme.FH, new SolidBrush(Theme.Navy), 8, 6);
        if (Series.Count == 0 || Labels.Length == 0) { g.DrawString("Aucune donnée", Theme.F, Brushes.Gray, 10, 34); return; }
        var area = new Rectangle(50, 34, Width - 66, Height - 76);
        if (Type == Kind.HBars) area = new Rectangle(190, 34, Width - 240, Height - 44);
        if (area.Width < 40 || area.Height < 30) return;
        double max = Math.Max(1, Series.SelectMany(s => s.values).DefaultIfEmpty(0).Max());
        using var fs = new Font("Segoe UI", 7.5f);
        if (Type == Kind.Pie)
        {
            var vals = Series[0].values; double tot = vals.Sum(); if (tot <= 0) return;
            int d = Math.Min(area.Height, area.Width / 2);
            var rc = new Rectangle(area.Left, area.Top, d, d);
            float a = -90;
            for (int i = 0; i < vals.Length; i++)
            {
                float sw = (float)(vals[i] / tot * 360);
                g.FillPie(new SolidBrush(Theme.Palette[i % Theme.Palette.Length]), rc, a, sw); a += sw;
                g.FillRectangle(new SolidBrush(Theme.Palette[i % Theme.Palette.Length]), rc.Right + 16, area.Top + i * 18, 12, 12);
                g.DrawString($"{Labels[i]} : {vals[i]:0} ({vals[i] / tot:P0})", Theme.F, Brushes.Black, rc.Right + 32, area.Top + i * 18 - 2);
            }
            return;
        }
        if (Type == Kind.HBars)
        {
            var vals = Series[0].values; int n = vals.Length;
            float h = Math.Min(22, area.Height / (float)n);
            for (int i = 0; i < n; i++)
            {
                float y = area.Top + i * h;
                float w = (float)(vals[i] / max * area.Width);
                g.DrawString(Labels[i].Length > 32 ? Labels[i][..32] + "…" : Labels[i], fs, Brushes.Black, 6, y + 2);
                g.FillRectangle(new SolidBrush(Theme.Palette[0]), area.Left, y + 2, Math.Max(1, w), h - 5);
                g.DrawString(vals[i].ToString("0"), fs, Brushes.Black, area.Left + w + 4, y + 2);
            }
            return;
        }
        var axis = new Pen(Theme.Gray);
        for (int k = 0; k <= 4; k++)
        {
            float y = area.Bottom - k * area.Height / 4f;
            g.DrawLine(axis, area.Left, y, area.Right, y);
            g.DrawString((max * k / 4).ToString("0"), fs, Brushes.Gray, 4, y - 7);
        }
        int nL = Labels.Length;
        float slot = area.Width / (float)nL;
        int step = Math.Max(1, (int)Math.Ceiling(nL * 60.0 / area.Width));
        for (int i = 0; i < nL; i += step)
            g.DrawString(Labels[i], fs, Brushes.Black, area.Left + i * slot, area.Bottom + 4);
        for (int s = 0; s < Series.Count; s++)
        {
            var col = Theme.Palette[s % Theme.Palette.Length];
            var vals = Series[s].values;
            if (Type == Kind.Bars)
            {
                float bw = Math.Max(2, slot * 0.75f / Series.Count);
                for (int i = 0; i < vals.Length && i < nL; i++)
                {
                    float h = (float)(vals[i] / max * area.Height);
                    g.FillRectangle(new SolidBrush(col), area.Left + i * slot + slot * 0.12f + s * bw, area.Bottom - h, bw - 1, h);
                }
            }
            else
            {
                var pts = vals.Take(nL).Select((v, i) => new PointF(area.Left + i * slot + slot / 2, area.Bottom - (float)(v / max * area.Height))).ToArray();
                if (pts.Length > 1) g.DrawLines(new Pen(col, 2.2f), pts);
                foreach (var p in pts) g.FillEllipse(new SolidBrush(col), p.X - 3, p.Y - 3, 6, 6);
            }
            if (Series.Count > 1)
            {
                g.FillRectangle(new SolidBrush(col), Width - 170, 8 + s * 15, 10, 10);
                g.DrawString(Series[s].name, fs, Brushes.Black, Width - 156, 6 + s * 15);
            }
        }
    }
}

public class AgendaView : Panel
{
    public enum Mode { Jour, Semaine, Mois }
    public Mode Vue = Mode.Semaine;
    public DateTime Date = DateTime.Today;
    public List<(long id, string titre, DateTime deb, DateTime fin, string prio, string statut)> Items = new();
    public event Action<long> Open;
    public event Action<DateTime> NewAt;
    readonly List<(RectangleF r, long id)> hits = new();
    const int H0 = 7, H1 = 20;

    public AgendaView() { DoubleBuffered = true; BackColor = Color.White; Dock = DockStyle.Fill; ResizeRedraw = true; }

    public DateTime Debut => Vue == Mode.Jour ? Date.Date : Vue == Mode.Semaine ? Date.Date.AddDays(-(((int)Date.DayOfWeek + 6) % 7)) : new DateTime(Date.Year, Date.Month, 1).AddDays(-(((int)new DateTime(Date.Year, Date.Month, 1).DayOfWeek + 6) % 7));
    public DateTime Fin => Vue == Mode.Jour ? Debut.AddDays(1) : Vue == Mode.Semaine ? Debut.AddDays(7) : Debut.AddDays(42);

    Color ColorOf(string prio, string statut) => statut == "Proposée" ? Theme.Orange : statut == "Terminée" ? Color.Gray : statut == "Annulée" ? Color.Silver : prio == "Haute" ? Theme.Red : Theme.Accent;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; hits.Clear();
        using var fs = new Font("Segoe UI", 8f);
        var pen = new Pen(Theme.Gray);
        if (Vue == Mode.Mois)
        {
            float cw = (Width - 2) / 7f, ch = (Height - 26) / 6f;
            string[] j = { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
            for (int i = 0; i < 7; i++) { g.FillRectangle(new SolidBrush(Theme.Light), i * cw, 0, cw, 24); g.DrawString(j[i], Theme.FB, new SolidBrush(Theme.Navy), i * cw + 4, 4); }
            for (int k = 0; k < 42; k++)
            {
                var d = Debut.AddDays(k);
                float x = (k % 7) * cw, y = 26 + (k / 7) * ch;
                if (d.Month != Date.Month) g.FillRectangle(new SolidBrush(Color.FromArgb(248, 248, 248)), x, y, cw, ch);
                if (d.Date == Engine.Now.Date) g.FillRectangle(new SolidBrush(Color.FromArgb(235, 244, 255)), x, y, cw, ch);
                g.DrawRectangle(pen, x, y, cw, ch);
                g.DrawString(d.Day.ToString(), Theme.FB, Brushes.Black, x + 3, y + 2);
                int n = 0;
                foreach (var it in Items.Where(i => i.deb.Date == d.Date).OrderBy(i => i.deb))
                {
                    float yy = y + 18 + n * 15;
                    if (yy + 14 > y + ch) { g.DrawString("…", fs, Brushes.Gray, x + cw - 14, y + 2); break; }
                    var r = new RectangleF(x + 2, yy, cw - 4, 14);
                    g.FillRectangle(new SolidBrush(ColorOf(it.prio, it.statut)), r);
                    g.DrawString($"{it.deb:HH:mm} {it.titre}", fs, Brushes.White, r);
                    hits.Add((r, it.id)); n++;
                }
            }
            return;
        }
        int days = Vue == Mode.Jour ? 1 : 7;
        float left = 48, top = 26;
        float colW = (Width - left - 2) / days, hourH = Math.Max(24, (Height - top - 2) / (float)(H1 - H0));
        for (int dd = 0; dd < days; dd++)
        {
            var d = Debut.AddDays(dd);
            float x = left + dd * colW;
            g.FillRectangle(new SolidBrush(d.Date == Engine.Now.Date ? Color.FromArgb(204, 224, 245) : Theme.Light), x, 0, colW, top - 2);
            g.DrawString(d.ToString(days == 1 ? "dddd d MMMM yyyy" : "ddd dd/MM", U.Fr), Theme.FB, new SolidBrush(Theme.Navy), x + 4, 5);
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) g.FillRectangle(new SolidBrush(Color.FromArgb(248, 248, 248)), x, top, colW, hourH * (H1 - H0));
        }
        for (int h = H0; h <= H1; h++)
        {
            float y = top + (h - H0) * hourH;
            g.DrawLine(pen, left, y, Width, y);
            g.DrawString($"{h:00}:00", fs, Brushes.Gray, 4, y - 6);
        }
        for (int dd = 0; dd <= days; dd++) g.DrawLine(pen, left + dd * colW, 0, left + dd * colW, Height);
        var now = Engine.Now;
        if (now >= Debut && now < Fin && now.Hour >= H0 && now.Hour < H1)
        {
            int di = (now.Date - Debut).Days;
            float y = top + (float)((now - now.Date).TotalHours - H0) * hourH;
            g.DrawLine(new Pen(Theme.Red, 2), left + di * colW, y, left + (di + 1) * colW, y);
        }
        foreach (var it in Items)
        {
            int di = (it.deb.Date - Debut).Days;
            if (di < 0 || di >= days) continue;
            double h0 = Math.Max(H0, (it.deb - it.deb.Date).TotalHours), h1 = Math.Min(H1, (it.fin - it.deb.Date).TotalHours);
            if (h1 <= h0) h1 = h0 + 0.5;
            var r = new RectangleF(left + di * colW + 3, top + (float)(h0 - H0) * hourH + 1, colW - 6, (float)(h1 - h0) * hourH - 2);
            using var br = new SolidBrush(ColorOf(it.prio, it.statut));
            g.FillRectangle(br, r);
            g.DrawString($"{it.deb:HH:mm}-{it.fin:HH:mm}  {it.titre}" + (it.statut == "Proposée" ? " (proposée)" : ""), fs, Brushes.White, r);
            hits.Add((r, it.id));
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        foreach (var h in hits) if (h.r.Contains(e.Location)) { Open?.Invoke(h.id); return; }
        if (Vue == Mode.Mois)
        {
            float cw = (Width - 2) / 7f, ch = (Height - 26) / 6f;
            int k = (int)((e.Y - 26) / ch) * 7 + (int)(e.X / cw);
            if (k >= 0 && k < 42) NewAt?.Invoke(Debut.AddDays(k).AddHours(10));
            return;
        }
        int days = Vue == Mode.Jour ? 1 : 7;
        float left = 48, top = 26;
        float colW = (Width - left - 2) / days, hourH = Math.Max(24, (Height - top - 2) / (float)(H1 - H0));
        if (e.X < left || e.Y < top) return;
        int di = (int)((e.X - left) / colW);
        double hh = H0 + (e.Y - top) / hourH;
        NewAt?.Invoke(Debut.AddDays(di).AddHours(Math.Floor(hh * 2) / 2));
    }
}
