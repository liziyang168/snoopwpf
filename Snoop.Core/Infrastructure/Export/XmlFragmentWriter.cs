namespace Snoop.Infrastructure;

using System.IO;
using System.Text;
using System.Xml;

public class XmlFragmentWriter : XmlTextWriter

{
    private bool skip;

    public XmlFragmentWriter(TextWriter w)
        : base(w)
    {
    }

    public XmlFragmentWriter(Stream w, Encoding encoding)
        : base(w, encoding)
    {
    }

    public XmlFragmentWriter(string filename, Encoding encoding)
        : base(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None), encoding)
    {
    }

    public override void WriteStartAttribute(string? prefix, string localName, string? ns)
    {
        if (prefix == "xmlns"
            && localName is "xsd" or "xsi")
        {
            this.skip = true;

            return;
        }

        base.WriteStartAttribute(prefix, localName, ns);
    }

    public override void WriteString(string? text)
    {
        if (this.skip)
        {
            return;
        }

        base.WriteString(text);
    }

    public override void WriteEndAttribute()
    {
        if (this.skip)
        {
            // Reset the flag, so we keep writing.
            this.skip = false;

            return;
        }

        base.WriteEndAttribute();
    }

    public override void WriteStartDocument()
    {
        this.WriteStartDocument(true);
    }
}