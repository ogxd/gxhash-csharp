using System.Text.RegularExpressions;
using System.Threading;
using BenchmarkDotNet.Loggers;

/// <summary>
/// Only prints errors and end summary table
/// </summary>
public class SummaryConsoleLogger : ILogger
{
    private readonly ConsoleLogger _logger = new();
    private readonly Regex _regex = new(@"\/\/ \* [A-Za-z]* \*");
    private long _isSummarySection = 0;

    public void Write(LogKind logKind, string text)
    {
        TryWrite(logKind, text);
    }

    public void WriteLine()
    {
        if (Interlocked.Read(ref _isSummarySection) == 1)
        {
            _logger.WriteLine();
        }
    }

    public void WriteLine(LogKind logKind, string text)
    {
        if (TryWrite(logKind, text))
        {
            _logger.WriteLine();
        }
    }

    private bool TryWrite(LogKind logKind, string text)
    {
        var isSummarySection = _regex.IsMatch(text) ? Interlocked.Exchange(ref _isSummarySection, text == "// * Summary *" ? 1 : 0) : Interlocked.Read(ref _isSummarySection);

        if ((logKind == LogKind.Error && !text.StartsWith("Failed to set up high priority"))
          || logKind == LogKind.Help
          || (isSummarySection == 1 && logKind == LogKind.Statistic))
        {
            _logger.Write(logKind, text);
            return true;
        }

        //_logger.Write(logKind, "█");
        return false;
    }

    public void Flush() => _logger.Flush();

    public string Id => _logger.Id;
    public int Priority => _logger.Priority;
}