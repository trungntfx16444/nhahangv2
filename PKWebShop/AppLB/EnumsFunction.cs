using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace PKWebShop.AppLB
{
    public static class EnumFunction
    {
        public static string[] GetEnumDesc<T>(this T enumerationValue)
    where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(EnumDataAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((EnumDataAttribute)attrs[0]).getValues();
                }
            }
            return null;
        }

        public static List<T> GetList<T>()
    where T : struct
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }
        public static T ToEnum<T>(this string value)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch (Exception)
            {
                return default(T);
            }
            
        }
    }

    public class EnumDataAttribute : Attribute
    {
        private string[] v;
        public EnumDataAttribute(params string[] v)
        {
            this.v = v;
        }
        public string[] getValues()
        {
            return v;
        }
    }


}