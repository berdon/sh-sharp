using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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
        private readonly IServiceProvider _services;
        private readonly Dictionary<string, IShell> _shells = new Dictionary<string, IShell>();

        public CSharpInterpreter(string file, bool echo, IServiceProvider services) {
            _file = file;
            _services = services;
        }

        public async Task<int> RunAsync(params string[] args) {
            // Open the file
            var script = File.ReadAllText(_file);

            // Replace <# #> tags
            var tagRegex = new Regex(@"<#([\S]*)([\s\S]+?)#>", RegexOptions.Multiline);
            var match = tagRegex.Match(script);
            while (match != null && match.Success) {
                var shellName = String.IsNullOrWhiteSpace(match.Groups[1].Value) ? "zsh" : match.Groups[1].Value;
                if (!_shells.ContainsKey(shellName)) {
                    await LoadShell(shellName);
                }

                var commandString = match.Result($"await Process(\"{shellName}\", $@\"{Escape(match.Groups[2].Value)}\");\n");
                script = script.Remove(match.Index, match.Length);
                script = script.Insert(match.Index, commandString);

                match = tagRegex.Match(script, match.Index + commandString.Length);
            }

            var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default;
            var mscorlib = typeof(object).GetTypeInfo().Assembly;
            var systemCore = typeof(System.Linq.Enumerable).GetTypeInfo().Assembly;
            scriptOptions = scriptOptions.AddReferences(new[] { mscorlib, systemCore });
            var scriptState = await CSharpScript.RunAsync(
                script,
                scriptOptions,
                globals: new Context(_shells),
                globalsType: typeof(Context));

            foreach (IShell shell in _shells.Values) {
                shell.Dispose();
            }

            return 0;
        }

        // TODO : Abstract outside of interpreter implementation
        public class Context {
            private readonly Dictionary<string, IShell> _shells;
            public string CurrentWorkingDirectory { get; private set; }
            public string Path(string path) => System.IO.Path.Combine(CurrentWorkingDirectory, path);
            private Dictionary<string, string> _environment;
            public IDictionary<string, string> Env => _environment.ToImmutableDictionary();
            private Dictionary<string, object> _variables;
            public IDictionary<string, object> Var => _variables.ToImmutableDictionary();

            public Context(Dictionary<string, IShell> shells) {
                _shells = shells;
                _environment = new Dictionary<string, string>();
                _variables = new Dictionary<string, object>();
            }

            public async Task Process(string shellName, string commands) {
                if (!_shells.ContainsKey(shellName)) {
                    throw new Exception("Invalid shell");
                }

                var result = await _shells[shellName].ExecuteAsync(commands);

                result.Environment?.ToList().ForEach(kvp => {
                    _environment[kvp.Key] = kvp.Value;
                });

                result.Variables?.ToList().ForEach(kvp => {
                    _variables[kvp.Key] = kvp.Value;
                });

                CurrentWorkingDirectory = result.CurrentWorkingDirectory;
            }
        }

        private async Task LoadShell(string name) {
            if (name.Equals("zsh")) {
                var shell = (ZshShell) ActivatorUtilities.CreateInstance(_services, typeof(ZshShell));
                await shell.StartAsync();
                _shells.Add(name, shell);
            }
        }

        public void Dispose() {
        }

        private string Escape(string input) {
            input = input.Replace("\"", "\"\"");
            return input;
        }
    }
}
