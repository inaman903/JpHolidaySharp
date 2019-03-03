using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using JpHolidaySharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JpHolidaySharpTest
{
    [TestClass]
    public class UnitTest
    {
        private static readonly DateTime StatDate = new DateTime(1948, 7, 20);
        private static readonly DateTime EndDate = new DateTime(2050, 12, 31);

        [TestMethod]
        public void TestIsHoliday()
        {
            var holidayTable = GetHolidayTable();
            for (var date = StatDate; date <= EndDate; date = date.AddDays(1))
            {
                var expected = holidayTable.ContainsKey(date);
                var actual = JpHoliday.IsHoliday(date);
                Assert.AreEqual(
                    expected,
                    actual,
                    @"[{0}]は[{1}]と判定されましたがテストデータの[{2}]と一致しません",
                    date.ToString(@"yyyy/MM/dd"),
                    (actual ? @"休日" : @"休日ではない"),
                    (expected ? @"休日" : @"休日ではない"));
            }
        }

        [TestMethod]
        public void TestExistsHoliday()
        {
            var holidayTable = GetHolidayTable();
            for (var date = StatDate; date <= EndDate; date = date.AddMonths(1))
            {
                var start = new DateTime(date.Year, date.Month, 1);
                var end = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

                var expected = holidayTable.Keys.Where((d) => d.Year == date.Year && d.Month == date.Month).Any();
                var actual = JpHoliday.ExistsHoliday(start, end);
                Assert.AreEqual(
                    expected,
                    actual,
                    @"[{0}]は[{1}]と判定されましたがテストデータの[{2}]と一致しません",
                    date.ToString(@"yyyy/MM"),
                    (actual ? @"休日あり" : @"休日なし"),
                    (expected ? @"休日あり" : @"休日なし"));
            }
        }

        [TestMethod]
        public void TestGetHoliday()
        {
            var holidayTable = GetHolidayTable();
            for (var date = StatDate; date <= EndDate; date = date.AddDays(1))
            {
                var holiday = JpHoliday.GetHoliday(date);
                if (holiday != null && !holidayTable.ContainsKey(date))
                {
                    Assert.Fail(
                        @"[{0}]は[{1}]と判定されましたがテストデータに含まれていません",
                        date.ToString(@"yyyy/MM/dd"),
                        holiday.Name);
                }
                else if (holiday == null && holidayTable.ContainsKey(date))
                {
                    Assert.Fail(
                        @"[{0}]は休日ではないと判定されましたがテストデータには[{1}]として含まれています",
                        date.ToString(@"yyyy/MM/dd"),
                        holidayTable[date]);
                }

                if (holiday != null)
                {
                    Assert.AreEqual(
                        holidayTable[date],
                        holiday.Name,
                        true,
                        @"[{0}]は[{1}]と判定されましたがテストデータの[{2}]と一致しません",
                        date.ToString(@"yyyy/MM/dd"),
                        holiday.Name,
                        holidayTable[date]);
                }
            }
        }

        [TestMethod]
        public void TestGetHolidays()
        {
            var holidayTable = GetHolidayTable();
            for (var year = StatDate.Year; year <= EndDate.Year; year++)
            {
                var start = new DateTime(year, 1, 1);
                var end = new DateTime(year, 12, DateTime.DaysInMonth(year, 12));

                var holidays = JpHoliday.GetHolidays(start, end);
                foreach (var date in holidayTable.Keys.Where((d) => d.Year == year))
                {
                    var holiday = holidays.FirstOrDefault((h) => h.Date == date);
                    Assert.IsNotNull(
                        holiday,
                         @"[{0}]は休日ではないと判定されましたがテストデータには[{1}]として含まれています",
                         date.ToString(@"yyyy/MM/dd"),
                         holidayTable[date]);

                    Assert.AreEqual(
                        holidayTable[date],
                        holiday.Name,
                        true,
                        @"[{0}]は[{1}]と判定されましたがテストデータの[{2}]と一致しません",
                        date.ToString(@"yyyy/MM/dd"),
                        holiday.Name,
                        holidayTable[date]);
                }
            }
        }

        private Dictionary<DateTime, string> GetHolidayTable()
        {
            var result = new Dictionary<DateTime, string>();

            var doc = new XmlDocument();
            doc.Load(@"Holidays.xml");

            var holidayNodes = doc.SelectNodes(@"/holidays/holiday");
            foreach (XmlElement holidayElement in holidayNodes)
            {
                var date = DateTime.Parse(holidayElement.GetAttribute(@"date"));
                var name = holidayElement.GetAttribute(@"name");

                result[date] = name;
            }

            return result;
        }
    }
}