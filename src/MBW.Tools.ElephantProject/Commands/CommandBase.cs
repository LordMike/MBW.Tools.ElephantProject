using System.Threading.Tasks;

namespace MBW.Tools.ElephantProject.Commands
{
    abstract class CommandBase
    {
        public abstract Task<int> Execute();
    }
}