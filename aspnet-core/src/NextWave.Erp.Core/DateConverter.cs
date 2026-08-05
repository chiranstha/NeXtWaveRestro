using System;
using NepDate;

namespace NextWave.Erp
{
    public class DateConverter
    {
        public static DateTime ConvertToEnglish(string nepaliDate)
        {
            return new NepaliDate(nepaliDate).EnglishDate.Date;
        }

        public static string ConvertToNepali(DateTime engDate)
        {
            return new NepaliDate(engDate).ToString();
        }

        public static string AddMonth(string date, int noOfMonth)
        {
            return new NepaliDate(date).AddMonths(noOfMonth).ToString();
        }

        public static string AddDays(string date, int noOfDays)
        {
            return new NepaliDate(date).AddMonths(noOfDays).ToString();
        }
    }
}
