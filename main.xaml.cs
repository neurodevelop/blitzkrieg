using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace blitz
{
    public partial class main : Window
    {
        ObservableCollection<stage> bank = new ObservableCollection<stage>();
        ICollectionView view;
        int part = 0;
        string file = "data.json";
        string path = "";
        string lang = "ru";

        public main()
        {
            InitializeComponent();
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "blitz");
            try { Directory.CreateDirectory(dir); } catch { }
            path = System.IO.Path.Combine(dir, file);
            view = CollectionViewSource.GetDefaultView(bank);
            view.Filter = f => {
                stage s = f as stage;
                if (s == null) return false;
                if (part == 0) return true;
                return s.tier == part;
            };
            grid.ItemsSource = view;
            load();
            if (bank.Count > 0) part = bank.Min(x => x.tier);
            else part = 0;
            talk();
            show();
            refresh();
            trip(null, null);
        }

        void load()
        {
            try
            {
                if (!File.Exists(path)) return;
                string raw = File.ReadAllText(path);
                pack data = JsonSerializer.Deserialize<pack>(raw);
                if (data == null) return;
                field.Text = data.text ?? "";
                if (!string.IsNullOrWhiteSpace(data.lang)) lang = data.lang;
                bank.Clear();
                if (data.grid != null)
                {
                    foreach (stage s in data.grid)
                    {
                        stage t = new stage();
                        t.from = s.from;
                        t.to = s.to;
                        t.rank = s.rank;
                        t.tier = s.tier == 0 ? 1 : s.tier;
                        bank.Add(t);
                    }
                }
            }
            catch { }
        }

        void save()
        {
            try
            {
                pack data = new pack();
                data.text = field.Text;
                data.grid = bank.ToList();
                data.lang = lang;
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        List<double> parse(string raw)
        {
            List<double> bag = new List<double>();
            if (string.IsNullOrWhiteSpace(raw)) return bag;
            string clean = raw.Replace("%", " ").Replace("→", " ").Replace("–", " ").Replace("-", " ");
            string[] piece = clean.Split(new char[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string bit in piece)
            {
                string item = bit.Trim();
                if (item.Length == 0) continue;
                double val;
                if (double.TryParse(item, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val))
                {
                    if (val >= 0 && val <= 100) bag.Add(val);
                    continue;
                }
                string alt = item.Replace(",", ".");
                if (double.TryParse(alt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val))
                {
                    if (val >= 0 && val <= 100) bag.Add(val);
                    continue;
                }
                if (double.TryParse(item, out val))
                {
                    if (val >= 0 && val <= 100) bag.Add(val);
                }
            }
            bag = bag.Distinct().ToList();
            bag.Sort();
            if (bag.Count == 1)
            {
                double step = bag[0];
                if (step > 0 && step <= 50)
                {
                    List<double> temp = new List<double>();
                    for (double d = 0; d < 100; d += step) temp.Add(d);
                    return temp;
                }
            }
            return bag;
        }

        void build(object root, RoutedEventArgs args)
        {
            List<double> spot = parse(field.Text);
            spot = spot.Where(x => x < 100 && x >= 0).Distinct().ToList();
            spot.Sort();
            if (spot.Count == 0) return;

            List<double> all = new List<double>();
            all.Add(0);
            all.AddRange(spot);
            all.Add(100);
            all = all.Distinct().ToList();
            all.Sort();

            List<stage> temp = new List<stage>();
            for (int w = 1; w < all.Count; w++)
            {
                for (int i = 0; i + w < all.Count; i++)
                {
                    double a = all[i];
                    double b = all[i + w];
                    if (a >= b) continue;
                    stage s = new stage();
                    s.from = a;
                    s.to = b;
                    s.rank = 0;
                    s.tier = w;
                    temp.Add(s);
                }
            }

            bank.Clear();
            foreach (stage s in temp) bank.Add(s);

            foreach (stage s in bank) s.rank = 0;
            var first = bank.OrderBy(x => x.tier).ThenBy(x => x.from).FirstOrDefault();
            if (first != null) first.rank = 1;
            part = 1;
            if (bank.Count > 0) part = bank.Min(x => x.tier);

            show();
            view.Refresh();
            refresh();
            save();
        }

        void wipe(object root, RoutedEventArgs args)
        {
            bank.Clear();
            field.Text = "";
            part = 0;
            show();
            view.Refresh();
            refresh();
            save();
        }

        void pick(object root, MouseButtonEventArgs args)
        {
            Border box = root as Border;
            if (box == null) return;
            stage s = box.DataContext as stage;
            if (s == null) return;

            if (s.rank == 0)
            {
                foreach (stage x in bank.Where(x => x.rank == 1).ToList()) x.rank = 0;
                s.rank = 1;
                part = s.tier;
                view.Refresh();
                show();
                refresh();
                save();
                return;
            }
            if (s.rank == 1)
            {
                s.rank = 2;
                next();
                refresh();
                save();
                return;
            }
        }

        void next()
        {
            foreach (stage x in bank.Where(x => x.rank == 1).ToList()) x.rank = 0;

            var ordered = bank.OrderBy(x => x.tier).ThenBy(x => x.from).ToList();

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                if (ordered[i].rank == 2 && ordered[i + 1].rank == 0)
                {
                    ordered[i + 1].rank = 1;
                    part = ordered[i + 1].tier;
                    view.Refresh();
                    show();
                    return;
                }
            }

            var firstIdle = ordered.FirstOrDefault(x => x.rank == 0);
            if (firstIdle != null)
            {
                firstIdle.rank = 1;
                part = firstIdle.tier;
                view.Refresh();
                show();
            }
        }

        void undo(object root, RoutedEventArgs args)
        {
            Button btn = root as Button;
            if (btn == null) return;
            stage s = btn.DataContext as stage;
            if (s == null) return;

            bool wasWork = s.rank == 1;
            s.rank = 0;

            if (wasWork)
            {
                next();
            }

            view.Refresh();
            show();
            refresh();
            save();
            args.Handled = true;
        }

        void jump(object root, RoutedEventArgs args)
        {
            Button btn = root as Button;
            if (btn == null) return;
            object tag = btn.Tag;
            if (tag == null) return;
            int v = 0;
            try { v = Convert.ToInt32(tag); } catch { return; }
            part = v;
            view.Refresh();
            show();
            refresh();
        }

        void swap(object root, RoutedEventArgs args)
        {
            Button btn = root as Button;
            if (btn == null) return;
            object tag = btn.Tag;
            if (tag == null) return;
            lang = tag.ToString();
            talk();
            show();
            refresh();
            save();
        }

        void talk()
        {
            bool ru = lang == "ru";
            Brush on = (Brush)new BrushConverter().ConvertFromString("#4a90e2");
            Brush off = (Brush)new BrushConverter().ConvertFromString("#2d2d32");
            if (btnru != null) btnru.Background = ru ? on : off;
            if (btnen != null) btnen.Background = ru ? off : on;
            if (pos != null) pos.Text = ru ? "стартпосы" : "startposes";
            if (hint != null) hint.Text = ru ? "20, 50, 80 -- через пробел или запятую, можно с %." : "20, 50, 80 -- space or comma, % allowed.";
            if (go != null) go.Content = ru ? "начать" : "start";
            if (cut != null) cut.Content = ru ? "очистить" : "clear";
        }

        void show()
        {
            tabs.Children.Clear();
            var set = bank.Select(x => x.tier).Distinct().OrderBy(x => x).ToList();
            if (set.Count == 0) return;
            if (part == 0) part = set[0];
            bool ru = lang == "ru";
            string name = ru ? "стейдж" : "stage";
            foreach (int t in set)
            {
                Button b = new Button();
                int have = bank.Count(x => x.tier == t && x.rank == 2);
                int here = bank.Count(x => x.tier == t);
                b.Content = $"{name} {t}  {have}/{here}";
                b.Tag = t;
                b.Padding = new Thickness(14, 7, 14, 7);
                b.Margin = new Thickness(0, 0, 8, 0);
                b.FontSize = 12;
                b.Click += jump;
                if (t == part) b.Background = (Brush)new BrushConverter().ConvertFromString("#4a90e2");
                else b.Background = (Brush)new BrushConverter().ConvertFromString("#2d2d32");
                tabs.Children.Add(b);
            }
        }

        void trip(object root, TextChangedEventArgs args)
        {
            try
            {
                if (hint != null) hint.Visibility = string.IsNullOrWhiteSpace(field.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        void press(object root, KeyEventArgs args)
        {
            if (args.Key == Key.Enter) build(root, null);
        }

        void link(object root, RequestNavigateEventArgs args)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = args.Uri.AbsoluteUri;
                info.UseShellExecute = true;
                Process.Start(info);
                args.Handled = true;
            }
            catch { }
        }

        void refresh()
        {
            int total = bank.Count;
            int done = bank.Count(x => x.rank == 2);
            double rate = total == 0 ? 0 : (double)done / total * 100.0;
            if (head != null) head.Value = rate;

            if (bank.Count == 0 || part == 0)
            {
                if (info != null) info.Text = "";
                if (more != null) more.Text = "";
                if (stat != null) stat.Text = "";
                if (tabs != null) tabs.Visibility = Visibility.Collapsed;
                if (grid != null) grid.Visibility = Visibility.Collapsed;
                return;
            }

            int here = bank.Count(x => x.tier == part);
            int have = bank.Count(x => x.tier == part && x.rank == 2);
            double pace = here == 0 ? 0 : (double)have / here * 100.0;

            bool ru = lang == "ru";
            string tname = ru ? "стейдж" : "stage";
            string of = ru ? "из" : "of";
            string all = ru ? "всего" : "total";

            if (info != null) info.Text = $"{tname} {part}";
            if (more != null) more.Text = $"{have} {of} {here}  {pace:0.#}%";
            if (stat != null) stat.Text = $"{all} {done} {of} {total}";
            if (tabs != null) tabs.Visibility = Visibility.Visible;
            if (grid != null) grid.Visibility = Visibility.Visible;
        }
    }
}
