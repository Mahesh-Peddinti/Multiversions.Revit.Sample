using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace MEPClashResolution.Models
{
    /// <summary>
    /// Represents a 3D point in space with double precision
    /// </summary>
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3D(XYZ revitPoint)
        {
            X = revitPoint.X;
            Y = revitPoint.Y;
            Z = revitPoint.Z;
        }

        public XYZ ToXYZ() => new XYZ(X, Y, Z);

        /// <summary>
        /// Calculate Euclidean distance to another point
        /// </summary>
        public double DistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";

        public override bool Equals(object obj)
        {
            if (obj is Point3D p)
                return Math.Abs(X - p.X) < 0.01 && Math.Abs(Y - p.Y) < 0.01 && Math.Abs(Z - p.Z) < 0.01;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(X.GetHashCode(), Y.GetHashCode(), Z.GetHashCode());
    }

    /// <summary>
    /// Represents an axis-aligned bounding box for spatial queries
    /// </summary>
    public class BoundingBox
    {
        public Point3D Min { get; set; }
        public Point3D Max { get; set; }

        public BoundingBox(Point3D min, Point3D max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Check if this bounding box intersects with another
        /// </summary>
        public bool IntersectsWith(BoundingBox other)
        {
            return !(Max.X < other.Min.X || Min.X > other.Max.X ||
                     Max.Y < other.Min.Y || Min.Y > other.Max.Y ||
                     Max.Z < other.Min.Z || Min.Z > other.Max.Z);
        }

        /// <summary>
        /// Expand the bounding box by a clearance amount in all directions
        /// </summary>
        public BoundingBox ExpandByClearance(double clearance)
        {
            return new BoundingBox(
                new Point3D(Min.X - clearance, Min.Y - clearance, Min.Z - clearance),
                new Point3D(Max.X + clearance, Max.Y + clearance, Max.Z + clearance)
            );
        }

        /// <summary>
        /// Get center point of bounding box
        /// </summary>
        public Point3D GetCenter()
        {
            return new Point3D(
                (Min.X + Max.X) / 2,
                (Min.Y + Max.Y) / 2,
                (Min.Z + Max.Z) / 2
            );
        }
    }

    /// <summary>
    /// Enumeration for fitting types used in conduit/tray routing
    /// </summary>
    public enum FittingType
    {
        Straight,
        Elbow45,
        Elbow90,
        Tee90,
        Tee45,
        Reducer,
        Custom
    }

    /// <summary>
    /// Specification for a fitting type with constraints
    /// </summary>
    public class FittingSpec
    {
        public string Name { get; set; }
        public FittingType Type { get; set; }
        public double MinBendRadius_mm { get; set; }
        public List<double> AllowedBendAngles_deg { get; set; } // e.g., [45, 90, 135]
        public double Cost { get; set; } // Path cost multiplier
        public string Description { get; set; }

        public FittingSpec(string name, FittingType type, double bendRadius, 
            List<double> allowedAngles, double cost = 1.0)
        {
            Name = name;
            Type = type;
            MinBendRadius_mm = bendRadius;
            AllowedBendAngles_deg = allowedAngles ?? new List<double> { 0 };
            Cost = cost;
        }

        /// <summary>
        /// Validate if a bend angle is allowed for this fitting
        /// </summary>
        public bool IsValidBendAngle(double angle, double tolerance = 5.0)
        {
            return AllowedBendAngles_deg.Any(a => Math.Abs(angle - a) <= tolerance);
        }
    }

    /// <summary>
    /// Represents a single route segment (conduit or cable tray section)
    /// </summary>
    public class RouteSegment
    {
        public string RouteId { get; set; }
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public double Diameter_mm { get; set; }
        public double RequiredClearance_mm { get; set; }
        public FittingType FittingType { get; set; }
        public double BendRadius_mm { get; set; }
        public string Material { get; set; }
        public ElementId RevitElementId { get; set; }
        public string ElementType { get; set; } // "Conduit" or "CableTray"

        public RouteSegment(
            string routeId,
            Point3D start,
            Point3D end,
            double diameter,
            double clearance,
            FittingType fitting = FittingType.Straight
        )
        {
            RouteId = routeId;
            StartPoint = start;
            EndPoint = end;
            Diameter_mm = diameter;
            RequiredClearance_mm = clearance;
            FittingType = fitting;
            Material = "Steel";
            ElementType = "Conduit";
        }

        /// <summary>
        /// Get the length of this segment
        /// </summary>
        public double GetLength() => StartPoint.DistanceTo(EndPoint);

        /// <summary>
        /// Get bounding box with clearance buffer
        /// </summary>
        public BoundingBox GetBoundingBoxWithClearance()
        {
            double expandVal = Diameter_mm / 2 + RequiredClearance_mm;
            double minX = Math.Min(StartPoint.X, EndPoint.X) - expandVal;
            double maxX = Math.Max(StartPoint.X, EndPoint.X) + expandVal;
            double minY = Math.Min(StartPoint.Y, EndPoint.Y) - expandVal;
            double maxY = Math.Max(StartPoint.Y, EndPoint.Y) + expandVal;
            double minZ = Math.Min(StartPoint.Z, EndPoint.Z) - expandVal;
            double maxZ = Math.Max(StartPoint.Z, EndPoint.Z) + expandVal;

            return new BoundingBox(
                new Point3D(minX, minY, minZ),
                new Point3D(maxX, maxY, maxZ)
            );
        }

        public override string ToString() => $"Route: {RouteId} | {ElementType} {Diameter_mm}mm | {StartPoint} → {EndPoint}";
    }

    /// <summary>
    /// Represents a waypoint (node) in a calculated route
    /// </summary>
    public class Waypoint
    {
        public Point3D Position { get; set; }
        public FittingType FittingType { get; set; }
        public List<int> ConnectedWaypointIndices { get; set; }
        public double CumulativeCost { get; set; }
        public Waypoint Parent { get; set; }

        public Waypoint(Point3D position, FittingType fitting = FittingType.Straight)
        {
            Position = position;
            FittingType = fitting;
            ConnectedWaypointIndices = new List<int>();
            CumulativeCost = double.MaxValue;
            Parent = null;
        }

        /// <summary>
        /// Reconstruct the path from this waypoint back to start
        /// </summary>
        public List<Waypoint> ReconstructPath()
        {
            var path = new List<Waypoint> { this };
            Waypoint current = this;
            while (current.Parent != null)
            {
                path.Insert(0, current.Parent);
                current = current.Parent;
            }
            return path;
        }

        public override string ToString() => $"WP: {Position} | Fitting: {FittingType} | Cost: {CumulativeCost:F2}";
    }

    /// <summary>
    /// Represents a clash detection result
    /// </summary>
    public enum ClashSeverity
    {
        Critical,   // Physical intersection (0mm clearance)
        Major,      // Clearance < minimum required
        Minor,      // Clearance acceptable but suboptimal
        Resolved    // Alternative route found
    }

    public class ClashInfo
    {
        public string ClashId { get; set; }
        public RouteSegment Route1 { get; set; }
        public RouteSegment Route2 { get; set; }
        public Point3D ClashLocation { get; set; }
        public double ActualClearance_mm { get; set; }
        public double RequiredClearance_mm { get; set; }
        public ClashSeverity Severity { get; set; }
        public List<Waypoint> ResolvedRoute { get; set; }
        public DateTime DetectedTime { get; set; }
        public string ResolutionStatus { get; set; } // "Detected", "Processing", "Resolved", "Unresolvable"

        public ClashInfo(string id, RouteSegment route1, RouteSegment route2, Point3D location)
        {
            ClashId = id;
            Route1 = route1;
            Route2 = route2;
            ClashLocation = location;
            DetectedTime = DateTime.Now;
            ResolutionStatus = "Detected";
            ResolvedRoute = new List<Waypoint>();
        }

        /// <summary>
        /// Classify clash severity based on clearance
        /// </summary>
        public void ClassifySeverity()
        {
            if (ActualClearance_mm < 0.1)
                Severity = ClashSeverity.Critical;
            else if (ActualClearance_mm < RequiredClearance_mm)
                Severity = ClashSeverity.Major;
            else
                Severity = ClashSeverity.Minor;
        }

        public override string ToString() => 
            $"Clash {ClashId}: {Route1.RouteId} ↔ {Route2.RouteId} | Severity: {Severity} | Clearance: {ActualClearance_mm:F2}mm";
    }

    /// <summary>
    /// Configuration container for clash detection and resolution
    /// </summary>
    public class ClashResolutionConfig
    {
        public double MinClearance_mm { get; set; } = 50.0;
        public double SearchResolution_mm { get; set; } = 100.0;
        public bool EnableSolidIntersection { get; set; } = true;
        public int MaxPathfindingIterations { get; set; } = 10000;
        
        public double CostWeight_Distance { get; set; } = 1.0;
        public double CostWeight_BendPenalty { get; set; } = 0.5;
        public double CostWeight_ClearancePenalty { get; set; } = 2.0;
        public double CostWeight_MaterialChange { get; set; } = 1.5;

        public string ColorClash { get; set; } = "255,0,0";      // Red
        public string ColorResolved { get; set; } = "0,255,0";   // Green
    }

    /// <summary>
    /// Summary report of clash resolution operation
    /// </summary>
    public class ResolutionReport
    {
        public int TotalClashesDetected { get; set; }
        public int ClashesResolved { get; set; }
        public int ClashesUnresolvable { get; set; }
        public List<ClashInfo> ClashDetails { get; set; }
        public double TotalProcessingTime_sec { get; set; }
        public double TotalRouteDistance_mm { get; set; }
        public DateTime ReportGeneratedTime { get; set; }

        public ResolutionReport()
        {
            ClashDetails = new List<ClashInfo>();
            ReportGeneratedTime = DateTime.Now;
        }

        /// <summary>
        /// Generate a summary string
        /// </summary>
        public override string ToString()
        {
            return $@"
═══════════════════════════════════════════════════════════
  MEP CLASH RESOLUTION REPORT
═══════════════════════════════════════════════════════════
  Total Clashes Detected:    {TotalClashesDetected}
  Clashes Resolved:          {ClashesResolved}
  Clashes Unresolvable:      {ClashesUnresolvable}
  Success Rate:              {(ClashesResolved * 100.0 / Math.Max(1, TotalClashesDetected)):F1}%
  
  Processing Time:           {TotalProcessingTime_sec:F2} seconds
  Total Route Distance:      {TotalRouteDistance_mm:F0} mm
  Generated:                 {ReportGeneratedTime:yyyy-MM-dd HH:mm:ss}
═══════════════════════════════════════════════════════════
";
        }
    }
}
