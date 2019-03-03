using System;
using System.Collections.Generic;
using System.Linq;

namespace JpHolidaySharp
{
    public static class JpHoliday
    {
        private interface Rule
        {
            bool Eval(DateTime date);
        }

        private sealed class YearRule : Rule
        {
            private readonly int _start;
            private readonly int _end;

            private YearRule(int year)
            {
                _start = year;
                _end = year;
            }

            private YearRule(int start, int end)
            {
                _start = start;
                _end = end;
            }

            public bool Eval(DateTime date) => date.Year >= _start && date.Year <= _end;

            public static YearRule Just(int year) => new YearRule(year);
            public static YearRule Before(int year) => new YearRule(int.MinValue, year);
            public static YearRule After(int year) => new YearRule(year, int.MaxValue);
            public static YearRule Range(int start, int end) => new YearRule(start, end);
            public static YearRule Any() => new YearRule(int.MinValue, int.MaxValue);
        }

        private sealed class MonthRule : Rule
        {
            private readonly int _start;
            private readonly int _end;

            private MonthRule(int month)
            {
                _start = month;
                _end = month;
            }

            private MonthRule(int start, int end)
            {
                _start = start;
                _end = end;
            }

            public bool Eval(DateTime date) => date.Month >= _start && date.Month <= _end;

            public static MonthRule Just(int month) => new MonthRule(month);
            public static MonthRule Before(int month) => new MonthRule(int.MinValue, month);
            public static MonthRule After(int month) => new MonthRule(month, int.MaxValue);
            public static MonthRule Range(int start, int end) => new MonthRule(start, end);
            public static MonthRule Any() => new MonthRule(int.MinValue, int.MaxValue);
        }

        private sealed class DayRule : Rule
        {
            private sealed class JustRule : Rule
            {
                private int _day;

                public JustRule(int day)
                {
                    _day = day;
                }

                public bool Eval(DateTime date) => date.Day == _day;
            }

            private sealed class WeekDayRule : Rule
            {
                private int _week;
                private DayOfWeek _dayOfWeek;

                public WeekDayRule(int week, DayOfWeek dayOfWeek)
                {
                    _week = week;
                    _dayOfWeek = dayOfWeek;
                }

                public bool Eval(DateTime date)
                {
                    var tmp = new DateTime(date.Year, date.Month, 1);
                    tmp = tmp.AddDays((7 * (_week - 1)) +
                        ((int)_dayOfWeek >= (int)tmp.DayOfWeek ?
                            (int)_dayOfWeek - (int)tmp.DayOfWeek :
                            (int)DayOfWeek.Saturday - (int)tmp.DayOfWeek + (int)_dayOfWeek + 1));
                    return date.Month == tmp.Month && date.Day == tmp.Day;
                }
            }

            private sealed class FuncRule : Rule
            {
                private Func<DateTime, bool> _func;

                public FuncRule(Func<DateTime, bool> func)
                {
                    _func = func;
                }

                public bool Eval(DateTime date) => _func?.Invoke(date) ?? false;
            }

            private Rule _rule;

            private DayRule(Rule rule)
            {
                _rule = rule;
            }

            public bool Eval(DateTime date) => _rule?.Eval(date) ?? false;

            public static DayRule Just(int day) => new DayRule(new JustRule(day));
            public static DayRule WeekDay(int week, DayOfWeek dayOfWeek) => new DayRule(new WeekDayRule(week, dayOfWeek));
            public static DayRule Func(Func<DateTime, bool> func) => new DayRule(new FuncRule(func));
        }

        private sealed class DateRule : Rule
        {
            public enum DateType
            {
                Holiday,
                SubstituteHoliday,
                NationalHoliday
            }

            public string Name { get; private set; }
            public DateType Type { get; private set; }
            public YearRule YearRule { get; private set; }
            public MonthRule MonthRule { get; private set; }
            public DayRule DayRule { get; private set; }

            private DateRule(string name, DateType type, YearRule yearRule, MonthRule monthRule, DayRule dayRule)
            {
                Name = name;
                Type = type;
                YearRule = yearRule;
                MonthRule = monthRule;
                DayRule = dayRule;
            }

            public bool Eval(DateTime date)
            {
                if (!YearRule.Eval(date)) return false;
                if (!MonthRule.Eval(date)) return false;
                if (!DayRule.Eval(date)) return false;

                return true;
            }

            public static DateRule Holiday(string name, YearRule yearRule, MonthRule monthRule, DayRule dayRule) =>
                new DateRule(name, DateType.Holiday, yearRule, monthRule, dayRule);
            public static DateRule SubstituteHoliday(string name, YearRule yearRule, MonthRule monthRule, DayRule dayRule) =>
                new DateRule(name, DateType.SubstituteHoliday, yearRule, monthRule, dayRule);
            public static DateRule NationalHoliday(string name, YearRule yearRule, MonthRule monthRule, DayRule dayRule) =>
                new DateRule(name, DateType.NationalHoliday, yearRule, monthRule, dayRule);
        }

        public sealed class Holiday
        {
            public string Name { get; private set; }
            public DateTime Date { get; private set; }

