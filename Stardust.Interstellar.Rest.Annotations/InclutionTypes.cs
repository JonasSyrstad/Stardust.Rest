namespace Stardust.Interstellar.Rest.Annotations
{
    public enum InclusionTypes
    {
        Path,
        Query,
        Body, 
        Header,
        /// <summary>
        /// Parameters marked as Ignore are not sent over the wire.
        /// Used for CancellationToken parameters which are handled locally.
        /// Do not use this inclusion type directly, the framework manages it automatically.
        /// </summary>
        Ignore
    }
}