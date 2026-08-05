using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Common
{

    public static class CurrencyToAmount
    {
        public static string AmountWords(decimal decAmount)
        {
            var aountInWords = string.Empty; // To return the amount in words

            var strAmount =
                decAmount.ToString(); // Just keeping the whole amount as string for performing split operation on it
            var strAmountinwordsOfIntiger = string.Empty; // To hold amount in words of intiger
            var strAmountInWordsOfDecimal = string.Empty; // To hold amoutn in words of decimal part
            var strPartsArray = strAmount.Split('.'); // Splitting with "." to get intiger part and decimal part seperately
            var strDecimaPart = string.Empty; // To hold decimal part
            if (strPartsArray.Length > 1)
                if (strPartsArray[1] != null)
                    strDecimaPart = strPartsArray[1]; // Holding decimal portion if any
            if (strPartsArray[0] != null)
                strAmount = strPartsArray[0]; // Holding intiger part of amount
            else
                strAmount = string.Empty;
            ;
            if (strAmount.Trim() != string.Empty && decimal.Parse(strAmount) != 0)
                strAmountinwordsOfIntiger = NumberToText(long.Parse(strAmount));
            if (strDecimaPart.Trim() != string.Empty && decimal.Parse(strDecimaPart) != 0)
                strAmountInWordsOfDecimal = NumberToText(long.Parse(strDecimaPart));

            // Showing currency as prefix
            if (strAmountinwordsOfIntiger != string.Empty) aountInWords = strAmountinwordsOfIntiger;
            if (strAmountInWordsOfDecimal != string.Empty)
            {
                if (aountInWords != string.Empty)
                    aountInWords = aountInWords + " and " + strAmountInWordsOfDecimal + " Paisa";
                else
                    aountInWords = strAmountInWordsOfDecimal + " Paisa";
            }

            aountInWords = aountInWords + " only";
            return aountInWords;
        }

        public static string NumberToText(long number)
        {
            // Converting the number to words
            if (number == 0) return "Zero";
            if (number == -2147483648)
                return
                    "Minus Two Hundred and Fourteen Crore Seventy Four Lakh Eighty Three Thousand Six Hundred and Forty Eight";
            var num = new long[4];
            long first = 0;
            long u, h, t;
            var sb = new StringBuilder();
            if (number < 0)
            {
                sb.Append("Minus ");
                number = -number;
            }

            string[] words0 = { "", "One ", "Two ", "Three ", "Four ", "Five ", "Six ", "Seven ", "Eight ", "Nine " };
            string[] words1 =
            {
            "Ten ", "Eleven ", "Twelve ", "Thirteen ", "Fourteen ", "Fifteen ", "Sixteen ", "Seventeen ", "Eighteen ",
            "Nineteen "
        };
            string[] words2 = { "Twenty ", "Thirty ", "Forty ", "Fifty ", "Sixty ", "Seventy ", "Eighty ", "Ninety " };
            string[] words3 = { "Thousand ", "Lakh ", "Crore " };
            num[0] = number % 1000; // units 
            num[1] = number / 1000;
            num[2] = number / 100000;
            num[1] = num[1] - 100 * num[2]; // thousands 
            num[3] = number / 10000000; // crores 
            num[2] = num[2] - 100 * num[3]; // lakhs 
            for (var i = 3; i > 0; i--)
                if (num[i] != 0)
                {
                    first = i;
                    break;
                }

            for (var i = first; i >= 0; i--)
            {
                if (num[i] == 0) continue;
                u = num[i] % 10; // ones 
                t = num[i] / 10;
                h = num[i] / 100; // hundreds 
                t = t - 10 * h; // tens 
                try
                {
                    if (h > 0) sb.Append(words0[h] + "Hundred ");
                }
                catch
                {
                    //index out of range
                }

                if (u > 0 || t > 0)
                {
                    if (h > 0 || i == 0) sb.Append(" ");
                    if (t == 0)
                        sb.Append(words0[u]);
                    else if (t == 1)
                        sb.Append(words1[u]);
                    else
                        sb.Append(words2[t - 2] + words0[u]);
                }

                if (i != 0) sb.Append(words3[i - 1]);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
