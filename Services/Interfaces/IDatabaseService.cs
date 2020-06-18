using Microsoft.EntityFrameworkCore;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Services.Interfaces
{
    public interface IDatabaseService
    {
        UnitPlanContext Context { get; }

        void SetOptions(DbContextOptions<UnitPlanContext> dbOptions);
    }
}
