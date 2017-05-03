using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace SH_Sharp.Shell
{
    public class ShellProvider : IDisposable
    {
        private readonly IServiceProvider _services;

        public ShellProvider(IServiceProvider services) {
            _services = services;
        }

        public IShell GetShell(string shellType) {
            // TODO: Not this
            switch (shellType) {
                case "zsh":
                    return (IShell) ActivatorUtilities.CreateInstance(_services, typeof(ZshShell));
            }

            return null;
        }

        public void Dispose() {
            // Nothing
        }
    }
}