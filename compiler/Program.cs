using cAlgo.API;

namespace cAlgo.Robots;

[Robot(AccessRights = AccessRights.None)]
public class TestBot : Robot
{
    protected override void OnStart()
    {
        Print("Hello from cTrader");
    }
}