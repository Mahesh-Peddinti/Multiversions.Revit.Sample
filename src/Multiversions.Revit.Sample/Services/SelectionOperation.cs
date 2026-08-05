using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Services
{
    public enum SelectionOperation
    {
        StartConnector,
        EndConnector,
        ConnectorSet
    }
}

//Other Model data
//Folder Strucrure
/*
ClashDetectionTool/
 ├── ClashDetectionApp.cs              // ExternalApplication entry (Ribbon setup)
 ├── ClashDetectionCommand.cs          // ExternalCommand entry (launches modeless WPF)
 ├── ClashDetectionTool.addin          // Revit add-in manifest
 │
 ├── Models/
 │    └── ClashResult.cs               // Clash result data model with metadata + suggestions
 │
 ├── Services/
 │    ├── ClashDetectionService.cs     // Detection engine (bounding box + solid intersection)
 │    ├── VisualizationService.cs      // Highlight clashes in Revit view
 │    ├── NavigationService.cs         // Zoom/pan to clash location
 │    ├── ReportService.cs             // Export clash reports (CSV)
 │    ├── ResolutionService.cs         // Rule-based + smart routing suggestions
 │    └── RoutingService.cs            // A* pathfinding + reroute element replacement
 │
 ├── UI/
 │    ├── MainWindow.xaml              // Modeless WPF window with stage tabs
 │    ├── MainWindow.xaml.cs
 │    │
 │    ├── Views/
 │    │    ├── DetectionView.xaml      // Stage 1–5 integrated UI
 │    │    ├── DetectionView.xaml.cs
 │    │    ├── VisualizationView.xaml  // (future expansion)
 │    │    ├── NavigationView.xaml     // (future expansion)
 │    │    ├── WorkflowView.xaml       // (future expansion)
 │    │    └── ResolutionView.xaml     // (future expansion)
 │    │
 │    └── ViewModels/
 │         ├── DetectionViewModel.cs   // Connected to all services, manages clash workflow
 │         ├── VisualizationViewModel.cs
 │         ├── NavigationViewModel.cs
 │         ├── WorkflowViewModel.cs
 │         └── ResolutionViewModel.cs
 │
 ├── Utils/
 │    └── RelayCommand.cs              // MVVM command helper
 │
 └── Properties/
      └── AssemblyInfo.cs

      
*/
//Application
public class ClashDetectionApp : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        // Add ribbon panel & button
        RibbonPanel panel = application.CreateRibbonPanel("Clash Tool");
        PushButtonData buttonData = new PushButtonData(
            "ClashToolBtn", "Clash Tool",
            Assembly.GetExecutingAssembly().Location,
            "ClashDetectionTool.ClashDetectionCommand");
        panel.AddItem(buttonData);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}
//Command
[Transaction(TransactionMode.Manual)]
public class ClashDetectionCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiApp = commandData.Application;
        MainWindow window = new MainWindow(uiApp);
        window.Show(); // Modeless
        return Result.Succeeded;
    }
}

//UI-Main Window
<Window x:Class="ClashDetectionTool.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Clash Detection Tool" Height="600" Width="800">
    <TabControl>
        <TabItem Header="Detection">
            <local:DetectionView />
        </TabItem>
        <TabItem Header="Visualization">
            <local:VisualizationView />
        </TabItem>
        <TabItem Header="Navigation">
            <local:NavigationView />
        </TabItem>
        <TabItem Header="Workflow">
            <local:WorkflowView />
        </TabItem>
        <TabItem Header="Resolution">
            <local:ResolutionView />
        </TabItem>
    </TabControl>
</Window>

