using Stardust.Interstellar.Rest.Annotations;

namespace NetFxWithDITest.Apis
{
    [IRoutePrefix("api/dummy")]
    public interface IDummyService
    {
        [Get("","Test service.")]
        
        string Test([InQuery]string message);
    }

    internal class DummyService : IDummyService
    {
        public string Test(string message)
        {
            return message;
        }
    }
}