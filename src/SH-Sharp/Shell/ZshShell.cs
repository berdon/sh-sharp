using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace SH_Sharp.Shell
{
    public class ZshShell : IShell
    {
        private Process _process;

        public StreamWriter Input => _process.StandardInput;
        public StreamReader Output => _process.StandardOutput;
        public StreamReader Error => _process.StandardError;

        public Task StartAsync() {
            _process = new Process();
            _process.StartInfo.FileName = "/bin/zsh";
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.Start();

            return Task.CompletedTask;
        }

        public async Task<ExecutionResult> ExecuteAsync(string commands) {
            Console.WriteLine(commands);
            await Input.WriteLineAsync(commands);
            await Input.WriteLineAsync(@"echo ""DONE""");
            await Input.WriteLineAsync(@"env");
            await Input.WriteLineAsync(@"echo ""DONE""");
            await Input.WriteLineAsync(@"typeset");
            await Input.WriteLineAsync(@"echo ""DONE""");
            await Input.WriteLineAsync(@"pwd");
            await Input.WriteLineAsync(@"echo ""DONE""");
            await Input.FlushAsync();

            string outputLine;
            while((outputLine = await Output.ReadLineAsync()) != "DONE") {
                Console.WriteLine(outputLine);
            }
            
            // Read in environment variables
            var environmentVariables = new Dictionary<string, string>();
            while((outputLine = await Output.ReadLineAsync()) != "DONE") {
                var separator = outputLine.IndexOf('=');
                environmentVariables.Add(outputLine.Substring(0, separator), outputLine.Substring(separator + 1));
            }

            // Read in shell variables
            var builder = new StringBuilder();
            while((outputLine = await Output.ReadLineAsync()) != "DONE") {
                builder.Append(outputLine).Append("\r\n");
            }
            var shellVariableData = builder.ToString();

            // Parse the shell variables
            var shellVariables = ParseTypesetResponse(shellVariableData);

            // Read in shell variables
            builder = new StringBuilder();
            while((outputLine = await Output.ReadLineAsync()) != "DONE") {
                builder.Append(outputLine).Append("\r\n");
            }
            var currentWorkingDirectory = builder.ToString().Trim();

            return new ExecutionResult {
                Result = "",
                ResultCode = 0,
                Environment = environmentVariables,
                Variables = shellVariables,
                CurrentWorkingDirectory = currentWorkingDirectory
            };
        }

        private Dictionary<string, object> ParseTypesetResponse(string data) {
            var variables = new Dictionary<string, object>();
            int position = 0;
            while (position < data.Length) {
                var line = ReadToNext(data, ref position, true, '=', '\n');

                // Ignore non set statements
                if (position > 1 && data[position - 1] != '=') {
                    continue;
                }

                var words = SplitStrings(line);
                switch (words[0]) {
                    case "array":
                        ParseTypesetArray(variables, words, data, ref position);
                        break;
                    case "association":
                        ParseTypesetAssociation(variables, words, data, ref position);
                        break;
                    case "integer":
                        ParseTypesetInteger(variables, words, data, ref position);
                        break;
                    case "undefined":
                        ReadToNextLine(data, ref position);
                        break;
                    default:
                        ParseTypesetString(variables, words, data, ref position);
                        break;
                }
            }
            return variables;
        }

        private void ParseTypesetString(Dictionary<string, object> variables, string[] words, string data, ref int position) {
            var variableName = TrimString(words.LastOrDefault().Trim());
            if (String.IsNullOrWhiteSpace(variableName)) {
                ReadToNextLine(data, ref position);
                return;
            }
            variables[variableName] = TrimString(ReadToNextLine(data, ref position).Trim());
        }

        private void ParseTypesetArray(Dictionary<string, object> variables, string[] words, string data, ref int position) {
            var variableName = TrimString(words.LastOrDefault().Trim());
            if (String.IsNullOrWhiteSpace(variableName)) {
                ReadToNextLine(data, ref position);
                return;
            }

            variables[variableName] = SplitStrings(ReadToNextLine(data, ref position).Trim().Trim('(', ')'))
                .Select(s => TrimString(s))
                .ToArray();
        }

        private void ParseTypesetAssociation(Dictionary<string, object> variables, string[] words, string data, ref int position) {
            var variableName = TrimString(words.LastOrDefault().Trim());
            if (String.IsNullOrWhiteSpace(variableName)) {
                ReadToNextLine(data, ref position);
                return;
            }

            var dictionary = new Dictionary<string, object>();
            var keyValues = SplitStrings(ReadToNextLine(data, ref position).Trim()).Select(s => TrimString(s)).ToArray();
            for (var i = 0; i < keyValues.Length; i += 2) {
                dictionary[keyValues[i]] = keyValues[i + 1];
            }
            variables[variableName] = dictionary;
        }

        private void ParseTypesetInteger(Dictionary<string, object> variables, string[] words, string data, ref int position) {
            var variableName = TrimString(words.LastOrDefault().Trim());
            if (String.IsNullOrWhiteSpace(variableName)) {
                ReadToNextLine(data, ref position);
                return;
            }
            variables[variableName] = int.Parse(ReadToNextLine(data, ref position));
        }

        private string TrimString(string value) {
            if (String.IsNullOrWhiteSpace(value) || value.Length == 1) {
                return value;
            }

            if ((value[0] == '\'' && value[value.Length - 1] == '\'') ||
                (value[0] == '\"' && value[value.Length - 1] == '\"')) {
                    return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        private string ReadToNext(string data, ref int position, bool eatEnd, params char[] end) {
            var builder = new StringBuilder();
            char blit;
            while (position < data.Length && !end.Contains(blit = data[position++])) {
                builder.Append(blit);
            }
            if (!eatEnd) {
                position--;
            }
            return builder.ToString();
        }

        private string ReadToNextLine(string data, ref int position) {
            if (position >= data.Length) return null;

            var builder = new StringBuilder();
            int inString = -1;
            for (; position < data.Length; position++) {
                if (inString > -1) {
                    if (data[position] == inString && data[position-1] != '\\') {
                        inString = -1;
                        builder.Append(data[position]);
                        continue;
                    }
                } else if ((data[position] == '\'' || data[position] == '\"') &&
                            (position == 0 || data[position-1] != '\\')) {
                    inString = data[position];
                } else if (data[position] == '\n') {
                    position++;
                    break;
                }

                builder.Append(data[position]);
            }
            return builder.ToString().Trim();
        }

        private string NextWord(string data, ref int position) {
            return ReadToNext(data, ref position, true, ' ', '\n');
        }

        private string[] SplitStrings(string data) {
            var strings = new List<string>();
            int inString = -1;
            var builder = new StringBuilder();
            for (var i = 0; i < data.Length; i++) {
                if (inString > -1) {
                    if (data[i] == inString && data[i-1] != '\\') {
                        inString = -1;
                        builder.Append(data[i]);
                        continue;
                    }
                } else if ((data[i] == '\'' || data[i] == '\"') && (i == 0 || data[i-1] != '\\')) {
                    inString = data[i];
                } else if (data[i] == ' ') {
                    strings.Add(builder.ToString());
                    builder = new StringBuilder();
                    continue;
                }

                builder.Append(data[i]);
            }
            strings.Add(builder.ToString());
            return strings.ToArray();
        }

        public void Dispose() {
            // Nothing
        }
    }
}