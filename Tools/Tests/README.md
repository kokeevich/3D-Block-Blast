# Board model tests

The same NUnit tests can run without a scene in Unity 2022.3.62f3
(Test Runner → EditMode → BlockBlast.Model.Tests), or with .NET 8:

```sh
cd /workspace/3D-Block-Blast
dotnet test Tools/Tests/BoardModel.Tests.csproj
```

In the current cloud instance the SDK is installed outside the checkout:

```sh
/workspace/tools/dotnet/dotnet test Tools/Tests/BoardModel.Tests.csproj
```

14 cases cover occupancy, non-origin placement, boundaries, overlap rejection
without partial writes, invalid shapes, full/incomplete planes on all axes,
intersecting plane clearing, and placement availability on a full board.
These tests do not validate Unity object creation, scene wiring, rendering,
or the offer/orientation policy in GameManager. Those still need Unity checks.
No transition to line clearing is included here; this preserves the existing
plane rules until implementation-plan step 4.
