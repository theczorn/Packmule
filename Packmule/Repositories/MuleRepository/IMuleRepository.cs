using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Packmule.Repositories.MuleRepository
{
    public interface IMuleRepository
    {
        Task<IEnumerable<Notifications>> GetUsersNeedingNotification(IEnumerable<Line> potentialTargets);
        Task UpdateUsersSentNotification(IEnumerable<Notifications> usersToUpdate);
    }
}