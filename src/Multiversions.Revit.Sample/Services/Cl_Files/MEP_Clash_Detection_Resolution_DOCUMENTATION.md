# MEP Clash Detection & Resolution System
## Technical Architecture & Implementation Guide

---

## 1. SYSTEM OVERVIEW

### Problem Statement
Automated detection and resolution of clashes between conduit runs and cable trays in MEP coordination, while maintaining fitting constraints (bend radii, connection standards) and generating alternative routing paths.

### Scope
- **Input**: Existing conduit/cable tray elements from Revit model
- **Detection**: Spatial intersection analysis with configurable clearance tolerance
- **Resolution**: A* pathfinding algorithm to compute alternative routes
- **Constraints**: Bend radius and fitting type validation
- **Output**: New routing elements, clash reports, resolution summary

### Key Features
1. **Multi-component clash detection** (conduit-to-conduit, conduit-to-tray, tray-to-tray)
2. **Fitting constraint library** (elbows, tees, reducers, bends)
3. **3D spatial pathfinding** with cost minimization
4. **Automatic route generation** with Revit element creation
5. **Clash severity reporting** (critical, major, minor)

---

## 2. SYSTEM ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│         MEP CLASH DETECTION & RESOLUTION SYSTEM              │
└─────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  Revit Integration Layer                                      │
│  • Document access & element collection                       │
│  • MEP route parsing (conduit runs, cable trays)             │
│  • Element creation & property assignment                     │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Geometry & Spatial Analysis                                  │
│  • 3D bounding box construction                              │
│  • Intersection testing (AABB, swept volume)                 │
│  • Clearance validation                                       │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Clash Detection Engine                                       │
│  • Graph-based route representation                          │
│  • Clash identification & severity classification            │
│  • Clash zone marking                                        │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Fitting Constraint Manager                                   │
│  • Bend radius validation                                     │
│  • Connection type verification                              │
│  • Fitting cost/penalty calculation                          │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Pathfinding & Route Resolution                              │
│  • A* algorithm with cost heuristic                          │
│  • Multi-objective optimization                              │
│  • Waypoint generation & smoothing                           │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  Route Generation & Reporting                                │
│  • MEP conduit/tray creation                                 │
│  • Color tagging for clash visualization                     │
│  • JSON/CSV resolution report export                         │
└──────────────────────────────────────────────────────────────┘
```

---

## 3. COMPONENT SPECIFICATIONS

### 3.1 Geometric Models

#### RouteSegment (Conduit/Tray Section)
```
Properties:
  - RouteId: unique identifier
  - StartPoint: XYZ coordinate
  - EndPoint: XYZ coordinate
  - Diameter: nominal size (mm)
  - Clearance: safety buffer (mm)
  - FittingType: elbow, tee, reducer, straight
  - BendRadius: minimum bend radius per fitting spec
  - Material: material specification
  - ElementId: linked Revit element
```

#### Waypoint (Route Node)
```
Properties:
  - Position: XYZ coordinate
  - FittingType: connection type at this point
  - ConnectedSegments: list of adjacent segments
  - Cost: cumulative path cost to reach this point
```

### 3.2 Fitting Constraint Library

```
FittingSpec:
  - Name: "Elbow 90° Standard"
  - BendRadius: 150 mm (for 16mm conduit, example)
  - AllowedAngles: [45°, 90°, 135°]
  - Connection: screw-on, push-fit, welded
  - MaxTravelSpeed: affects routing cost
  
FittingDatabase (Extensible):
  - StandardElbow90
  - StandardElbow45
  - ThreewayTee
  - Reducer
  - StraightConnector
  - CustomFitting
```

### 3.3 Clash Classification

| Severity | Criteria | Action |
|----------|----------|--------|
| **Critical** | Physical intersection (0 mm clearance) | Must resolve immediately |
| **Major** | Clearance < min required | Reroute recommended |
| **Minor** | Clearance acceptable but suboptimal | Flag for review |
| **Resolved** | Alternative route found & validated | New route created |

---

## 4. ALGORITHM SPECIFICATIONS

### 4.1 Clash Detection Algorithm

```
Input: RouteList[], tolerance, clearance_min
Output: ClashList[]

Algorithm:
  1. FOR EACH route_pair IN RouteList:
       2. ComputeBoundingBoxes(route_pair)
       3. IF AABBIntersection(bbox1, bbox2):
            4. ComputeDistance(route1, route2)
            5. IF distance < clearance_min:
                 6. ClashList.Add({severity, location, routes})

  Complexity: O(n²) for n routes; can optimize with spatial indexing
```

### 4.2 A* Pathfinding Algorithm

```
Input: StartPoint, EndPoint, ObstacleZones[], FittingConstraints[]
Output: WaypointPath[]

Algorithm:
  1. OpenSet = {StartPoint}
  2. WHILE OpenSet not empty:
       3. Current = node with lowest f_score
       4. IF Current == EndPoint:
            5. RETURN reconstructed path
       6. FOR EACH neighbor IN GetNeighbors(Current):
            7. tentative_g = g_score[Current] + distance(Current, neighbor)
            8. IF tentative_g < g_score[neighbor]:
                 9. Update neighbor scores
                10. Add neighbor to OpenSet
  
  Cost Function:
    f(n) = g(n) + h(n)
    where:
      g(n) = actual path length + fitting penalties
      h(n) = heuristic distance to goal
      
  Fitting Penalty:
    + bend_radius_violation_cost
    + clearance_buffer_cost
    + material_change_cost
