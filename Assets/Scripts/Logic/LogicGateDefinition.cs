using System;
using System.Collections.Generic;

/// <summary>Small, deterministic source of truth for every gate shown by LogicLab.</summary>
public enum LogicGateType
{
    And,
    Or,
    Not,
    Nand,
    Nor,
    Xor,
    Xnor
}

public static class LogicGateDefinition
{
    public static readonly LogicGateType[] All =
    {
        LogicGateType.And, LogicGateType.Or, LogicGateType.Not,
        LogicGateType.Nand, LogicGateType.Nor, LogicGateType.Xor,
        LogicGateType.Xnor
    };

    public static string Name(LogicGateType gate)
    {
        switch (gate)
        {
            case LogicGateType.And: return "AND";
            case LogicGateType.Or: return "OR";
            case LogicGateType.Not: return "NOT";
            case LogicGateType.Nand: return "NAND";
            case LogicGateType.Nor: return "NOR";
            case LogicGateType.Xor: return "XOR";
            default: return "XNOR";
        }
    }

    public static string Formula(LogicGateType gate)
    {
        switch (gate)
        {
            case LogicGateType.And: return "Y = A && B";
            case LogicGateType.Or: return "Y = A || B";
            case LogicGateType.Not: return "Y = !A";
            case LogicGateType.Nand: return "Y = !(A && B)";
            case LogicGateType.Nor: return "Y = !(A || B)";
            case LogicGateType.Xor: return "Y = A ^ B";
            default: return "Y = !(A ^ B)";
        }
    }

    public static bool Evaluate(LogicGateType gate, bool a, bool b)
    {
        switch (gate)
        {
            case LogicGateType.And: return a && b;
            case LogicGateType.Or: return a || b;
            case LogicGateType.Not: return !a;
            case LogicGateType.Nand: return !(a && b);
            case LogicGateType.Nor: return !(a || b);
            case LogicGateType.Xor: return a ^ b;
            default: return !(a ^ b);
        }
    }

    public static string[] CodeLines(LogicGateType gate)
    {
        switch (gate)
        {
            case LogicGateType.And: return new[] { "bool Evaluate(bool a, bool b)", "{", "    return a && b;", "}" };
            case LogicGateType.Or: return new[] { "bool Evaluate(bool a, bool b)", "{", "    return a || b;", "}" };
            case LogicGateType.Not: return new[] { "bool Evaluate(bool a)", "{", "    return !a;", "}" };
            case LogicGateType.Nand: return new[] { "bool Evaluate(bool a, bool b)", "{", "    return !(a && b);", "}" };
            case LogicGateType.Nor: return new[] { "bool Evaluate(bool a, bool b)", "{", "    return !(a || b);", "}" };
            case LogicGateType.Xor: return new[] { "bool Evaluate(bool a, bool b)", "{", "    return a ^ b;", "}" };
            default: return new[] { "bool Evaluate(bool a, bool b)", "{", "    return !(a ^ b);", "}" };
        }
    }
}

[Serializable]
public sealed class LogicLesson
{
    public string title;
    public string concept;
    public string example;
    public LogicGateType gate;

    public LogicLesson(string title, string concept, string example, LogicGateType gate)
    {
        this.title = title;
        this.concept = concept;
        this.example = example;
        this.gate = gate;
    }
}

public static class LogicLessonCatalog
{
    public static readonly LogicLesson[] Lessons =
    {
        new LogicLesson("01  |  Binary basics", "A logic circuit reads LOW (0) or HIGH (1). Each input switch supplies one binary value, and the output LED shows the result.", "Try every input combination in the Lab. A truth table is simply the complete set of those experiments.", LogicGateType.And),
        new LogicLesson("02  |  AND & OR", "AND needs both inputs to be 1. OR needs at least one input to be 1.", "Think of AND as two safety checks that must both pass, and OR as either of two valid ways to unlock a door.", LogicGateType.And),
        new LogicLesson("03  |  Inverting gates", "NOT flips one input. NAND and NOR apply AND or OR, then flip the result.", "A small output bubble on a gate symbol means NOT. NAND and NOR are universal building blocks.", LogicGateType.Nand),
        new LogicLesson("04  |  Exclusive logic", "XOR is 1 when inputs differ. XNOR is 1 when inputs match.", "XOR is useful for comparing bits and building adders. XNOR is useful for equality checks.", LogicGateType.Xor),
        new LogicLesson("05  |  Build & verify", "Choose a gate, set its switches, and compare the live output with its rule.", "Change one input at a time. Predict the LED first, then check the circuit and read the matching code.", LogicGateType.Xor)
    };
}

public struct LogicQuizQuestion
{
    public LogicGateType gate;
    public bool a;
    public bool b;
    public bool answer;

    public static List<LogicQuizQuestion> CreateBank()
    {
        var questions = new List<LogicQuizQuestion>();
        foreach (LogicGateType gate in LogicGateDefinition.All)
            for (int bits = 0; bits < 4; bits++)
            {
                bool a = (bits & 2) != 0;
                bool b = (bits & 1) != 0;
                questions.Add(new LogicQuizQuestion
                {
                    gate = gate, a = a, b = b,
                    answer = LogicGateDefinition.Evaluate(gate, a, b)
                });
            }
        return questions;
    }
}
