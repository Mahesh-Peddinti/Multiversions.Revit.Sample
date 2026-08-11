using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace MBDevApplication
{
    [Transaction(TransactionMode.Manual)]
    public class ResolveCableTrayClashes : IExternalCommand
    {
        // ================================================================
        // CONFIGURATION
        // ================================================================

        private const double MmToFt = 1.0 / 304.8;

        // Required free space above the conduit.
        private const double ClearanceMm = 50.0;

        // Minimum vertical rise.
        private const double MinimumRiseMm = 100.0;

        // Minimum clash/geometry tolerance.
        private const double PointToleranceMm = 1.0;

        // Minimum accepted clash volume.
        private const double MinimumIntersectionVolume = 1e-9;

        // First-stage routing supports horizontal trays in any XY direction.
        private const double HorizontalTrayZTolerance = 0.01;

        // ================================================================
        // MAIN COMMAND
        // ================================================================

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                List<CableTray> trays = CollectCableTrays(doc);
                List<Conduit> conduits = CollectConduits(doc);

                if (trays.Count == 0 || conduits.Count == 0)
                {
                    ShowSummary(
                        "Cable Tray Clash Resolver",
                        $"Cable Trays : {trays.Count}\n" +
                        $"Conduits    : {conduits.Count}\n\n" +
                        "Nothing to process.");

                    return Result.Succeeded;
                }

                List<ClashInfo> clashes = FindClashes(trays, conduits);

                if (clashes.Count == 0)
                {
                    ShowSummary(
                        "Cable Tray Clash Resolver",
                        $"Cable Trays : {trays.Count}\n" +
                        $"Conduits    : {conduits.Count}\n\n" +
                        "No solid clashes detected.");

                    return Result.Succeeded;
                }

                int successfulRoutes = 0;
                int skippedRoutes = 0;
                int createdSegments = 0;
                int createdFittings = 0;

                // TEST MODE:
                // The original cable trays are NOT deleted/split yet.
                // Each generated route is isolated in a SubTransaction so
                // a failed route is completely rolled back.
                using (Transaction tx =
                    new Transaction(doc, "Auto Resolve Cable Tray Clashes"))
                {
                    tx.Start();

                    HashSet<ElementId> processedTrays =
                        new HashSet<ElementId>();

                    foreach (ClashInfo clash in clashes)
                    {
                        if (processedTrays.Contains(clash.Tray.Id))
                        {
                            skippedRoutes++;
                            continue;
                        }

                        RouteDefinition route;

                        if (!TryBuild45DegreeRoute(clash, out route))
                        {
                            skippedRoutes++;
                            continue;
                        }

                        using (SubTransaction st =
                            new SubTransaction(doc))
                        {
                            st.Start();

                            try
                            {
                                List<CableTray> segments =
                                    CreateRouteSegments(
                                        doc,
                                        clash.Tray,
                                        route.Points);

                                if (segments.Count != 5)
                                {
                                    st.RollBack();
                                    skippedRoutes++;
                                    continue;
                                }

                                doc.Regenerate();

                                // THIS WAS THE MAJOR MISSING PIECE:
                                // explicitly connect adjacent segments at
                                // every route vertex so Revit can create
                                // the required Cable Tray fittings.
                                List<ElementId> fittingIds =
                                    ConnectRouteSegments(
                                        doc,
                                        segments,
                                        route.Points);

                                if (fittingIds.Count < 2)
                                {
                                    st.RollBack();
                                    skippedRoutes++;
                                    continue;
                                }

                                doc.Regenerate();

                                // Validate the generated route against the
                                // original conduit before committing.
                                if (RouteStillClashes(
                                    segments,
                                    fittingIds,
                                    clash.Conduit))
                                {
                                    st.RollBack();
                                    skippedRoutes++;
                                    continue;
                                }

                                foreach (CableTray segment in segments)
                                {
                                    OverrideAsPink(
                                        doc.ActiveView,
                                        segment.Id);
                                }

                                st.Commit();

                                successfulRoutes++;
                                createdSegments += segments.Count;
                                createdFittings += fittingIds.Count;
                                processedTrays.Add(clash.Tray.Id);
                            }
                            catch
                            {
                                st.RollBack();
                                skippedRoutes++;
                            }
                        }
                    }

                    tx.Commit();
                }

                ShowSummary(
                    "Cable Tray Clash Resolver",
                    $"Cable Trays found : {trays.Count}\n" +
                    $"Conduits found    : {conduits.Count}\n" +
                    $"Clashes found     : {clashes.Count}\n\n" +
                    $"Successful routes : {successfulRoutes}\n" +
                    $"New tray segments : {createdSegments}\n" +
                    $"New fittings      : {createdFittings}\n" +
                    $"Skipped           : {skippedRoutes}\n\n" +
                    "TEST MODE:\n" +
                    "Original Cable Trays were NOT modified or deleted.\n" +
                    "Generated routes are shown in pink.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.ToString();

                TaskDialog.Show(
                    "Cable Tray Clash Resolver - Error",
                    ex.ToString());

                return Result.Failed;
            }
        }

        // ================================================================
        // COLLECTION
        // ================================================================

        private static List<CableTray> CollectCableTrays(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(CableTray))
                .WhereElementIsNotElementType()
                .Cast<CableTray>()
                .ToList();
        }

        private static List<Conduit> CollectConduits(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Conduit))
                .WhereElementIsNotElementType()
                .Cast<Conduit>()
                .ToList();
        }

        // ================================================================
        // CLASH DETECTION
        // ================================================================

        private static List<ClashInfo> FindClashes(
            List<CableTray> trays,
            List<Conduit> conduits)
        {
            List<ClashInfo> result =
                new List<ClashInfo>();

            Dictionary<ElementId, BoundingBoxXYZ> trayBoxes =
                trays.ToDictionary(
                    t => t.Id,
                    t => t.get_BoundingBox(null));

            Dictionary<ElementId, BoundingBoxXYZ> conduitBoxes =
                conduits.ToDictionary(
                    c => c.Id,
                    c => c.get_BoundingBox(null));

            Dictionary<ElementId, List<Solid>> traySolids =
                trays.ToDictionary(
                    t => t.Id,
                    GetSolids);

            Dictionary<ElementId, List<Solid>> conduitSolids =
                conduits.ToDictionary(
                    c => c.Id,
                    GetSolids);

            foreach (CableTray tray in trays)
            {
                BoundingBoxXYZ trayBox =
                    trayBoxes[tray.Id];

                if (trayBox == null)
                    continue;

                foreach (Conduit conduit in conduits)
                {
                    BoundingBoxXYZ conduitBox =
                        conduitBoxes[conduit.Id];

                    if (conduitBox == null)
                        continue;

                    // Broad phase.
                    if (!BoxesIntersect(
                        trayBox,
                        conduitBox,
                        1.0 * MmToFt))
                    {
                        continue;
                    }

                    // Narrow phase.
                    Solid intersection =
                        FindSolidIntersection(
                            traySolids[tray.Id],
                            conduitSolids[conduit.Id]);

                    if (intersection == null)
                        continue;

                    result.Add(
                        new ClashInfo
                        {
                            Tray = tray,
                            Conduit = conduit,
                            Intersection = intersection
                        });
                }
            }

            return result;
        }

        private static Solid FindSolidIntersection(
            List<Solid> firstSolids,
            List<Solid> secondSolids)
        {
            foreach (Solid first in firstSolids)
            {
                foreach (Solid second in secondSolids)
                {
                    try
                    {
                        Solid intersection =
                            BooleanOperationsUtils
                                .ExecuteBooleanOperation(
                                    first,
                                    second,
                                    BooleanOperationsType.Intersect);

                        if (intersection != null &&
                            intersection.Volume >
                            MinimumIntersectionVolume)
                        {
                            return intersection;
                        }
                    }
                    catch
                    {
                        // Some Revit geometry combinations can fail
                        // BooleanOperations. Continue to the next pair.
                    }
                }
            }

            return null;
        }

        // ================================================================
        // ROUTE SOLVER
        // ================================================================

        private sealed class RouteDefinition
        {
            public List<XYZ> Points { get; set; }
            public double Rise { get; set; }
            public double Transition { get; set; }
            public double ClashStart { get; set; }
            public double ClashEnd { get; set; }
        }

        private static bool TryBuild45DegreeRoute(
            ClashInfo clash,
            out RouteDefinition route)
        {
            route = null;

            LocationCurve location =
                clash.Tray.Location as LocationCurve;

            if (location == null ||
                !(location.Curve is Line))
            {
                return false;
            }

            Line trayLine =
                location.Curve as Line;

            XYZ start =
                trayLine.GetEndPoint(0);

            XYZ end =
                trayLine.GetEndPoint(1);

            XYZ trayDirection =
                end - start;

            if (trayDirection.GetLength() < 1e-6)
                return false;

            trayDirection =
                trayDirection.Normalize();

            // First-stage solver:
            // horizontal tray in any XY orientation.
            if (Math.Abs(trayDirection.Z) >
                HorizontalTrayZTolerance)
            {
                return false;
            }

            XYZ Z = XYZ.BasisZ;

            double trayHeight =
                GetParameterValue(
                    clash.Tray,
                    BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM);

            if (trayHeight <= 0)
                return false;

            BoundingBoxXYZ conduitBox =
                clash.Conduit.get_BoundingBox(null);

            if (conduitBox == null)
                return false;

            double currentZ =
                start.Z;

            // Cable tray location line is treated as the tray centre
            // elevation for this first-stage solver.
            double targetZ =
                conduitBox.Max.Z +
                ClearanceMm * MmToFt +
                trayHeight / 2.0;

            targetZ =
                Math.Max(
                    targetZ,
                    currentZ +
                    MinimumRiseMm * MmToFt);

            double rise =
                targetZ - currentZ;

            // 45 degree condition:
            // horizontal transition = vertical rise.
            double transition = rise;

            double minStation;
            double maxStation;

            if (!TryGetStationRange(
                clash.Intersection,
                start,
                trayDirection,
                out minStation,
                out maxStation))
            {
                return false;
            }

            double trayLength =
                start.DistanceTo(end);

            double entryStation =
                minStation - transition;

            double exitStation =
                maxStation + transition;

            // The first test version keeps the generated route inside
            // the original tray extents.
            if (entryStation <= 0 ||
                exitStation >= trayLength)
            {
                return false;
            }

            if (exitStation <= entryStation)
                return false;

            XYZ p0 = start;

            XYZ p1 =
                start +
                trayDirection * entryStation;

            // 45 degree UP:
            // horizontal distance = rise.
            XYZ p2 =
                p1 +
                trayDirection * transition +
                Z * rise;

            XYZ p3 =
                start +
                trayDirection * maxStation +
                Z * rise;

            // 45 degree DOWN.
            XYZ p4 =
                start +
                trayDirection * exitStation;

            XYZ p5 = end;

            List<XYZ> points =
                new List<XYZ>();

            AddPoint(points, p0);
            AddPoint(points, p1);
            AddPoint(points, p2);
            AddPoint(points, p3);
            AddPoint(points, p4);
            AddPoint(points, p5);

            // Expected topology:
            //
            // P0 -------- P1
            //              \
            //               \ 45°
            //                P2 -------- P3
            //                            /
            //                           / 45°
            //                          P4 -------- P5
            //
            // => 5 tray segments
            if (points.Count != 6)
                return false;

            route = new RouteDefinition
            {
                Points = points,
                Rise = rise,
                Transition = transition,
                ClashStart = minStation,
                ClashEnd = maxStation
            };

            return true;
        }

        // ================================================================
        // CREATE ROUTE SEGMENTS
        // ================================================================

        private static List<CableTray> CreateRouteSegments(
            Document doc,
            CableTray sourceTray,
            IList<XYZ> points)
        {
            List<CableTray> segments =
                new List<CableTray>();

            ElementId typeId =
                sourceTray.GetTypeId();

            ElementId levelId =
                sourceTray.LevelId;

            double width =
                GetParameterValue(
                    sourceTray,
                    BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM);

            double height =
                GetParameterValue(
                    sourceTray,
                    BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM);

            for (int i = 0;
                 i < points.Count - 1;
                 i++)
            {
                XYZ start = points[i];
                XYZ end = points[i + 1];

                if (start.DistanceTo(end) <
                    PointToleranceMm * MmToFt)
                {
                    continue;
                }

                CableTray segment =
                    CableTray.Create(
                        doc,
                        typeId,
                        start,
                        end,
                        levelId);

                SetCableTraySize(
                    segment,
                    width,
                    height);

                segments.Add(segment);
            }

            return segments;
        }

        private static void SetCableTraySize(
            CableTray tray,
            double width,
            double height)
        {
            Parameter widthParameter =
                tray.get_Parameter(
                    BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM);

            if (widthParameter != null &&
                !widthParameter.IsReadOnly &&
                width > 0)
            {
                widthParameter.Set(width);
            }

            Parameter heightParameter =
                tray.get_Parameter(
                    BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM);

            if (heightParameter != null &&
                !heightParameter.IsReadOnly &&
                height > 0)
            {
                heightParameter.Set(height);
            }
        }

        // ================================================================
        // FITTING / CONNECTION ENGINE
        // ================================================================

        private static List<ElementId> ConnectRouteSegments(
            Document doc,
            IList<CableTray> segments,
            IList<XYZ> routePoints)
        {
            List<ElementId> fittingIds =
                new List<ElementId>();

            if (segments.Count < 2)
                return fittingIds;

            // A 6-point route produces 5 segments and therefore
            // 4 connection/fitting locations.
            for (int i = 0;
                 i < segments.Count - 1;
                 i++)
            {
                XYZ connectionPoint =
                    routePoints[i + 1];

                Connector first =
                    GetEndpointConnector(
                        segments[i],
                        connectionPoint);

                Connector second =
                    GetEndpointConnector(
                        segments[i + 1],
                        connectionPoint);

                if (first == null ||
                    second == null)
                {
                    throw new InvalidOperationException(
                        $"Could not find route connectors at " +
                        $"point {i + 1}.");
                }

                if (first.Domain != second.Domain)
                {
                    throw new InvalidOperationException(
                        "Cable Tray connector domains do not match.");
                }

                double distance =
                    first.Origin.DistanceTo(second.Origin);

                if (distance >
                    5.0 * MmToFt)
                {
                    throw new InvalidOperationException(
                        $"Connector gap is too large: " +
                        $"{distance / MmToFt:F1} mm.");
                }

                // Capture fittings before the connection.
                HashSet<ElementId> before =
                    GetCableTrayFittingIds(doc);

                bool connected = false;

                try
                {
                    // Revit can create the required fitting when the
                    // two physical connectors are connected.
                    first.ConnectTo(second);
                    connected = true;
                }
                catch (Exception ex)
                {
                    // Do not leave a partially connected route.
                    throw new InvalidOperationException(
                        $"Failed to connect route segments " +
                        $"at {connectionPoint}.\n{ex.Message}",
                        ex);
                }

                if (!connected)
                    throw new InvalidOperationException(
                        "Cable Tray connector connection failed.");

                doc.Regenerate();

                // Both segment connectors should now be physically
                // connected, normally through a CableTrayFitting.
                if (!first.IsConnected ||
                    !second.IsConnected)
                {
                    throw new InvalidOperationException(
                        "ConnectTo completed but the route connectors " +
                        "are not physically connected.");
                }

                HashSet<ElementId> after =
                    GetCableTrayFittingIds(doc);

                List<ElementId> newFittings =
                    after
                        .Except(before)
                        .ToList();

                // A direction change is expected here. We require a
                // fitting to be generated for the 45-degree route.
                if (newFittings.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No Cable Tray fitting was generated at " +
                        $"route vertex {i + 1}.");
                }

                fittingIds.AddRange(newFittings);
            }

            return fittingIds
                .Distinct()
                .ToList();
        }

        private static Connector GetEndpointConnector(
            MEPCurve curve,
            XYZ point)
        {
            Connector best = null;
            double bestDistance = double.MaxValue;

            foreach (Connector connector
                in curve.ConnectorManager.Connectors)
            {
                if (connector.ConnectorType !=
                    ConnectorType.End)
                {
                    continue;
                }

                double distance =
                    connector.Origin.DistanceTo(point);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = connector;
                }
            }

            if (best == null ||
                bestDistance >
                5.0 * MmToFt)
            {
                return null;
            }

            return best;
        }

        private static HashSet<ElementId>
            GetCableTrayFittingIds(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(CableTrayFitting))
                .WhereElementIsNotElementType()
                .ToElementIds()
                .ToHashSet();
        }

        // ================================================================
        // ROUTE VALIDATION
        // ================================================================

        private static bool RouteStillClashes(
            IList<CableTray> segments,
            IList<ElementId> fittingIds,
            Conduit conduit)
        {
            List<Solid> conduitSolids =
                GetSolids(conduit);

            if (conduitSolids.Count == 0)
                return true;

            foreach (CableTray segment in segments)
            {
                List<Solid> segmentSolids =
                    GetSolids(segment);

                if (FindSolidIntersection(
                    segmentSolids,
                    conduitSolids) != null)
                {
                    return true;
                }
            }

            // Fittings are also part of the physical route. Check them
            // because a fitting can extend beyond the tray segments.
            Document doc =
                conduit.Document;

            foreach (ElementId fittingId in fittingIds)
            {
                Element fitting =
                    doc.GetElement(fittingId);

                if (fitting == null)
                    continue;

                List<Solid> fittingSolids =
                    GetSolids(fitting);

                if (FindSolidIntersection(
                    fittingSolids,
                    conduitSolids) != null)
                {
                    return true;
                }
            }

            return false;
        }

        // ================================================================
        // STATION / GEOMETRY
        // ================================================================

        private static bool TryGetStationRange(
            Solid solid,
            XYZ origin,
            XYZ direction,
            out double minStation,
            out double maxStation)
        {
            minStation = double.MaxValue;
            maxStation = double.MinValue;

            if (solid == null)
                return false;

            foreach (Edge edge in solid.Edges)
            {
                IList<XYZ> points =
                    edge.Tessellate();

                foreach (XYZ point in points)
                {
                    double station =
                        (point - origin)
                            .DotProduct(direction);

                    minStation =
                        Math.Min(
                            minStation,
                            station);

                    maxStation =
                        Math.Max(
                            maxStation,
                            station);
                }
            }

            return minStation != double.MaxValue &&
                   maxStation != double.MinValue;
        }

        private static void AddPoint(
            IList<XYZ> points,
            XYZ point)
        {
            if (points.Count == 0 ||
                points[points.Count - 1]
                    .DistanceTo(point) >
                PointToleranceMm * MmToFt)
            {
                points.Add(point);
            }
        }

        // ================================================================
        // SOLID EXTRACTION
        // ================================================================

        private static List<Solid> GetSolids(
            Element element)
        {
            List<Solid> solids =
                new List<Solid>();

            Options options =
                new Options
                {
                    ComputeReferences = false,
                    DetailLevel = ViewDetailLevel.Fine,
                    IncludeNonVisibleObjects = false
                };

            GeometryElement geometry =
                element.get_Geometry(options);

            if (geometry == null)
                return solids;

            CollectSolids(
                geometry,
                solids);

            return solids;
        }

        private static void CollectSolids(
            GeometryElement geometry,
            IList<Solid> solids)
        {
            foreach (GeometryObject obj in geometry)
            {
                Solid solid =
                    obj as Solid;

                if (solid != null &&
                    solid.Volume >
                    MinimumIntersectionVolume)
                {
                    solids.Add(solid);
                    continue;
                }

                GeometryInstance instance =
                    obj as GeometryInstance;

                if (instance == null)
                    continue;

                GeometryElement instanceGeometry =
                    instance.GetInstanceGeometry();

                if (instanceGeometry != null)
                {
                    CollectSolids(
                        instanceGeometry,
                        solids);
                }
            }
        }

        // ================================================================
        // BOUNDING BOX
        // ================================================================

        private static bool BoxesIntersect(
            BoundingBoxXYZ a,
            BoundingBoxXYZ b,
            double tolerance)
        {
            return
                a.Min.X <= b.Max.X + tolerance &&
                a.Max.X + tolerance >= b.Min.X &&
                a.Min.Y <= b.Max.Y + tolerance &&
                a.Max.Y + tolerance >= b.Min.Y &&
                a.Min.Z <= b.Max.Z + tolerance &&
                a.Max.Z + tolerance >= b.Min.Z;
        }

        // ================================================================
        // PARAMETER / GRAPHICS
        // ================================================================

        private static double GetParameterValue(
            Element element,
            BuiltInParameter parameter)
        {
            Parameter parameterValue =
                element.get_Parameter(parameter);

            if (parameterValue == null ||
                !parameterValue.HasValue)
            {
                return 0.0;
            }

            return parameterValue.AsDouble();
        }

        private static void OverrideAsPink(
            View view,
            ElementId elementId)
        {
            OverrideGraphicSettings settings =
                new OverrideGraphicSettings();

            settings.SetProjectionLineColor(
                new Color(255, 0, 180));

            settings.SetProjectionLineWeight(6);

            view.SetElementOverrides(
                elementId,
                settings);
        }

        // ================================================================
        // UI
        // ================================================================

        private static void ShowSummary(
            string title,
            string message)
        {
            TaskDialog.Show(
                title,
                message);
        }

        // ================================================================
        // DATA
        // ================================================================

        private sealed class ClashInfo
        {
            public CableTray Tray { get; set; }
            public Conduit Conduit { get; set; }
            public Solid Intersection { get; set; }
        }
    }
}
