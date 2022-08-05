namespace BatchEvaluationWpf.Mathmatics
{
    public class OperandToken : IToken
    {
        public long Value { get; }

        public OperandToken(long value)
        {
            Value = value;
        }
    }
}
