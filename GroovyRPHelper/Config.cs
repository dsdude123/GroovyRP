using System;
using System.Xml.Serialization;

namespace GroovyRP
{
    [Serializable()]
    [XmlRoot("Config")]
    public class Config
    {
        [XmlElement("IsFirstRun")]
        public bool IsFirstRun { get; set; }

        [XmlElement("RunWhenGrooveMusicOpens")]
        public bool RunWhenGrooveMusicOpens { get; set; }

        [XmlElement("HideInSystemTray")]
        public bool HideInSystemTray { get; set; }
    }
}
