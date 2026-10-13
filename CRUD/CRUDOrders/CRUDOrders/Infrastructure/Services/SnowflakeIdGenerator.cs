using IdGen;

namespace CRUDOrders.Infrastructure.Services;

public interface IIdGenerator
{
    long NewId();
}

public class SnowflakeIdGenerator : IIdGenerator
{
    private readonly IdGenerator _generator;

    public SnowflakeIdGenerator()
    {
        // Machine ID từ environment hoặc từ processor count
        int machineId = Environment.ProcessorCount % 32;
        _generator = new IdGenerator(machineId);
    }

    public long NewId() => _generator.CreateId();
}
