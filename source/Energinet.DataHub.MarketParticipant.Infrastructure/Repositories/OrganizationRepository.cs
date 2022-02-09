using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        public Task SaveAsync(Organization organization)
        {
            throw new System.NotImplementedException();
        }
    }
}
