using System;
using System.Collections.Generic;
using System.Linq;
using MEPClashResolution.Models;
using MEPClashResolution.Geometry;

namespace MEPClashResolution.Pathfinding
{
    /// <summary>
    /// A* pathfinding algorithm implementation for MEP route resolution
    /// Handles obstacle avoidance and constraint validation
    /// </summary>
    public class RoutePathfinder
    {
        private ClashResolutionConfig _config;
        private List<BoundingBox> _obstacleZones;
        private FittingConstraintManager _fittingMgr;
        private int _iterationCount;

        public RoutePathfinder(
            ClashResolutionConfig config,
            FittingConstraintManager fittingMgr)
        {
            _config = config;
            _fittingMgr = fittingMgr;
            _obstacleZones = new List<BoundingBox>();
        }

        /// <summary>
        /// Add an obstacle zone (clash region) to avoid
        /// </summary>
        public void AddObstacleZone(BoundingBox obstacle)
        {
            _obstacleZones.Add(obstacle);
        }

        /// <summary>
        /// Clear all obstacle zones
        /// </summary>
        public void ClearObstacles()
        {
            _obstacleZones.Clear();
        }

        /// <summary>
        /// Main A* pathfinding algorithm
        /// Finds an alternative route avoiding clashes
        /// </summary>
        public List<Waypoint> FindAlternativeRoute(
            Point3D startPoint,
            Point3D endPoint,
            double minBendRadius,
            double diameter)
        {
            _iterationCount = 0;

            // Initialize open and closed sets
            var openSet = new HashSet<Waypoint>();
            var closedSet = new HashSet<Waypoint>();
            
            // Start waypoint
            var startWaypoint = new Waypoint(startPoint, FittingType.Straight);
            startWaypoint.CumulativeCost = 0;
            openSet.Add(startWaypoint);

            // End waypoint (goal)
            var endWaypoint = new Waypoint(endPoint, FittingType.Straight);

            while (openSet.Count > 0 && _iterationCount < _config.MaxPathfindingIterations)
            {
                _iterationCount++;

                // Find waypoint with lowest f_score in openSet
                var current = openSet.OrderBy(w => CalculateFScore(w, endWaypoint)).First();

                if (current.Position.DistanceTo(endPoint) < _config.SearchResolution_mm)
                {
                    // Goal reached - reconstruct and return path
                    return current.ReconstructPath();
                }

                openSet.Remove(current);
                closedSet.Add(current);

                // Generate neighboring waypoints
                var neighbors = GenerateNeighbors(current, endPoint, diameter);

                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Any(w => w.Position.Equals(neighbor.Position)))
                        continue; // Already evaluated

                    // Check if neighbor is obstructed
                    if (IsWaypointObstructed(neighbor.Position, diameter))
                        continue;

                    // Validate fitting constraints if this is a bend
                    double bendAngle = CalculateBendAngle(current, neighbor, endWaypoint);
                    if (!ValidateFittingConstraints(bendAngle, minBendRadius, current.FittingType))
                        continue;

                    // Calculate costs
                    double tentativeGScore = current.CumulativeCost + 
                        current.Position.DistanceTo(neighbor.Position) +
                        CalculateFittingCost(neighbor.FittingType);

                    var existingNeighbor = openSet.FirstOrDefault(w => w.Position.Equals(neighbor.Position));
                    if (existingNeighbor != null)
                    {
                        if (tentativeGScore < existingNeighbor.CumulativeCost)
                        {
                            existingNeighbor.CumulativeCost = tentativeGScore;
                            existingNeighbor.Parent = current;
                        }
                    }
                    else
                    {
                        neighbor.CumulativeCost = tentativeGScore;
                        neighbor.Parent = current;
                        openSet.Add(neighbor);
                    }
                }
            }

