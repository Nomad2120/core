using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OSI.Core.Comparer
{
    /// <summary>
    /// применение класса
    /// </summary>
    /// var stringCollection = new List<string>();

    //stringCollection.Add("9");
    //stringCollection.Add("37");
    //stringCollection.Add("38");
    //stringCollection.Add("38А");
    //stringCollection.Add("39");
    //stringCollection.Add("3А");
    //stringCollection.Add("6");
    //stringCollection.Add("7");
    //stringCollection.Add("8");

    //var sortedCollection = stringCollection
    //            .OrderBy(x => x, NaturalComparer.Instance)
    //            .ToList();
    //foreach (var item in sortedCollection)
    //{
    //    Console.WriteLine(item);
    //}

    //Console.ReadLine();

    public class NaturalComparer : IComparer<string>
    {
        private StringComparer innerComparer = StringComparer.OrdinalIgnoreCase;

        public static NaturalComparer Instance { get; } = new();

        public int Compare(string x, string y)
        {
            if (x == y)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            var xParts = GetParts(x);
            var yParts = GetParts(y);
            for (int i = 0; i < Math.Min(xParts.Count, yParts.Count); i++)
            {
                if (xParts[i].IsNumeric && yParts[i].IsNumeric)
                {
                    var maxLength = Math.Max(xParts[i].Value.Length, yParts[i].Value.Length);
                    var result = innerComparer.Compare(xParts[i].Value.PadLeft(maxLength, '0'), yParts[i].Value.PadLeft(maxLength, '0'));
                    if (result != 0) return result;
                }
                else
                {
                    var result = innerComparer.Compare(xParts[i].Value, yParts[i].Value);
                    if (result != 0) return result;
                }
            }
            if (xParts.Count == yParts.Count) return innerComparer.Compare(x, y);
            if (xParts.Count > yParts.Count) return 1;
            else return -1;
        }

        public List<(string Value, bool IsNumeric)> GetParts(string x)
        {
            var parts = new List<(string, bool)>();
            string part = "";
            bool isNumeric = false;
            for (int i = 0; i < x.Length; i++)
            {
                char c = x[i];
                if (char.IsLetterOrDigit(c))
                {
                    if (part.Length == 0)
                    {
                        part += c;
                        isNumeric = char.IsDigit(c);
                    }
                    else
                    {
                        if (isNumeric == char.IsDigit(c))
                        {
                            part += c;
                        }
                        else
                        {
                            parts.Add((part, isNumeric));
                            part = "" + c;
                            isNumeric = char.IsDigit(c);
                        }
                    }
                }
                else if (part.Length > 0)
                {
                    parts.Add((part, isNumeric));
                    part = "";
                }
            }
            if (part.Length > 0)
            {
                parts.Add((part, isNumeric));
            }
            return parts;
        }
    }
}
