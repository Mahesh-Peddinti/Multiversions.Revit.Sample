//VisualizationView.xaml
<UserControl x:Class="ClashDetectionTool.UI.Views.VisualizationView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <StackPanel Margin="10">
        <TextBlock Text="Visualization Stage" FontWeight="Bold" Margin="5"/>
        <Button Content="Highlight Selected Clash" Command="{Binding HighlightCommand}" />
        <Button Content="Clear Highlights" Command="{Binding ClearHighlightsCommand}" />
    </StackPanel>
</UserControl>
============================================================================================
  //VisualizationViewModel.cs
  public class VisualizationViewModel
{
    public ICommand HighlightCommand { get; }
    public ICommand ClearHighlightsCommand { get; }

    private readonly VisualizationService _vizService;
    private readonly Document _doc;

    public VisualizationViewModel(Document doc)
    {
        _doc = doc;
        _vizService = new VisualizationService(doc);

        HighlightCommand = new RelayCommand(HighlightSelected);
        ClearHighlightsCommand = new RelayCommand(ClearHighlights);
    }

    private void HighlightSelected()
    {
        // Example: highlight all clashes currently detected
        // Extend with SelectedClash binding later
    }

    private void ClearHighlights()
    {
        using (Transaction tx = new Transaction(_doc, "Clear Highlights"))
        {
            tx.Start();
            foreach (ElementId id in new FilteredElementCollector(_doc).WhereElementIsNotElementType().ToElementIds())
            {
                _doc.ActiveView.SetElementOverrides(id, new OverrideGraphicSettings());
            }
            tx.Commit();
        }
    }
}
====================================================================================================================

  //NavigationView.xaml
  <UserControl x:Class="ClashDetectionTool.UI.Views.NavigationView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <StackPanel Margin="10">
        <TextBlock Text="Navigation Stage" FontWeight="Bold" Margin="5"/>
        <Button Content="Go To Selected Clash" Command="{Binding NavigateCommand}" />
    </StackPanel>
</UserControl>

//NavigationViewModel.cs

  public class NavigationViewModel
{
    public ICommand NavigateCommand { get; }
    private readonly NavigationService _navService;
    private readonly UIApplication _uiApp;

    public NavigationViewModel(UIApplication uiApp)
    {
        _uiApp = uiApp;
        _navService = new NavigationService(uiApp);
        NavigateCommand = new RelayCommand(NavigateToClash);
    }

    private void NavigateToClash()
    {
        // Example: navigate to currently selected clash
        // Extend with SelectedClash binding later
    }
}


//WorkflowView.xaml
<UserControl x:Class="ClashDetectionTool.UI.Views.WorkflowView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <StackPanel Margin="10">
        <TextBlock Text="Workflow Stage" FontWeight="Bold" Margin="5"/>
        <Button Content="Mark Clash Resolved" Command="{Binding MarkResolvedCommand}" />
        <Button Content="Mark Clash Ignored" Command="{Binding MarkIgnoredCommand}" />
        <Button Content="Export Workflow Report" Command="{Binding ExportWorkflowCommand}" />
    </StackPanel>
</UserControl>

//WorkflowViewModel.cs
  public class WorkflowViewModel
{
    public ICommand MarkResolvedCommand { get; }
    public ICommand MarkIgnoredCommand { get; }
    public ICommand ExportWorkflowCommand { get; }

    private readonly ReportService _reportService;
    private readonly ObservableCollection<ClashResult> _clashes;

    public WorkflowViewModel(ObservableCollection<ClashResult> clashes)
    {
        _clashes = clashes;
        _reportService = new ReportService();

        MarkResolvedCommand = new RelayCommand(MarkResolved);
        MarkIgnoredCommand = new RelayCommand(MarkIgnored);
        ExportWorkflowCommand = new RelayCommand(ExportWorkflow);
    }

    private void MarkResolved()
    {
        if (_clashes.Any()) _clashes.First().Status = "Resolved";
    }

    private void MarkIgnored()
    {
        if (_clashes.Any()) _clashes.First().Status = "Ignored";
    }

    private void ExportWorkflow()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WorkflowReport.csv");
        _reportService.ExportToCsv(_clashes, path);
        TaskDialog.Show("Export", $"Workflow report exported to {path}");
    }
}


//ResolutionView.xam
<UserControl x:Class="ClashDetectionTool.UI.Views.ResolutionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <StackPanel Margin="10">
        <TextBlock Text="Resolution Stage" FontWeight="Bold" Margin="5"/>
        <Button Content="Generate Auto-Resolution Suggestions" Command="{Binding GenerateSuggestionsCommand}" />
        <TextBlock Text="Suggestions:" FontWeight="Bold" Margin="5"/>
        <ItemsControl ItemsSource="{Binding SelectedClash.Suggestions}" />
    </StackPanel>
</UserControl>


  //ResolutionViewModel.cs
  public class ResolutionViewModel
{
    public ICommand GenerateSuggestionsCommand { get; }
    public ClashResult SelectedClash { get; set; }

    private readonly ResolutionService _resolutionService;

    public ResolutionViewModel(Document doc)
    {
        _resolutionService = new ResolutionService(doc);
        GenerateSuggestionsCommand = new RelayCommand(GenerateSuggestions);
    }

    private void GenerateSuggestions()
    {
        if (SelectedClash != null)
        {
            _resolutionService.GenerateSuggestions(SelectedClash);
        }
    }
}
===================================================================================================
//MainWindow.xaml
<Window x:Class="ClashDetectionTool.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:ClashDetectionTool.UI.Views"
        Title="Clash Detection & Resolution Tool"
        Height="600" Width="900">

    <Grid>
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
    </Grid>
</Window>


  //MainWindow.xaml.cs
  using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Windows;

namespace ClashDetectionTool.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow(UIApplication uiApp)
        {
            InitializeComponent();

            // Example: wire up DataContexts for each tab
            Document doc = uiApp.ActiveUIDocument.Document;

            // Detection tab
            DetectionView detectionView = new DetectionView();
            detectionView.DataContext = new ViewModels.DetectionViewModel(doc, uiApp);

            // Visualization tab
            VisualizationView visualizationView = new VisualizationView();
            visualizationView.DataContext = new ViewModels.VisualizationViewModel(doc);

            // Navigation tab
            NavigationView navigationView = new NavigationView();
            navigationView.DataContext = new ViewModels.NavigationViewModel(uiApp);

            // Workflow tab
            WorkflowView workflowView = new WorkflowView();
            workflowView.DataContext = new ViewModels.WorkflowViewModel(
                ((ViewModels.DetectionViewModel)detectionView.DataContext).ClashResults);

            // Resolution tab
            ResolutionView resolutionView = new ResolutionView();
            resolutionView.DataContext = new ViewModels.ResolutionViewModel(doc);

            // Assign views to TabItems
            ((System.Windows.Controls.TabItem)((System.Windows.Controls.TabControl)this.Content).Items[0]).Content = detectionView;
            ((System.Windows.Controls.TabItem)((System.Windows.Controls.TabControl)this.Content).Items[1]).Content = visualizationView;
            ((System.Windows.Controls.TabItem)((System.Windows.Controls.TabControl)this.Content).Items[2]).Content = navigationView;
            ((System.Windows.Controls.TabItem)((System.Windows.Controls.TabControl)this.Content).Items[3]).Content = workflowView;
            ((System.Windows.Controls.TabItem)((System.Windows.Controls.TabControl)this.Content).Items[4]).Content = resolutionView;
        }
    }
}


  
