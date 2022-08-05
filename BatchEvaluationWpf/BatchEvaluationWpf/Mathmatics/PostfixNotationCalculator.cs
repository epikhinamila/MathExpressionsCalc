using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BatchEvaluationWpf.Mathmatics
{
    public class PostfixNotationCalculator
    {
        private readonly Stack<OperandToken> _operandTokensStack;
        public Action<OperatorType, TimeSpan> UpdateStatistics;

        public PostfixNotationCalculator()
        {
            _operandTokensStack = new Stack<OperandToken>();
        }

        public OperandToken Calculate(IEnumerable<IToken> tokens)
        {
            Reset();
            foreach (var token in tokens)
            {
                ProcessToken(token);
            }
            return GetResult();
        }

        private void Reset()
        {
            _operandTokensStack.Clear();
        }

        private void ProcessToken(IToken token)
        {
            switch (token)
            {
                case OperandToken operandToken:
                    StoreOperand(operandToken);
                    break;
                case OperatorToken operatorToken:
                    ApplyOperator(operatorToken);
                    break;
                default:
                    throw new SyntaxException($"An unknown token type: {token.GetType()}.");
            }
        }

        private void StoreOperand(OperandToken operandToken)
        {
            _operandTokensStack.Push(operandToken);
        }

        private void ApplyOperator(OperatorToken operatorToken)
        {
            switch (operatorToken.OperatorType)
            {
                case OperatorType.Addition:
                    ApplyAdditionOperator(OperatorType.Addition);
                    break;
                case OperatorType.Subtraction:
                    ApplySubtractionOperator(OperatorType.Subtraction);
                    break;
                case OperatorType.Multiplication:
                    ApplyMultiplicationOperator(OperatorType.Multiplication);
                    break;
                case OperatorType.UnaryMinus:
                    ApplyMultiplicationOperator(OperatorType.UnaryMinus);
                    break;
                case OperatorType.Division:
                    ApplyDivisionOperator(OperatorType.Division);
                    break;
                default:
                    throw new SyntaxException($"An unknown operator type: {operatorToken.OperatorType}.");
            }
        }

        private void ApplyAdditionOperator(OperatorType operatorType)
        {
            ApplyWithStopwatchMeasure((operand1, operand2) => new OperandToken(operand1.Value + operand2.Value), operatorType);
        }

        private void ApplySubtractionOperator(OperatorType operatorType)
        {
            ApplyWithStopwatchMeasure((operand1, operand2) => new OperandToken(operand1.Value - operand2.Value), operatorType);
        }

        private void ApplyMultiplicationOperator(OperatorType operatorType)
        {
            ApplyWithStopwatchMeasure((operand1, operand2) => new OperandToken(operand1.Value * operand2.Value), operatorType);
        }

        private void ApplyDivisionOperator(OperatorType operatorType)
        {
            ApplyWithStopwatchMeasure((operand1, operand2) => new OperandToken(operand1.Value / operand2.Value), operatorType);
        }

        private Tuple<OperandToken, OperandToken> GetBinaryOperatorArguments()
        {
            if (_operandTokensStack.Count < 2)
                throw new SyntaxException("Not enough arguments for applying a binary operator.");

            var right = _operandTokensStack.Pop();
            var left = _operandTokensStack.Pop();

            return Tuple.Create(left, right);
        }

        public void ApplyWithStopwatchMeasure(Func<OperandToken, OperandToken, OperandToken> func, OperatorType operatorType)
        {
            var operands = GetBinaryOperatorArguments();

            var timePernOperation = Stopwatch.StartNew();
            var result = func(operands.Item1, operands.Item2);
            timePernOperation.Stop();

            _operandTokensStack.Push(result);

            if (UpdateStatistics != null)
                UpdateStatistics(operatorType, timePernOperation.Elapsed);
        }

        private OperandToken GetResult()
        {
            if (_operandTokensStack.Count == 0)
                throw new SyntaxException("The expression is empty or invalid.");

            if (_operandTokensStack.Count != 1)
                throw new SyntaxException("The expression is invalid.");

            return _operandTokensStack.Pop();
        }
    }
}
