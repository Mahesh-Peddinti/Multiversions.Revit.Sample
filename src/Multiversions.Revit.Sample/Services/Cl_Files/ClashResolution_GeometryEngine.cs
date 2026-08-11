using System;
using System.Collections.Generic;
using System.Linq;
using MEPClashResolution.Models;

namespace MEPClashResolution.Geometry
{
    /// <summary>
    /// Handles all geometric calculations and spatial queries for clash detection
    /// </summary>
    public static class GeometryEngine
    {
        /// <summary>
        /// Calculate the minimum distance between two line segments in 3D space
        /// Uses the algorithm for distance between two skew lines
        /// </summary>
        public static double DistanceBetweenSegments(
            Point3D p1Start, Point3D p1End,
            Point3D p2Start, Point3D p2End)
        {
            Point3D d1 = new Point3D(p1End.X - p1Start.X, p1End.Y - p1Start.Y, p1End.Z - p1Start.Z);
            Point3D d2 = new Point3D(p2End.X - p2Start.X, p2End.Y - p2Start.Y, p2End.Z - p2Start.Z);
            Point3D w = new Point3D(p1Start.X - p2Start.X, p1Start.Y - p2Start.Y, p1Start.Z - p2Start.Z);

            double a = DotProduct(d1, d1);
            double b = DotProduct(d1, d2);
            double c = DotProduct(d2, d2);
            double d = DotProduct(d1, w);
            double e = DotProduct(d2, w);

            double denom = a * c - b * b;
            double sc, tc;

            if (Math.Abs(denom) < 1e-6)
            {
                sc = 0.0;
                tc = Math.Abs(b) > 1e-6 ? d / b : 0.0;
            }
            else
            {
                sc = (b * e - c * d) / denom;
                tc = (a * e - b * d) / denom;
            }

            // Clamp to [0, 1] to stay within segments
            sc = Math.Max(0, Math.Min(1, sc));
            tc = Math.Max(0, Math.Min(1, tc));

            Point3D closestPoint1 = new Point3D(
                p1Start.X + sc * d1.X,
                p1Start.Y + sc * d1.Y,
                p1Start.Z + sc * d1.Z
            );

            Point3D closestPoint2 = new Point3D(
                p2Start.X + tc * d2.X,
                p2Start.Y + tc * d2.Y,
                p2Start.Z + tc * d2.Z
            );

            return closestPoint1.DistanceTo(closestPoint2);
        }

