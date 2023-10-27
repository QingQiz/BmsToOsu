namespace BmsToOsu;


internal class GenerationFailedException : Exception
{
    public readonly List<string> FailedList;

    public GenerationFailedException(IEnumerable<string> generationFailedList)
    {
        FailedList = generationFailedList.ToList();
    }
}

internal class BmsParserException : GenerationFailedException
{
    public BmsParserException(IEnumerable<string> generationFailedList) : base(generationFailedList)
    {
    }
}

internal class InvalidNoteConfigException : Exception
{
}

internal class SampleSetTooSmallException : Exception
{
}

internal class SampleRemixException : Exception
{
    public SampleRemixException(string message) : base(message)
    {
    }
}

internal class InvalidBmsFileException : Exception
{
}

