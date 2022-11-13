using System;
using static System.Math;

namespace SpikingNeuroEvolution
{
    record class FunctionType(string Name, Func<double, double> NodeFunc)
    {
        public static readonly FunctionType Sin = new FunctionType("Sin", Math.Sin);
        public static readonly FunctionType Identity = new FunctionType("Identity", x => x);
        public static readonly FunctionType Heaviside = new FunctionType("Heaviside", x => x > 0 ? 1 : 0);
        public static readonly FunctionType Exponent = new FunctionType("Exponent", Exp);
        public static readonly FunctionType Log = new FunctionType("Log", x => x <= 0 ? 0 : Log(x));
        public static readonly FunctionType Sigmoid = new FunctionType("Sigmoid", x => 1 / (1 + Exp(-4.9 * x)));

        public override string ToString() => Name;
    }
}
