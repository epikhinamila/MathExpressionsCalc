using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BatchEvaluationWpf.Mathmatics
{
    public class Tokenizer
    {
        private static readonly char[] OperatorCharacter = new char[] { '(', ')', '+', '-', '*', '/' };
        private readonly StringBuilder _valueTokenBuilder;
        private readonly List<IToken> _infixNotationTokens;

        public Tokenizer()
        {
            _valueTokenBuilder = new StringBuilder();
            _infixNotationTokens = new List<IToken>();
        }

        public IEnumerable<IToken> Parse(string expression)
        {
            Reset();
            foreach (char next in expression)
            {
                FeedCharacter(next);
            }

            if (_valueTokenBuilder.Length > 0)
                _infixNotationTokens.Add(GetOperandToken(_valueTokenBuilder));

            return _infixNotationTokens;
        }

        private void Reset()
        {
            _valueTokenBuilder.Clear();
            _infixNotationTokens.Clear();
        }

        private void FeedCharacter(char next)
        {
            if (IsSpacingCharacter(next))
            {
                if (_valueTokenBuilder.Length > 0)
                    _infixNotationTokens.Add(GetOperandToken(_valueTokenBuilder));
            }
            else if (IsOperatorCharacter(next))
            {
                if (_valueTokenBuilder.Length > 0)
                    _infixNotationTokens.Add(GetOperandToken(_valueTokenBuilder));

                if (IsUnaryMinus(next))
                {
                    _infixNotationTokens.Add(new OperandToken(-1));
                    _infixNotationTokens.Add(new OperatorToken(OperatorType.UnaryMinus));
                }
                else
                {
                    _infixNotationTokens.Add(CreateOperatorToken(next));
                }
            }
            else
            {
                _valueTokenBuilder.Append(next);
            }
        }

        private static bool IsOperatorCharacter(char c) => c switch
        {
            var x when OperatorCharacter.Contains(x) => true,
            _ => false
        };

        private static bool IsSpacingCharacter(char c) => c switch
        {
            ' ' => true,
            _ => false,
        };

        private IToken GetOperandToken(StringBuilder valueTokenBuilder)
        {
            var token = CreateOperandToken(valueTokenBuilder.ToString());
            valueTokenBuilder.Clear();
            return token;
        }

        private static IToken CreateOperandToken(string raw)
        {
            if (long.TryParse(raw, out long result))
            {
                return new OperandToken(result);
            }

            throw new SyntaxException($"The operand {raw} has an invalid format.");
        }

        private static OperatorToken CreateOperatorToken(char c)
        {
            return c switch
            {
                '(' => new OperatorToken(OperatorType.OpeningBracket),
                ')' => new OperatorToken(OperatorType.ClosingBracket),
                '+' => new OperatorToken(OperatorType.Addition),
                '-' => new OperatorToken(OperatorType.Subtraction),
                '*' => new OperatorToken(OperatorType.Multiplication),
                '/' => new OperatorToken(OperatorType.Division),
                _ => throw new SyntaxException($"There's no a suitable operator for the char {c}"),
            };
        }

        private bool IsUnaryMinus(char c)
        {
            return (c == '-') &&
                ((_infixNotationTokens.Count == 0) ||
                (_infixNotationTokens.Last() is OperatorToken));
        }
    }
}
