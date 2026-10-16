using DocsAgent.Shared;

namespace Planner;

public sealed class PlannerService : IPlanner
{
    private readonly IPlanGenerator _generator;
    private readonly IAgentRunner _stepRunner;
    private readonly IAgentRunner _finalRunner;

    public PlannerService(
        IPlanGenerator generator,
        IAgentRunner stepRunner,
        IAgentRunner finalRunner)
    {
        _generator = generator;
        _stepRunner = stepRunner;
        _finalRunner = finalRunner;
    }

    public async Task<PlanResult> RunAsync(string question, CancellationToken ct = default)
    {
        Console.WriteLine($"[Planner/Run] Question: {question}");

        var gen = await _generator.GenerateAsync(question, ct);
        if (!gen.Success)
        {
            Console.WriteLine($"[Planner/Run] Plan failed: {gen.Error}");
            return new PlanResult(false, gen.Steps, Array.Empty<string>(), "", "Plan invalid: " + gen.Error);
        }

        var outputs = new List<string>();
        foreach (var step in gen.Steps)
        {
            ct.ThrowIfCancellationRequested();
            Console.WriteLine($"[Planner/Run] Step {step.Number}/{gen.Steps.Count}: {step.Description}");

            // Give the agent the original question + earlier results so it has context.
            var prompt = BuildStepPrompt(question, step, outputs);
            string output;
            try
            {
                output = await _stepRunner.AskAsync(prompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Planner/Run] Step {step.Number} threw: {ex.Message}");
                return new PlanResult(false, gen.Steps, outputs, "", $"Step {step.Number} failed: {ex.Message}");
            }

            Console.WriteLine($"[Planner/Run] Step {step.Number} output: {output}");
            outputs.Add(output);
        }

        var finalPrompt = BuildFinalPrompt(question, gen.Steps, outputs);
        string final;
        try
        {
            final = await _finalRunner.AskAsync(finalPrompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Planner/Run] Final answer threw: {ex.Message}");
            return new PlanResult(false, gen.Steps, outputs, "", "Final answer failed: " + ex.Message);
        }

        Console.WriteLine($"[Planner/Run] Final answer: {final}");
        return new PlanResult(true, gen.Steps, outputs, final, null);
    }

    private static string BuildStepPrompt(string question, PlanStep step, List<string> previous)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Original question: {question}");
        for (int i = 0; i < previous.Count; i++)
            sb.AppendLine($"Result of step {i + 1}: {previous[i]}");
        sb.AppendLine($"Now do ONLY this step: {step.Description}");
        if (step.ToolNeeded != null)
            sb.AppendLine($"Use the tool: {step.ToolNeeded}");
        return sb.ToString();
    }

    private static string BuildFinalPrompt(string question, IReadOnlyList<PlanStep> steps, List<string> outputs)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Question: {question}");
        for (int i = 0; i < steps.Count; i++)
            sb.AppendLine($"Step {steps[i].Number} ({steps[i].Description}) result: {outputs[i]}");
        sb.AppendLine("Write the final answer using only the results above. Do not call tools.");
        return sb.ToString();
    }
}