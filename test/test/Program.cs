using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

public static class TableParser
{
    static void Main(string[] args)
    {
        IEnumerable<Tuple<int, string, string,string>> authors =
          new[]
          {
      Tuple.Create(1, "IsaacIsaacIsaacIsaacIsaac", "Asimov","Ralalala"),
      Tuple.Create(2, "Robert", "Heinlein","Ralalala"),
      Tuple.Create(3, "Frank", "Herbert","Ralalala"),
      Tuple.Create(4, "Aldous", "Huxley","Ralalala"),
          };

        Console.WriteLine(authors.ToStringTable(
          new[] { "Id", "First Name", "Surname","Testoo",},
          a => a.Item1, a => a.Item2, a => a.Item3,a=> a.Item4));

        /* Result:        
        | Id | First Name | Surname  |
        |----------------------------|
        | 1  | Isaac      | Asimov   |
        | 2  | Robert     | Heinlein |
        | 3  | Frank      | Herbert  |
        | 4  | Aldous     | Huxley   |
        */
        Console.ReadKey();
    }
    public static string ToStringTable<T>(
      this IEnumerable<T> values,
      string[] columnHeaders,
      params Func<T, object>[] valueSelectors)
    {
        return ToStringTable(values.ToArray(), columnHeaders, valueSelectors);
    }

    public static string ToStringTable<T>(
      this T[] values,
      string[] columnHeaders,
      params Func<T, object>[] valueSelectors)
    {
        Debug.Assert(columnHeaders.Length == valueSelectors.Length);

        var arrValues = new string[values.Length + 1, valueSelectors.Length];

        // Fill headers
        for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
        {
            arrValues[0, colIndex] = columnHeaders[colIndex];
        }

        // Fill table rows
        for (int rowIndex = 1; rowIndex < arrValues.GetLength(0); rowIndex++)
        {
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                arrValues[rowIndex, colIndex] = valueSelectors[colIndex]
                  .Invoke(values[rowIndex - 1]).ToString();
            }
        }

        return ToStringTable(arrValues);
    }

    public static string ToStringTable(this string[,] arrValues)
    {
        int[] maxColumnsWidth = GetMaxColumnsWidth(arrValues);
        var headerSpliter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

        var sb = new StringBuilder();
        for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
        {
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                // Print cell
                string cell = arrValues[rowIndex, colIndex];
                cell = cell.PadRight(maxColumnsWidth[colIndex]);
                sb.Append(" | ");
                sb.Append(cell);
            }

            // Print end of line
            sb.Append(" | ");
            sb.AppendLine();

            // Print splitter
            if (rowIndex == 0)
            {
                sb.AppendFormat(" |{0}| ", headerSpliter);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static int[] GetMaxColumnsWidth(string[,] arrValues)
    {
        var maxColumnsWidth = new int[arrValues.GetLength(1)];
        for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
        {
            for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                int newLength = arrValues[rowIndex, colIndex].Length;
                int oldLength = maxColumnsWidth[colIndex];

                if (newLength > oldLength)
                {
                    maxColumnsWidth[colIndex] = newLength;
                }
            }
        }

        return maxColumnsWidth;
    }
}