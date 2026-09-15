using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Rendering.Skia;

var pdf = args.ElementAtOrDefault(0)
    ?? @"c:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-DNI-2025-2026.pdf";

var regex = new Regex(
    @"\b(?<codigo>DNI[\s-]*\d{3,5}(?:-[A-Z0-9]{1,4})?)\b|(?<codigo>DNI(?:\s*DNI){1,6}\s*\d{8,24})\b",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

var regexLoose = new Regex(
    @"DNI(?:\s*DNI)*[\s-]*\d{3,5}",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

var glued = new Regex(
    @"DNI(?:\s*DNI){1,6}\s*\d{8,40}",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

using var doc = PdfDocument.Open(pdf, SkiaRenderingParsingOptions.Instance);
var totalStrict = 0;
var totalLoose = 0;
var totalGlued = 0;
var pagesWith = 0;
var sample = new List<string>();

for (var p = 1; p <= doc.NumberOfPages; p++)
{
    var text = doc.GetPage(p).Text ?? "";
    var s = regex.Matches(text).Count;
    var l = regexLoose.Matches(text).Count;
    var g = glued.Matches(text).Count;
    totalStrict += s;
    totalLoose += l;
    totalGlued += g;
    if (s > 0 || l > 0) pagesWith++;
    if (sample.Count < 25 && (s > 0 || l > 0 || g > 0))
    {
        sample.Add($"p{p} strict={s} loose={l} glued={g} snippet={(text.Length > 200 ? text[..200] : text).Replace('\n', ' ')}");
    }
}

Console.WriteLine($"Pages={doc.NumberOfPages} pagesWithCodes={pagesWith}");
Console.WriteLine($"strictMatches={totalStrict} looseMatches={totalLoose} gluedBlocks={totalGlued}");
foreach (var line in sample) Console.WriteLine(line);
