using System;
using System.Linq;
using UglyToad.PdfPig;

public static class DumpPages
{
    public static void Run(string pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        foreach (var n in new[] { 40, 41, 42, 100, 150 })
        {
            if (n > doc.NumberOfPages) continue;
            var page = doc.GetPage(n);
            var t = page.Text ?? string.Empty;
            Console.WriteLine("==== PAGE {n} len={t.Length} imgs={page.GetImages().Count()} ====");
            Console.WriteLine(t.Length > 1500 ? t[..1500] : t);
            Console.WriteLine();
        }
    }
}
