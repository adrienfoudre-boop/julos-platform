using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ParquetPro;

/// Icônes vectorielles issues des polices système Windows (Segoe Fluent Icons / Segoe MDL2 Assets).
public static class Icons
{
    static readonly Dictionary<string, Bitmap> cache = new();
    static readonly string fam = Detect();
    static string Detect()
    {
        try
        {
            using var ifc = new InstalledFontCollection();
            foreach (var n in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
                if (ifc.Families.Any(f => f.Name == n)) return n;
        }
        catch { }
        return null;
    }

    public static readonly Dictionary<string, char> G = new()
    {
        ["cabinet"] = '', ["dossier"] = '', ["enquete"] = '', ["decision"] = '', ["mail"] = '', ["phone"] = '', ["agenda"] = '',
        ["personne"] = '', ["recherche"] = '', ["casier"] = '', ["detenu"] = '', ["organisme"] = '', ["services"] = '', ["delegation"] = '',
        ["document"] = '', ["print"] = '', ["stats"] = '', ["archive"] = '', ["notif"] = '', ["journal"] = '', ["assistant"] = '', ["admin"] = '',
        ["add"] = '', ["edit"] = '', ["delete"] = '', ["send"] = '', ["reply"] = '', ["forward"] = '', ["flag"] = '', ["check"] = '',
        ["warning"] = '', ["refresh"] = '', ["play"] = '', ["pause"] = '', ["view"] = '', ["attach"] = '', ["important"] = '', ["car"] = '',
        ["history"] = '', ["save"] = '', ["cancel"] = ''
    };

    public static Bitmap Get(string key, int size = 18, Color? color = null)
    {
        var col = color ?? Theme.Navy;
        string k = $"{key}|{size}|{col.ToArgb()}";
        lock (cache)
        {
            if (cache.TryGetValue(k, out var b)) return b;
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                using var br = new SolidBrush(col);
                if (fam != null && G.TryGetValue(key, out var ch))
                {
                    using var f = new Font(fam, size * 0.72f, GraphicsUnit.Pixel);
                    var sz = g.MeasureString(ch.ToString(), f, PointF.Empty, StringFormat.GenericTypographic);
                    g.DrawString(ch.ToString(), f, br, (size - sz.Width) / 2f, (size - sz.Height) / 2f, StringFormat.GenericTypographic);
                }
                else
                {
                    using var p = new Pen(col, Math.Max(1.2f, size / 12f));
                    g.DrawEllipse(p, 1, 1, size - 3, size - 3);
                    using var f = new Font("Segoe UI", size * 0.45f, FontStyle.Bold, GraphicsUnit.Pixel);
                    var t = char.ToUpper(key[0]).ToString();
                    var sz = g.MeasureString(t, f);
                    g.DrawString(t, f, br, (size - sz.Width) / 2f + 0.5f, (size - sz.Height) / 2f);
                }
            }
            cache[k] = bmp;
            return bmp;
        }
    }

    public static Button Btn(string key, string text, EventHandler click, bool primary = false)
    {
        var b = primary ? Ui.PrimaryBtn(text, click) : Ui.Btn(text, click);
        b.Image = Get(key, 16, primary ? Color.White : Theme.Blue);
        b.ImageAlign = ContentAlignment.MiddleLeft;
        b.TextImageRelation = TextImageRelation.ImageBeforeText;
        b.Padding = new Padding(4, 0, 6, 0);
        return b;
    }

    public static ToolStripButton Tool(string key, string text, Action a, string tip = null)
        => new(text, Get(key, 18, Theme.Blue), (s, e) => Ui.Try(a)) { ToolTipText = tip ?? text, ForeColor = Theme.Navy, ImageScaling = ToolStripItemImageScaling.None, Margin = new Padding(2, 1, 4, 1) };
}