        /// <summary>
        /// Calculate dot product of two 3D vectors
        /// </summary>
        private static double DotProduct(Point3D v1, Point3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        /// <summary>
        /// Calculate cross product of two 3D vectors
        /// </summary>
        public static Point3D CrossProduct(Point3D v1, Point3D v2)
        {
            return new Point3D(
                v1.Y * v2.Z - v1.Z * v2.Y,
                v1.Z * v2.X - v1.X * v2.Z,
                v1.X * v2.Y - v1.Y * v2.X
            );
        }

        /// <summary>
        /// Calculate the magnitude (length) of a 3D vector
        /// </summary>
        public static double VectorMagnitude(Point3D v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        /// <summary>
        /// Normalize a 3D vector to unit length
        /// </summary>
        public static Point3D NormalizeVector(Point3D v)
        {
            double mag = VectorMagnitude(v);
            if (Math.Abs(mag) < 1e-6) return new Point3D(0, 0, 0);
            return new Point3D(v.X / mag, v.Y / mag, v.Z / mag);
        }

        /// <summary>
        /// Calculate angle between two 3D vectors in degrees
        /// </summary>
        public static double AngleBetweenVectors(Point3D v1, Point3D v2)
        {
            double dotProd = DotProduct(v1, v2);
            double mag1 = VectorMagnitude(v1);
            double mag2 = VectorMagnitude(v2);

            if (mag1 < 1e-6 || mag2 < 1e-6) return 0;

            double cosAngle = dotProd / (mag1 * mag2);
            cosAngle = Math.Max(-1, Math.Min(1, cosAngle)); // Clamp to [-1, 1]
            return Math.Acos(cosAngle) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Find the closest point on a line segment to a given point
        /// </summary>
        public static Point3D ClosestPointOnSegment(Point3D point, Point3D segStart, Point3D segEnd)
        {
            Point3D segVec = new Point3D(segEnd.X - segStart.X, segEnd.Y - segStart.Y, segEnd.Z - segStart.Z);
            Point3D pointVec = new Point3D(point.X - segStart.X, point.Y - segStart.Y, point.Z - segStart.Z);

            double segLenSq = DotProduct(segVec, segVec);
            if (segLenSq < 1e-6) return segStart;

            double t = DotProduct(pointVec, segVec) / segLenSq;
            t = Math.Max(0, Math.Min(1, t));

            return new Point3D(
                segStart.X + t * segVec.X,
                segStart.Y + t * segVec.Y,
                segStart.Z + t * segVec.Z
            );
        }

        /// <summary>
        /// Check if a point is within a cylindrical volume (used for conduit/tray collision)
        /// </summary>
        public static bool IsPointInCylinder(Point3D point, Point3D cylinderStart, Point3D cylinderEnd, double radius)
        {
            Point3D closest = ClosestPointOnSegment(point, cylinderStart, cylinderEnd);
            return point.DistanceTo(closest) <= radius;
        }

        /// <summary>
        /// Check if a point is inside or on the surface of a bounding box
        /// </summary>
        public static bool IsPointInBoundingBox(Point3D point, BoundingBox bbox)
        {
            return point.X >= bbox.Min.X && point.X <= bbox.Max.X &&
                   point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y &&
                   point.Z >= bbox.Min.Z && point.Z <= bbox.Max.Z;
        }

        /// <summary>
        /// Find the closest point between two line segments
        /// Returns both points as a tuple
        /// </summary>
        public static (Point3D point1, Point3D point2) ClosestPointsBetweenSegments(
            Point3D p1Start, Point3D p1End,
            Point3D p2Start, Point3D p2End)
        {
            Point3D d1 = new Point3D(p1End.X - p1Start.X, p1End.Y - p1Start.Y, p1End.Z - p1Start.Z);
            Point3D d2 = new Point3D(p2End.X - p2Start.X, p2End.Y - p2Start.Y, p2End.Z - p2Start.Z);
            Point3D w = new Point3D(p1Start.X - p2Start.X, p1Start.Y - p2Start.Y, p1Start.Z - p2Start.Z);

            double a = DotProduct(d1, d1);
            double b = DotProduct(d1, d2);
            double c = DotProduct(d2, d2);
            double d = DotProduct(d1, w);
            double e = DotProduct(d2, w);

            double denom = a * c - b * b;
            double sc, tc;

            if (Math.Abs(denom) < 1e-6)
            {
                sc = 0.0;
                tc = Math.Abs(b) > 1e-6 ? d / b : 0.0;
            }
            else
            {
                sc = (b * e - c * d) / denom;
                tc = (a * e - b * d) / denom;
            }

            sc = Math.Max(0, Math.Min(1, sc));
            tc = Math.Max(0, Math.Min(1, tc));

            Point3D cp1 = new Point3D(
                p1Start.X + sc * d1.X,
                p1Start.Y + sc * d1.Y,
                p1Start.Z + sc * d1.Z
            );

            Point3D cp2 = new Point3D(
                p2Start.X + tc * d2.X,
                p2Start.Y + tc * d2.Y,
                p2Start.Z + tc * d2.Z
            );

            return (cp1, cp2);
        }

        /// <summary>
        /// Interpolate a point along a line segment by parameter t [0, 1]
        /// </summary>
        public static Point3D InterpolateAlongSegment(Point3D start, Point3D end, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return new Point3D(
                start.X + t * (end.X - start.X),
                start.Y + t * (end.Y - start.Y),
                start.Z + t * (end.Z - start.Z)
            );
        }

        /// <summary>
        /// Divide a line segment into multiple waypoints at specified intervals
        /// Useful for creating pathfinding nodes
        /// </summary>
        public static List<Point3D> DivideSegmentIntoWaypoints(Point3D start, Point3D end, double intervalDistance)
        {
            var waypoints = new List<Point3D> { start };
            
            double totalDistance = start.DistanceTo(end);
            if (totalDistance < intervalDistance)
            {
                waypoints.Add(end);
                return waypoints;
            }

            int numIntervals = (int)Math.Ceiling(totalDistance / intervalDistance);
            for (int i = 1; i < numIntervals; i++)
            {
                double t = (double)i / numIntervals;
                waypoints.Add(InterpolateAlongSegment(start, end, t));
            }

            waypoints.Add(end);
            return waypoints;
        }

        /// <summary>
        /// Calculate the total path length from a list of waypoints
        /// </summary>
        public static double CalculatePathLength(List<Waypoint> waypoints)
        {
            if (waypoints.Count < 2) return 0;
            
            double length = 0;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                length += waypoints[i].Position.DistanceTo(waypoints[i + 1].Position);
            }
            return length;
        }

        /// <summary>
        /// Smooth a path by reducing sharp angles (simple linear interpolation smoothing)
        /// </summary>
        public static List<Waypoint> SmoothPath(List<Waypoint> waypoints, double smoothingFactor = 0.3)
        {
            if (waypoints.Count < 3) return waypoints;

            var smoothedPath = new List<Waypoint> { waypoints[0] };

            for (int i = 1; i < waypoints.Count - 1; i++)
            {
                Point3D prev = waypoints[i - 1].Position;
                Point3D curr = waypoints[i].Position;
                Point3D next = waypoints[i + 1].Position;

                // Weighted average position
                Point3D smoothed = new Point3D(
                    prev.X * smoothingFactor + curr.X * (1 - 2 * smoothingFactor) + next.X * smoothingFactor,
                    prev.Y * smoothingFactor + curr.Y * (1 - 2 * smoothingFactor) + next.Y * smoothingFactor,
                    prev.Z * smoothingFactor + curr.Z * (1 - 2 * smoothingFactor) + next.Z * smoothingFactor
                );

                smoothedPath.Add(new Waypoint(smoothed, waypoints[i].FittingType));
            }

            smoothedPath.Add(waypoints[waypoints.Count - 1]);
            return smoothedPath;
        }

        /// <summary>
        /// Check if a point is obstructed (inside any of the given obstacle bounding boxes)
        /// </summary>
        public static bool IsPointObstructed(Point3D point, List<BoundingBox> obstacles)
        {
            return obstacles.Any(obstacle => IsPointInBoundingBox(point, obstacle));
        }

        /// <summary>
        /// Build a 3D grid of waypoints for pathfinding within a region
        /// </summary>
        public static List<Waypoint> BuildWaypointGrid(
            Point3D regionMin, Point3D regionMax, double gridSpacing)
        {
            var waypoints = new List<Waypoint>();

            for (double x = regionMin.X; x <= regionMax.X; x += gridSpacing)
            {
                for (double y = regionMin.Y; y <= regionMax.Y; y += gridSpacing)
                {
                    for (double z = regionMin.Z; z <= regionMax.Z; z += gridSpacing)
                    {
                        waypoints.Add(new Waypoint(new Point3D(x, y, z)));
                    }
                }
            }

            return waypoints;
        }
    }
}
