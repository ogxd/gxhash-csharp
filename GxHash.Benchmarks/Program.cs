using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

var config = ManualConfig.Create(DefaultConfig.Instance);
((List<ILogger>)config.GetLoggers()).Clear(); // BDN api... 🙄
config.AddLogger(new SummaryConsoleLogger());

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);