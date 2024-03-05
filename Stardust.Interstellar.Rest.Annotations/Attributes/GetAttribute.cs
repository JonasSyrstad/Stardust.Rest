using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Property)]
    public class GetAttribute : VerbAttribute
    {
        public GetAttribute():base("GET")
        {
        }

        public GetAttribute(string route) : base("GET",route)
        {
        }

	    public GetAttribute(string route, string serviceDescription) : base("GET", route, serviceDescription)
	    {
	    }
	}
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class LockAttribute : VerbAttribute
    {
        public LockAttribute() : base("LOCK")
        {
        }

        public LockAttribute(string route) : base("LOCK", route)
        {
        }

        public LockAttribute(string route, string serviceDescription) : base("LOCK", route, serviceDescription)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class UnLockAttribute : VerbAttribute
    {
        public UnLockAttribute() : base("UNLOCK")
        {
        }

        public UnLockAttribute(string route) : base("UNLOCK", route)
        {
        }

        public UnLockAttribute(string route, string serviceDescription) : base("UNLOCK", route, serviceDescription)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class MoveAttribute : VerbAttribute
    {
        public MoveAttribute() : base("MOVE")
        {
        }

        public MoveAttribute(string route) : base("MOVE", route)
        {
        }

        public MoveAttribute(string route, string serviceDescription) : base("MOVE", route, serviceDescription)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class CopyAttribute : VerbAttribute
    {
        public CopyAttribute() : base("COPY")
        {
        }

        public CopyAttribute(string route) : base("COPY", route)
        {
        }

        public CopyAttribute(string route, string serviceDescription) : base("COPY", route, serviceDescription)
        {
        }
    }
}