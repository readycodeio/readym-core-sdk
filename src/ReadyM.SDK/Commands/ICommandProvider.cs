using System.ComponentModel;

namespace ReadyM.SDK.Commands;
 
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ICommandProvider
{
    void RegisterCommands(CommandRegistry registry);
}
