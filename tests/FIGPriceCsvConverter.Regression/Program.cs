using System.Globalization;
using FIGPriceCsvConverter;

internal static class Program
{
    static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    static void Reject(Action action, string label)
    {
        try { action(); } catch (Exception ex) when (ex is InvalidDataException or ArgumentException or IOException or OperationCanceledException) { return; }
        throw new Exception("Expected rejection: " + label);
    }
    [STAThread]
    static void Main(string[] args)
    {
        var root = Path.GetFullPath(args.Length > 0 ? args[0] : "converter-test-output");
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "esignal.csv"); var target = Path.Combine(root, "converted.csv");
        const string header = "Date,Time,Bar#,Bar Index,Tick Range,Open,High,Low,Close,Vol\n";
        const string sample = "2026-10-20,10:37:00 PM,3780062/3780062,0,9,29984,29986,29983.75,29986,13\n2026-10-20,10:36:00 PM,3780061/3780062,-1,35,29981.75,29987.5,29978.75,29982.75,82\n2026-10-20,10:35:00 PM,3780060/3780062,-2,50,29988.25,29988.25,29975.75,29982.75,194\n";
        File.WriteAllText(source, header + sample);
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-CA");
        var result = Converter.ConvertFile(source, target, eastern, AmbiguousTimePolicy.Reject, true);
        var prices = FIGSignalBacktest.PriceCsv.Read(target, 0);
        Check(result.Count == 3 && prices.Count == 3 && prices[0].Volume == 194 && prices[2].Volume == 13, "reverse export sorted, Vol mapped, backtester accepts output");
        Check(result.FirstUtc == new DateTimeOffset(2026, 10, 21, 2, 35, 0, TimeSpan.Zero), "Eastern daylight time to UTC crosses midnight");
        Check(prices[0].Low == 29975.75 && prices[2].Time - prices[0].Time == 120, "decimal precision and minute spacing");
        foreach (var (date, time, expected) in new[]
        {
            ("10/28/2026", "09:15:00 AM", new DateTimeOffset(2026, 10, 28, 13, 15, 0, TimeSpan.Zero)),
            ("1/2/2026", "9:15:00 PM", new DateTimeOffset(2026, 1, 3, 2, 15, 0, TimeSpan.Zero)),
            ("01/02/2026", "21:15:00", new DateTimeOffset(2026, 1, 3, 2, 15, 0, TimeSpan.Zero))
        })
        {
            File.WriteAllText(source, header + $"{date},{time},1152778/1152778,0,21,29911.25,29914,29908.75,29908.75,72\n");
            var usResult = Converter.ConvertFile(source, target, eastern, AmbiguousTimePolicy.Reject, true);
            var usPrices = FIGSignalBacktest.PriceCsv.Read(target, 0);
            Check(usResult.Count == 1 && usResult.FirstUtc == expected, "US date and time parsed independently of regional settings");
            Check(usPrices[0].Open == 29911.25 && usPrices[0].High == 29914 && usPrices[0].Low == 29908.75 && usPrices[0].Close == 29908.75 && usPrices[0].Volume == 72, "reported row prices and volume preserved");
        }
        File.WriteAllText(source, header + sample);
        Check(Converter.ToUnixTime(new(2026, 1, 20, 12, 0, 0), eastern, AmbiguousTimePolicy.Reject) == new DateTimeOffset(2026, 1, 20, 17, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(), "winter uses standard offset");
        var india = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        Check(Converter.ToUnixTime(new(2026, 10, 20, 12, 0, 0), india, AmbiguousTimePolicy.Reject) == new DateTimeOffset(2026, 10, 20, 6, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(), "half-hour timezone");
        var fold = new DateTime(2026, 11, 1, 1, 30, 0);
        Reject(() => Converter.ToUnixTime(fold, eastern, AmbiguousTimePolicy.Reject), "ambiguous fall-back");
        Check(Converter.ToUnixTime(fold, eastern, AmbiguousTimePolicy.LaterUtc) - Converter.ToUnixTime(fold, eastern, AmbiguousTimePolicy.EarlierUtc) == 3600, "explicit fall-back policies");
        Reject(() => Converter.ToUnixTime(new(2026, 3, 8, 2, 30, 0), eastern, AmbiguousTimePolicy.EarlierUtc), "nonexistent spring-forward");
        var original = File.ReadAllText(target);
        Reject(() => Converter.ConvertFile(source, source, eastern, AmbiguousTimePolicy.Reject, true), "same file");
        Reject(() => Converter.ConvertFile(source, target, eastern, AmbiguousTimePolicy.Reject, false), "no overwrite without consent");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Reject(() => Converter.ConvertFile(source, target, eastern, AmbiguousTimePolicy.Reject, true, cancellation: cancelled.Token), "cancel");
        foreach (var content in new[] { header, header + sample + sample, header + "2026-10-20,22:37:00,x,0,9,29984,29986,29983.75,29986,bad\n", header + "2026-03-08,2:30:00 AM,x,0,9,1,2,0,1,1\n" })
        {
            File.WriteAllText(source, content);
            Reject(() => Converter.ConvertFile(source, target, eastern, AmbiguousTimePolicy.Reject, true), "invalid input");
            Check(File.ReadAllText(target) == original, "failed conversion preserves target");
        }
        File.WriteAllText(source, "\"Date\",\"Time\",Open,High,Low,Close,Volume\n2026-10-20,22:37:00,1,2,0,1,42\n");
        Check(Converter.ConvertFile(source, target, TimeZoneInfo.Utc, AmbiguousTimePolicy.Reject, true).Count == 1, "quoted header, Volume alias and 24-hour time");
        Check(Directory.GetFiles(root, ".fig-convert-*.tmp").Length == 0, "no temporary files left");
        ApplicationConfiguration.Initialize();
        using var form = new MainForm();
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-30000, -30000);
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(root, "converter-window.png"));
        Console.WriteLine("PASS: CSV compatibility, sorting, culture, time zones, DST, validation, cancellation, target preservation, UI render.");
        Console.WriteLine(root);
    }
}
