namespace Snoop.Core.Tests.Infrastructure.Export;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using NUnit.Framework;
using Snoop.Data.Tree;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Diagnostics;
using Snoop.Infrastructure.Diagnostics.Providers;
using VerifyNUnit;

[TestFixture]
public class DiagnosticsExporterTests
{
    [Test]
    public Task TestExport()
    {
        var textWriter = new StringWriter();

        IEnumerable<DiagnosticItem> diagnosticItems = new[] { new DiagnosticItem(new FreezeFreezablesDiagnosticProvider()) { TreeItem = GetTestTreeItem() } };
        DiagnosticsExporter.Export(diagnosticItems, textWriter);

        var result = textWriter.ToString();

        return Verifier.Verify(result);
    }

    private static TreeItem GetTestTreeItem()
    {
        var target = new StackPanel();
        target.Children.Add(new TextBlock { Text = "test" });
        target.Children.Add(new Border { Child = new CheckBox { Content = "check" } });

        using var treeService = TreeService.From(TreeType.Visual);
        return treeService.Construct(target, null);
    }
}