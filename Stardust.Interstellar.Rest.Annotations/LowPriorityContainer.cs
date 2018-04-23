using System;

namespace Stardust.Interstellar.Rest.Extensions
{
    class LowPriorityContainer
    {
        public StateDictionary StateReference { get; set; }

        public LowPriorityContainer()
        {
            Created = DateTime.UtcNow;
        }

        public DateTime Created { get; private set; }
    }
}