using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

using SH_Sharp.Interpreter;
using SH_Sharp.Shell;

namespace SH_Sharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var application = new CommandLineApplication(false);
            var file = application.Argument("file", "The SH# Script to execute.", false);
            var echo = application.Option("-e|--echo", "Whether commands should be echoed", CommandOptionType.NoValue);
            application.OnExecute(async () => {
                if (String.IsNullOrWhiteSpace(file.Value)) {
                    application.ShowHelp("file");
                    return 1;
                }

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton<ShellProvider>();
                serviceCollection.AddSingleton<ExecutionContext>();
                var provider = serviceCollection.BuildServiceProvider();

                var interpreter = (IInterpreter) ActivatorUtilities.CreateInstance(provider, typeof(CSharpInterpreter), file.Value, echo.HasValue());
                return await interpreter.RunAsync();
            });
            application.Execute(args);
        }
    }
}
