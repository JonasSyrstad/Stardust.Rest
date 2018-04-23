using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    public abstract class VerbAttribute : Attribute
    {
        public string Route { get; set; }

        protected VerbAttribute()
        {
            
        }

        protected VerbAttribute(string verb)
        {
            Verb = verb;
        }

        protected VerbAttribute(string verb,string route)
        {
            Route = route;
            Verb = verb;
        }
        public string Verb { get;  set; }
    }
}