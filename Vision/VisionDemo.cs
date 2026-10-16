namespace DocsAgent.Vision;

// Test entry point: one image + one question, answer printed in green.
public static class VisionDemo
{
    private const string Tag = "[Vision/Demo]";

    public static async Task RunAsync(IImageDescriber describer, string imagePath, string question)
    {
        Console.WriteLine($"{Tag} Start");
        string answer = await describer.DescribeAsync(imagePath, question);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(answer);
        Console.ResetColor();
        Console.WriteLine($"{Tag} Done");
    }
}