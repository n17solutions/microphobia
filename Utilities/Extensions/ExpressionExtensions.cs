using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.Utilities.Expressions;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class ExpressionExtensions
    {
        public static TaskInfo ToTaskInfo<TExecutor>(this Expression<Action<TExecutor>> expression, IEnumerable<string> tags = default)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Action<TExecutor>>) methodCallArgumentResolutionVisitor.Visit(expression);

            var methodExpression = (MethodCallExpression) expressionWithArgumentsResolved?.Body;
            return methodExpression == null ? null : GetTaskInfo(methodExpression, tags);
        }

        public static TaskInfo ToTaskInfo(this Expression<Action> expression, IEnumerable<string> tags = default)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Action>) methodCallArgumentResolutionVisitor.Visit(expression);

            var methodExpression = (MethodCallExpression) expressionWithArgumentsResolved?.Body;
            return methodExpression == null ? null : GetTaskInfo(methodExpression, tags);
        }
        
        public static TaskInfo ToTaskInfo(this Expression<Func<Task>> expression, IEnumerable<string> tags = default)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Func<Task>>) methodCallArgumentResolutionVisitor.Visit(expression);

            var methodExpression = (MethodCallExpression) expressionWithArgumentsResolved?.Body;
            return methodExpression == null ? null : GetTaskInfo(methodExpression, tags);
        }

        public static TaskInfo ToTaskInfo<TExecutor>(this Expression<Func<TExecutor, Task>> expression, IEnumerable<string> tags = default)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Func<TExecutor, Task>>) methodCallArgumentResolutionVisitor.Visit(expression);

            var methodExpression = (MethodCallExpression) expressionWithArgumentsResolved?.Body;
            return methodExpression == null ? null : GetTaskInfo(methodExpression, tags);
        }

        private static TaskInfo GetTaskInfo(MethodCallExpression expression, IEnumerable<string> tags = default)
        {
            var method = expression.Method;
            var arguments = expression.Arguments.Select(arg => ((ConstantExpression) arg).Value).ToArray();

            var taskInfo = method.ToTaskInfo(arguments);
            taskInfo.Tags = tags as string[] ?? tags?.ToArray();
            
            return taskInfo;
        }
    }
}