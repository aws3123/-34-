using ISO11820.App.Runtime.Controller;

namespace ISO11820.App.Runtime.Services;

public sealed class DaqWorker
{
    private readonly TestController _testController;

    public DaqWorker(TestController testController)
    {
        _testController = testController;
    }

    public void Start()
    {
        _testController.BroadcastInitialState();
    }
}
