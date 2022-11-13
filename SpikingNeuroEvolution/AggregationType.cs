using System;
using System.Collections.Generic;
using System.Linq;

namespace SpikingNeuroEvolution
{
    record class AggregationType(string Name, Func<IEnumerable<double>, double> AggregateFunction)
    {
        public static readonly AggregationType Sum = new AggregationType("Sum", inputs => inputs.Sum());
        public static readonly AggregationType Multiply = new AggregationType("Multiply", inputs => inputs.Aggregate(1.0, (x, y) => x * y));
        public static readonly AggregationType Avg = new AggregationType("Avg", inputs => inputs.Average());
        public static readonly AggregationType Max = new AggregationType("Max", inputs => inputs.Max());
        public static readonly AggregationType Min = new AggregationType("Sin", inputs => inputs.Min());
        public static readonly AggregationType MaxAbs = new AggregationType("MaxAbs", inputs => inputs.Max(Math.Abs));
        public static readonly AggregationType MinAbs = new AggregationType("MinAbs", inputs => inputs.Min(Math.Abs));
    }
}
