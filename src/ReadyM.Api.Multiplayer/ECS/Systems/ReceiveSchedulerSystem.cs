using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Systems;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public class ReceiveSchedulerSystem(ILogger logger) : SchedulerSystemBase(logger);