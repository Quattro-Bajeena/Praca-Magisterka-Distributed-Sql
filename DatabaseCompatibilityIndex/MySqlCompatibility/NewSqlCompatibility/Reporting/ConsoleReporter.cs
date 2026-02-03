namespace NSCI.Reporting;

public class ConsoleReporter
{
    public void ReportTestStart(string testName, string description, string category)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[{category}] ");
        Console.ResetColor();
        Console.WriteLine($"Running: {testName}");
        Console.WriteLine($"  Description: {description}");
    }

    public void ReportTestEnd(Testing.TestResult result)
    {
        if (result.Passed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✓ PASS");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("✗ FAIL");
            Console.ResetColor();
        }

        Console.WriteLine($" ({result.Duration.TotalMilliseconds:F2}ms)");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Error: {result.ErrorMessage}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public void ReportSummary(int total, int passed, int failed)
    {
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("TEST SUMMARY");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Total tests: {total}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed: {passed}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Failed: {failed}");
        Console.ResetColor();

        double successRate = total > 0 ? (passed * 100.0 / total) : 0;
        Console.WriteLine($"Success rate: {successRate:F2}%");
        Console.WriteLine(new string('=', 60));
    }
}
