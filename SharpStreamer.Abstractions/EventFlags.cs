namespace SharpStreamer.Abstractions;

[Flags]
public enum EventFlags
{
    Succeeded = 1, // 2 ^ 0 => 00000001
    Failed = 2,    // 2 ^ 1 => 00000010
    Sent = 4,      // 2 ^ 2 => 00000100
}