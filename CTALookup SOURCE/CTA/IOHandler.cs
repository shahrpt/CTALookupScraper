using System.Collections.Generic;
using System.Linq;
using System.Text;
using LINQtoCSV;
using System;
using System.IO;
using System.Windows;

namespace CTALookup
{
    class IOHandler
    {
        public IList<Item> Import(string filename)
        {
            CsvContext c = new CsvContext();
            CsvFileDescription f = new CsvFileDescription
            {
                FirstLineHasColumnNames = false,
                EnforceCsvColumnAttribute = true,
                SeparatorChar = ','
            };
            var users =
                c.Read<Item>(filename,
                    f);

            var list = users.ToList();
            return list;
        }

        public void Export<T>(IList<T> items, string filename)
        {
            CsvContext c = new CsvContext();
            CsvFileDescription f = new CsvFileDescription
            {
                FirstLineHasColumnNames = true,
                EnforceCsvColumnAttribute = true,
                SeparatorChar = ',',
                TextEncoding = Encoding.Default,
                QuoteAllFields = true,
                FileCultureName = "en"
            };
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));

                c.Write(items, filename, f);
            }catch(Exception x)
            {
                MessageBox.Show("Error exporting file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*public List<String> GetLines(String Filename, Boolean OnlyParcel, Boolean IncludeColumns = false)
        {
            var lines = System.IO.File.ReadAllLines(Filename).ToList();
            if (lines == null || lines.Count == 0) return lines;
            var columns = lines.First().Split(',');
            var appendstr = "";
            for (Int32 i = lines.Count - 1; i >= 1; --i)
            {
                if (String.IsNullOrEmpty(lines[i])) continue;
                var values = (String.IsNullOrEmpty(appendstr) ? lines[i] : lines[i] + Environment.NewLine + appendstr).Split(',');
                if (values.Count() >= columns.Count())
                {
                    appendstr = "";
                    if (OnlyParcel) lines[i] = values.First();
                    continue;
                }
                appendstr = lines[i];
                lines.RemoveAt(i);
            }
            if (!IncludeColumns) lines.RemoveAt(0);
            return lines;
        }*/
        public List<String> GetLines(String Filename, Boolean OnlyParcel, Boolean IncludeColumns = false)
        {
            var lines = System.IO.File.ReadAllLines(Filename).ToList();
            if (lines == null || lines.Count == 0) return lines;
            String[] columns = lines.First().Split(',');
            var nextInd = 0;
            var appendNext = false;
            var count = IncludeColumns ? 1 : 0;
            var startInd = -1;
            List<String> Values = null;
            var isQuote = false;
            for (Int32 i = 1; i < lines.Count; ++i )
            {
                var linestr = lines[i];
                if (String.IsNullOrEmpty(linestr)) continue;

                if (!appendNext)
                {
                    startInd = i;
                    Values = new List<String>();
                }
                do
                {
                    if (!appendNext) isQuote = linestr.Length>0 && linestr[0] == '"';
                    nextInd = linestr.IndexOf((isQuote ? "\"" : "") + ",");
                    if (nextInd == -1)
                    {
                        if (appendNext) Values[Values.Count - 1] += Environment.NewLine + linestr;
                        else Values.Add(linestr);
                        if (Values.Count != columns.Count()) appendNext = true;
                        break;
                    }
                    else 
                    {
                        if (appendNext)
                            Values[Values.Count - 1] += Environment.NewLine + linestr.Substring(0, nextInd + (isQuote ? 1 : 0));
                        else Values.Add(linestr.Substring(0, nextInd + (isQuote ? 1 : 0)));
                        linestr = linestr.Substring(nextInd + (isQuote ? 2 : 1));
                        appendNext = false;
                    }
                }
                while (nextInd >= 0);
                if (appendNext) continue;
                lines[count] = OnlyParcel ? Values[0] : String.Join(",", Values);
                ++count;
            }
            if (count != lines.Count) lines.RemoveRange(count, lines.Count - count);

            /*
            {
                if (String.IsNullOrEmpty(lines[i])) continue;
                
                if (!appendNext)
                {
                    lines[count] = lines[i];
                    startInd = i;
                    Values = new List<String>();
                }
                do
                {
                    if (!appendNext) ind = lines[i].IndexOf('"', ind + 1);
                    var nextInd = lines[i].IndexOf("\",", ind + 1);
                    if (nextInd == -1)
                    {
                        if (appendNext) Values[Values.Count - 1] += Environment.NewLine + lines[i].Substring(ind+1);
                        else Values.Add(lines[i].Substring(ind));
                        if (Values.Count != columns.Count()) appendNext = true;
                        ind = -1;
                        continue;
                    }
                    else
                    {
                        if (appendNext) 
                            Values[Values.Count - 1] += Environment.NewLine + lines[i].Substring(ind+1, nextInd + 1);
                        else Values.Add(lines[i].Substring(ind, nextInd - ind + 1));
                        ind = nextInd;
                        appendNext = false;
                    }
                }
                while (ind > 0);
                if (appendNext)
                {
                    if (startInd != i) lines[count] += Environment.NewLine + lines[i];
                    continue;
                }
                if (OnlyParcel) lines[count] = Values[0];
                else lines[count] = lines[i];
                ++count;
            }*/

            //if (!IncludeColumns) lines.RemoveAt(0);
            return lines;
        }


        private String SplitValues<T>(T item)
        {
            var res = "";
            var props = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            foreach (var prop in props)
            {
                if (prop.PropertyType != typeof(String)) continue;
                res += (String.IsNullOrEmpty(res) ? "" : ",") + "\"" + prop.GetValue(item, null) + "\"";
            }
            return res;
        }

        private String GetAttrStr<T>(T item)
        {
            var res = "";
            var props = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            foreach (var prop in props)
            {
                if (prop.PropertyType != typeof(String)) continue;
                var attr = (CsvColumnAttribute)CsvColumnAttribute.GetCustomAttribute(prop, typeof(CsvColumnAttribute), true);
                res += (String.IsNullOrEmpty(res) ? "" : ",") + "\"" + attr.Name + "\"";
            }
            return res;
        }

        public void Export<T>(IList<T> items, string filename, Int32 startInd = 0)
        {
            var lines = GetLines(filename, false, true);
            var outstr = lines[0] +","+ GetAttrStr<T>(items[0]) + Environment.NewLine;
            lines.RemoveAt(0);
            for (Int32 i = 0; i < items.Count; ++i)
            {
                outstr += lines[startInd + i] + "," + SplitValues<T>(items[i]) + Environment.NewLine;
            }
            System.IO.File.WriteAllText(filename, outstr);
        }
    }
}
