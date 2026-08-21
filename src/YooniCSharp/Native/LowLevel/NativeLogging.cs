using Microsoft.Extensions.Logging;

namespace Yooni.Native.LowLevel;

public static class NativeLogging
{
    public static ILogger Logger = new DefaultNativeLogger();

    public static void SetupLogging(ILogger logger)
    {
        Logger = logger;
    }
}
