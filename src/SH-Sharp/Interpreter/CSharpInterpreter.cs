using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Linq;
using System.Diagnostics;
using SH_Sharp.Shell;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace SH_Sharp.Interpreter
{
    public class CSharpInterpreter : IInterpreter, IDisposable
    {
        private readonly string _file;
        private readonly bool _echo;
        private readonly ExecutionContext _context;
        private readonly Dictionary<string, IShell> _shells = new Dictionary<string, IShell>();

        public CSharpInterpreter(string file, bool echo, ExecutionContext context) {
            _file = file;
            _context = context;
        }

        public async Task<int> RunAsync(params string[] args) {
            // Open the file
            var script = File.ReadAllText(_file);

            // Replace <# #> tags
            var tagRegex = new Regex(@"<#([\S]*)([\s\S]+?)#>", RegexOptions.Multiline);
            var match = tagRegex.Match(script);
            while (match != null && match.Success) {
                var shellName = String.IsNullOrWhiteSpace(match.Groups[1].Value) ? "zsh" : match.Groups[1].Value;
                var commandString = match.Result($"Process(\"{shellName}\", $@\"{Escape(match.Groups[2].Value)}\").Wait();\n");
                script = script.Remove(match.Index, match.Length);
                script = script.Insert(match.Index, commandString);
                match = tagRegex.Match(script, match.Index + commandString.Length);
            }

            var scriptOptions = ScriptOptions.Default
                                .AddReferences(
                                    typeof(object).GetTypeInfo().Assembly,
                                    typeof(System.Linq.Enumerable).GetTypeInfo().Assembly,
                                    typeof(System.Dynamic.DynamicObject).GetTypeInfo().Assembly,  // System.Code
                                    typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).GetTypeInfo().Assembly,  // Microsoft.CSharp
                                    typeof(System.Dynamic.ExpandoObject).GetTypeInfo().Assembly)
                                .AddImports(
                                    "System.Dynamic",
                                    "System",
                                    "System.IO",
                                    "System.Collections.Generic",
                                    "System.Threading.Tasks"
                                );

            // TODO : Add to some IOption / Config
            // For debugging
            // File.WriteAllText("./tmp.cs", script);

            var scriptState = await CSharpScript.RunAsync(
                script,
                scriptOptions,
                globals: _context,
                globalsType: typeof(ExecutionContext));

            foreach (IShell shell in _shells.Values) {
                shell.Dispose();
            }

            return 0;
        }

        public void Dispose() {
        }

        private string Escape(string input) {
            input = input.Replace("\"", "\"\"");
            return input;
        }
    }
}
