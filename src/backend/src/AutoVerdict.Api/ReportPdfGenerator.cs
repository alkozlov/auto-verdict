using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoVerdict.Api;

public static class ReportPdfGenerator
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static byte[] Generate(
        string title,
        string? listingUrl,
        DateTimeOffset createdAt,
        string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Content().Column(col =>
                {
                    col.Spacing(5);

                    // Header
                    col.Item()
                        .DefaultTextStyle(s => s.FontSize(20).Bold().FontColor(Colors.Grey.Darken4))
                        .Text(title);

                    col.Item()
                        .DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium))
                        .Text($"Created: {createdAt:d MMMM yyyy, HH:mm}");

                    if (!string.IsNullOrWhiteSpace(listingUrl))
                        col.Item()
                            .DefaultTextStyle(s => s.FontSize(9).FontColor("#1d4ed8"))
                            .Text(listingUrl);

                    col.Item()
                        .PaddingVertical(4, Unit.Point)
                        .LineHorizontal(1, Unit.Point)
                        .LineColor(Colors.Grey.Lighten2);

                    foreach (var block in document)
                        RenderBlock(col, block);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("AutoVerdict Report  •  Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    // ── Block rendering ──────────────────────────────────────────────────────

    private static void RenderBlock(ColumnDescriptor col, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                RenderHeading(col, heading);
                break;

            case ParagraphBlock paragraph:
                col.Item().Text(t => AppendInlines(t, paragraph.Inline));
                break;

            case ListBlock list:
                RenderList(col, list, 0);
                break;

            case Table table:
                RenderTable(col, table);
                break;

            case ThematicBreakBlock:
                col.Item()
                    .PaddingVertical(4, Unit.Point)
                    .LineHorizontal(0.5f, Unit.Point)
                    .LineColor(Colors.Grey.Lighten2);
                break;

            case CodeBlock code:
                col.Item()
                    .Background(Colors.Grey.Lighten4)
                    .Padding(6, Unit.Point)
                    .DefaultTextStyle(s => s.FontFamily("Courier New").FontSize(9).FontColor(Colors.Grey.Darken3))
                    .Text(code.Lines.ToString());
                break;
        }
    }

    private static void RenderHeading(ColumnDescriptor col, HeadingBlock heading)
    {
        if (heading.Level <= 2)
            col.Item().PaddingTop(6, Unit.Point).Text("");

        (float size, Color color) = heading.Level switch
        {
            1 => (18f, Colors.Grey.Darken4),
            2 => (14f, Colors.Grey.Darken4),
            3 => (12f, Colors.Grey.Darken3),
            _ => (11f, Colors.Grey.Darken3),
        };

        col.Item()
            .DefaultTextStyle(s => s.FontSize(size).Bold().FontColor(color))
            .Text(t => AppendInlines(t, heading.Inline));

        if (heading.Level <= 2)
            col.Item()
                .LineHorizontal(0.5f, Unit.Point)
                .LineColor(Colors.Grey.Lighten2);
    }

    private static void RenderList(ColumnDescriptor col, ListBlock list, int indent)
    {
        int orderedIndex = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            string bullet = list.IsOrdered ? $"{orderedIndex++}." : "•";

            col.Item().PaddingLeft(indent * 12, Unit.Point).Row(row =>
            {
                row.ConstantItem(20, Unit.Point)
                    .DefaultTextStyle(s => s.FontSize(10).FontColor(Colors.Grey.Medium))
                    .Text(bullet);

                row.RelativeItem().Column(itemCol =>
                {
                    itemCol.Spacing(1);
                    foreach (var child in item)
                    {
                        if (child is ParagraphBlock para)
                            itemCol.Item().Text(t => AppendInlines(t, para.Inline));
                        else if (child is ListBlock nested)
                            RenderList(itemCol, nested, indent + 1);
                    }
                });
            });
        }
    }

    private static void RenderTable(ColumnDescriptor col, Table table)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0) return;

        int colCount = table.ColumnDefinitions.Count > 0
            ? table.ColumnDefinitions.Count
            : rows[0].OfType<TableCell>().Count();

        if (colCount == 0) return;

        col.Item().PaddingVertical(4, Unit.Point).Table(t =>
        {
            t.ColumnsDefinition(defs =>
            {
                for (int i = 0; i < colCount; i++)
                    defs.RelativeColumn();
            });

            foreach (var row in rows)
            {
                var cells = row.OfType<TableCell>().ToList();
                foreach (var cell in cells)
                {
                    var firstPara = cell.OfType<ParagraphBlock>().FirstOrDefault();
                    var cellContainer = t.Cell();

                    if (row.IsHeader)
                    {
                        cellContainer
                            .Background(Colors.Grey.Lighten4)
                            .Border(0.5f, Colors.Grey.Lighten1)
                            .Padding(5, Unit.Point)
                            .DefaultTextStyle(s => s.Bold().FontSize(9).FontColor(Colors.Grey.Darken4))
                            .Text(tx => { if (firstPara != null) AppendInlines(tx, firstPara.Inline); });
                    }
                    else
                    {
                        cellContainer
                            .Border(0.5f, Colors.Grey.Lighten2)
                            .Padding(5, Unit.Point)
                            .DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken2))
                            .Text(tx => { if (firstPara != null) AppendInlines(tx, firstPara.Inline); });
                    }
                }
            }
        });
    }

    // ── Inline rendering ─────────────────────────────────────────────────────

    private static void AppendInlines(
        TextDescriptor text,
        ContainerInline? inlines,
        bool bold = false,
        bool italic = false)
    {
        if (inlines == null) return;
        foreach (var inline in inlines)
            AppendInline(text, inline, bold, italic);
    }

    private static void AppendInline(TextDescriptor text, Inline inline, bool bold, bool italic)
    {
        switch (inline)
        {
            case LiteralInline literal:
            {
                var content = literal.Content.ToString();
                if (string.IsNullOrEmpty(content)) break;
                var span = text.Span(content);
                if (bold) span.Bold();
                if (italic) span.Italic();
                break;
            }
            case EmphasisInline emphasis:
            {
                bool b = bold || emphasis.DelimiterCount >= 2;
                bool i = italic || emphasis.DelimiterCount == 1;
                AppendInlines(text, emphasis, b, i);
                break;
            }
            case CodeInline code:
                text.Span(code.Content).FontFamily("Courier New").FontSize(9);
                break;

            case LinkInline link:
                AppendInlines(text, link, bold, italic);
                break;

            case TaskList taskList:
                text.Span(taskList.Checked ? "☑ " : "☐ ").FontColor(Colors.Blue.Medium);
                break;

            case LineBreakInline { IsHard: true }:
                text.Span("\n");
                break;
        }
    }
}
