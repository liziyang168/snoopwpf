namespace Snoop;

using System.Text;
using System.Xml;

public static class XmlWriterExtensions
{
    public static void WriteAttributeStringEx(this XmlWriter writer, string localName, string? value)
    {
        value = string.IsNullOrEmpty(value)
            ? value
            : Escape(value!);

        writer.WriteAttributeString(localName, value);
    }

    private static string Escape(string value)
    {
        var escapedValue = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            if (c < 32)
            {
                escapedValue.Append($"\\x{((byte)c).ToString("x2")}");
            }
            else
            {
                switch (c)
                {
                    case '\r':
                        escapedValue.Append("\\r");
                        break;
                    case '\n':
                        escapedValue.Append("\\n");
                        break;
                    case '\t':
                        escapedValue.Append("\\t");
                        break;
                    case '\0':
                        escapedValue.Append("\\0");
                        break;
                    default:
                        escapedValue.Append(c);
                        break;
                }
            }
        }

        return escapedValue.ToString();
    }
}