using System;
using System.Xml.Serialization;

namespace GroovyRP
{
    [Serializable()]
    [XmlRoot("VersionInfo")]
    public class VersionInfo
    {
        [XmlElement("Version")]
        public string Version { get; set; }
    }
}