
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
        private static DateTime GetNextStartDate(DateTime date, DayOfWeek startDay)
        {
            int daysUntilStartDay = ((int)startDay - (int)date.DayOfWeek + 7) % 7;

            return date.AddDays(daysUntilStartDay == 0 ? 7 : daysUntilStartDay);
        }

        private static DateTime GetNextEndDate(DateTime date, DayOfWeek endDay)
        {
            int daysUntilEndDay = ((int)endDay - (int)date.DayOfWeek + 7) % 7;

            return date.AddDays(daysUntilEndDay == 0 ? 7 : daysUntilEndDay);
        }

        public static List<DateTime> GenerateDates(GuildSettings settings, bool late = false)
        {
            DateTime today = DateTime.Today;

            // If we are starting late, start it the next day from today to plan for the next days
            DateTime windowStart = late ? today.AddDays(1) : GetNextStartDate(today, settings.PollPeriod.Start);

            DateTime windowEnd = windowStart.DayOfWeek == settings.PollPeriod.End ? windowStart : GetNextEndDate(windowStart, settings.PollPeriod.End);

            var result = new List<DateTime>();

            for (DateTime date = windowStart; date <= windowEnd; date = date.AddDays(1))
            {
                if (settings.SelectedDays.Contains(date.DayOfWeek))
                    result.Add(date);
            }

            return result;
        }
    }
}
