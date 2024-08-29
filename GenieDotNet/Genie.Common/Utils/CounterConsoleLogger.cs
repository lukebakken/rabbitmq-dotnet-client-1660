
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Timers;

namespace Genie.Common.Utils;


public class CounterConsoleLogger
{
    public static DateTime? Started { get; set; }
    public static int Counter { get; set; }
    public static DateTime? BatchStarted { get; set; }
    public static int BatchCounter { get; set; }
    private static int ErrorCounter { get; set; }

    public void Process()
    {
        var counter = CounterConsoleLogger.Counter;
        Interlocked.Increment(ref counter);
        CounterConsoleLogger.Counter = counter;

        var batchCounter = CounterConsoleLogger.BatchCounter;
        Interlocked.Increment(ref batchCounter);
        CounterConsoleLogger.BatchCounter = batchCounter;

        if (CounterConsoleLogger.Started == null)
            CounterConsoleLogger.Started = DateTime.UtcNow;

        if (CounterConsoleLogger.BatchStarted == null)
            CounterConsoleLogger.BatchStarted = DateTime.UtcNow;

        if (counter % 25000 == 0)
        {
            ProcessInt(counter, batchCounter);
        }
    }

    public void ProcessError()
    {
        var counter = CounterConsoleLogger.ErrorCounter;
        Interlocked.Increment(ref counter);
        CounterConsoleLogger.ErrorCounter = counter;
    }

    private static void ProcessInt(int counter, int batchCounter)
    {
        var elapsed = (DateTime.UtcNow - CounterConsoleLogger.Started!.Value).TotalSeconds;
        var batchElapsed = (DateTime.UtcNow - CounterConsoleLogger.BatchStarted!.Value).TotalSeconds;
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($@"[{DateTime.Now.ToLongTimeString()}] Overall {Convert.ToInt32(counter / elapsed)}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($@" Period: {Convert.ToInt32(batchCounter / batchElapsed)}");
        if (CounterConsoleLogger.ErrorCounter > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($@" Errors: {CounterConsoleLogger.ErrorCounter}");
        }
        else
            Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        CounterConsoleLogger.ResetBatch();
    }

    public static void ResetBatch()
    {
        BatchStarted = DateTime.UtcNow;
        BatchCounter = 0;
    }
}