=============================================================================================================
//DetectionView.xaml
<UserControl x:Class="ClashDetectionTool.UI.Views.DetectionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <StackPanel Margin="10">
        <Button Content="Run Clash Detection" Command="{Binding RunDetectionCommand}" />

        <ListBox ItemsSource="{Binding FilteredResults}" DisplayMemberPath="Description"
                 SelectedItem="{Binding SelectedClash}" Height="200"/>

        <StackPanel Orientation="Horizontal" Margin="5">
            <Button Content="Mark Resolved" Command="{Binding MarkResolvedCommand}" />
            <Button Content="Mark Ignored" Command="{Binding MarkIgnoredCommand}" />
            <Button Content="Generate Suggestions" Command="{Binding GenerateSuggestionsCommand}" />
        </StackPanel>

        <TextBlock Text="Resolution Suggestions:" FontWeight="Bold" Margin="5"/>
        <ItemsControl ItemsSource="{Binding SelectedClash.Suggestions}" />

        <Button Content="Export Report" Command="{Binding ExportReportCommand}" />
    </StackPanel>
</UserControl>







===================================================================================================
//DetectionViewModel.xaml.cs
public class DetectionViewModel
{
    private readonly ClashDetectionService _clashService;
    private readonly ReportService _reportService;
    private readonly ResolutionService _resolutionService;

    public ObservableCollection<ClashResult> ClashResults { get; set; } = new();
    public ObservableCollection<ClashResult> FilteredResults { get; set; } = new();

    public ICommand RunDetectionCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand MarkResolvedCommand { get; }
    public ICommand MarkIgnoredCommand { get; }
    public ICommand GenerateSuggestionsCommand { get; }

    public ClashResult SelectedClash { get; set; }

    public DetectionViewModel(Document doc)
    {
        _clashService = new ClashDetectionService(doc);
        _reportService = new ReportService();
        _resolutionService = new ResolutionService(doc);

        RunDetectionCommand = new RelayCommand(RunDetection);
        ExportReportCommand = new RelayCommand(ExportReport);
        ApplyFilterCommand = new RelayCommand(ApplyFilter);
        MarkResolvedCommand = new RelayCommand(MarkResolved);
        MarkIgnoredCommand = new RelayCommand(MarkIgnored);
        GenerateSuggestionsCommand = new RelayCommand(GenerateSuggestions);
    }

    private void RunDetection()
    {
        ClashResults.Clear();
        var clashes = _clashService.DetectClashes();
        foreach (var clash in clashes)
        {
            ClashResults.Add(clash);
        }
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredResults.Clear();
        foreach (var clash in ClashResults)
        {
            if (SelectedClash != null && clash != SelectedClash) continue;
            FilteredResults.Add(clash);
        }
    }

    private void MarkResolved()
    {
        if (SelectedClash != null)
        {
            SelectedClash.Status = "Resolved";
            ApplyFilter();
        }
    }

    private void MarkIgnored()
    {
        if (SelectedClash != null)
        {
            SelectedClash.Status = "Ignored";
            ApplyFilter();
        }
    }

    private void GenerateSuggestions()
    {
        if (SelectedClash != null)
        {
            _resolutionService.GenerateSuggestions(SelectedClash);
            ApplyFilter();
        }
    }

    private void ExportReport()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ClashReport.csv");
        _reportService.ExportToCsv(FilteredResults, path);
        TaskDialog.Show("Export", $"Report exported to {path}");
    }
}



=====================================================================================================================
//Service layer
//ClashDetectionService
public class ClashDetectionService
{
    private readonly Document _doc;
    public ClashDetectionService(Document doc) => _doc = doc;

    public List<ClashResult> DetectClashes()
    {
        var results = new List<ClashResult>();

        // Collect candidate elements (example: ducts + pipes + walls)
        var collector = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.OST_DuctCurves)
            .ToList();