            // No path found - return fallback path
            return GenerateFallbackPath(startPoint, endPoint);
        }

        /// <summary>
        /// Generate neighboring waypoints from current position
        /// Uses a mix of straight-line and perpendicular directions
        /// </summary>
        private List<Waypoint> GenerateNeighbors(
            Waypoint current,
            Point3D goal,
            double routeDiameter)
        {
            var neighbors = new List<Waypoint>();
            double stepSize = _config.SearchResolution_mm;

            // Direction toward goal (straight line heuristic)
            Point3D toGoal = new Point3D(
                goal.X - current.Position.X,
                goal.Y - current.Position.Y,
                goal.Z - current.Position.Z
            );
            Point3D toGoalNormalized = GeometryEngine.NormalizeVector(toGoal);

            // Primary direction: toward goal
            var primaryDirection = new Point3D(
                toGoalNormalized.X * stepSize,
                toGoalNormalized.Y * stepSize,
                toGoalNormalized.Z * stepSize
            );

            neighbors.Add(new Waypoint(
                new Point3D(
                    current.Position.X + primaryDirection.X,
                    current.Position.Y + primaryDirection.Y,
                    current.Position.Z + primaryDirection.Z
                ),
                FittingType.Straight
            ));

            // Perpendicular directions (axis-aligned for better routing)
            var axisDirections = new[]
            {
                new Point3D(stepSize, 0, 0),
                new Point3D(-stepSize, 0, 0),
                new Point3D(0, stepSize, 0),
                new Point3D(0, -stepSize, 0),
                new Point3D(0, 0, stepSize),
                new Point3D(0, 0, -stepSize)
            };

            foreach (var direction in axisDirections)
            {
                neighbors.Add(new Waypoint(
                    new Point3D(
                        current.Position.X + direction.X,
                        current.Position.Y + direction.Y,
                        current.Position.Z + direction.Z
                    ),
                    FittingType.Straight
                ));
            }

            // Diagonal directions (for better space utilization)
            var diagonalDirections = new[]
            {
                new Point3D(stepSize * 0.7, stepSize * 0.7, 0),
                new Point3D(-stepSize * 0.7, stepSize * 0.7, 0),
                new Point3D(stepSize * 0.7, -stepSize * 0.7, 0),
                new Point3D(-stepSize * 0.7, -stepSize * 0.7, 0),
                new Point3D(stepSize * 0.7, 0, stepSize * 0.7),
                new Point3D(-stepSize * 0.7, 0, stepSize * 0.7),
                new Point3D(stepSize * 0.7, 0, -stepSize * 0.7),
                new Point3D(-stepSize * 0.7, 0, -stepSize * 0.7)
            };

            foreach (var direction in diagonalDirections)
            {
                neighbors.Add(new Waypoint(
                    new Point3D(
                        current.Position.X + direction.X,
                        current.Position.Y + direction.Y,
                        current.Position.Z + direction.Z
                    ),
                    FittingType.Straight
                ));
            }

            return neighbors;
        }

        /// <summary>
        /// Calculate F score (g + h) for A* algorithm
        /// </summary>
        private double CalculateFScore(Waypoint waypoint, Waypoint goal)
        {
            double g = waypoint.CumulativeCost; // Actual cost from start
            double h = HeuristicDistance(waypoint.Position, goal.Position); // Estimated cost to goal
            return g + h;
        }

        /// <summary>
        /// Heuristic function: Euclidean distance to goal
        /// </summary>
        private double HeuristicDistance(Point3D from, Point3D to)
        {
            return from.DistanceTo(to) * _config.CostWeight_Distance;
        }

        /// <summary>
        /// Calculate the bend angle at a waypoint
        /// </summary>
        private double CalculateBendAngle(Waypoint prev, Waypoint current, Waypoint next)
        {
            Point3D v1 = new Point3D(
                current.Position.X - prev.Position.X,
                current.Position.Y - prev.Position.Y,
                current.Position.Z - prev.Position.Z
            );

            Point3D v2 = new Point3D(
                next.Position.X - current.Position.X,
                next.Position.Y - current.Position.Y,
                next.Position.Z - current.Position.Z
            );

            return GeometryEngine.AngleBetweenVectors(v1, v2);
        }

        /// <summary>
        /// Validate fitting constraints for a bend
        /// </summary>
        private bool ValidateFittingConstraints(
            double bendAngle,
            double minBendRadius,
            FittingType fittingType)
        {
            if (bendAngle < 5) // Straight or near-straight
                return true;

            // Determine fitting type for this bend angle
            FittingType bendFitting = bendAngle switch
            {
                < 60 => FittingType.Straight,
                >= 60 and < 120 => FittingType.Elbow45,
                >= 120 and < 150 => FittingType.Elbow90,
                _ => FittingType.Tee90
            };

            return _fittingMgr.ValidateBendConstraints(bendAngle, minBendRadius, bendFitting);
        }

        /// <summary>
        /// Check if a waypoint is obstructed by obstacles
        /// </summary>
        private bool IsWaypointObstructed(Point3D position, double routeDiameter)
        {
            double radius = routeDiameter / 2 + _config.MinClearance_mm;
            
            return _obstacleZones.Any(obstacle => 
            {
                var expandedObstacle = obstacle.ExpandByClearance(radius);
                return GeometryEngine.IsPointInBoundingBox(position, expandedObstacle);
            });
        }

        /// <summary>
        /// Calculate cost for a fitting type
        /// </summary>
        private double CalculateFittingCost(FittingType fittingType)
        {
            return _fittingMgr.GetFittingCost(fittingType) * _config.CostWeight_BendPenalty;
        }

        /// <summary>
        /// Generate fallback path when A* cannot find solution
        /// Creates a simple waypoint path skirting obstacles
        /// </summary>
        private List<Waypoint> GenerateFallbackPath(Point3D start, Point3D end)
        {
            var path = new List<Waypoint>
            {
                new Waypoint(start, FittingType.Straight)
            };

            // Intermediate waypoint above the clash zone
            Point3D mid = new Point3D(
                (start.X + end.X) / 2,
                (start.Y + end.Y) / 2,
                Math.Max(start.Z, end.Z) + _config.SearchResolution_mm * 2
            );

            path.Add(new Waypoint(mid, FittingType.Elbow90));
            path.Add(new Waypoint(end, FittingType.Straight));

            return path;
        }

        /// <summary>
        /// Get number of iterations used in last pathfinding
        /// </summary>
        public int GetLastIterationCount() => _iterationCount;
    }

    /// <summary>
    /// Route resolution orchestrator
    /// Coordinates pathfinding and constraint validation
    /// </summary>
    public class RouteResolutionOrchestrator
    {
        private RoutePathfinder _pathfinder;
        private FittingConstraintManager _fittingMgr;
        private ClashResolutionConfig _config;

        public RouteResolutionOrchestrator(
            ClashResolutionConfig config,
            FittingConstraintManager fittingMgr)
        {
            _config = config;
            _fittingMgr = fittingMgr;
            _pathfinder = new RoutePathfinder(config, fittingMgr);
        }

        /// <summary>
        /// Resolve a clash by finding alternative route
        /// </summary>
        public bool ResolveSingleClash(ClashInfo clash, List<RouteSegment> allRoutes)
        {
            // Clear previous obstacles
            _pathfinder.ClearObstacles();

            // Add all conflicting routes as obstacles
            foreach (var route in allRoutes)
            {
                if (route.RouteId != clash.Route1.RouteId && route.RouteId != clash.Route2.RouteId)
                {
                    var bbox = route.GetBoundingBoxWithClearance();
                    _pathfinder.AddObstacleZone(bbox);
                }
            }

            // Add the clash zone itself
            var clashZone = clash.Route2.GetBoundingBoxWithClearance();
            _pathfinder.AddObstacleZone(clashZone);

            // Find alternative route
            var newPath = _pathfinder.FindAlternativeRoute(
                clash.Route1.StartPoint,
                clash.Route1.EndPoint,
                clash.Route1.BendRadius_mm,
                clash.Route1.Diameter_mm
            );

            if (newPath != null && newPath.Count >= 2)
            {
                clash.ResolvedRoute = newPath;
                clash.ResolutionStatus = "Resolved";
                return true;
            }

            clash.ResolutionStatus = "Unresolvable";
            return false;
        }

        /// <summary>
        /// Resolve multiple clashes with priority ordering
        /// </summary>
        public List<ClashInfo> ResolveMultipleClashes(
            List<ClashInfo> clashes,
            List<RouteSegment> allRoutes)
        {
            // Sort by severity (critical first)
            var sortedClashes = clashes
                .OrderByDescending(c => c.Severity)
                .ToList();

            var resolvedClashes = new List<ClashInfo>();

            foreach (var clash in sortedClashes)
            {
                if (ResolveSingleClash(clash, allRoutes))
                {
                    resolvedClashes.Add(clash);
                }
            }

            return resolvedClashes;
        }
    }
}
