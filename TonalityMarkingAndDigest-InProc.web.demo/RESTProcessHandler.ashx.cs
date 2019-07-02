using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

using log4net;
using Newtonsoft.Json;
using captcha;
using Lingvistics;
using Lingvistics.Client;
using TextMining.Core;
using Digest;
using OpinionMining;
using TonalityMarking;

namespace TonalityMarkingAndDigestInProc.web.demo
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RESTProcessHandler : IHttpHandler
    {
        /// <summary>
        /// 
        /// </summary>
        internal sealed class Result
        {
            public Result( Exception ex ) 
            {
                ErrorMessage = ex.ToString();
            }
            public Result( string html, TimeSpan elapsed )
            {
                Html    = html;
                Elapsed = elapsed;
            }

            [JsonProperty(PropertyName="error")]
            public string ErrorMessage
            {
                get;
                private set;
            }

            [JsonProperty(PropertyName="html")]
            public string Html
            {
                get;
                private set;
            }

            [JsonProperty(PropertyName="elapsed")]
            public TimeSpan Elapsed
            {
                get;
                private set;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        internal enum ProcessTypeEnum
        {
            TonalityMarking,
            Digest,
        }
        /// <summary>
        /// 
        /// </summary>
        private enum OutputTypeEnum
        {
            Xml,
            Xml_Custom,
            Html_FinalTonality,
            Html_FinalTonalityDividedSentence,
            Html_ToplevelTonality,
            Html_ToplevelTonalityDividedSentence,
            Html_BackcolorAllTonality,
        }
        /// <summary>
        /// 
        /// </summary>
        private struct LocalParams
        {
            public LocalParams( HttpContext context ) : this()
            {
                Context = context;
            }

            public HttpContext Context { get; private set; }
            public string Text { get; set; }
            public ProcessTypeEnum ProcessType { get; set; }
            public OutputTypeEnum OutputType { get; set; }
            public string InquiryText { get; set; }
            public ObjectAllocateMethod? ObjectAllocateMethod { get; set; }

            public bool IsTextDummy
            {
                get { return (Text == TextDummy); }
            }
            public static string TextDummy
            {
                get { return ("_dummy_"); }
            }
            public static LingvisticsTextInput CreateLingvisticsTextInput4TextDummy()
            {
                var ip = new LingvisticsTextInput()
			    {
				    Text                 = TextDummy,
				    AfterSpellChecking   = false,
				    BaseDate             = DateTime.Now,
				    Mode                 = SelectEntitiesMode.Full,
				    GenerateAllSubthemes = false,
                    Options              = LingvisticsResultOptions.All,
                };
                return (ip);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private static class LingvisticsWorkInProcessorHelper
        {
            private static readonly object _SyncLock = new object();

            private static LingvisticsWorkInProcessor _LingvisticsWorkInProcessor;

            public static bool IsCreated
            {
                get
                {
                    lock ( _SyncLock )
                    {
                        return (_LingvisticsWorkInProcessor != null);
                    }                    
                }
            }

            public static LingvisticsResult ProcessText( LingvisticsTextInput input )
            {
                lock ( _SyncLock )
                {
                    #region [.create.]
                    var p = _LingvisticsWorkInProcessor;
                    if ( p == null )
                    {
                        {
                            p = new LingvisticsWorkInProcessor( Config.USE_COREFERENCE_RESOLUTION,
                                                                Config.USE_GEONAMES_DICTIONARY,
                                                                Config.MAX_ENTITY_LENGTH );
                        }
                        {
                            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                            GC.WaitForPendingFinalizers();
                            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                        }

                        _LingvisticsWorkInProcessor = p;
                    }
                    #endregion

                    var result = _LingvisticsWorkInProcessor.ProcessText( input );
                    return (result);
                }                
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private static class Log
        {
            public static void Info( ref LocalParams lp )
            {
                try
                {
                    var message = string.Format( "IP: '{0}', TYPE: '{1}', TEXT: '{2}'", (lp.Context.Request.UserHostName ?? lp.Context.Request.UserHostAddress), lp.ProcessType, lp.Text );
                    LogManager.GetLogger( string.Empty ).Info( message );
                }
                catch
                {
                    ;
                }
            }
            public static void Error( ref LocalParams lp, Exception ex )
            {
                try
                {
                    var message = string.Format( "IP: '{0}', TYPE: '{1}', TEXT: '{2}'", (lp.Context.Request.UserHostName ?? lp.Context.Request.UserHostAddress), lp.ProcessType, lp.Text );
                    LogManager.GetLogger( string.Empty ).Error( message, ex );
                }
                catch
                {
                    ;
                }
            }

            public static void Info( string message )
            {
                LogManager.GetLogger( string.Empty ).Info( message );
            }
            public static void Error( string message, Exception ex )
            {
                LogManager.GetLogger( string.Empty ).Error( message, ex );
            }

            /*public static string Read( HttpServerUtility server, bool throwIfError )
            {
                try
                {
                    using ( var fs = new FileStream( server.MapPath( "~/(logs)/all.txt" ), FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                    using ( var sr = new StreamReader( fs ) )
                    {
                        return (sr.ReadToEnd());
                    }
                }
                catch ( Exception ex )
                {
                    Debug.WriteLine( ex );

                    if ( throwIfError )
                    {
                        throw (ex);
                    }
                }
                return (null);
            }*/
        }

        static RESTProcessHandler()
        {
            //IMPORTANT for TM-OM resources
            Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );

            #region [.config 'log4net'.]
            try
            {
                log4net.Config.XmlConfigurator.Configure();
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex );
            }
            #endregion

            #region [.force load 'LingvisticsWorkInProcess-with-OM-TM'.]
            ForceLoadModel();
            #endregion
        }

        private static readonly object _SyncLockForceLoadModel = new object();
        private static void ForceLoadModel()
        {
            if ( LingvisticsWorkInProcessorHelper.IsCreated )
            {
                return;
            }

            Task.Factory.StartNew( () =>
            {
                lock ( _SyncLockForceLoadModel )
                {
                    if ( LingvisticsWorkInProcessorHelper.IsCreated )
                    {
                        return;
                    }

                    try
                    {
                        //---Log.Info( "start 'ForceLoadModel'" );

                        var result = LingvisticsWorkInProcessorHelper.ProcessText( LocalParams.CreateLingvisticsTextInput4TextDummy() );
                        Console.WriteLine( ((result.IsNotNull() && result.RDF.IsNotNull()) ? result.RDF.Length : 0) );

                        //---Log.Info( "end 'ForceLoadModel', IsCreated: " + LingvisticsWorkInProcessorHelper.IsCreated );
                    }
                    catch ( Exception ex )
                    {
                        Log.Error( "error 'ForceLoadModel'", ex );
                    }
                }

            }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness ); 
        }

        public bool IsReusable
        {
            get { return (true); }
        }

        public void ProcessRequest( HttpContext context )
        {
            try
            {
                #region [.anti-bot.]
                var antiBot = context.ToAntiBot();
                if ( antiBot.IsNeedRedirectOnCaptchaIfRequestNotValid() )
                {
                    antiBot.SendGotoOnCaptchaJsonResponse();
                    return;
                }
                #endregion

                #region [.force load 'LingvisticsWorkInProcess-with-OM-TM'.]
                var forceLoadModel = context.Request[ "forceLoadModel" ].TryToBool( false );
                if ( forceLoadModel )
                {
                    ForceLoadModel();
                    context.Response.SendJson( "bull-shit-mother-fucka", TimeSpan.Zero );
                    return;
                }
                #endregion

                #region [.log.]
                #region [.view.]
                var viewLog = context.Request[ "viewLog" ].TryToBool( false );
                if ( viewLog )
                {
                    //context.Response.SendText( Log.Read( context.Server, true ) );
                    context.Response.SendTextFile( context.Server.MapPath( "~/(logs)/all.txt" ) );
                    return;
                }
                #endregion

                #region [.delete.]
                var deleteLog = context.Request[ "deleteLog" ].TryToBool( false );
                if ( deleteLog )
                {
                    Directory.Delete( context.Server.MapPath( "~/(logs)" ), true );
                    context.Response.SendJson( "bull-shit-mother-fucka", TimeSpan.Zero );
                    return;
                }
                #endregion
                #endregion

                var lp = new LocalParams( context )
                {
                    Text                 = context.GetRequestStringParam( "text", Config.MAX_INPUTTEXT_LENGTH ),
                    ProcessType          = context.Request[ "processType" ].TryConvert2Enum< ProcessTypeEnum >().GetValueOrDefault( ProcessTypeEnum.TonalityMarking ),
                    OutputType           = (context.Request[ "splitBySentences" ].TryToBool( false ) ? OutputTypeEnum.Html_FinalTonalityDividedSentence : OutputTypeEnum.Html_FinalTonality),
                    InquiryText          = context.GetRequestStringParam( "inquiryText", Config.MAX_INPUTTEXT_LENGTH ),
                    ObjectAllocateMethod = context.Request[ "objectAllocateMethod" ].TryToEnum< ObjectAllocateMethod >(),
                };

                #region [.anti-bot.]
                antiBot.MarkRequestEx( lp.Text );
                #endregion

                var sw = Stopwatch.StartNew();
                var html = GetResultHtml( lp );
                sw.Stop();

                context.Response.SendJson( html, sw.Elapsed );
            }
            catch ( Exception ex )
            {
                context.Response.SendJson( ex );
            }
        }

        private static string GetResultHtml( LocalParams lp )
        {
            var lingvisticsInput = new LingvisticsTextInput()
			{
				Text                 = lp.Text,
				AfterSpellChecking   = false,
				BaseDate             = DateTime.Now,
				Mode                 = SelectEntitiesMode.Full,
				GenerateAllSubthemes = false, 
			};

            if ( lingvisticsInput.Text.IsNullOrWhiteSpace() )
            {
                return ("<div style='text-align: center; border: 1px silver solid; border-radius: 2px; background: lightgray; color: darkgray; padding: 15px;'>[INPUT TEXT IS EMPTY]</div>");
            }

            try
            {
                var html = default(string);

                #region [.result.]
                switch ( lp.ProcessType )
                {
                    case ProcessTypeEnum.Digest:
                    #region [.code.]
                    {
                        lingvisticsInput.Options = LingvisticsResultOptions.OpinionMiningWithTonality;

                        var lingvisticResult = LingvisticsWorkInProcessorHelper.ProcessText( lingvisticsInput );

                        html = ConvertToHtml( lp.Context, lingvisticResult.OpinionMiningWithTonalityResult );
                    }
                    #endregion
                    break;                    

                    case ProcessTypeEnum.TonalityMarking:
                    #region [.code.]
                    {
                        lingvisticsInput.TonalityMarkingInput = new TonalityMarkingInputParams4InProcess();
                        if ( !lp.InquiryText.IsNullOrWhiteSpace() )
                        {
                            lingvisticsInput.TonalityMarkingInput.InquiriesSynonyms = lp.InquiryText.ToTextList();
                        }
                        lingvisticsInput.ObjectAllocateMethod = lp.ObjectAllocateMethod.GetValueOrDefault( ObjectAllocateMethod.FirstVerbEntityWithRoleObj );
                        lingvisticsInput.Options = LingvisticsResultOptions.Tonality;                    

                        var lingvisticResult = LingvisticsWorkInProcessorHelper.ProcessText( lingvisticsInput );

                        html = ConvertToHtml( lp.Context, lingvisticResult.TonalityResult, lp.OutputType );
                    }
                    #endregion
                    break;

                    default:
                        throw (new ArgumentException( lp.ProcessType.ToString() ));
                }
                #endregion

                if ( !lp.IsTextDummy )
                {
                    Log.Info( ref lp );
                }

                return (html);
            }
            catch ( Exception ex )
            {
                Log.Error( ref lp, ex );
                throw;
            }
        }

        private static string ConvertToHtml( HttpContext context, TonalityMarkingOutputResult result, OutputTypeEnum outputType )
        {
            var xdoc = new XmlDocument(); 
            xdoc.LoadXml( result.OutputXml );

            var xslt = new XslCompiledTransform( false );

            var xsltFilename = default(string);
            switch ( outputType )
            {
                case OutputTypeEnum.Xml_Custom:
                    xsltFilename = "Xml.xslt";
                    break;
                case OutputTypeEnum.Html_ToplevelTonality:
                    xsltFilename = "ToplevelTonality.xslt"; 
                    break;
                case OutputTypeEnum.Html_ToplevelTonalityDividedSentence:
                    xsltFilename = "ToplevelTonalityDividedSentence.xslt";
                    break;
                case OutputTypeEnum.Html_FinalTonality:
                    xsltFilename = "FinalTonality.xslt"; 
                    break;
                case OutputTypeEnum.Html_FinalTonalityDividedSentence:
                    xsltFilename = "FinalTonalityDividedSentence.xslt";
                    break;
                case OutputTypeEnum.Html_BackcolorAllTonality:
                    xsltFilename = "BackcolorAllTonality.xslt"; 
                    break;
                default:
                    throw (new ArgumentException(outputType.ToString()));
            }

            xslt.Load( context.Server.MapPath( "~/App_Data/" + xsltFilename ) );

            var sb = new StringBuilder();
            using ( var sw = new StringWriter( sb ) )
            {
                xslt.Transform( xdoc.CreateNavigator(), null, sw );
            }
            return (sb.ToString());
        }

        private static string ConvertToHtml( HttpContext context, DigestOutputResult result )
        {
            const string ANYTHING_ISNT_PRESENT = "<span style='color: maroon; font-size: x-small;'>[Ничего нет.]</span>";
            const string TABLE_START           = "<table border='1' style='font-size: x-small;'><tr><th>#</th><th>SUBJECT</th><th>OBJECT</th><th>SENTENCE</th></tr>";
            const string TABLE_END             = "</table>";
            const string TABLE_ROW_FORMAT      = "<tr valign='top'><td>{0}</td><td>&nbsp;{1}</td><td>&nbsp;{2}</td><td style='padding: 3px;'>{3}</td></tr>";

            if ( !result.Tuples.Any() )
            {
                return (ANYTHING_ISNT_PRESENT);
            }                

            const string XSLT_FILENAME = "FinalTonality.Digest.test.xslt";

            var xslt = new XslCompiledTransform( false );
            xslt.Load( context.Server.MapPath( "~/App_Data/" + XSLT_FILENAME ) );

            var tmp = new StringBuilder();
            var sb = new StringBuilder( TABLE_START );
            var number = 0;
            foreach ( var tuple in result.Tuples )
            {
                sb.AppendFormat
                (
                TABLE_ROW_FORMAT,
                (++number),
                tuple.Subject.ToHtml(),
                tuple.Object .ToHtml(),
                tuple.GetSentence().Transform( xslt, tmp )
                );
            }
            sb.Append( TABLE_END );

            return (sb.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }

        public static List< string > ToTextList( this string text )
        {
            return (text.Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList());
        }


        public static string ToHtml( this SubjectEssence subject )
        {
            return (subject.IsAuthor ? "<span style='color: silver;'>{0}</span>" : "{0}").FormatEx( subject.ToString() );
        }
        public static string ToHtml( this ObjectEssence @object )
        {
            return ((@object != null) ? (@object + ((@object.IsSubjectIndeed) ? "<span style='color: silver;'>&nbsp;(субъект-как-объект)</span>" : string.Empty)) : "-");
        }

        public static string Transform( this XElement xe, XslCompiledTransform xslt, StringBuilder sb )
        {
            if ( sb == null ) sb = new StringBuilder();
            sb.Clear();

            using ( var sw = new StringWriter( sb ) )
            {
                xslt.Transform( xe.CreateReader(), null, sw );
            }
            return (sb.ToString());
        }


        public static void SendJson( this HttpResponse response, string html, TimeSpan elapsed )
        {
            response.SendJson( new RESTProcessHandler.Result( html, elapsed ) );
        }
        public static void SendJson( this HttpResponse response, Exception ex )
        {
            response.SendJson( new RESTProcessHandler.Result( ex ) );
        }
        public static void SendJson( this HttpResponse response, RESTProcessHandler.Result result )
        {
            response.ContentType = "application/json"; //"text/html"
            //---response.Headers.Add( "Access-Control-Allow-Origin", "*" );

            var json = JsonConvert.SerializeObject( result );
            response.Write( json );
        }
        /*public static void SendHtml( this HttpResponse response, string html )
        {
            response.ContentType = "text/html; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.Write( html );
        }
        public static void SendText( this HttpResponse response, string text )
        {
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.Write( text );
        }*/
        public static void SendTextFile( this HttpResponse response, string filename )
        {
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.WriteFile( filename );
        }
    }
}