            public Holiday(string name, DateTime date)
            {
                Name = name;
                Date = date;
            }
        }

        private static readonly DateTime SubstituteHolidayStartDate = new DateTime(1973, 4, 12);

        private static readonly List<DateRule> _holidayRules = new List<DateRule>()
        {
            DateRule.Holiday(@"元日", YearRule.After(1949), MonthRule.Just(1), DayRule.Just(1)),
            DateRule.Holiday(@"成人の日", YearRule.After(2000), MonthRule.Just(1), DayRule.WeekDay(2, DayOfWeek.Monday)),
            DateRule.Holiday(@"成人の日", YearRule.Range(1949, 1999), MonthRule.Just(1), DayRule.Just(15)),
            DateRule.Holiday(@"建国記念の日", YearRule.After(1967), MonthRule.Just(2), DayRule.Just(11)),
            DateRule.Holiday(@"昭和の日", YearRule.After(2007), MonthRule.Just(4), DayRule.Just(29)),
            DateRule.Holiday(@"憲法記念日", YearRule.After(1949), MonthRule.Just(5), DayRule.Just(3)),
            DateRule.Holiday(@"みどりの日", YearRule.After(2007), MonthRule.Just(5), DayRule.Just(4)),
            DateRule.Holiday(@"みどりの日", YearRule.Range(1989, 2006), MonthRule.Just(4), DayRule.Just(29)),
            DateRule.Holiday(@"こどもの日", YearRule.After(1949), MonthRule.Just(5), DayRule.Just(5)),
            DateRule.Holiday(@"海の日", YearRule.After(2021), MonthRule.Just(7), DayRule.WeekDay(3, DayOfWeek.Monday)),
            DateRule.Holiday(@"海の日", YearRule.Just(2020), MonthRule.Just(7), DayRule.Just(23)),
            DateRule.Holiday(@"海の日", YearRule.Range(2003, 2019), MonthRule.Just(7), DayRule.WeekDay(3, DayOfWeek.Monday)),
            DateRule.Holiday(@"海の日", YearRule.Range(1996, 2002), MonthRule.Just(7), DayRule.Just(20)),
            DateRule.Holiday(@"山の日", YearRule.After(2021), MonthRule.Just(8), DayRule.Just(11)),
            DateRule.Holiday(@"山の日", YearRule.Just(2020), MonthRule.Just(8), DayRule.Just(10)),
            DateRule.Holiday(@"山の日", YearRule.Range(2016, 2019), MonthRule.Just(8), DayRule.Just(11)),
            DateRule.Holiday(@"敬老の日", YearRule.After(2003), MonthRule.Just(9), DayRule.WeekDay(3, DayOfWeek.Monday)),
            DateRule.Holiday(@"敬老の日", YearRule.Range(1966, 2002), MonthRule.Just(9), DayRule.Just(15)),
            DateRule.Holiday(@"体育の日", YearRule.Range(2000, 2019), MonthRule.Just(10), DayRule.WeekDay(2, DayOfWeek.Monday)),
            DateRule.Holiday(@"体育の日", YearRule.Range(1966, 1999), MonthRule.Just(10), DayRule.Just(10)),
            DateRule.Holiday(@"スポーツの日", YearRule.After(2021), MonthRule.Just(10), DayRule.WeekDay(2, DayOfWeek.Monday)),
            DateRule.Holiday(@"スポーツの日", YearRule.Just(2020), MonthRule.Just(7), DayRule.Just(24)),
            DateRule.Holiday(@"文化の日", YearRule.After(1948), MonthRule.Just(11), DayRule.Just(3)),
            DateRule.Holiday(@"勤労感謝の日", YearRule.After(1948), MonthRule.Just(11), DayRule.Just(23)),
            DateRule.Holiday(@"天皇誕生日", YearRule.After(2020), MonthRule.Just(2), DayRule.Just(23)),
            DateRule.Holiday(@"天皇誕生日", YearRule.Range(1989, 2018), MonthRule.Just(12), DayRule.Just(23)),
            DateRule.Holiday(@"天皇誕生日", YearRule.Range(1949, 1988), MonthRule.Just(4), DayRule.Just(29)),

            DateRule.Holiday(@"春分の日", YearRule.Range(1949, 1979), MonthRule.Just(3), DayRule.Func((date) =>
            {
                return date.Day == (int)(20.8357 + 0.242194 * (date.Year - 1980)) - (int)((date.Year - 1983) / 4.0);
            })),
            DateRule.Holiday(@"春分の日", YearRule.Range(1980, 2099), MonthRule.Just(3), DayRule.Func((date) =>
            {
                return date.Day == (int)(20.8431 + 0.242194 * (date.Year - 1980)) - (int)((date.Year - 1980) / 4.0);
            })),
            DateRule.Holiday(@"春分の日", YearRule.Range(2100, 2150), MonthRule.Just(3), DayRule.Func((date) =>
            {
                return date.Day == (int)(21.8510 + 0.242194 * (date.Year - 1980)) - (int)((date.Year - 1980) / 4.0);
            })),

            DateRule.Holiday(@"秋分の日", YearRule.Range(1948, 1979), MonthRule.Just(9), DayRule.Func((date) =>
            {
                return date.Day == (int)(23.2588 + 0.242194 * (date.Year - 1980)) - (int)((date.Year - 1983) / 4.0);
            })),
            DateRule.Holiday(@"秋分の日", YearRule.Range(1980, 2099), MonthRule.Just(9), DayRule.Func((date) =>
            {
                return date.Day == (int)(23.2488 + 0.242194 * (date.Year - 1980)) - (int)((date.Year - 1980) / 4.0);
            })),
            DateRule.Holiday(@"秋分の日", YearRule.Range(2100, 2150), MonthRule.Just(9), DayRule.Func((date) =>
            {
                return date.Day == (int)(24.2488 + 0.242194 * (date.Year - 1980)) - (int)((date.Year - 1980) / 4.0);
            })),

            DateRule.Holiday(@"即位礼正殿の儀", YearRule.Just(2019), MonthRule.Just(10), DayRule.Just(22)),
            DateRule.Holiday(@"即位礼正殿の儀", YearRule.Just(1990), MonthRule.Just(11), DayRule.Just(12)),

            DateRule.Holiday(@"天皇の即位の日", YearRule.Just(2019), MonthRule.Just(5), DayRule.Just(1)),
            DateRule.Holiday(@"皇太子徳仁親王の結婚の儀", YearRule.Just(1993), MonthRule.Just(6), DayRule.Just(9)),
            DateRule.Holiday(@"昭和天皇の大喪の礼", YearRule.Just(1989), MonthRule.Just(2), DayRule.Just(24)),
            DateRule.Holiday(@"皇太子明仁親王の結婚の儀", YearRule.Just(1959), MonthRule.Just(4), DayRule.Just(10)),

            DateRule.SubstituteHoliday(@"振替休日", YearRule.After(2007), MonthRule.Any(), DayRule.Func((date) =>
            {
                var tmp = date.AddDays(-1);
                while (FindHoliday(tmp, false, false) != null)
                {
                    if (tmp.DayOfWeek == DayOfWeek.Sunday)return true;
                    tmp = tmp.AddDays(-1);
                }
                return false;
            })),
            DateRule.SubstituteHoliday(@"振替休日", YearRule.After(1973), MonthRule.Any(), DayRule.Func((date) =>
            {
                if (date >= SubstituteHolidayStartDate)
                {
                    var tmp = date.AddDays(-1);
                    if (FindHoliday(tmp, false, false) != null && tmp.DayOfWeek == DayOfWeek.Sunday)return true;
                }
                return false;
            })),

            DateRule.NationalHoliday(@"国民の休日", YearRule.After(1986), MonthRule.Any(), DayRule.Func((date) =>
            {
                if (date.DayOfWeek == DayOfWeek.Sunday)return false;

                var tmp1 = date.AddDays(-1);
                var tmp2 = date.AddDays(1);
                return FindHoliday(tmp1, false, false) != null && FindHoliday(tmp2, false, false) != null;
            })),
        };

