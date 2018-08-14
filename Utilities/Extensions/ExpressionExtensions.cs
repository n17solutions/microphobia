using System;
using System.Linq;
using System.Linq.Expressions;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.Utilities.Expressions;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class ExpressionExtensions
    {
        public static TaskInfo ToTaskInfo<TExecutor>(this Expression<Action<TExecutor>> expression)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Action<TExecutor>>) methodCallArgumentResolutionVisitor.Visit(expression);

            var methodExpression = (MethodCallExpression) expressionWithArgumentsResolved?.Body;
            return methodExpression == null ? null : GetTaskInfo(methodExpression);
        }

        public static TaskInfo ToTaskInfo(this Expression<Action> expression)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Action>) methodCallArgumentResolutionVisitor.Visit(expression);

            var methodExpression = (MethodCallExpression) expressionWithArgumentsResolved?.Body;
            return methodExpression == null ? null : GetTaskInfo(methodExpression);
        }

        private static TaskInfo GetTaskInfo(MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments.Select(arg => ((ConstantExpression) arg).Value).ToArray();

            var taskInfo = method.ToTaskInfo(arguments);
            return taskInfo;
        }
    }
}