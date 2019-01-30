using System.Threading.Tasks;
using Consumer.Domains.Models;

namespace Consumer.Domains.Services
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