        var others = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.OST_Walls)
            .ToList();

        foreach (var e1 in collector)
        {
            Solid s1 = GetSolid(e1);
            if (s1 == null) continue;

            foreach (var e2 in others)
            {
                Solid s2 = GetSolid(e2);
                if (s2 == null) continue;

                // Quick bounding box check
                BoundingBoxXYZ bb1 = e1.get_BoundingBox(null);
                BoundingBoxXYZ bb2 = e2.get_BoundingBox(null);
                if (!BoundingBoxesIntersect(bb1, bb2)) continue;

                // Precise solid intersection
                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(s1, s2, BooleanOperationsType.Intersect);
                if (intersection != null && intersection.Volume > 1e-6)
                {
                    results.Add(new ClashResult
                    {
                        Element1 = e1.Id,
                        Element2 = e2.Id,
                        Description = $"{e1.Name} clashes with {e2.Name}"
                    });
                }
            }
        }

        return results;
    }

    private Solid GetSolid(Element e)
    {
        Options opt = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
        GeometryElement geo = e.get_Geometry(opt);
        if (geo == null) return null;

        foreach (GeometryObject obj in geo)
        {
            if (obj is Solid solid && solid.Volume > 0) return solid;
        }
        return null;
    }

    private bool BoundingBoxesIntersect(BoundingBoxXYZ bb1, BoundingBoxXYZ bb2)
    {
        if (bb1 == null || bb2 == null) return false;
        return !(bb1.Max.X < bb2.Min.X || bb1.Min.X > bb2.Max.X ||
                 bb1.Max.Y < bb2.Min.Y || bb1.Min.Y > bb2.Max.Y ||
                 bb1.Max.Z < bb2.Min.Z || bb1.Min.Z > bb2.Max.Z);
    }
}

public class ClashResult
{
    public ElementId Element1 { get; set; }
    public ElementId Element2 { get; set; }
    public string Description { get; set; }
    public string Category1 { get; set; }
    public string Category2 { get; set; }
    public string Severity { get; set; } // e.g., Critical, Warning
    public string Status { get; set; } = "Open"; // Open, Resolved, Ignored
    public string DisciplineGroup => $"{Category1}-{Category2}";
    public List<string> Suggestions { get; set; } = new();
}



//Visualization Service
public class VisualizationService
{
    private readonly Document _doc;
    public VisualizationService(Document doc) => _doc = doc;

    public void HighlightElements(List<ElementId> ids)
    {
        using (Transaction tx = new Transaction(_doc, "Highlight Clash"))
        {
            tx.Start();
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(new Autodesk.Revit.DB.Color(255, 0, 0)); // red
            ogs.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(255, 200, 200));

            foreach (var id in ids)
            {
                _doc.ActiveView.SetElementOverrides(id, ogs);
            }
            tx.Commit();
        }
    }
}

//Navigation Services

public class NavigationService
{
    private readonly UIApplication _uiApp;
    public NavigationService(UIApplication uiApp) => _uiApp = uiApp;

    public void ZoomToElements(List<ElementId> ids)
    {
        UIDocument uidoc = _uiApp.ActiveUIDocument;
        View activeView = uidoc.ActiveView;

        BoundingBoxXYZ combined = null;
        foreach (var id in ids)
        {
            Element e = uidoc.Document.GetElement(id);
            BoundingBoxXYZ bb = e.get_BoundingBox(activeView);
            if (bb == null) continue;

            if (combined == null) combined = bb;
            else
            {
                combined.Min = new XYZ(
                    Math.Min(combined.Min.X, bb.Min.X),
                    Math.Min(combined.Min.Y, bb.Min.Y),
                    Math.Min(combined.Min.Z, bb.Min.Z));
                combined.Max = new XYZ(
                    Math.Max(combined.Max.X, bb.Max.X),
                    Math.Max(combined.Max.Y, bb.Max.Y),
                    Math.Max(combined.Max.Z, bb.Max.Z));
            }
        }

        if (combined != null)
        {
            UIView uiview = _uiApp.ActiveUIDocument.GetOpenUIViews()
                .FirstOrDefault(v => v.ViewId == activeView.Id);
            if (uiview != null)
            {
                uiview.ZoomAndCenterRectangle(combined.Min, combined.Max);
            }
        }
    }
}



