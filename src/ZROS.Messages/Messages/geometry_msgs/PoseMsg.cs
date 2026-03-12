using ZROS.Core;

namespace ZROS.Messages.geometry_msgs
{
    public class PoseMsg : IMessage
    {
        public string MessageType => "geometry_msgs/Pose";
        public Point Position { get; set; } = new Point();
        public Quaternion Orientation { get; set; } = new Quaternion();

        public PoseMsg() { }

        public PoseMsg(Point position, Quaternion orientation)
        {
            Position = position;
            Orientation = orientation;
        }

        public class Point
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Point() { }
            public Point(double x, double y, double z) { X = x; Y = y; Z = z; }
        }

        public class Quaternion
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double W { get; set; }

            public Quaternion() { }
            public Quaternion(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }
        }
    }
}
