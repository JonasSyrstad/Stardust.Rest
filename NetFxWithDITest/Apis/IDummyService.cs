using Stardust.Interstellar.Rest.Annotations;

namespace NetFxWithDITest.Apis
{
    [IRoutePrefix("api/dummy")]
    public interface IDummyService
    {
        [Get]
        [IRoute("")]
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