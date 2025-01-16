using DocumentFormat.OpenXml;

namespace EcoSystemsAutomation.XMLHelper
{
    public interface IXMLDeserializer
    {
        // Method to read the XML file from multiple paths
         List<string> ReadXML(List<string> xmlPaths);

        // Method to deserialize an XML string into a provided object
        void DeserializeXML(string xmlString, object reportObj);
    }
}
