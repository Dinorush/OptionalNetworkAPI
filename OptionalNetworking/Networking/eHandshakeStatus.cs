namespace OptionalNetworking.Networking
{
    internal enum eHandshakeStatus : byte
    {
        Initiated,
        Received,
        Missed,
        CompleteButMissed,
        Completed,
        Failed
    }
}
