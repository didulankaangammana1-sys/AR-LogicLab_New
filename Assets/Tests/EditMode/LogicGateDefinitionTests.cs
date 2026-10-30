using NUnit.Framework;

public sealed class LogicGateDefinitionTests
{
    [TestCase(LogicGateType.And, false, false, false)]
    [TestCase(LogicGateType.And, false, true, false)]
    [TestCase(LogicGateType.And, true, false, false)]
    [TestCase(LogicGateType.And, true, true, true)]
    [TestCase(LogicGateType.Or, false, false, false)]
    [TestCase(LogicGateType.Or, false, true, true)]
    [TestCase(LogicGateType.Or, true, false, true)]
    [TestCase(LogicGateType.Or, true, true, true)]
    [TestCase(LogicGateType.Not, false, false, true)]
    [TestCase(LogicGateType.Not, true, false, false)]
    [TestCase(LogicGateType.Nand, true, true, false)]
    [TestCase(LogicGateType.Nor, false, false, true)]
    [TestCase(LogicGateType.Xor, false, true, true)]
    [TestCase(LogicGateType.Xor, true, true, false)]
    [TestCase(LogicGateType.Xnor, false, false, true)]
    [TestCase(LogicGateType.Xnor, true, false, false)]
    public void Evaluate_MatchesTruthTable(LogicGateType gate, bool a, bool b, bool expected)
    {
        Assert.That(LogicGateDefinition.Evaluate(gate, a, b), Is.EqualTo(expected));
    }

    [Test]
    public void QuizBank_ContainsEveryGateAndInputCombination()
    {
        var bank = LogicQuizQuestion.CreateBank();
        Assert.That(LogicGateDefinition.All, Has.Length.EqualTo(7));
        Assert.That(bank, Has.Count.EqualTo(28));
        foreach (LogicGateType gate in LogicGateDefinition.All)
            Assert.That(bank.FindAll(question => question.gate == gate), Has.Count.EqualTo(4));
    }

    [Test]
    public void Course_HasFiveLessonsAndEveryGateHasReadableCode()
    {
        Assert.That(LogicLessonCatalog.Lessons, Has.Length.EqualTo(5));
        foreach (LogicGateType gate in LogicGateDefinition.All)
        {
            Assert.That(LogicGateDefinition.Name(gate), Is.Not.Empty);
            Assert.That(LogicGateDefinition.Formula(gate), Is.Not.Empty);
            Assert.That(LogicGateDefinition.CodeLines(gate), Has.Length.EqualTo(4));
        }
    }
}
