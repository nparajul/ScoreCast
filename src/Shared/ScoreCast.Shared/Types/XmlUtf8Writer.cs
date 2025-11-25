using System.Text;

namespace ScoreCast.Shared.Types;

public class XmlUtf8Writer : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}
