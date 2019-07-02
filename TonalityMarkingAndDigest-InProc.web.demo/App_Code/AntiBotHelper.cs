using System.Web;

using TonalityMarkingAndDigestInProc.web.demo;

namespace captcha
{
    /// <summary>
    /// 
    /// </summary>
    internal static class AntiBotHelper
    {
        private const string CAPTCHA_PAGE_URL      = "~/Captcha.aspx";
        private const string LOAD_MODEL_DUMMY_TEXT = "_dummy_";

        public static AntiBot ToAntiBot( this HttpContext httpContext )
        {
            var config = new AntiBotConfig() 
            { 
                HttpContext                    = httpContext, 
                CaptchaPageUrl                 = CAPTCHA_PAGE_URL,
                SameIpBannedIntervalInSeconds  = Config.SAME_IP_BANNED_INTERVAL_IN_SECONDS,
                SameIpIntervalRequestInSeconds = Config.SAME_IP_INTERVAL_REQUEST_IN_SECONDS,
                SameIpMaxRequestInInterval     = Config.SAME_IP_MAX_REQUEST_IN_INTERVAL,
            };
            var antiBot = new AntiBot( config );
            return (antiBot);
        }

        public static void MarkRequestEx( this AntiBot antiBot, string text )
        {
            if ( text != LOAD_MODEL_DUMMY_TEXT )
            {
                antiBot.MarkRequest();
            }
        }
    }
}