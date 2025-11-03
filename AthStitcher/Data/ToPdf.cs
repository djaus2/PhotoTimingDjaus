using AthStitcher.Data;
using AthStitcherGUI.ViewModels;
using global::AthStitcherGUI.ViewModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AthStitcher.Data
{
    public static class PdfExporter
    {
        public static string AppIcon { get; set; } = "djcolor.jpg";
        public static string InfoLink { get; set; } = "https://davidjones.sportronics.com.au/tags/athstitcher/";
        public static string InfoLinkText { get; set; } = "Blogs about AthStitcher app";
        public static string GitHubLink { get; set; } = "https://github.com/djaus2/PhotoTimingDjaus";
        public static string GitHubLinkText { get; set; } = "App Repository ... See AthStitcher project";


        private static string headerImagePath 
        {
            get
            {
                if (File.Exists(AppIcon))
                    return AppIcon;
                else
                {
                    string path = Path.Combine(AppContext.BaseDirectory, "Media", AppIcon);
                    if(File.Exists(path))
                        return path;
                }
                return "";
            }
        }

        [ModuleInitializer]
        public static void Init()
        {
            // one-time per assembly
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Exports a single heat to a PDF containing header, optional stitched image and a results table.
        /// </summary>
        public static void ExportHeatToPdf(AthStitcherModel vm, Heat heat, string outputPdfPath, string? stitchedImagePath = null)
        {
            //QuestPDF.Settings.License = LicenseType.Community;
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (heat == null) throw new ArgumentNullException(nameof(heat));
            if (string.IsNullOrWhiteSpace(outputPdfPath)) throw new ArgumentNullException(nameof(outputPdfPath));

            var meet = vm.CurrentMeet;
            var ev = vm.CurrentEvent;
            List<LaneResult> results = (heat.Results ?? Enumerable.Empty<LaneResult>())
               .OrderBy(r => r.ResultSeconds ?? double.MaxValue)  // nulls last
               .ToList();
            var document = Document.Create(container =>
            {
                _ = container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Keep only meet info in the repeating page header
                    // repeating meet header (image + link + meet in one row)
                    
                    page.Header().Row(row =>
                    {
                        // Left: flexible text column (link + meet)
                        row.RelativeItem().Column(col =>
                        {

                            col.Item()
                                .PaddingBottom(2)
                                .Hyperlink(GitHubLink)
                                .Text(GitHubLinkText)
                                .FontColor(Colors.Blue.Medium)
                                .Underline()
                                .FontSize(10);

                            col.Item()
                                .PaddingBottom(2)
                                .Hyperlink(InfoLink)
                                .Text(InfoLinkText)
                                .FontColor(Colors.Blue.Medium)
                                .Underline()
                                .FontSize(10);

                            col.Item()
                                .PaddingBottom(4)
                                .Text($"{meet}")
                                .SemiBold()
                                .FontSize(16);
                        });

                        // Right: fixed-width image column. Set alignment/height on the cell (not inside Element).
                        // ConstantItem(width).AlignRight().Height(h) then single Element(...) which adds the Image child.
                        row.ConstantItem(30)
                            .AlignRight()    // set alignment on the container (safe)
                            .Height(40)      // set element height on the container (safe)
                            .Element(img =>
                            {
                                if (File.Exists(headerImagePath))
                                {
                                    img.Image(headerImagePath).FitArea(); // single child only
                                }
                            });
                    });

                    page.Content().PaddingVertical(8).Column(content =>
                    {

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(40);   // Posn/width
                                cols.ConstantColumn(40);   // Lane
                                cols.RelativeColumn(1);    // Bib
                                cols.RelativeColumn(3);    // Name
                                cols.RelativeColumn(1);    // Result
                            });

                            // Header rows (title + column headers)
                            table.Header(header =>
                            {
                                header.Cell().ColumnSpan(5).Padding(6).Text($"Event: {ev?.ToString() ?? "-"}    Heat: {heat.HeatNo}").SemiBold().FontSize(12);
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Posn").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Lane").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Bib").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Name").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Result").SemiBold();
                            });

                            int Posn = 1;
                            foreach (var r in results)
                            {
                                table.Cell().Padding(4).AlignRight().Text(Posn.ToString());
                                table.Cell().Padding(4).Text(r.Lane.ToString());
                                table.Cell().Padding(4).Text(r.BibNumberStr ?? string.Empty);
                                table.Cell().Padding(4).Text(r.NameStr ?? string.Empty);
                                var resultText = r.ResultStr ?? (r.ResultSeconds.HasValue ? r.ResultSeconds.Value.ToString("0.000") : string.Empty);
                                table.Cell().Padding(4).Text(resultText);
                                Posn++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span($"Generated: {DateTime.Now:G}").FontSize(9);
                    });
                });
            });

            // Ensure directory exists
            var dir = Path.GetDirectoryName(outputPdfPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(dir);

            // Generate PDF file
            document.GeneratePdf(outputPdfPath);
        }

        /// <summary>
        /// Export all heats for a given event into a single continuous PDF.
        /// Heats flow continuously; QuestPDF will paginate automatically when content overflows.
        /// Each heat is rendered as a table whose header (title + column row) will repeat if that table spans pages.
        /// </summary>
        public static void ExportEventToPdf(AthStitcherModel vm, AthStitcher.Data.Event ev, string outputPdfPath, string? stitchedImagePath = null)
        {
            //QuestPDF.Settings.License = LicenseType.Community;
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (ev == null) throw new ArgumentNullException(nameof(ev));
            if (string.IsNullOrWhiteSpace(outputPdfPath)) throw new ArgumentNullException(nameof(outputPdfPath));

            // Ensure heats and results are loaded
            List<Heat> heats;
            if (ev.Heats != null && ev.Heats.Any() && ev.Heats.All(h => h.Results != null))
            {
                heats = ev.Heats.OrderBy(h => h.HeatNo).ToList();
            }
            else
            {
                using var ctx = new AthStitcherDbContext();
                heats = ctx.Heats
                    .AsNoTracking()
                    .Include(h => h.Results)
                    .Where(h => h.EventId == ev.Id)
                    .OrderBy(h => h.HeatNo)
                    .ToList();
            }

            var meet = vm.CurrentMeet;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // repeating meet header
                    page.Header().Row(row =>
                    {
                        // Left: flexible text column (link + meet)
                        row.RelativeItem().Column(col =>
                        {
                            col.Item()
                                .PaddingBottom(2)
                                .Hyperlink(GitHubLink)
                                .Text(GitHubLinkText)
                                .FontColor(Colors.Blue.Medium)
                                .Underline()
                                .FontSize(10);

                            col.Item()
                                .PaddingBottom(2)
                                .Hyperlink(InfoLink)
                                .Text(InfoLinkText)
                                .FontColor(Colors.Blue.Medium)
                                .Underline()
                                .FontSize(10);

                            col.Item()
                                .PaddingBottom(4)
                                .Text($"{meet}")
                                .SemiBold()
                                .FontSize(16);
                        });

                        // Right: fixed-width image column. Set alignment/height on the cell (not inside Element).
                        // ConstantItem(width).AlignRight().Height(h) then single Element(...) which adds the Image child.
                        row.ConstantItem(30)
                            .AlignRight()    // set alignment on the container (safe)
                            .Height(40)      // set element height on the container (safe)
                            .Element(img =>
                            {
                                if (File.Exists(headerImagePath))
                                {
                                    img.Image(headerImagePath).FitArea(); // single child only
                                }
                            });
                    });


                    // Single content stream: iterate heats and render a table per heat.
                    // QuestPDF will automatically paginate the content when it overflows pages.
                    page.Content().PaddingVertical(8).Stack(stack =>
                    {
                        foreach (var heat in heats)
                        {
                            var results = (heat.Results ?? Enumerable.Empty<LaneResult>())
                                          .OrderBy(r => r.ResultSeconds ?? double.MaxValue)
                                          .ToList();

                            stack.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(40);   // Posn
                                    cols.ConstantColumn(40);   // Lane
                                    cols.RelativeColumn(1);    // Bib
                                    cols.RelativeColumn(3);    // Name
                                    cols.RelativeColumn(1);    // Result
                                });

                                // Table header contains heat title (spanning columns) and the column labels.
                                table.Header(header =>
                                {
                                    header.Cell().ColumnSpan(5).Padding(6).Text($"Event: {ev?.ToString() ?? "-"}    Heat: {heat.HeatNo}").SemiBold().FontSize(12);
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Posn").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Lane").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Bib").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Result").SemiBold();
                                });

                                int posn = 1;
                                foreach (var r in results)
                                {
                                    table.Cell().Padding(4).AlignRight().Text(posn.ToString());
                                    table.Cell().Padding(4).Text(r.Lane.ToString());
                                    table.Cell().Padding(4).Text(r.BibNumberStr ?? string.Empty);
                                    table.Cell().Padding(4).Text(r.NameStr ?? string.Empty);
                                    var resultText = r.ResultStr ?? (r.ResultSeconds.HasValue ? r.ResultSeconds.Value.ToString("0.000") : string.Empty);
                                    table.Cell().Padding(4).Text(resultText);
                                    posn++;
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span($"Generated: {DateTime.Now:G}").FontSize(9);
                    });
                });
            });

            var dir = Path.GetDirectoryName(outputPdfPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(dir);
            document.GeneratePdf(outputPdfPath);
        }
    }
}