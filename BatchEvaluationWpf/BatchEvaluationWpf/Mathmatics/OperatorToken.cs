namespace BatchEvaluationWpf.Mathmatics
{
    public class OperatorToken : IToken
    {
        public OperatorType OperatorType { get; }

        public OperatorToken(OperatorType operatorType)
        {
            OperatorType = operatorType;
        }
    }
}
