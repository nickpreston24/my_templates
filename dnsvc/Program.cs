using System;
using System.Threading;


namespace dnsvc
{
  class Program
  {
    static void Main(
      string[] args)
    {
      var sleep = 3000;
      if (args.Length > 0) { int.TryParse(args[0], out sleep); }
      while (true)
      {
        Console.WriteLine($"Working, pausing for {sleep}ms");
        Thread.Sleep(sleep);
      }
    }
  }
}
