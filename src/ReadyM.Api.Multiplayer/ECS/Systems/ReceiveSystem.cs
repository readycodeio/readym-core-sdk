using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Systems;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

internal class ReceiveSystem(ILogger logger) : SchedulerSystemBase(logger);