//ClashDetectionServices.cs
private string GetSeverity(Solid intersection)
{
    if (intersection.Volume > 0.1) return "Critical";
    if (intersection.Volume > 0.01) return "Warning";
    return "Minor";
}

public List<ClashResult> DetectClashes()
{
    var results = new List<ClashResult>();
    // ... detection loop ...
    if (intersection != null && intersection.Volume > 1e-6)
    {
        results.Add(new ClashResult
        {
            Element1 = e1.Id,
            Element2 = e2.Id,
            Description = $"{e1.Name} clashes with {e2.Name}",
            Category1 = e1.Category?.Name ?? "Unknown",
            Category2 = e2.Category?.Name ?? "Unknown",
            Severity = GetSeverity(intersection)
        });
    }
    // ...
    return results;
}


//ReportServices.Cs
public class ReportService
{
    public void ExportToCsv(IEnumerable<ClashResult> clashes, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Element1,Element2,Category1,Category2,Severity,Status,DisciplineGroup,Description");
            foreach (var clash in clashes)
            {
                writer.WriteLine($"{clash.Element1},{clash.Element2},{clash.Category1},{clash.Category2},{clash.Severity},{clash.Status},{clash.DisciplineGroup},{clash.Description}");
            }
        }
    }
}


=================================================================================================
//Resolution Services.cs
public class ResolutionService
{
    private readonly Document _doc;
    private readonly RoutingService _routingService;

    public ResolutionService(Document doc)
    {
        _doc = doc;
        _routingService = new RoutingService(doc);
    }

    public void GenerateSuggestions(ClashResult clash)
    {
        clash.Suggestions.Clear();

        if (clash.Category1.Contains("Pipe") || clash.Category2.Contains("Pipe"))
        {
            clash.Suggestions.Add("Shift pipe laterally by 100mm.");
            clash.Suggestions.Add("Consider reducing pipe diameter if design allows.");

            // Smart routing suggestion
            Element pipeElement = _doc.GetElement(clash.Element1);
            if (pipeElement == null || !(pipeElement is Pipe))
                pipeElement = _doc.GetElement(clash.Element2);

            if (pipeElement is Pipe pipe)
            {
                XYZ start = (pipe.Location as LocationCurve)?.Curve.GetEndPoint(0);
                XYZ goal = (pipe.Location as LocationCurve)?.Curve.GetEndPoint(1);

                var obstacles = CollectObstacles();
                var path = _routingService.ComputeRoute(start, goal, obstacles);

                if (path.Any())
                {
                    clash.Suggestions.Add($"Smart reroute path applied with {path.Count} segments.");
                    _routingService.ReplacePipeWithRoute(pipe.Id, path);
                }
            }
        }
    }

    private List<BoundingBoxXYZ> CollectObstacles()
    {
        var obstacles = new List<BoundingBoxXYZ>();
        var collector = new FilteredElementCollector(_doc)
            .WhereElementIsNotElementType()
            .OfCategory(BuiltInCategory.OST_Walls);

        foreach (var e in collector)
        {
            var bb = e.get_BoundingBox(null);
            if (bb != null) obstacles.Add(bb);
        }
        return obstacles;
    }
}


==============================================================================================================
//Routing Services
//A* Path finding implementation
public class RoutingService
{
    private readonly Document _doc;
    public RoutingService(Document doc) => _doc = doc;

    public List<XYZ> ComputeRoute(XYZ start, XYZ goal, List<BoundingBoxXYZ> obstacles)
    {
        var openSet = new SortedSet<Node>(new NodeComparer());
        var closedSet = new HashSet<XYZ>();

        var startNode = new Node(start, null, 0, Heuristic(start, goal));
        openSet.Add(startNode);

        while (openSet.Any())
        {
            var current = openSet.Min;
            openSet.Remove(current);

            if (IsClose(current.Position, goal))
                return ReconstructPath(current);

            closedSet.Add(current.Position);

            foreach (var neighbor in GetNeighbors(current.Position))
            {
                if (closedSet.Contains(neighbor)) continue;
                if (IsBlocked(neighbor, obstacles)) continue;

                double tentativeG = current.G + current.Position.DistanceTo(neighbor);
                var neighborNode = new Node(neighbor, current, tentativeG, Heuristic(neighbor, goal));

                var existing = openSet.FirstOrDefault(n => n.Position.IsAlmostEqualTo(neighbor));
                if (existing == null || tentativeG < existing.G)
                {
                    if (existing != null) openSet.Remove(existing);
                    openSet.Add(neighborNode);
                }
            }
        }
        return new List<XYZ>(); // no path found
    }

