using System.Threading.Tasks;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Services.Interfaces
{
    public interface ICurriculumImportService
    {
        bool IoError { get; }

        Task<Curriculum> ImportAsync(string fileName);
    }
}
