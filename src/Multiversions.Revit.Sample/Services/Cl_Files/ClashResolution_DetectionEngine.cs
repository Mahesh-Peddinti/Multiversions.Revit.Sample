using System;
using System.Collections.Generic;
using System.Linq;
using MEPClashResolution.Models;
using MEPClashResolution.Geometry;

namespace MEPClashResolution.Detection
{
    /// <summary>
    /// Manages fitting specifications and constraint validation
    /// Extensible database for various fitting types
    /// </summary>
    public class FittingConstraintManager
    {
        private Dictionary<FittingType, FittingSpec> _fittingDatabase;

        public FittingConstraintManager()
        {
            _fittingDatabase = new Dictionary<FittingType, FittingSpec>();
            InitializeStandardFittings();
        }

        /// <summary>
        /// Initialize standard fitting specifications (extensible)
        /// </summary>
        private void InitializeStandardFittings()
        {
            // Straight connectors (no bend)
            _fittingDatabase[FittingType.Straight] = new FittingSpec(
                "Straight Connector",
                FittingType.Straight,
                minBendRadius: 0.0,
                allowedAngles: new List<double> { 0, 180 },
                cost: 0.1
            );

            // 45° Elbows
            _fittingDatabase[FittingType.Elbow45] = new FittingSpec(
                "Standard 45° Elbow",
                FittingType.Elbow45,
                minBendRadius: 75.0,
                allowedAngles: new List<double> { 45, 315 },
                cost: 0.8
            );

            // 90° Elbows (most common)
            _fittingDatabase[FittingType.Elbow90] = new FittingSpec(
                "Standard 90° Elbow",
                FittingType.Elbow90,
                minBendRadius: 150.0,
                allowedAngles: new List<double> { 90, 270 },
                cost: 1.0
            );

            // Three-way Tees
            _fittingDatabase[FittingType.Tee90] = new FittingSpec(
                "90° Tee Junction",
                FittingType.Tee90,
                minBendRadius: 150.0,
                allowedAngles: new List<double> { 45, 90, 135 },
                cost: 1.2
            );

            // 45° Tees
            _fittingDatabase[FittingType.Tee45] = new FittingSpec(
                "45° Tee Junction",
                FittingType.Tee45,
                minBendRadius: 100.0,
                allowedAngles: new List<double> { 45 },
                cost: 1.1
            );

            // Reducers (diameter changes)
            _fittingDatabase[FittingType.Reducer] = new FittingSpec(
                "Diameter Reducer",
                FittingType.Reducer,
                minBendRadius: 200.0,
                allowedAngles: new List<double> { 0, 180 },
                cost: 1.5
            );
        }

        /// <summary>
        /// Get fitting specification by type
        /// </summary>
        public FittingSpec GetFittingSpec(FittingType type)
        {
            return _fittingDatabase.ContainsKey(type) 
                ? _fittingDatabase[type] 
                : _fittingDatabase[FittingType.Straight];
        }

        /// <summary>
        /// Register a custom fitting specification
        /// </summary>
        public void RegisterCustomFitting(FittingType type, FittingSpec spec)
        {
            _fittingDatabase[type] = spec;
        }

        /// <summary>
        /// Validate a bend angle and radius against fitting constraints
        /// </summary>
        public bool ValidateBendConstraints(
            double bendAngle_deg,
            double bendRadius_mm,
            FittingType fittingType)
        {
            var spec = GetFittingSpec(fittingType);

            // Check if bend angle is allowed
            if (!spec.IsValidBendAngle(bendAngle_deg))
                return false;

            // Check if bend radius meets minimum requirement
            if (bendRadius_mm < spec.MinBendRadius_mm)
                return false;

            return true;
        }

        /// <summary>
        /// Get the cost multiplier for a specific fitting
        /// </summary>
        public double GetFittingCost(FittingType fittingType)
        {
            var spec = GetFittingSpec(fittingType);
            return spec.Cost;
        }

        /// <summary>
        /// Get all registered fittings
        /// </summary>
        public List<FittingSpec> GetAllFittings()
        {
            return _fittingDatabase.Values.ToList();
        }
    }

    /// <summary>
    /// Core clash detection engine
    /// Identifies conflicts between MEP routes
    /// </summary>
    public class ClashDetectionEngine
    {
        private List<RouteSegment> _routes;
        private List<ClashInfo> _detectedClashes;
        private ClashResolutionConfig _config;
        private FittingConstraintManager _fittingMgr;

        public ClashDetectionEngine(ClashResolutionConfig config = null)
        {
            _config = config ?? new ClashResolutionConfig();
            _routes = new List<RouteSegment>();
            _detectedClashes = new List<ClashInfo>();
            _fittingMgr = new FittingConstraintManager();
        }

        /// <summary>
        /// Add a route segment for clash detection
        /// </summary>
        public void AddRoute(RouteSegment route)
        {
            if (route != null)
                _routes.Add(route);
        }

        /// <summary>
        /// Add multiple routes at once
        /// </summary>
        public void AddRoutes(IEnumerable<RouteSegment> routes)
        {
            _routes.AddRange(routes);
        }

        /// <summary>
        /// Clear all routes and clashes
        /// </summary>
        public void Clear()
        {
            _routes.Clear();
            _detectedClashes.Clear();
        }

        /// <summary>
        /// Main detection algorithm: find all clashes between routes
        /// </summary>
        public List<ClashInfo> DetectClashes()
        {
            _detectedClashes.Clear();
            var clashCounter = 0;

            // O(n²) pairwise comparison of all routes
            for (int i = 0; i < _routes.Count; i++)
            {
                for (int j = i + 1; j < _routes.Count; j++)
                {
                    var clash = CheckRouteIntersection(_routes[i], _routes[j]);
                    if (clash != null)
                    {
                        clash.ClashId = $"CLH_{clashCounter:D4}";
                        clash.ClassifySeverity();
                        _detectedClashes.Add(clash);
                        clashCounter++;
                    }
                }
            }

            return _detectedClashes;
        }

