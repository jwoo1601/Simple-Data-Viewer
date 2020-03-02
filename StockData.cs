using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;

namespace DataViewer
{
    public class StockData
    {
        public string CompanySymbol { get; set; }
        public DateTime Date { get; set; }
        public decimal PriceAtOpen { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal PriceAtClose { get; set; }
        
        public static async Task<ICollection<StockData>> ParseStockDataFromFile(string fileName, LoadingProgress progress)
        {
            StreamReader reader = null;
            progress.Reset();

            try
            {
                reader = new StreamReader(fileName);
                var data = await reader.ReadToEndAsync();
                var lines = await Task.Run(() => data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

                var headerRow = lines[0];
                var dataRows = lines.Skip(1);
                progress.SetEnd(lines.Length - 1);

                Regex csvPattern = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                var headers = csvPattern.Split(headerRow);
                Dictionary<string, int> columnIndexes = new Dictionary<string, int>(headers.Length);
                for (int i =0; i < headers.Length; i++)
                {
                    columnIndexes.Add(headers[i].Trim().ToLower(), i);
                }

                var stockDataList = new ConcurrentBag<StockData>();
                Parallel.ForEach(dataRows,
                () => new List<StockData>(),
                (row, loop, subList) =>
                {
                    System.Threading.Thread.Sleep(1); // to make the loading progress more visible

                    progress.Progress(1);
                    var columns = csvPattern.Split(row);

                    StockData newData = new StockData();
                    newData.CompanySymbol = columns[columnIndexes["symbol"]].Replace("\"", "");
                    newData.Date = DateTime.Parse(columns[columnIndexes["date"]].Replace("\"", ""));
                    newData.PriceAtOpen = decimal.Parse(columns[columnIndexes["open"]].Replace("\"", ""), NumberStyles.Any);
                    newData.HighPrice = decimal.Parse(columns[columnIndexes["high"]].Replace("\"", ""), NumberStyles.Any);
                    newData.LowPrice = decimal.Parse(columns[columnIndexes["low"]].Replace("\"", ""), NumberStyles.Any);
                    newData.PriceAtClose = decimal.Parse(columns[columnIndexes["close"]].Replace("\"", ""), NumberStyles.Any);

                    var list = subList as List<StockData>;
                    list.Add(newData);
                    return subList;
                },
                (finalResult) =>
                {
                    Parallel.ForEach(finalResult, (result) => { stockDataList.Add(result); });
                });

                return stockDataList.ToList();
            }
            catch { throw; }
            finally
            {
                reader?.Dispose();
            }
        }
    }
}
