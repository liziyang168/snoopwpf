namespace Snoop.Infrastructure;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Snoop.Data.Tree;
using Snoop.Infrastructure.Diagnostics;

public static class DiagnosticsExporter
{
    public static void Export(IEnumerable<DiagnosticItem> diagnosticItems, TextWriter textWriter)
    {
        var dataItems = diagnosticItems
            .Select(x => new DiagnosticItemData(x.Area, x.Level, x.Name, x.Description, x.SourceObject?.ToString(), TreeItemDiagnosticPathData.Build(x.TreeItem)))
            .ToList();
        new XmlSerializer(typeof(List<DiagnosticItemData>)).Serialize(textWriter, dataItems);
    }

    [PublicAPI]
    public record DiagnosticItemData(DiagnosticArea Area, DiagnosticLevel Level, string Name, string Description, string? SourceObject, List<TreeItemDiagnosticPathData>? TreeItemPath)
    {
        public DiagnosticItemData()
            : this(DiagnosticArea.Misc, DiagnosticLevel.Info, string.Empty, string.Empty, null, null)
        {
        }

        public DiagnosticArea Area { get; set; } = Area;

        public DiagnosticLevel Level { get; set; } = Level;

        public string Name { get; set; } = Name;

        public string Description { get; set; } = Description;

        public string? SourceObject { get; set; } = SourceObject;

        public List<TreeItemDiagnosticPathData>? TreeItemPath { get; set; } = TreeItemPath;
    }

    [PublicAPI]
    public record TreeItemDiagnosticPathData(string DisplayName, string TypeName)
    {
        public TreeItemDiagnosticPathData()
            : this(string.Empty, string.Empty)
        {
        }

        public string DisplayName { get; set; } = DisplayName;

        public string TypeName { get; set; } = TypeName;

        public static List<TreeItemDiagnosticPathData>? Build(TreeItem? treeItem)
        {
            if (treeItem is null)
            {
                return null;
            }

            var items = new List<TreeItemDiagnosticPathData>();

            var current = treeItem;
            while (current is not null)
            {
                items.Add(new(current.DisplayName, current.TargetType.Name));
                current = current.Parent;
            }

            items.Reverse();
            return items;
        }
    }
}