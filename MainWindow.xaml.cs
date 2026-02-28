using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UglyToad.PdfPig;
using Xceed.Words.NET;
namespace FileDrop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string _currentFile = "";
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if(e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var file = ((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault();
            if (File.Exists(file))
            {
                _currentFile = file;
                FilePathTextBlock.Text = file;
                ResultsList.ItemsSource = null;
                StatusText.Text = "ფაილი შეყვანილია. შეიტანეთ სასურველი სიტყვა";
            }
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentFile) || !File.Exists(_currentFile))
        {
            MessageBox.Show("Please drop a valid file first.");
            return;
        }

        string keyword = SearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            MessageBox.Show("Please enter a keyword.");
            return;
        }

        StatusText.Text = "Searching...";
        LoadingBar.Visibility = Visibility.Visible;
        ResultsList.ItemsSource = null;

        var results = await Task.Run(() => SearchInFile(_currentFile, keyword));
        ResultsList.ItemsSource = results;

        StatusText.Text = $"Done. {results.Count} results found.";
        LoadingBar.Visibility = Visibility.Collapsed;
    }

    private List<ResultItem> SearchInFile(string path, string keyword)
    {
        var results = new List<ResultItem>();
        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".pdf")
        {
            using var doc = PdfDocument.Open(path);
            int pageNum = 1;
            foreach (var page in doc.GetPages())
            {
                string text = page.Text;
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    string snippet = GetSnippet(text, keyword);
                    results.Add(new ResultItem { Page = pageNum, Snippet = snippet });
                }
                pageNum++;
            }
        }
        else if (ext == ".docx")
        {
            var doc = DocX.Load(path);
            string fullText = doc.Text;
            if (fullText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ResultItem { Page = 1, Snippet = GetSnippet(fullText, keyword) });
            }
        }
        else if (ext == ".txt")
        {
            string[] lines = File.ReadAllLines(path);
            int lineNum = 1;
            foreach (string line in lines)
            {
                if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new ResultItem { Page = lineNum, Snippet = line.Trim() });
                }
                lineNum++;
            }
        }

        return results;
    }

    private string GetSnippet(string text, string keyword, int radius = 40)
    {
        int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "";

        int start = Math.Max(0, index - radius);
        int end = Math.Min(text.Length, index + keyword.Length + radius);
        return text.Substring(start, end - start).Replace("\n", " ").Replace("\r", " ").Trim();
    }
}

public class ResultItem
{
    public int Page { get; set; }
    public string Snippet { get; set; }
}