    private double Heuristic(XYZ a, XYZ b) => a.DistanceTo(b);

    private bool IsClose(XYZ a, XYZ b) => a.DistanceTo(b) < 0.1;

    private List<XYZ> GetNeighbors(XYZ pos)
    {
        double step = 0.5; // grid step in feet/meters
        return new List<XYZ>
        {
            new XYZ(pos.X+step, pos.Y, pos.Z),
            new XYZ(pos.X-step, pos.Y, pos.Z),
            new XYZ(pos.X, pos.Y+step, pos.Z),
            new XYZ(pos.X, pos.Y-step, pos.Z),
            new XYZ(pos.X, pos.Y, pos.Z+step),
            new XYZ(pos.X, pos.Y, pos.Z-step)
        };
    }

    private bool IsBlocked(XYZ point, List<BoundingBoxXYZ> obstacles)
    {
        foreach (var bb in obstacles)
        {
            if (point.X >= bb.Min.X && point.X <= bb.Max.X &&
                point.Y >= bb.Min.Y && point.Y <= bb.Max.Y &&
                point.Z >= bb.Min.Z && point.Z <= bb.Max.Z)
                return true;
        }
        return false;
    }

    private List<XYZ> ReconstructPath(Node node)
    {
        var path = new List<XYZ>();
        while (node != null)
        {
            path.Add(node.Position);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }

    private class Node
    {
        public XYZ Position { get; }
        public Node Parent { get; }
        public double G { get; }
        public double F { get; }

        public Node(XYZ pos, Node parent, double g, double h)
        {
            Position = pos;
            Parent = parent;
            G = g;
            F = g + h;
        }
    }

    private class NodeComparer : IComparer<Node>
    {
        public int Compare(Node x, Node y)
        {
            int comp = x.F.CompareTo(y.F);
            if (comp == 0) comp = x.Position.GetHashCode().CompareTo(y.Position.GetHashCode());
            return comp;
        }
    }
    public void DrawPath(List<XYZ> path)
    {
        if (path == null || path.Count < 2) return;

        using (Transaction tx = new Transaction(_doc, "Draw Routing Path"))
        {
            tx.Start();

            SketchPlane sp = SketchPlane.Create(_doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));

            for (int i = 0; i < path.Count - 1; i++)
            {
                Line line = Line.CreateBound(path[i], path[i + 1]);
                _doc.Create.NewModelCurve(line, sp);
            }

            tx.Commit();
        }
    } 

    public void ReplacePipeWithRoute(ElementId pipeId, List<XYZ> path)
    {
        if (path == null || path.Count < 2) return;

        using (Transaction tx = new Transaction(_doc, "Replace Pipe with Reroute"))
        {
            tx.Start();

            // Delete the original pipe
            _doc.Delete(pipeId);

            // Create new pipe segments along path
            MEPSystemType systemType = new FilteredElementCollector(_doc)
                .OfClass(typeof(MEPSystemType))
                .Cast<MEPSystemType>()
                .FirstOrDefault();

            PipeType pipeType = new FilteredElementCollector(_doc)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .FirstOrDefault();

            Level level = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault();

            for (int i = 0; i < path.Count - 1; i++)
            {
                Pipe.Create(_doc, systemType.Id, pipeType.Id, level.Id, path[i], path[i + 1]);
            }

            tx.Commit();
        }
    }
    
}








