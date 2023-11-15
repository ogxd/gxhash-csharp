using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace GxHash.Benchmarks;

[AttributeUsage(AttributeTargets.Class)]
public class ThroughputAttribute : Attribute, IConfigSource
{
    public IConfig Config { get; }

    public ThroughputAttribute(bool displayGenColumns = true)
    {
        Config = ManualConfig.CreateEmpty().AddColumn(new ThroughputColumn());
    }
}

public class ThroughputColumn : IColumn
{
    public string Id => "Throughput";

    public string ColumnName => "Thoughput (MiB/s)";

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Dimensionless;

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return GetValue(summary, benchmarkCase);
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var report = summary.Reports.FirstOrDefault(x => x.BenchmarkCase == benchmarkCase);
        if (report?.ResultStatistics == null)
        {
            return "N/A";
        }
        
        int inputSize = 1;
        var parameter = benchmarkCase.Parameters.Items.FirstOrDefault(x => x.Value is string);
        if (parameter != null && parameter.Value is string str)
        {
            inputSize = str.Length * sizeof(char);
        }

        double coeff = (summary.Style?.TimeUnit?.NanosecondAmount ?? 1d) * 1_000_000_000d / (1024d * 1024d);
        coeff *= inputSize;

        double mean = report.ResultStatistics.Mean;
        double meanThroughput = coeff / mean;

        double errorThroughput = coeff / report.ResultStatistics.ConfidenceInterval.Lower - coeff / report.ResultStatistics.ConfidenceInterval.Upper;

        return $"{meanThroughput:F2} ± {errorThroughput:F2}";
    }

    public string Legend => "Throughput";
}
