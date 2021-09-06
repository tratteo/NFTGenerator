namespace NFTGenerator
{
    /// <summary>
    ///   The metadata for each tag
    /// </summary>
    [System.Serializable]
    internal class Metadata
    {
        public string Id { get; init; }

        public string Description { get; init; }

        public int Amount { get; init; }

        public override string ToString()
        {
            return "{ Id: " + Id + ", Description: " + Description + ", Amount: " + Amount + "}";
        }
    }
}