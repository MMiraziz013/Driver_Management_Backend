namespace Clean.Application.Abstractions;

public interface IDataSeeder
{
    public Task SeedAsync();
}