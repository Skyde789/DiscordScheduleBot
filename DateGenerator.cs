
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FFDiscordBot
{
    public class GetNextDayData
    {
        public DateTime date;
        public bool valid;

        public GetNextDayData(DateTime date, bool valid)
        {
            this.date = date;
            this.valid = valid;
        }
    }
    public static class DateGenerator
    {
        private static DateTime GetNextTuesday(DateTime date)
        {
            int daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)date.DayOfWeek + 7) % 7;

            return date.AddDays(daysUntilTuesday == 0 ? 7 : daysUntilTuesday);
        }

        private static DateTime GetUpcomingMonday(DateTime date)
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;

            return date.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
        }

        public static List<DateTime> GenerateDates(List<DayOfWeek> selectedDays, bool late = false)
        {
            DateTime today = DateTime.Today;

            // If we are starting late, start it the next day from today to plan for the next days
            DateTime windowStart = late ? today.AddDays(1) : GetNextTuesday(today);
            Console.WriteLine("Next tuesday: " + GetNextTuesday(today));
            Console.WriteLine("Next Monday: " + GetUpcomingMonday(today));

            DateTime windowEnd = windowStart.DayOfWeek == DayOfWeek.Monday ? windowStart : GetUpcomingMonday(windowStart);

            var result = new List<DateTime>();

            for (DateTime date = windowStart; date <= windowEnd; date = date.AddDays(1))
            {
                if (selectedDays.Contains(date.DayOfWeek))
                    result.Add(date);
            }

            return result;
        }

     
    }

}
