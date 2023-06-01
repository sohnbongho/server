using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Helper
{
    public static class ConvertHelper
    {
        /// <summary>
        /// 클래스를 Dictionary로 변환해 준다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Dictionary<string, object> ConvertToDictionary<T>(T value) where T : class
        {
            var dict = new Dictionary<string, object>();

            foreach (var property in value.GetType().GetProperties())
            {
                if (property.CanRead)
                {
                    dict.Add(property.Name, property.GetValue(value, null));
                }
            }
            return dict;
        }

        /// <summary>
        /// Redis에 넣기 위해 변환
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>        
        public static RedisValue ConvertObjectToRedisValue(object value)
        {
            RedisValue redisValue;
            if (value is int intNum)
            {
                redisValue = intNum;
                return redisValue;
            }
            else if (value is long longNum)
            {
                redisValue = longNum;
                return redisValue;
            }
            else if (value is ulong ulongNum)
            {
                redisValue = ulongNum;
                return redisValue;
            }
            else if (value is double doubleNum)
            {
                redisValue = doubleNum;
                return redisValue;
            }
            else if (value is bool boolNum)
            {
                redisValue = boolNum;
                return redisValue;
            }

            // 위에 값들이 아니면 string이다
            redisValue = value.ToString();
            return redisValue;
        }

        public static object ConvertRedisValueToObject(RedisValue value)
        {
            if (value.IsNull)
            {
                return string.Empty;
            }

            var valueString = value.ToString();
            if (value.IsInteger)
            {                
                if (value.TryParse(out int intNum))
                {
                    return intNum;
                }
                else if (value.TryParse(out long longNum))
                {
                    return longNum;
                }
                else if (value.TryParse(out double doubleNum))
                {
                    return doubleNum;
                }
                else if (bool.TryParse(valueString, out bool boolValue))
                {
                    return boolValue;
                }
            }                        
            return valueString;
        }
    }
}
