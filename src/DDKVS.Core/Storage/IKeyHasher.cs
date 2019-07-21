namespace DDKVS.Core.Storage
{
    // These classes are just for starting out and examining the idea I am forming still.
    // Test classes as to the physical storage of files.


    public interface IKeyHasher
    {
        IKey ComputeHash(string key);
    }
}
