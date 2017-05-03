using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Dynamic;
using SH_Sharp.Helpers;

namespace SH_Sharp.Shell
{
    public class ExecutionContext : IDisposable
    {
        private readonly ShellProvider _shellProvider;
        private readonly Dictionary<string, IShell> _activeShells;
        private Dictionary<string, dynamic> _environment;
        private Dictionary<string, dynamic> _variables;
        public string CurrentWorkingDirectory { get; private set; }
        public string Path(string path) => System.IO.Path.Combine(CurrentWorkingDirectory, path);
        public dynamic Env => _environment.ToImmutableDictionary().ToExpando();
        public dynamic Var => _variables.ToImmutableDictionary().ToExpando();

        public ExecutionContext(ShellProvider shellProvider)
        {
            _shellProvider = shellProvider;
            _activeShells = new Dictionary<string, IShell>();
            _environment = new Dictionary<string, dynamic>();
            _variables = new Dictionary<string, dynamic>();
        }

        private async Task<IShell> LoadShell(string shellName) {
            if (!_activeShells.ContainsKey(shellName)) {
                // Load the shell
                var shell = _shellProvider.GetShell(shellName);
                if (shell == null) {
                    throw new Exception($"Unknown shell {shellName}");
                }

                // Store a reference to the active shell
                _activeShells[shellName] = shell;

                // Start the shell
                await shell.StartAsync();
            }

            return _activeShells[shellName];
        }

        public async Task Process(string shellName, string commands)
        {
            var shell = await LoadShell(shellName);

            var result = await shell.ExecuteAsync(commands);

            result.Environment?.ToList().ForEach(kvp =>
            {
                _environment[kvp.Key] = kvp.Value;
            });

            result.Variables?.ToList().ForEach(kvp =>
            {
                _variables[kvp.Key] = kvp.Value;
            });

            CurrentWorkingDirectory = result.CurrentWorkingDirectory;
        }

        public void Dispose() {
            foreach (var shell in _activeShells)
            {
                try { shell.Value?.Dispose(); } catch (Exception) { }
            }
        }
    }
}