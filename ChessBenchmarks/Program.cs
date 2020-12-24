using System;
using BenchmarkDotNet.Running;

namespace ChessBenchmarks
{
	class Program
	{
		static void Main(string[] args) {
			BenchmarkRunner.Run(typeof(MoveGenBenchmark));
		}
	}
}
