using System.Threading.Tasks;

namespace SH_Sharp.Interpreter
{
    public interface IInterpreter
    {
        Task<int> RunAsync(params string[] args);
    }
}