using Clean.Application.Abstractions;
using Clean.Domain.Entities;

namespace ClassLibrary1.Data.Repositories;

public class TripRepository : ITripRepository
{
    private readonly DataContext _context;

    public TripRepository(DataContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(Trip trip)
    {
        await _context.Trips.AddAsync(trip);
    }
}