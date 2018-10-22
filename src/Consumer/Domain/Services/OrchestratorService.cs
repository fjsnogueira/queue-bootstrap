using Consumer.Domain.Models;
using System.Threading.Tasks;

namespace Consumer.Domain.Services
{
    public interface IOrchestratorService
    {
        Task OrchestrateAsync(Message message);
    }

    public class OrchestratorService : IOrchestratorService
    {
        public async Task OrchestrateAsync(Message message)
        {
        }
    }
}
