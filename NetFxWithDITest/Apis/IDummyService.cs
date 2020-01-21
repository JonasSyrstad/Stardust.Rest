using Stardust.Interstellar.Rest.Annotations;

namespace NetFxWithDITest.Apis
{
    [Api("api")]
    public interface IDummyService
    {
        [Get("dummy", "Test service.")]
        
        string Test([InPath]string message);
    }

    [Api("api")]
    public interface IDummyService2
    {
        [Get("dummy", "Test service.")]
        string Test([InPath]string message);
    }
    internal class DummyService : IDummyService
    {
        public string Test(string message)
        {
            return message;
        }
    }
}