namespace ZROS.Core
{
    public class RosNodeOptions
    {
        public string Namespace { get; set; } = "/";
        public bool UseSim { get; set; } = false;
        public int DomainId { get; set; } = 0;
    }
}
