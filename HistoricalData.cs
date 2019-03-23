using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using Alveo.UserCode;
using DataItem = Alveo.UserCode.templateEA.BarData;

namespace HistoricalData
{
    public class HistoricalData
    {
        string dataFileDir = "C:\\temp\\";
        string dataFilename = null;
        public DateTime StartDate = DateTime.MinValue;
        public DateTime EndDate = DateTime.Now;
        public int lines = 0;
        internal List<DataItem> dataItems;

        public void LoadDataFile(string symbol, string timeframe, bool plugHoles = true)
        {
            lines = 0;
            dataItems = new List<DataItem>();
            if (dataFileDir.Length > 1 && symbol.Length > 1)
            {
                dataFilename = symbol + "." + timeframe + ".BAR.UTC.CSV";
                LogPrint("EA: Data file = " + dataFileDir + dataFilename);
                if (File.Exists(dataFileDir + dataFilename))
                {
                    LogPrint("EA: Data file = " + dataFileDir + dataFilename + " exists !");
                    using (System.IO.StreamReader file =
                        new System.IO.StreamReader(dataFileDir + dataFilename))
                    {
                        string line = file.ReadLine();
                        if (line.Length < 1)
                        {
                            string msg = "Exception: Data file = " + dataFileDir + dataFilename + "is empty!!";
                            LogPrint(msg);
                            throw new Exception(msg);
                        }
                        CheckHeaderFormat(line);
                        LogPrint("Golden Strategy EA: Data file = " + dataFileDir + dataFilename + " first line format verified.");
                        dataItems.Clear();

                        int tf;
                        switch (timeframe)
                        {
                            case "M1":
                                tf = 1 * 60;
                                break;
                            case "M5":
                                tf = 5 * 60;
                                break;
                            case "M15":
                                tf = 15 * 60;
                                break;
                            case "M30":
                                tf = 30 * 60;
                                break;
                            case "H1":
                                tf = 60 * 60;
                                break;
                            case "H2":
                                tf = 2 * 60 * 60;
                                break;
                            case "H4":
                                tf = 4 * 60 * 60;
                                break;
                            case "S10":
                                tf = 10;
                                break;
                            default:
                                tf = 10;
                                break;
                        }
                        while (!file.EndOfStream)
                        {
                            lines++;
                            line = file.ReadLine();
                            DataItem dataItem = CheckDataFormat(line);
                            if (dataItem != null)
                            {
                                if (plugHoles && dataItems.Count > 0)  // plug missing data holes
                                {
                                    dataItem.wd = dataItem.BarTime.DayOfWeek;
                                    dataItem.tod = dataItem.BarTime.TimeOfDay;
                                    TimeSpan dur = dataItem.BarTime.Subtract(dataItems.Last().BarTime);
                                    if (dur > TimeSpan.FromSeconds(tf) && dur < TimeSpan.FromHours(1))
                                    {
                                        DateTime newDate = dataItems.Last().BarTime + TimeSpan.FromSeconds(tf);
                                        while (newDate < dataItem.BarTime)
                                        {
                                            DataItem newDataItem = new DataItem();
                                            newDataItem.BarTime = newDate;
                                            newDataItem.volume = 0;
                                            newDataItem.open = dataItem.open;
                                            newDataItem.high = dataItem.high;
                                            newDataItem.low = dataItem.open;
                                            newDataItem.close = dataItem.open;
                                            newDataItem.symbol = dataItem.symbol;
                                            newDate += TimeSpan.FromSeconds(tf);
                                            newDataItem.typical = (double)(newDataItem.open + newDataItem.high + newDataItem.low + newDataItem.close) / 4.0;
                                            newDataItem.wd = newDataItem.BarTime.DayOfWeek;
                                            newDataItem.tod = newDataItem.BarTime.TimeOfDay;
                                            dataItems.Add(newDataItem);
                                        }
                                    }
                                }
                                dataItem.typical = (double)(dataItem.open + dataItem.high + dataItem.low + dataItem.close) / 4.0;
                                dataItem.wd = dataItem.BarTime.DayOfWeek;
                                dataItem.tod = dataItem.BarTime.TimeOfDay;
                                dataItems.Add(dataItem);
                            }
                        }
                        LogPrint("Golden Strategy EA: Data file = " + dataFileDir + dataFilename + " All data read and loaded.");
                        LogPrint("Golden Strategy EA: Data file = " + dataFileDir + dataFilename + " contains " + dataItems.Count + " dataItems.");
                    }
                }
                else
                {
                    string msg = "Exception: Data file = " + dataFileDir + dataFilename + " does not exist!!";
                    LogPrint(msg);
                    throw new Exception(msg);
                }
            }
            else
            {
                string msg = "Exception: Data file = " + dataFileDir + dataFilename + " does not exist!!";
                LogPrint(msg);
                throw new Exception(msg);
            }
        }

