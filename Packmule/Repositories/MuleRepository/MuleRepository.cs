using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Packmule.Repositories.MuleRepository
{
    public class MuleRepository : IMuleRepository
    {
        private readonly MuleContext _muleContext;
        public MuleRepository(MuleContext muleContext)
        {
            _muleContext = muleContext;
        }

        public async Task<IEnumerable<Notifications>> GetUsersNeedingNotification(IEnumerable<Line> potentialTargets)
        {
            // QOL: how do we handle multiple users with same names/hits?
            return await _muleContext.Notifications
                .Where(n => (n.LastMessaged == null
                        || n.LastMessaged > DateTime.UtcNow.AddDays(-1))
                    && potentialTargets.Any(line => line.Text.ToLower() == n.FullName.ToLower()))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpdateUsersSentNotification(IEnumerable<Notifications> usersToUpdate)
        {
            await Task.FromResult(true);
        }
    }
}
