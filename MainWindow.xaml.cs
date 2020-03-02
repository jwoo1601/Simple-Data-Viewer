using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        LoadingProgress loadProgress;
        public int progressValue;
        public int ProgressValue
        {
            get { return progressValue; }
            set { loadProgress.Set(value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            loadProgress = new LoadingProgress(this);
        }
        public void NotifyPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // A Synchronous Method that Calculates Factorial of n
        public int CalculateFactorial(int n)
        {
            return n > 1 ? n * CalculateFactorial(n - 1) : 1;
        }

        private Dictionary<string, List<StockData>> stockDataMap;
        private async void OpenStockDataFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Open Stock Data File";
            fileDialog.Multiselect = false;
            fileDialog.RestoreDirectory = true;
            fileDialog.Filter = "Stock data files (*.csv)|*.csv";
            fileDialog.CheckFileExists = true;
            fileDialog.InitialDirectory = Environment.CurrentDirectory;

            var dialogResult = fileDialog.ShowDialog();
            if (dialogResult == true)
            {
                FileNameTextbox.Text = fileDialog.FileName;

                stockDataMap = await Task.Run(() => FetchData(fileDialog.FileName));

                CompanyListView.DataContext = stockDataMap.Keys.ToList();
            }
        }

        private async Task<Dictionary<string, List<StockData>>> FetchData(string fileName)
        {
            try
            {
                var parsedStockData = await StockData.ParseStockDataFromFile(fileName, loadProgress);

                var tempStockDataMap = new ConcurrentDictionary<string, List<StockData>>();
                parsedStockData.AsParallel()
                                .GroupBy(stockData => stockData.CompanySymbol)
                                .ForAll(group =>
                                {

                                    tempStockDataMap.GetOrAdd(group.Key, new List<StockData>())
                                                    .AddRange(group.AsEnumerable()
                                                                   .AsParallel()
                                                                   .Where(stockData => stockData.HighPrice > 0 && stockData.LowPrice > 0 && stockData.PriceAtOpen > 0 && stockData.PriceAtClose > 0)
                                                                   .OrderBy(stockData => stockData.Date));
                                });

                return new Dictionary<string, List<StockData>>(tempStockDataMap);
            }
            catch (FileNotFoundException e1)
            {
                MessageBox.Show("Stock data file not found: " + e1.FileName, "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException e2)
            {
                MessageBox.Show("Failed to read stock data file: " + e2.Message, "Unable to Read File", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private void OnCompanySelected(object sender, MouseButtonEventArgs e)
        {
            var selected = (sender as ListView).SelectedItem;
            if (selected != null)
            {
                var company = selected as string;

                CompanySearchBox.Text = company;
                DisplayCompanyStockData(company);
            }
        }
        private void OnSearchCompany(object sender, RoutedEventArgs e)
        {
            ProgressValue += 5;

            var company = CompanySearchBox.Text;
            if (!string.IsNullOrEmpty(company))
            {
                DisplayCompanyStockData(company);
            }
        }
        private void OnSubmitCompany(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var company = CompanySearchBox.Text;
                if (!string.IsNullOrEmpty(company))
                {
                    DisplayCompanyStockData(company);
                }
            }
        }
        private void DisplayCompanyStockData(string company)
        {
            StockGrid.DataContext = stockDataMap.ContainsKey(company) ? stockDataMap[company] : null;
        }
    }
}
