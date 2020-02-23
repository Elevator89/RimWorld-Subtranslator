using System.Xml.Linq;

namespace Elevator.Subtranslator.Common
{
    public static class XElementExtensions
    {
        public static XElement Element(this XElement element, XName name, bool ignoreFirstCap)
        {
            XName nameCap = name.LocalName.CapitalizeFirst();
            XName nameDecap = name.LocalName.DecapitalizeFirst();
            return element.Element(nameCap) ?? element.Element(nameDecap);
        }
    }
}