```

### 4.3 Constraint Validation

```
ValidatePath(Waypoints[], FittingSpec[]):
  FOR EACH segment IN Waypoints:
    1. Compute bend angle at this waypoint
    2. Retrieve fitting spec for this connection type
    3. IF bend_angle NOT IN FittingSpec.AllowedAngles:
         RETURN invalid
    4. IF bend_radius < FittingSpec.MinBendRadius:
         RETURN invalid
    5. Check clearance from obstacles at segment
    6. IF clearance < required_minimum:
         RETURN invalid
  RETURN valid
```

---

## 5. DATA FLOW

### Clash Detection Flow
```
Revit Model
    ↓
Extract MEP Elements (Conduit, CableTray)
    ↓
Build Spatial Geometry (BoundingBoxes, Centerlines)
    ↓
Pairwise Intersection Testing
    ↓
Clash Detected? YES → Classify Severity
                ↓
            Add to ClashList
    ↓
Output: ClashList with locations & routes
```

### Resolution Flow
```
ClashList
    ↓
FOR EACH Clash:
    ↓
    Identify Clash Zone (obstacle region)
    ↓
    Extract Route Geometry (start, end, existing path)
    ↓
    Run A* Pathfinding (with fitting constraints)
    ↓
    Generate Alternative Route
    ↓
    Validate Against Constraints
    ↓
    Create Revit Elements (new conduit/tray)
    ↓
    Tag Original Route (color = red/clash)
    ↓
    Tag Resolved Route (color = green/resolved)
    ↓
    Add to Resolution Report
    ↓
Output: Updated Model + Report
```

---

## 6. CONFIGURATION & PARAMETERS

### Default Configuration
```yaml
ClashDetection:
  MinClearance_mm: 50          # Minimum safe clearance between routes
  SearchResolution_mm: 100     # Grid granularity for pathfinding
  EnableSolidIntersection: true # Use swept volume analysis

FittingConstraints:
  StandardBendRadius_mm: 150   # Default for most fittings
  AllowCustomFittings: true
  BendAngleTolerance_deg: 5    # Allow ±5° deviation

Pathfinding:
  MaxIterations: 10000
  CostWeighting:
    Distance: 1.0
    BendPenalty: 0.5
    ClearancePenalty: 2.0
    MaterialChangePenalty: 1.5

Output:
  ColorClash: RGB(255, 0, 0)   # Red for clashes
  ColorResolved: RGB(0, 255, 0) # Green for resolved
  ExportFormat: JSON, CSV
```

---

## 7. IMPLEMENTATION PHASES

### Phase 1: Core Geometry & Detection (Week 1)
- RouteSegment model & BoundingBox computation
- AABB intersection testing
- Basic clash classification

### Phase 2: Fitting Constraints (Week 2)
- FittingSpec library & database
- Constraint validation engine
- Bend radius & angle verification

### Phase 3: Pathfinding (Week 3)
- A* algorithm implementation
- Waypoint generation
- Cost function tuning

### Phase 4: Revit Integration (Week 4)
- MEP element extraction
- New conduit/tray creation
- Element tagging & coloring
- Report generation

### Phase 5: Testing & Optimization (Week 5)
- Integration tests with real models
- Performance profiling
- Algorithm tuning

---

## 8. VALIDATION & TESTING STRATEGY

### Unit Tests
```
✓ BoundingBoxIntersection
✓ DistanceCalculation
✓ FittingConstraintValidation
✓ PathfindingPathLength
✓ CostFunctionAccuracy
```

### Integration Tests
```
✓ End-to-end clash detection on sample model
✓ Route resolution quality assessment
✓ Fitting constraint enforcement
✓ Revit element creation accuracy
```

### Performance Benchmarks
```
Target: Process 100 routes in < 30 seconds
Target: Resolve 10 clashes in < 15 seconds
Memory: < 500 MB for typical MEP model
```

---

## 9. ERROR HANDLING & EDGE CASES

| Scenario | Handling |
|----------|----------|
| No valid route found | Return nearest alternative, flag for manual review |
| Impossible constraint combo | Mark as unresolvable, suggest constraint relaxation |
| Multiple simultaneous clashes | Process in severity order (critical → major → minor) |
| Cyclic route dependency | Detect & break cycle via temporary constraint relaxation |
| Disconnected geometry | Validate connectivity before/after resolution |

---

## 10. FUTURE ENHANCEMENTS

1. **Machine Learning Integration**: Train model on manual resolutions for smarter pathfinding
2. **Multi-discipline Coordination**: Structural, architectural, mechanical simultaneous checking
3. **Cost Optimization**: Material & labor cost in routing decisions
4. **VR Visualization**: Immersive clash visualization & manual adjustment
5. **API Export**: REST/GraphQL for BIM 360, Revit Cloud integration

---

## 11. DEPENDENCIES & TECH STACK

- **Revit API** (2022+)
- **.NET 6 / C# 10**
- **Autodesk.Revit.DB** namespace
- **Optional**: RhinoCommon (for advanced geometry)
- **Optional**: AI ML libraries (future ML phase)

---

## 12. GLOSSARY

| Term | Definition |
|------|-----------|
| **Route** | Continuous conduit or cable tray run from start to end point |
| **Segment** | Subsection of a route between two waypoints |
| **Waypoint** | Connection node on a route (fitting location) |
| **Clash Zone** | 3D region where two routes violate clearance tolerance |
| **Bend Radius** | Minimum curvature radius for a conduit/tray at a fitting |
| **Fitting** | Connection component (elbow, tee, reducer, straight) |
| **Clearance** | Safe distance between two MEP elements |

---

**Document Version**: 1.0  
**Last Updated**: August 2026  
**Author**: Senior Computational Design Engineer
