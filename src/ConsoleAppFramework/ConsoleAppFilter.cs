using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public abstract class ConsoleAppFilter
    {
        public int Order { get; set; }
        public abstract ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next);
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ConsoleAppFilterAttribute : Attribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public ConsoleAppFilterAttribute(Type type)
        {
            this.Type = type;
        }
    }

    internal class FilterRunner
    {
        readonly ConsoleAppFilter filter;
        readonly Func<ConsoleAppContext, ValueTask> next;

        public FilterRunner(ConsoleAppFilter filter, Func<ConsoleAppContext, ValueTask> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Func<ConsoleAppContext, ValueTask> GetDelegate() => InvokeAsync;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ValueTask InvokeAsync(ConsoleAppContext context)
        {
            return filter.Invoke(context, next);
        }
    }

    internal class WithFilterInvoker
    {
        readonly MethodInfo methodInfo;
        readonly object? instance;
        readonly object?[] invokeArgs;
        readonly IServiceProvider serviceProvider;
        readonly ConsoleAppFilter[] globalFilters;
        readonly ConsoleAppContext context;

        int? invokeResult;

        public WithFilterInvoker(MethodInfo methodInfo, object? instance, object?[] invokeArgs, IServiceProvider serviceProvider, ConsoleAppFilter[] globalFilters, ConsoleAppContext context)
        {
            this.methodInfo = methodInfo;
            this.instance = instance;
            this.invokeArgs = invokeArgs;
            this.serviceProvider = serviceProvider;
            this.globalFilters = globalFilters;
            this.context = context;
        }

        public async ValueTask<int?> InvokeAsync()
        {
            var list = new List<ConsoleAppFilter>(globalFilters);

            var classFilters = methodInfo.DeclaringType!.GetCustomAttributes<ConsoleAppFilterAttribute>(true);
            var methodFilters = methodInfo.GetCustomAttributes<ConsoleAppFilterAttribute>(true);
            foreach (var item in classFilters.Concat(methodFilters))
            {
                var filter = (ConsoleAppFilter) ActivatorUtilities.CreateInstance(serviceProvider, item.Type);
                filter.Order = item.Order;
                list.Add(filter);
            }

            var sortedAndReversedFilters = list.OrderBy(x => x.Order).Reverse().ToArray();

            Func<ConsoleAppContext, ValueTask> next = RunCore;
            foreach (var f in sortedAndReversedFilters)
            {
                next = new FilterRunner(f, next).GetDelegate();
            }

            await next(context);
            return invokeResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async ValueTask RunCore(ConsoleAppContext _)
        {
            var result = methodInfo.Invoke(instance, invokeArgs);
            if (result != null)
            {
                switch (result)
                {
                    case int exitCode:
                        invokeResult = exitCode;
                        break;
                    case Task<int> taskWithExitCode:
                        invokeResult = await taskWithExitCode;
                        break;
                    case Task task:
                        await task;
                        break;
                    case ValueTask<int> valueTaskWithExitCode:
                        invokeResult = await valueTaskWithExitCode;
                        break;
                    case ValueTask valueTask:
                        await valueTask;
                        break;
                }
            }
        }
    }
}