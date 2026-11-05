using Microsoft.Extensions.Logging;

namespace ReadyM.Native.Logging;

public static class NativeLogging
{
    public static ILogger Logger = new DefaultNativeLogger();

    public static void SetupLogging(ILogger logger)
    {
        Logger = logger;
    }
}
