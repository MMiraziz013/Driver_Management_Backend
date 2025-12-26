using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ITripRepository
{
    Task AddAsync(Trip trip);
}