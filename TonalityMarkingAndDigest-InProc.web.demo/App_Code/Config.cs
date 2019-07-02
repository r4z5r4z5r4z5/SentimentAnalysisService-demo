using System.Configuration;

namespace TonalityMarkingAndDigestInProc.web.demo
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
        //public static readonly int  CONCURRENT_FACTORY_INSTANCE_COUNT = ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ].ToInt32();
        public static readonly int  MAX_ENTITY_LENGTH          = ConfigurationManager.AppSettings[ "MAX_ENTITY_LENGTH"          ].ToInt32();
        public static readonly bool USE_GEONAMES_DICTIONARY    = ConfigurationManager.AppSettings[ "USE_GEONAMES_DICTIONARY"    ].ToBool();
        public static readonly bool USE_COREFERENCE_RESOLUTION = ConfigurationManager.AppSettings[ "USE_COREFERENCE_RESOLUTION" ].ToBool();

        public static readonly int MAX_INPUTTEXT_LENGTH                = ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH"                ].ToInt32();
        public static readonly int SAME_IP_INTERVAL_REQUEST_IN_SECONDS = ConfigurationManager.AppSettings[ "SAME_IP_INTERVAL_REQUEST_IN_SECONDS" ].ToInt32();
        public static readonly int SAME_IP_MAX_REQUEST_IN_INTERVAL     = ConfigurationManager.AppSettings[ "SAME_IP_MAX_REQUEST_IN_INTERVAL"     ].ToInt32();        
        public static readonly int SAME_IP_BANNED_INTERVAL_IN_SECONDS  = ConfigurationManager.AppSettings[ "SAME_IP_BANNED_INTERVAL_IN_SECONDS"  ].ToInt32();
    }
}