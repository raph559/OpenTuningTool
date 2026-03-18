using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace OpenTuningTool.Models;

public static class XdfEquationEvaluator
{
    private static readonly ConcurrentDictionary<string, ExpressionNode> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string? equation) =>
        string.IsNullOrWhiteSpace(equation) || TryGetExpression(equation, out _);

    public static bool IsIdentity(string? equation)
    {
        if (string.IsNullOrWhiteSpace(equation))
            return true;

        return TryGetExpression(equation, out ExpressionNode? expression) &&
               expression is VariableNode;
    }

    public static bool TryEvaluate(string? equation, double rawValue, out double result)
    {
        if (string.IsNullOrWhiteSpace(equation))
        {
            result = rawValue;
            return true;
        }

        if (!TryGetExpression(equation, out ExpressionNode? expression))
        {
            result = rawValue;
            return false;
        }

        try
        {
            result = expression.Evaluate(rawValue);
            return double.IsFinite(result);
        }
        catch
        {
            result = rawValue;
            return false;
        }
    }

    public static bool TryInvertDiscrete(string? equation, double displayValue, int bits, bool signed, out double rawValue)
    {
        rawValue = 0;

        if (string.IsNullOrWhiteSpace(equation) || IsIdentity(equation))
        {
            rawValue = displayValue;
            return true;
        }

        if (bits is not 8 and not 16)
            return false;

        if (!TryGetExpression(equation, out ExpressionNode? expression))
            return false;

        int minRaw = signed ? -(1 << (bits - 1)) : 0;
        int maxRaw = signed ? (1 << (bits - 1)) - 1 : (1 << bits) - 1;

        bool found = false;
        double bestDiff = double.MaxValue;
        int bestRaw = minRaw;

        for (int candidate = minRaw; candidate <= maxRaw; candidate++)
        {
            double mapped;
            try
            {
                mapped = expression.Evaluate(candidate);
            }
            catch
            {
                continue;
            }

            if (!double.IsFinite(mapped))
                continue;

            double diff = Math.Abs(mapped - displayValue);
            if (diff >= bestDiff)
                continue;

            bestDiff = diff;
            bestRaw = candidate;
            found = true;

            if (diff < 1e-12)
                break;
        }

        rawValue = bestRaw;
        return found;
    }

    private static bool TryGetExpression(string equation, [NotNullWhen(true)] out ExpressionNode? expression)
    {
        string normalized = Normalize(equation);
        if (Cache.TryGetValue(normalized, out ExpressionNode? cached))
        {
            expression = cached;
            return true;
        }

        expression = ParseOrNull(normalized);
        if (expression == null)
            return false;

        Cache.TryAdd(normalized, expression);
        return true;
    }

    private static string Normalize(string equation) =>
        new(equation.Where(ch => !char.IsWhiteSpace(ch)).ToArray());

    private static ExpressionNode? ParseOrNull(string equation)
    {
        try
        {
            return new Parser(equation).Parse();
        }
        catch
        {
            return null;
        }
    }

    private abstract class ExpressionNode
    {
        public abstract double Evaluate(double rawValue);
    }

    private sealed class ConstantNode(double value) : ExpressionNode
    {
        public override double Evaluate(double rawValue) => value;
    }

    private sealed class VariableNode : ExpressionNode
    {
        public static VariableNode Instance { get; } = new();

        private VariableNode() { }

        public override double Evaluate(double rawValue) => rawValue;
    }

    private sealed class UnaryMinusNode(ExpressionNode inner) : ExpressionNode
    {
        public override double Evaluate(double rawValue) => -inner.Evaluate(rawValue);
    }

    private sealed class BinaryNode(char op, ExpressionNode left, ExpressionNode right) : ExpressionNode
    {
        public override double Evaluate(double rawValue)
        {
            double lhs = left.Evaluate(rawValue);
            double rhs = right.Evaluate(rawValue);

            return op switch
            {
                '+' => lhs + rhs,
                '-' => lhs - rhs,
                '*' => lhs * rhs,
                '/' => lhs / rhs,
                _ => throw new InvalidOperationException($"Unsupported operator '{op}'."),
            };
        }
    }

    private sealed class Parser(string text)
    {
        private readonly string _text = text;
        private int _index;

        public ExpressionNode Parse()
        {
            if (string.IsNullOrWhiteSpace(_text))
                return VariableNode.Instance;

            ExpressionNode expression = ParseExpression();
            if (!IsAtEnd)
                throw new FormatException($"Unexpected token '{_text[_index]}' in equation '{_text}'.");

            return expression;
        }

        private ExpressionNode ParseExpression()
        {
            ExpressionNode node = ParseTerm();
            while (TryMatch('+') || TryMatch('-'))
            {
                char op = _text[_index - 1];
                ExpressionNode right = ParseTerm();
                node = new BinaryNode(op, node, right);
            }

            return node;
        }

        private ExpressionNode ParseTerm()
        {
            ExpressionNode node = ParseUnary();
            while (TryMatch('*') || TryMatch('/'))
            {
                char op = _text[_index - 1];
                ExpressionNode right = ParseUnary();
                node = new BinaryNode(op, node, right);
            }

            return node;
        }

        private ExpressionNode ParseUnary()
        {
            if (TryMatch('+'))
                return ParseUnary();

            if (TryMatch('-'))
                return new UnaryMinusNode(ParseUnary());

            return ParsePrimary();
        }

        private ExpressionNode ParsePrimary()
        {
            if (TryMatch('('))
            {
                ExpressionNode inner = ParseExpression();
                Expect(')');
                return inner;
            }

            if (!IsAtEnd && (_text[_index] == 'x' || _text[_index] == 'X'))
            {
                _index++;
                return VariableNode.Instance;
            }

            return ParseNumber();
        }

        private ExpressionNode ParseNumber()
        {
            int start = _index;
            bool hasDigits = false;

            while (!IsAtEnd && char.IsDigit(_text[_index]))
            {
                _index++;
                hasDigits = true;
            }

            if (!IsAtEnd && _text[_index] == '.')
            {
                _index++;
                while (!IsAtEnd && char.IsDigit(_text[_index]))
                {
                    _index++;
                    hasDigits = true;
                }
            }

            if (!hasDigits)
                throw new FormatException($"Expected a number at position {start} in '{_text}'.");

            if (!IsAtEnd && (_text[_index] == 'e' || _text[_index] == 'E'))
            {
                int exponentStart = _index++;
                if (!IsAtEnd && (_text[_index] == '+' || _text[_index] == '-'))
                    _index++;

                int exponentDigitsStart = _index;
                while (!IsAtEnd && char.IsDigit(_text[_index]))
                    _index++;

                if (exponentDigitsStart == _index)
                    throw new FormatException($"Invalid exponent at position {exponentStart} in '{_text}'.");
            }

            string token = _text[start.._index];
            double value = double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            return new ConstantNode(value);
        }

        private bool TryMatch(char expected)
        {
            if (IsAtEnd || _text[_index] != expected)
                return false;

            _index++;
            return true;
        }

        private void Expect(char expected)
        {
            if (!TryMatch(expected))
                throw new FormatException($"Expected '{expected}' in '{_text}'.");
        }

        private bool IsAtEnd => _index >= _text.Length;
    }
}
