using System.Collections.Generic;
using UnityEngine;

namespace Plugins.Framework_WWJ.Utils
{
    public static class DictionaryUtil
    {
        public static bool SafeContainsKey<IKey, IValue>(this Dictionary<IKey, IValue> dictionary, IKey key)
        {
            if (key == null) return false;
            return dictionary.ContainsKey(key);
        }
        
        public static void SafeAddRange<IKey, IValue>(this Dictionary<IKey, IValue> dictionary, Dictionary<IKey, IValue> addDic)
        {
            if (addDic == null || addDic.IsEmpty())
            {
                return;
            }

            foreach (var item in addDic)
            {
                if(dictionary.ContainsKey(item.Key)) continue;
                dictionary.Add(item.Key,item.Value);
            }
        }
        
        public static void SafeAdd<IKey, IValue>(this Dictionary<IKey, IValue> dictionary, IKey key, IValue value)
        {
            if (dictionary == null || key == null || value == null)
            {
                return;
            }

            if (!dictionary.TryAdd(key, value))
            {
                return;
            }
        }

        public static void SafeReset<IKey,IValue>(ref Dictionary<IKey,IValue> dictionary)
        {
            if (dictionary != null)
            {
                dictionary.Clear();
            }
            else
            {
                dictionary = new Dictionary<IKey, IValue>();
            }
        }
        
        public static bool IsEmpty<IKey,IValue>(this Dictionary<IKey,IValue> dictionary)
        {
            if (dictionary == null)
            {
                return true;
            }
            return dictionary.Count == 0;
        }
        
        public static int SafeCount<IKey,IValue>(this Dictionary<IKey,IValue> dictionary)
        {
            if (dictionary == null)
            {
                return 0;
            }
            return dictionary.Count;
        }
        
        public static IValue SafeGet<IKey,IValue>(this Dictionary<IKey,IValue> dictionary,IKey key)
        {
            if (dictionary == null || key == null)
            {
                return default;
            }
            if (dictionary.TryGetValue(key, out var get))
            {
                return get;
            }
            return default;
        }

        public static List<IKey> GetKeyList<IKey, IValue>(this Dictionary<IKey,IValue> dictionary)
        {
            List<IKey> keys = new List<IKey>();
            if (dictionary == null)
            {
                return keys;
            }
            foreach (var item in dictionary)
            {
                keys.Add(item.Key);
            }
            return keys;
        }
        
        public static List<IValue> GetValueList<IKey, IValue>(this Dictionary<IKey,IValue> dictionary)
        {
            List<IValue> values = new List<IValue>();
            if (dictionary == null)
            {
                return values;
            }
            foreach (var item in dictionary)
            {
                values.Add(item.Value);
            }
            return values;
        }
    }
}