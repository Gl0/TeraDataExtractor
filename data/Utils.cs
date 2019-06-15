using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Alkahest.Core.Collections;
using Alkahest.Core.Data;

namespace TeraDataExtractor
{
    class FunctorEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equalityComparer;
        private readonly Func<T, int> _getHashCode;

        public FunctorEqualityComparer(Func<T, T, bool> equalityComparer, Func<T, int> getHashCode)
        {
            _equalityComparer = equalityComparer;
            _getHashCode = getHashCode;
        }

        public FunctorEqualityComparer(Func<T, T, bool> equalityComparer): this(equalityComparer, null) {}

        public bool Equals(T x, T y)
        {
            return _equalityComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _getHashCode != null ? _getHashCode(obj) : obj.GetHashCode();
        }
    }
    class FunctorComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _IComparer;
        public FunctorComparer(Func<T, T, int> icomparer)
        {
            _IComparer = icomparer;
        }
        public int Compare(T x,T y)
        {
            return _IComparer(x, y);
        }
        
    }

    static class Extension
    {
          public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items,
            Func<T, T, bool> equals, Func<T, int> hashCode)
        {
            return items.Distinct(new FunctorEqualityComparer<T>(equals, hashCode));
        }
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items,
            Func<T, T, bool> equals)
        {
            return items.Distinct(new FunctorEqualityComparer<T>(equals, null));
        }
        public static IEnumerable<T> Union<T>(this IEnumerable<T> items, IEnumerable<T> newitems,
            Func<T, T, bool> equals, Func<T, int> hashCode)
        {
            return items.Union(newitems,new FunctorEqualityComparer<T>(equals, hashCode));
        }
        public static IEnumerable<T> Union<T>(this IEnumerable<T> items, IEnumerable<T> newitems,
            Func<T, T, bool> equals)
        {
            return items.Union(newitems,new FunctorEqualityComparer<T>(equals, null));
        }
        public static void Sort<T>(this List<T> items, Func<T, T, int> comparer)
        {
            items.Sort(new FunctorComparer<T>(comparer));
        }
        public static bool RemoveFromEnd(this string s, string suffix,out string left)
        {
            if (s.EndsWith(suffix))
            {
                left = s.Substring(0, s.Length - suffix.Length);
                return true;
            }
            {
                left = s;
                return false;
            }
        }
        public static bool Remove(this string s, string mod, out string left)
        {
            if (s.Contains(mod))
            {
                left = s.Replace(mod,"");
                return true;
            }
            {
                left = s;
                return false;
            }
        }

        public static string Cap(this string x)
        {
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x.ToLower());
        }

        public static DataCenterElement FirstChild(this DataCenterElement element, string name) => element.Children(name).FirstOrDefault();
        public static DataCenterElement Child(this DataCenterElement element, string name) => element.Children(name).SingleOrDefault();

        public static void WriteElement(this DataCenterElement element, XmlWriter writer)
        {
            writer.WriteStartElement(element.Name);

            foreach (var (name, value) in element.Attributes.Tuples())
                writer.WriteAttributeString(name, value.ToString());

            foreach (var elem in element.Children())
                WriteElement(elem, writer);

            writer.WriteEndElement();
        }
    }
}