        void CheckHeaderFormat(string line)
        {
            char[] delimiterChars = { ',', ':', '\t' };
            string[] words = line.Split(delimiterChars);
            if (words.Count() == 8)
            {
                if (words[0] == "Date") return;
            }
            string msg = "Exception: Golden Strategy EA: Data file = " + dataFileDir + dataFilename + " incorrect format!!";
            LogPrint(msg);
            throw new Exception(msg);
        }

        DataItem CheckDataFormat(string line)
        {
            char[] delimiterChars = { ',', '\t' };
            string[] words = line.Split(delimiterChars);
            while (true)
            {
                if (words.Count() == 8
                    || (words.Count() == 9 && words[8].Trim().Length == 0))
                {
                    DataItem dataItem = new DataItem();
                    int intDate;
                    TimeSpan interval;
                    double volume;
                    double open;
                    double high;
                    double low;
                    double close;
                    if (words[0].Length != 8) break;
                    if (words[1].Length != 8) break;
                    if (!Int32.TryParse(words[0], out intDate)) break;
                    if (!TimeSpan.TryParse(words[1], out interval)) break;
                    if (!double.TryParse(words[2], out volume)) break;
                    if (!double.TryParse(words[3], out open)) break;
                    if (!double.TryParse(words[4], out high)) break;
                    if (!double.TryParse(words[5], out low)) break;
                    if (!double.TryParse(words[6], out close)) break;
                    int year = intDate / 10000;
                    if (year < 1970 || year > DateTime.Now.Year) break;
                    int month = (intDate / 100) % 100;
                    if (month < 1 || month > 12) break;
                    int day = intDate % 100;
                    if (day < 1 || day > 31) break;
                    DateTime date = new DateTime(year, month, day, interval.Hours, interval.Minutes, interval.Seconds);
                    if (date >= DateTime.Now) break;
                    if (date < StartDate)
                        return null;
                    if (date > EndDate)
                        return null;
                    dataItem.startOfSession = false;
                    if (dataItems.Count == 0)
                    {
                        dataItem.startOfSession = true;
                    }
                    else // dataItems.Count > 0
                    {
                        DateTime last;
                        last = dataItems.Last().BarTime;
                        if (date <= last) break;
                        if (date.Subtract(last).TotalHours > 24)
                            dataItem.startOfSession = true;
                    }
                    dataItem.BarTime = date;
                    dataItem.volume = (long)(volume * 1000);
                    dataItem.open = open;
                    dataItem.high = high;
                    dataItem.low = low;
                    dataItem.close = close;
                    dataItem.symbol = (words[7].Length > 0) ? words[7] : null;
                    dataItem.change = close - open;
                    return dataItem;
                }
                else
                    break;
            }
            string msg = "Exception: Golden Strategy EA: Data file = " + dataFileDir + dataFilename + " data format error line " + lines + " !!";
            LogPrint(msg);
            throw new Exception(msg);
        }

        void LogPrint(object line)
        {
            Debug.WriteLine(line);
        }

        internal void ExportData(string timeframe, List<DataItem> dlist)
        {
            DataItem dataItem = dlist[0];
            var symbol = dataItem.symbol;
            dataFilename = symbol + "." + timeframe + ".BAR.CSV";
            using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(dataFileDir + dataFilename))
            {
                string line = "Date,Timestamp,Volume,Open,High,Low,Close, " + symbol + ",MFI,Fpct";
                file.WriteLine(line);
                for (var i = 0; i < dlist.Count; i++)
                {
                    dataItem = dlist[i];
                    var yearStr = dataItem.BarTime.Date.Year.ToString("d4");
                    var monthStr = dataItem.BarTime.Date.Month.ToString("d2");
                    var dayStr = dataItem.BarTime.Date.Day.ToString("d2");
                    var timeStr = dataItem.BarTime.TimeOfDay.ToString(@"hh\:mm\:ss");
                    var volStr = dataItem.volume.ToString("F6");
                    var openStr = dataItem.open.ToString("F6");
                    var highStr = dataItem.high.ToString("F6");
                    var lowStr = dataItem.low.ToString("F6");
                    var closeStr = dataItem.close.ToString("F6");
                    line = yearStr + monthStr + dayStr + ","
                        + timeStr + ","
                        + volStr + ","
                        + openStr + ","
                        + highStr + ","
                        + lowStr + ","
                        + closeStr + ","
                        + symbol;
                    file.WriteLine(line);
                }
            }
        }
    }
}
