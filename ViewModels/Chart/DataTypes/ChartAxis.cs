namespace ViewModels.Chart.DataTypes;

public struct ChartAxis
{
    public Guid Id { get; }
    public string Name { get; }

    public ChartAxis(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}