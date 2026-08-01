namespace InfoTrack.Data.Repositories;

public interface IDBRepository
{

}

public sealed class DBRepository : IDBRepository
{
    private readonly InfoTrackDbContext _dbContext;

    public DBRepository(InfoTrackDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    
}
