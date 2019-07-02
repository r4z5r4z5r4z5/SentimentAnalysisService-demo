using System;

namespace TonalityMarkingAndDigestInProc.web.demo
{
    /// <summary>
    /// 
    /// </summary>
    internal static partial class Extensions
    {
        public static bool ToBool( this string value )
        {
            return (bool.Parse( value ));
        }
        public static bool TryToBool( this string value, bool defaultValue )
        {
            bool result;
            return (bool.TryParse( value, out result ) ? result : defaultValue);
        }
        public static bool? TryToBool( this string value )
        {
            bool result;
            return (bool.TryParse( value, out result ) ? result : ((bool?) null));
        }

        public static T ToEnum< T >( this string value ) where T : struct
        {
            var result = (T) Enum.Parse( typeof(T), value, true );
            return (result);
        }
        public static T? TryToEnum< T >( this string value ) where T : struct
        {
            T t;
            return (Enum.TryParse( value, true, out t ) ? t : ((T?) null));
        }
        public static int ToInt32( this string value )
        {
            return (int.Parse( value ));
        }
        
        public static bool IsNullOrWhiteSpace( this string value )
        {
            return (string.IsNullOrWhiteSpace( value ));
        } 
    }
}