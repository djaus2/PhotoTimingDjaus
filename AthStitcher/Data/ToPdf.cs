using AthStitcher.Data;
using AthStitcherGUI.ViewModels;
using global::AthStitcherGUI.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;

namespace AthStitcher.Data
{
    public static class PdfExporter
    {
        /// <summary>
        /// Exports a single heat to a PDF containing header, optional stitched image and a results table.
        /// </summary>
        public static void ExportHeatToPdf(AthStitcherModel vm, Heat heat, string outputPdfPath, string? stitchedImagePath = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            if (heat == null) throw new ArgumentNullException(nameof(heat));
            if (string.IsNullOrWhiteSpace(outputPdfPath)) throw new ArgumentNullException(nameof(outputPdfPath));

            var meet = vm.CurrentMeet;
            var ev = vm.CurrentEvent;
            var results = (heat.Results ?? Enumerable.Empty<LaneResult>()).OrderBy(r => r.Lane).ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Keep only meet info in the repeating page header
                    page.Header().Column(column =>
                    {
                        // Padding extensions operate on IContainer, not on TextBlockDescriptor.
                        column.Item().PaddingBottom(5).Text($"{meet}").SemiBold().FontSize(16);
                        column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(8).Column(content =>
                    {
                        // Print event + heat as a bold heading immediately before the data (appears in content, not header)
                        // Call PaddingBottom on the container returned by Item(), not after Text(...)
                        content.Item().PaddingBottom(6).Text($"{ev} — Heat {heat.HeatNo}")
                                       .SemiBold()
                                       .FontSize(12);

                        // Optional stitched image (fit to width)
                        //if (!string.IsNullOrEmpty(stitchedImagePath) && File.Exists(stitchedImagePath))
                        //{
                        //    content.Item().Image(stitchedImagePath, ImageScaling.FitWidth).Height(200);
                        //    content.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        //}

                        // Results table
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(40);   // Lane
                                cols.RelativeColumn(1);    // Bib
                                cols.RelativeColumn(3);    // Name
                                cols.RelativeColumn(1);    // Result
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Lane").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Bib").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Name").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Result").SemiBold();
                            });

                            // Data rows
                            foreach (var r in results)
                            {
                                table.Cell().Padding(4).Text(r.Lane.ToString());
                                table.Cell().Padding(4).Text(r.BibNumberStr ?? string.Empty);
                                table.Cell().Padding(4).Text(r.NameStr ?? string.Empty);
                                var resultText = r.ResultStr ?? (r.ResultSeconds.HasValue ? r.ResultSeconds.Value.ToString("0.000") : string.Empty);
                                table.Cell().Padding(4).Text(resultText);
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
    }
}