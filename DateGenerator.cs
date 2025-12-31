
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
        static GetNextDayData GetNextDayOccurrence(DayOfWeek dayToCheck, int weeksFromNow)
        {
            DateTime today = DateTime.Today;
            int daysUntilNext = (weeksFromNow * 7) - (today.DayOfWeek - dayToCheck);
            
            return new GetNextDayData(today.AddDays(daysUntilNext), daysUntilNext >= 0);
        }

        public static List<DateTime> GenerateWeeklyPollDates(List<DayOfWeek> schedule, int weeksFromNow)
        {
            List<GetNextDayData> data = new List<GetNextDayData>();
            List<DateTime> result = new List<DateTime>();

            foreach (DayOfWeek day in schedule)
            {
                data.Add(GetNextDayOccurrence(day, weeksFromNow));
            }

            foreach (GetNextDayData dayData in data)
            {
                if (dayData.valid)
                    result.Add(dayData.date);
            }

            return result.OrderBy(d => d).ToList();
        }
    }

}
