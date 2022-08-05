using System.Collections.Generic;

namespace BatchEvaluationWpf.Mathmatics
{
    public class ShuntingYardAlgorithm
    {
        private readonly Stack<OperatorToken> _operatorsStack;
        private readonly List<IToken> _postfixNotationTokens;

        public ShuntingYardAlgorithm()
        {
            _operatorsStack = new Stack<OperatorToken>();
            _postfixNotationTokens = new List<IToken>();
        }

        public IEnumerable<IToken> Apply(IEnumerable<IToken> infixNotationTokens)
        {
            Reset();
            foreach (var token in infixNotationTokens)
            {
                ProcessToken(token);
            }
            
            GrabOperatorsFromStack();

            return _postfixNotationTokens;
        }

        private void Reset()
        {
            _operatorsStack.Clear();
            _postfixNotationTokens.Clear();
        }

        private void ProcessToken(IToken token)
        {
            switch (token)
            {
                case OperandToken operandToken:
                    StoreOperand(operandToken);
                    break;
                case OperatorToken operatorToken:
                    ProcessOperator(operatorToken);
                    break;
                default:
                    throw new SyntaxException($"An unknown token type: {token.GetType()}.");
            }
        }

        private void StoreOperand(OperandToken operandToken)
        {
            _postfixNotationTokens.Add(operandToken);
        }

        private void ProcessOperator(OperatorToken operatorToken)
        {
            switch (operatorToken.OperatorType)
            {
                case OperatorType.OpeningBracket:
                    PushOpeningBracket(operatorToken);
                    break;
                case OperatorType.ClosingBracket:
                    PushClosingBracket();
                    break;
                default:
                    PushOperator(operatorToken);
                    break;
            }
        }

        private void PushOpeningBracket(OperatorToken operatorToken)
        {
            _operatorsStack.Push(operatorToken);
        }

        private void PushClosingBracket()
        {
            bool openingBracketFound = false;

            while (_operatorsStack.Count > 0)
            {
                var stackOperatorToken = _operatorsStack.Pop();
                if (stackOperatorToken.OperatorType == OperatorType.OpeningBracket)
                {
                    openingBracketFound = true;
                    break;
                }

                _postfixNotationTokens.Add(stackOperatorToken);
            }

            if (!openingBracketFound)
            {
                throw new SyntaxException("An unexpected closing bracket.");
            }
        }

        private void PushOperator(OperatorToken operatorToken)
        {
            var operatorPriority = GetOperatorPriority(operatorToken);

            while (_operatorsStack.Count > 0)
            {
                var stackOperatorToken = _operatorsStack.Peek();
                if (stackOperatorToken.OperatorType == OperatorType.OpeningBracket)
                {
                    break;
                }

                var stackOperatorPriority = GetOperatorPriority(stackOperatorToken);
                if (stackOperatorPriority < operatorPriority)
                {
                    break;
                }

                _postfixNotationTokens.Add(_operatorsStack.Pop());
            }

            _operatorsStack.Push(operatorToken);
        }

        private static int GetOperatorPriority(OperatorToken operatorToken)
        {
            switch (operatorToken.OperatorType)
            {
                case OperatorType.OpeningBracket:
                    return 0;
                case OperatorType.Addition:
                case OperatorType.Subtraction:
                    return 1;
                case OperatorType.Multiplication:
                case OperatorType.Division:
                    return 2;
                case OperatorType.UnaryMinus:
                    return 3;
                default:
                    throw new SyntaxException($"An unexpected action for the operator: {operatorToken.OperatorType}.");
            }
        }

        private void GrabOperatorsFromStack()
        {
            while (_operatorsStack.Count > 0)
            {
                var token = _operatorsStack.Pop();
                if (token.OperatorType == OperatorType.OpeningBracket)
                {
                    throw new SyntaxException("A redundant opening bracket.");
                }
                _postfixNotationTokens.Add(token);
            }
        }
    }
}
