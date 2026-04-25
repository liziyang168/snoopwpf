namespace Snoop.Infrastructure.Helpers;

using System;
using System.Diagnostics;
using System.IO;

public static class ExportHelper
{
    public static string GetUniqueExportFilePath(string name, string extension)
    {
        var targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SnoopExport");

        Directory.CreateDirectory(targetFolder);

        using var proc = Process.GetCurrentProcess();
        var exportDateTimeText = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        var prefix = $"{proc.ProcessName} - {name} ({exportDateTimeText})";
        var fullFilePath = Path.Combine(targetFolder, $"{prefix}.{extension}");

        // Append "(n)" suffix to avoid overwriting existing files.
        for (var i = 1; i < 10000; i++)
        {
            if (File.Exists(fullFilePath) == false)
            {
                break;
            }

            fullFilePath = Path.Combine(targetFolder, $"{prefix} ({i}).{extension}");
        }

        return fullFilePath;
    }
}