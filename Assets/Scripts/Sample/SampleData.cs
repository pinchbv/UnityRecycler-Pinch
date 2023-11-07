using JetBrains.Annotations;

[UsedImplicitly]
public class SampleData
{
    public readonly string Name;
    public readonly string Description;
    public readonly int Code;

    public SampleData(string name, string description, int code)
    {
        Name = name;
        Description = description;
        Code = code;
    }
}