        /// <summary>
        /// Check if two route segments intersect or violate clearance
        /// </summary>
        private ClashInfo CheckRouteIntersection(RouteSegment route1, RouteSegment route2)
        {
            // First, quick AABB check
            var bbox1 = route1.GetBoundingBoxWithClearance();
            var bbox2 = route2.GetBoundingBoxWithClearance();

            if (!bbox1.IntersectsWith(bbox2))
                return null; // No bounding box overlap, no clash

            // Detailed distance check between centerlines
            double minDistance = GeometryEngine.DistanceBetweenSegments(
                route1.StartPoint, route1.EndPoint,
                route2.StartPoint, route2.EndPoint
            );

            // Account for both route diameters and required clearance
            double requiredClearance = 
                route1.Diameter_mm / 2 + 
                route2.Diameter_mm / 2 + 
                Math.Max(route1.RequiredClearance_mm, route2.RequiredClearance_mm);

            if (minDistance >= requiredClearance)
                return null; // Sufficient clearance, no clash

            // Clash detected - calculate exact location
            var (point1, point2) = GeometryEngine.ClosestPointsBetweenSegments(
                route1.StartPoint, route1.EndPoint,
                route2.StartPoint, route2.EndPoint
            );

            var clashLocation = new Point3D(
                (point1.X + point2.X) / 2,
                (point1.Y + point2.Y) / 2,
                (point1.Z + point2.Z) / 2
            );

            var clash = new ClashInfo(
                id: "",
                route1: route1,
                route2: route2,
                location: clashLocation
            )
            {
                ActualClearance_mm = minDistance,
                RequiredClearance_mm = requiredClearance
            };

            return clash;
        }

        /// <summary>
        /// Get all detected clashes
        /// </summary>
        public List<ClashInfo> GetDetectedClashes()
        {
            return new List<ClashInfo>(_detectedClashes);
        }

        /// <summary>
        /// Get clashes by severity level
        /// </summary>
        public List<ClashInfo> GetClashesBySeverity(ClashSeverity severity)
        {
            return _detectedClashes.Where(c => c.Severity == severity).ToList();
        }

        /// <summary>
        /// Filter routes that have clashes
        /// </summary>
        public List<RouteSegment> GetConflictingRoutes()
        {
            var conflictingRouteIds = new HashSet<string>();
            foreach (var clash in _detectedClashes)
            {
                conflictingRouteIds.Add(clash.Route1.RouteId);
                conflictingRouteIds.Add(clash.Route2.RouteId);
            }

            return _routes.Where(r => conflictingRouteIds.Contains(r.RouteId)).ToList();
        }

        /// <summary>
        /// Get all clashes involving a specific route
        /// </summary>
        public List<ClashInfo> GetClashesForRoute(string routeId)
        {
            return _detectedClashes
                .Where(c => c.Route1.RouteId == routeId || c.Route2.RouteId == routeId)
                .ToList();
        }

        /// <summary>
        /// Generate a detailed clash report
        /// </summary>
        public string GenerateClashReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("═══════════════════════════════════════════════════════════");
            report.AppendLine("  MEP CLASH DETECTION REPORT");
            report.AppendLine("═══════════════════════════════════════════════════════════");
            report.AppendLine($"  Total Routes Analyzed:     {_routes.Count}");
            report.AppendLine($"  Total Clashes Detected:    {_detectedClashes.Count}");
            report.AppendLine();

            var criticalCount = _detectedClashes.Count(c => c.Severity == ClashSeverity.Critical);
            var majorCount = _detectedClashes.Count(c => c.Severity == ClashSeverity.Major);
            var minorCount = _detectedClashes.Count(c => c.Severity == ClashSeverity.Minor);

            report.AppendLine($"  Critical Clashes:          {criticalCount}");
            report.AppendLine($"  Major Clashes:             {majorCount}");
            report.AppendLine($"  Minor Clashes:             {minorCount}");
            report.AppendLine("───────────────────────────────────────────────────────────");

            if (_detectedClashes.Count > 0)
            {
                report.AppendLine("\n  CLASH DETAILS:\n");
                foreach (var clash in _detectedClashes.OrderByDescending(c => c.Severity))
                {
                    report.AppendLine($"  [{clash.ClashId}] {clash.Severity}");
                    report.AppendLine($"    Route 1: {clash.Route1.RouteId} ({clash.Route1.ElementType})");
                    report.AppendLine($"    Route 2: {clash.Route2.RouteId} ({clash.Route2.ElementType})");
                    report.AppendLine($"    Location: {clash.ClashLocation}");
                    report.AppendLine($"    Actual Clearance: {clash.ActualClearance_mm:F2} mm");
                    report.AppendLine($"    Required Clearance: {clash.RequiredClearance_mm:F2} mm");
                    report.AppendLine();
                }
            }

            report.AppendLine("═══════════════════════════════════════════════════════════");
            return report.ToString();
        }

        /// <summary>
        /// Get number of routes
        /// </summary>
        public int GetRouteCount() => _routes.Count;

        /// <summary>
        /// Get number of detected clashes
        /// </summary>
        public int GetClashCount() => _detectedClashes.Count;

        /// <summary>
        /// Access to fitting constraint manager
        /// </summary>
        public FittingConstraintManager GetFittingManager() => _fittingMgr;
    }
}
