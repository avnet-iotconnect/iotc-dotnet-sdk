using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace iotdotnetsdk.common.Internals.Dynamic
{
    internal static class DynamicExpression
    {
        public static Expression Parse(Type resultType, string expression, params object[] values)
        {
            ExpressionParser parser = new ExpressionParser(null, expression, values);
            return parser.Parse(resultType);
        }

        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, List<string> identifiers, params object[] values)
        {
            return ParseLambda(new ParameterExpression[] { Expression.Parameter(itType, "") }, resultType, expression, identifiers, values);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type resultType, string expression, List<string> identifiers, params object[] values)
        {
            ExpressionParser parser = new ExpressionParser(parameters, expression, values);
            LambdaExpression le = Expression.Lambda(parser.Parse(resultType), parameters);
            if (identifiers != null) identifiers.AddRange(parser.Identifiers);
            return le;
        }

        public static Expression<Func<T, S>> ParseLambda<T, S>(string expression, params object[] values)
        {
            return (Expression<Func<T, S>>)ParseLambda(typeof(T), typeof(S), expression, null, values);
        }

        public static Type CreateClass(params DynamicProperty[] properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }

        public static Type CreateClass(IEnumerable<DynamicProperty> properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }
    }

}
