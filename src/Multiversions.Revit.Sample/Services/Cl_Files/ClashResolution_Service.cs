using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MEPClashResolution.Models;
using MEPClashResolution.Detection;
using MEPClashResolution.Geometry;
using MEPClashResolution.Pathfinding;

namespace MEPClashResolution.Services
{
    /// <summary>
    /// Main orchestration service for MEP clash detection and resolution
    /// Coordinates all components end-to-end
    /// </summary>
    public class MEPClashResolutionService
    {
        private ClashDetectionEngine _detectionEngine;
        private RouteResolutionOrchestrator _resolutionEngine;
        private ClashResolutionConfig _config;
        private ResolutionReport _currentReport;
        private Stopwatch _stopwatch;

        public MEPClashResolutionService(ClashResolutionConfig config = null)
        {
            _config = config ?? new ClashResolutionConfig();
            _detectionEngine = new ClashDetectionEngine(_config);
            _resolutionEngine = new RouteResolutionOrchestrator(
                _config,
                _detectionEngine.GetFittingManager()
            );
            _currentReport = new ResolutionReport();
            _stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Add a single route for analysis
        /// </summary>
        public void AddRoute(RouteSegment route)
        {
            _detectionEngine.AddRoute(route);
        }

        /// <summary>
        /// Add multiple routes
        /// </summary>
        public void AddRoutes(IEnumerable<RouteSegment> routes)
        {
            _detectionEngine.AddRoutes(routes);
        }

        /// <summary>
        /// Main workflow: Detect clashes and attempt resolution
        /// </summary>
        public ResolutionReport ExecuteFullWorkflow()
        {
            _stopwatch.Restart();
            _currentReport = new ResolutionReport();

            try
            {
                // Step 1: Detect clashes
                Console.WriteLine("[1/3] Detecting clashes...");
                var detectedClashes = _detectionEngine.DetectClashes();
                _currentReport.TotalClashesDetected = detectedClashes.Count;

                Console.WriteLine($"     Found {detectedClashes.Count} clashes");

                if (detectedClashes.Count == 0)
                {
                    _stopwatch.Stop();
                    _currentReport.TotalProcessingTime_sec = _stopwatch.Elapsed.TotalSeconds;
                    return _currentReport;
                }

                // Step 2: Resolve clashes
                Console.WriteLine("[2/3] Resolving clashes...");
                var allRoutes = new List<RouteSegment>();
                for (int i = 0; i < _detectionEngine.GetRouteCount(); i++)
                {
                    // In real implementation, retrieve actual routes from detection engine
                }

                var resolvedClashes = _resolutionEngine.ResolveMultipleClashes(
                    detectedClashes,
                    allRoutes
                );
                _currentReport.ClashesResolved = resolvedClashes.Count;
                _currentReport.ClashesUnresolvable = detectedClashes.Count - resolvedClashes.Count;

                Console.WriteLine($"     Resolved: {resolvedClashes.Count}/{detectedClashes.Count}");

                // Step 3: Generate report
                Console.WriteLine("[3/3] Generating report...");
                _currentReport.ClashDetails = detectedClashes;
                _currentReport.TotalRouteDistance_mm = CalculateTotalRouteDistance(resolvedClashes);

                _stopwatch.Stop();
                _currentReport.TotalProcessingTime_sec = _stopwatch.Elapsed.TotalSeconds;

                return _currentReport;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                _stopwatch.Stop();
                return _currentReport;
            }
        }

        /// <summary>
        /// Get detailed clash detection report
        /// </summary>
        public string GetClashDetectionReport()
        {
            return _detectionEngine.GenerateClashReport();
        }

        /// <summary>
        /// Get current resolution report
        /// </summary>
        public ResolutionReport GetCurrentReport()
        {
            return _currentReport;
        }

        /// <summary>
        /// Get all detected clashes
        /// </summary>
        public List<ClashInfo> GetDetectedClashes()
        {
            return _detectionEngine.GetDetectedClashes();
        }

        /// <summary>
        /// Get clashes by severity
        /// </summary>
        public List<ClashInfo> GetClashesBySeverity(ClashSeverity severity)
        {
            return _detectionEngine.GetClashesBySeverity(severity);
        }

        /// <summary>
        /// Calculate total distance of alternative routes
        /// </summary>
        private double CalculateTotalRouteDistance(List<ClashInfo> resolvedClashes)
        {
            return resolvedClashes
                .Where(c => c.ResolvedRoute != null && c.ResolvedRoute.Count > 0)
                .Sum(c => GeometryEngine.CalculatePathLength(c.ResolvedRoute));
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        public void Clear()
        {
            _detectionEngine.Clear();
            _currentReport = new ResolutionReport();
        }

        /// <summary>
        /// Get access to configuration
        /// </summary>
        public ClashResolutionConfig GetConfig() => _config;
    }

    /// <summary>
    /// Complete end-to-end example demonstrating the system
    /// </summary>
    public static class SystemExample
    {
        /// <summary>
        /// Run a complete demonstration with sample data
        /// </summary>
        public static void RunDemonstration()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  MEP CLASH DETECTION & RESOLUTION SYSTEM - DEMONSTRATION   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            // Initialize service with configuration
            var config = new ClashResolutionConfig
            {
                MinClearance_mm = 50.0,
                SearchResolution_mm = 100.0,
                MaxPathfindingIterations = 5000,
                CostWeight_Distance = 1.0,
                CostWeight_BendPenalty = 0.5,
                CostWeight_ClearancePenalty = 2.0
            };

            var service = new MEPClashResolutionService(config);

            // Create sample routes (conduits and cable trays)
            Console.WriteLine("→ Creating sample MEP routes...");
            CreateSampleRoutes(service);
            Console.WriteLine("  ✓ 8 routes created\n");

            // Execute workflow
            Console.WriteLine("→ Executing clash detection and resolution...\n");
            var report = service.ExecuteFullWorkflow();

            // Display results
            Console.WriteLine("\n" + report);

            // Print detailed clash information
            var clashes = service.GetDetectedClashes();
            if (clashes.Count > 0)
            {
                Console.WriteLine("\nDETAILED CLASH ANALYSIS:");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                
                int clashIndex = 1;
                foreach (var clash in clashes.OrderByDescending(c => c.Severity))
                {
                    Console.WriteLine($"\n[Clash {clashIndex}] {clash.Severity}");
                    Console.WriteLine($"  ID: {clash.ClashId}");
                    Console.WriteLine($"  Route 1: {clash.Route1.RouteId} ({clash.Route1.ElementType} {clash.Route1.Diameter_mm}mm)");
                    Console.WriteLine($"  Route 2: {clash.Route2.RouteId} ({clash.Route2.ElementType} {clash.Route2.Diameter_mm}mm)");
                    Console.WriteLine($"  Location: {clash.ClashLocation}");
                    Console.WriteLine($"  Actual Clearance: {clash.ActualClearance_mm:F2} mm");
                    Console.WriteLine($"  Required Clearance: {clash.RequiredClearance_mm:F2} mm");
                    Console.WriteLine($"  Status: {clash.ResolutionStatus}");
                    
                    if (clash.ResolvedRoute != null && clash.ResolvedRoute.Count > 0)
                    {
                        double resolvedLength = GeometryEngine.CalculatePathLength(clash.ResolvedRoute);
                        Console.WriteLine($"  Alternative Route Length: {resolvedLength:F0} mm ({clash.ResolvedRoute.Count} waypoints)");
                    }
                    
                    clashIndex++;
                }
            }

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  DEMONSTRATION COMPLETE                                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Create sample conduit and cable tray routes for testing
        /// </summary>
        private static void CreateSampleRoutes(MEPClashResolutionService service)
        {
            var routes = new List<RouteSegment>
            {
                // Horizontal conduit runs
                new RouteSegment(
                    "HOR_CONDUIT_01",
                    new Point3D(0, 0, 3000),
                    new Point3D(10000, 0, 3000),
                    diameter: 32,
                    clearance: 50
                ) { ElementType = "Conduit", Material = "Steel" },

                new RouteSegment(
                    "HOR_CONDUIT_02",
                    new Point3D(0, 5000, 3000),
                    new Point3D(10000, 5000, 3000),
                    diameter: 32,
                    clearance: 50
                ) { ElementType = "Conduit", Material = "Steel" },

                // Vertical cable tray
                new RouteSegment(
                    "VERT_TRAY_01",
                    new Point3D(5000, 0, 2000),
                    new Point3D(5000, 0, 5000),
                    diameter: 400,
                    clearance: 75
                ) { ElementType = "CableTray", Material = "Steel", FittingType = FittingType.Elbow90, BendRadius_mm = 500 },

                // Perpendicular conduit (clash potential)
                new RouteSegment(
                    "PERP_CONDUIT_01",
                    new Point3D(3000, 0, 3000),
                    new Point3D(7000, 5000, 3000),
                    diameter: 25,
                    clearance: 50
                ) { ElementType = "Conduit", Material = "Plastic" },

                // Another cable tray
                new RouteSegment(
                    "VERT_TRAY_02",
                    new Point3D(2000, 2500, 1500),
                    new Point3D(2000, 2500, 4500),
                    diameter: 300,
                    clearance: 60
                ) { ElementType = "CableTray", Material = "Aluminum" },

                // Diagonal conduit
                new RouteSegment(
                    "DIAG_CONDUIT_01",
                    new Point3D(0, 0, 2500),
                    new Point3D(8000, 8000, 4000),
                    diameter: 40,
                    clearance: 50
                ) { ElementType = "Conduit", Material = "Steel" },

                // Lower horizontal tray
                new RouteSegment(
                    "HOR_TRAY_01",
                    new Point3D(1000, 1000, 1500),
                    new Point3D(9000, 1000, 1500),
                    diameter: 350,
                    clearance: 70
                ) { ElementType = "CableTray", Material = "Steel" },

                // Upper horizontal conduit
                new RouteSegment(
                    "HOR_CONDUIT_03",
                    new Point3D(2000, 3000, 4500),
                    new Point3D(8000, 3000, 4500),
                    diameter: 32,
                    clearance: 50
                ) { ElementType = "Conduit", Material = "Steel" }
            };

            service.AddRoutes(routes);
        }
    }
}

/// <summary>
/// Program entry point - demonstrates complete workflow
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // Run the demonstration
            MEPClashResolution.Services.SystemExample.RunDemonstration();

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
