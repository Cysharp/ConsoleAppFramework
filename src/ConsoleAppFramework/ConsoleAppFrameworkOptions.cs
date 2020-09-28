using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public class ConsoleAppFrameworkOptions
    {
        public bool StrictOption { get; set; }
        public bool ShowDefaultCommand { get; set; }

        public IConsoleAppFrameworkFilter[]? GlobalFilters { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ConsoleAppFrameworkFilterAttribute : Attribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public ConsoleAppFrameworkFilterAttribute(Type type)
        {
            this.Type = type;
        }
    }

    public interface IConsoleAppFrameworkFilter
    {
        public int Order { get; set; }
        ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next);
    }

    internal class FilterRunner
    {
        readonly IConsoleAppFrameworkFilter filter;
        readonly Func<ConsoleAppContext, ValueTask> next;

        public FilterRunner(IConsoleAppFrameworkFilter filter, Func<ConsoleAppContext, ValueTask> next)
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
        readonly object instance;
        readonly object[] invokeArgs;
        readonly IServiceProvider serviceProvider;
        readonly IConsoleAppFrameworkFilter[] globalFilters;
        readonly ConsoleAppContext context;

        object? invokeResult;

        public WithFilterInvoker(MethodInfo methodInfo, object instance, object[] invokeArgs, IServiceProvider serviceProvider, IConsoleAppFrameworkFilter[] globalFilters, ConsoleAppContext context)
        {
            this.methodInfo = methodInfo;
            this.instance = instance;
            this.invokeArgs = invokeArgs;
            this.serviceProvider = serviceProvider;
            this.globalFilters = globalFilters;
            this.context = context;
        }

        public async ValueTask<object?> InvokeAsync()
        {
            var list = new List<IConsoleAppFrameworkFilter>(globalFilters);

            var classFilters = methodInfo.DeclaringType.GetCustomAttributes<ConsoleAppFrameworkFilterAttribute>(true);
            var methodFilters = methodInfo.GetCustomAttributes<ConsoleAppFrameworkFilterAttribute>(true);
            foreach (var item in classFilters.Concat(methodFilters))
            {
                var filter = serviceProvider.GetRequiredService(item.Type) as IConsoleAppFrameworkFilter;
                if (filter != null)
                {
                    filter.Order = item.Order;
                    list.Add(filter);
                }
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
        ValueTask RunCore(ConsoleAppContext _)
        {
            var result = methodInfo.Invoke(instance, invokeArgs);
            invokeResult = result;
            return default;
        }
    }
}