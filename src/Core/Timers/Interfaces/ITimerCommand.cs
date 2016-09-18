using System.Threading.Tasks;

namespace Core.Timers.Interfaces
{
	public interface ITimerCommand
	{
		Task Execute();
	}
}