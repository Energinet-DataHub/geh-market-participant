using System.Data.SqlClient;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Dapper;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly ActorDbConfig _actorDbConfig;

        public OrganizationRepository(ActorDbConfig actorDbConfig)
        {
            _actorDbConfig = actorDbConfig;
        }

        public async Task SaveAsync(Organization organization)
        {
            await using var connection = new SqlConnection(_actorDbConfig.ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.InsertAsync(organization).ConfigureAwait(false);
        }
    }
}
