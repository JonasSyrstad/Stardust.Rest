namespace Stardust.Interstellar.Rest.Annotations.Cache
{
    public class ETagHandler : ICacheHelper
    {
        private ICacheHelper implementation;

        public string GetEtag(string itemId)
        {
            return implementation.GetEtag(itemId);
        }

        public void SetImplementation(ICacheHelper instance)
        {
            implementation = instance;
        }


    }
}