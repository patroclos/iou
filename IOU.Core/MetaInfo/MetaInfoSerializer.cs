using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace IOU
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MetaInfoPropertyAttribute : Attribute
    {
        public string? Name;
        public MetaInfoPropertyAttribute(string? name)
        {
            Name = name;
        }
    }
    
    // 5:asdfx -> BStr
    // 5:asdfx -> string
    public static class MetaInfoSerializer
    {
        public static BEnc? Serialize(object content)
        {
            switch (content)
            {
                case BEnc enc:
                    return enc;
                case string s:
                    return new BStr(s);
                case int i:
                    return new BInt(i);
                case long i:
                    return new BInt(i);
                case IDictionary d:
                    return new BDict(d.Keys.Cast<object>()
                        .Zip(d.Values.Cast<object>(), (k, v) => new KeyValuePair<BStr, BEnc>(Serialize(k) as BStr, Serialize(v)))
                        .ToImmutableList()
                    );
                case IEnumerable e:
                    return new BLst(e.Cast<object>().Select(Serialize));
                default:
                {
                    var props = content.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    // .Where(p => p. p.GetCustomAttribute(typeof(MetaInfoPropertyAttribute)) != null);
                    
                    var dict = new Dictionary<string, object>();

                    foreach (var p in props)
                    {
                        var attr = p.GetCustomAttribute(typeof(MetaInfoPropertyAttribute)) as MetaInfoPropertyAttribute;
                        var val = p.GetValue(content);
                        if (val == null || attr == null)
                            continue;
                        var enc = Serialize(val);
                        if (enc == null)
                            continue;
                        dict.Add(attr.Name ?? p.Name, enc);
                            
                    }

                    return Serialize(dict);
                }
            }
        }

        public static T Deserialize<T>(BEnc encoded) => (T)Deserialize(encoded, typeof(T));

        public static object Deserialize(BEnc encoded, Type type)
        {
            if (type.IsInstanceOfType(encoded))
                return encoded;
            
            switch (encoded)
            {
                case BStr str:
                {
                    if (type == typeof(string))
                        return str.Utf8String;
                    if (type == typeof(byte[]))
                        return str.Value.ToArray();
                    if (type == typeof(ReadOnlyMemory<byte[]>))
                        return str.Value;
                    throw new InvalidOperationException($"Can't deserialize a {encoded.Type} to {type}");
                }
                case BInt bint:
                    if (type == typeof(int))
                        return bint.IntValue;
                    
                    if (type == typeof(uint))
                        return (uint)bint.Value;
                    
                    if(type == typeof(long))
                        return bint.Value;
                    
                    if (type == typeof(ulong))
                        return (ulong) bint.Value;
                    throw new InvalidOperationException($"Can't deserialize a {encoded.Type} to {type}");
                case BLst lst:
                    if (type.IsArray)
                    {
                        var items = lst.Value.Select(v => Deserialize(v, type.GetElementType())).ToArray();
                        var arr = Array.CreateInstance(type.GetElementType(), items.Length);
                        for (var i = 0; i < items.Length; i++)
                            arr.SetValue(items[i], i);
                        return arr;
                    }

                    if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var inst = Activator.CreateInstance(type) as IList;
                        foreach (var v in lst.Value.Select(v => Deserialize(v, type.GenericTypeArguments[0])))
                            inst.Add(v);
                        return inst;
                    }

                    throw new InvalidOperationException($"Can't deserialize a {encoded.Type} to {type}");
                case BDict dict:
                {
                    var dictType = type.GetInterfaces().FirstOrDefault(iface =>
                        iface.IsConstructedGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                    if (dictType != null)
                    {
                        var tk = dictType.GenericTypeArguments[0];
                        var tv = dictType.GenericTypeArguments[0];
                        var inst = Activator.CreateInstance(type) as IDictionary;
                        foreach(var kv in dict.Value)
                            inst.Add(Deserialize(kv.Key, tk), Deserialize(kv.Value, tv));

                        return inst;
                    }
                    else
                    {

                        var inst = Activator.CreateInstance(type);

                        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(p => p.CanWrite)
                            .ToArray();

                        foreach (var p in props)
                        {
                            var attr =
                                p.GetCustomAttribute(typeof(MetaInfoPropertyAttribute)) as MetaInfoPropertyAttribute;
                            if (attr == null)
                                continue;

                            var name = attr.Name ?? p.Name;
                            var val = dict[name];
                            if (val != null)
                                p.SetValue(inst, Deserialize(val, p.PropertyType));
                        }

                        return inst;
                    }

                    throw new InvalidOperationException($"Can't deserialize a {encoded.Type} to {type}");
                }
                default:
                    throw new InvalidOperationException($"Can't deserialize a {encoded.Type} to {type}");
            }
        }
    }
}