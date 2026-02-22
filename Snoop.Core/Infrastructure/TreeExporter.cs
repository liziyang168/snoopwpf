namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Snoop.Data.Tree;
using Snoop.Infrastructure.Helpers;

public static class TreeExporter
{
    public static void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, bool recurse = true)
    {
        new XMLTreeExporter().Export(treeItem, textWriter, filter, recurse);
    }

    public static void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, ExportOptions options)
    {
        new XMLTreeExporter().Export(treeItem, textWriter, filter, options);
    }
}

/* Without disabling these two warnings the code becomes very odd when trying to set the default value.
    public bool UseFilter
{
        get; set => this.Set(ref field, value);
    } = true;

Becomes:
    public bool UseFilter
    {
        get; set => this.Set(ref field, value);
    }

    = true;
*/
#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1500 // Braces for multi-line statements should not share line

public class ExportOptions : BaseNotifyObject
{
    public bool UseFilter
    {
        get;
        set => this.Set(ref field, value);
    }

    public TreeItem? TreeItem
    {
        get;
        set => this.Set(ref field, value);
    }

    public bool Recurse
    {
        get;
        set => this.Set(ref field, value);
    }

    public bool ExportXamlStyle
    {
        get;
        set => this.Set(ref field, value);
    } = true;

    public bool IncludeDefaultEmptyValues
    {
        get;
        set => this.Set(ref field, value);
    } = false;

    public bool IncludeTypenameOnlyValues
    {
        get;
        set => this.Set(ref field, value);
    } = false;

    public bool IncludeSystemCollectionNamespaceValues
    {
        get;
        set => this.Set(ref field, value);
    } = false;

    public bool RoundDecimals
    {
        get;
        set => this.Set(ref field, value);
    } = true;
}
#pragma warning restore SA1513 // Closing brace should be followed by blank line
#pragma warning restore SA1500 // Braces for multi-line statements should not share line

public class XMLTreeExporter
{
    private static readonly Dictionary<Type, object?> defaultValueCache = new();

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid);
    }

    public void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, bool recurse = true)
    {
        var options = new ExportOptions { Recurse = recurse, ExportXamlStyle = true, RoundDecimals = true, IncludeDefaultEmptyValues = false };
        this.Export(treeItem, textWriter, filter, options);
    }

    public void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, ExportOptions options)
    {
        var writerSettings = new XmlWriterSettings { Encoding = textWriter.Encoding, Indent = true, NewLineOnAttributes = false };

        using var xmlWriter = XmlWriter.Create(textWriter, writerSettings);
        xmlWriter.WriteStartDocument(true);
        this.ExportItem(treeItem, xmlWriter, filter, options);
        xmlWriter.WriteEndDocument();
    }

    private void ExportItem(TreeItem treeItem, XmlWriter xmlWriter, PropertyFilter? filter, ExportOptions options)
    {
        if (options.ExportXamlStyle is false)
        {
            xmlWriter.WriteStartElement("node");
            xmlWriter.WriteAttributeString("name", treeItem.Name);
            xmlWriter.WriteAttributeString("displayName", treeItem.DisplayName);
            xmlWriter.WriteAttributeString("targetType", treeItem.TargetType.FullName!);
        }
        else
        {
            var elementName = GetValidXmlName(treeItem.TargetType.Name);
            xmlWriter.WriteStartElement(elementName);
        }

        var propertyInformations = PropertyInformation.GetProperties(treeItem.Target);

        if (propertyInformations.Any() is false)
        {
            xmlWriter.WriteEndElement();
            return;
        }

        if (options.ExportXamlStyle is false)
        {
            xmlWriter.WriteStartElement("properties");
        }

        foreach (var propertyInformation in propertyInformations)
        {
            if (filter is not null
                && filter.ShouldShow(propertyInformation) == false)
            {
                continue;
            }

            var rawVal = propertyInformation.Value;
            if (options.IncludeDefaultEmptyValues is false)
            {
                var skipValue = false;
                skipValue |= rawVal is null;
                skipValue |= rawVal is string strVal && string.IsNullOrEmpty(strVal);

                if (skipValue is false
                    && IsSimpleType(propertyInformation.PropertyType.Type))
                {
                    if (defaultValueCache.TryGetValue(propertyInformation.PropertyType.Type, out var defaultValue) is false)
                    {
                        defaultValueCache[propertyInformation.PropertyType.Type] = defaultValue = Activator.CreateInstance(propertyInformation.PropertyType.Type);
                    }

                    // rawVal can't be null if skipValue is true
                    if (rawVal!.Equals(defaultValue))
                    {
                        skipValue = true;
                    }
                }

                if (skipValue is false
                    && options.IncludeSystemCollectionNamespaceValues is false)
                {
                    var typeNamespace = propertyInformation.PropertyType.Type.Namespace ?? string.Empty;
                    if (typeNamespace.StartsWith("System.Collections", StringComparison.Ordinal))
                    {
                        skipValue = true;
                    }
                }

                if (skipValue)
                {
                    propertyInformation.Teardown();
                    continue;
                }
            }

            rawVal ??= "null";

            string? value;
            if (options.RoundDecimals)
            {
                value = rawVal switch
                {
                    double d => d.ToString("0.#"),
                    float f => f.ToString("0.#"),
                    decimal m => m.ToString("0.#"),
                    Size sz => $"{sz.Width:0.#}x{sz.Height:0.#}",
                    Point pt => $"({pt.X:0.#},{pt.Y:0.#})",
                    Thickness thick => thick.Left.AreClose(thick.Right) && thick.Right.AreClose(thick.Top) && thick.Top.AreClose(thick.Bottom)
                        ? thick.Left.ToString("0.#") // all values were the same
                        : $"{thick.Left:0.#},{thick.Top:0.#},{thick.Right:0.#},{thick.Bottom:0.#}",
                    Geometry geo => $"{geo.GetOutlinedPathGeometry().Figures?.Count} Geo Figures",
                    var _ => rawVal.ToString()
                };
            }
            else
            {
                value = rawVal.ToString();
            }

            if (options.IncludeTypenameOnlyValues is false)
            {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                if (value?.Contains('.') is true
                    && value == propertyInformation.PropertyType.Type.FullName)
                {
                    propertyInformation.Teardown();
                    continue;
                }
#pragma warning restore CA1307 // Specify StringComparison for clarity
            }

            if (options.ExportXamlStyle is false)
            {
                xmlWriter.WriteStartElement("property");
                xmlWriter.WriteAttributeString("displayName", propertyInformation.DisplayName);
                xmlWriter.WriteAttributeString("value", value);
                xmlWriter.WriteEndElement();
            }
            else
            {
                var attributeName = GetValidXmlName(propertyInformation.DisplayName);
                xmlWriter.WriteAttributeString(attributeName, value);
            }

            propertyInformation.Teardown();
        }

        if (options.ExportXamlStyle is false)
        {
            xmlWriter.WriteEndElement();
        }

        if (options.Recurse)
        {
            foreach (var treeItemChild in treeItem.Children)
            {
                this.ExportItem(treeItemChild, xmlWriter, filter, options);
            }
        }

        xmlWriter.WriteEndElement();
    }

    private static string GetValidXmlName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Element";
        }

        return XmlConvert.EncodeLocalName(name)!; // can't be null as name can't be null
    }
}