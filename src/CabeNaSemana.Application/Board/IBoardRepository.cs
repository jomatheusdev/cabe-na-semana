using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Application.Board;

public interface IBoardRepository
{
    Task<IReadOnlyList<StudyTask>> ListAsync(CancellationToken cancellationToken);
    Task<StudyTask?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(StudyTask task, CancellationToken cancellationToken);
    void Remove(StudyTask task);
    Task<decimal> GetWeeklyCapacityAsync(CancellationToken cancellationToken);
    Task SetWeeklyCapacityAsync(decimal hours, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