        private static Holiday FindHoliday(DateTime date, bool includeSubstitudeHoliday, bool includeNationalHoliday)
        {
            Holiday result = null;

            var holidayRule = _holidayRules.FirstOrDefault((rule) =>
            {
                if (!includeSubstitudeHoliday && rule.Type == DateRule.DateType.SubstituteHoliday) return false;
                if (!includeNationalHoliday && rule.Type == DateRule.DateType.NationalHoliday) return false;
                return rule.Eval(date);
            });
            if (holidayRule != null)
            {
                result = new Holiday(holidayRule.Name, date);
            }

            return result;
        }

        /// <summary>
        /// 判定日が休日かどうかを取得します。
        /// </summary>
        /// <param name="date">判定日</param>
        /// <returns>判定日が休日であればtrue、それ以外はfalse</returns>
        public static bool IsHoliday(DateTime date) => GetHoliday(date) != null;

        /// <summary>
        /// 開始日から終了日までに休日が存在するかどうかを取得します。
        /// </summary>
        /// <param name="start">開始日</param>
        /// <param name="end">終了日</param>
        /// <returns>開始日から終了日までに休日が存在する場合はtrue、それ以外はfalse</returns>
        public static bool ExistsHoliday(DateTime start, DateTime end) => GetHolidays(start, end).Any();

        /// <summary>
        /// 判定日の休日情報を取得します。
        /// </summary>
        /// <param name="date">判定日</param>
        /// <returns>判定日が休日であれば休日情報、それ以外はnull</returns>
        public static Holiday GetHoliday(DateTime date) => FindHoliday(date, true, true);

        /// <summary>
        /// 開始日から終了日までの休日情報のリストを取得します。
        /// </summary>
        /// <param name="start">開始日</param>
        /// <param name="end">終了日</param>
        /// <returns>開始日から終了日までの休日情報のリスト、休日が無い場合は空のリスト</returns>
        public static List<Holiday> GetHolidays(DateTime start, DateTime end)
        {
            var result = new List<Holiday>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var holiday = FindHoliday(date, true, true);
                if (holiday != null)
                {
                    result.Add(holiday);
                }
            }
            return result;
        }
    }
}