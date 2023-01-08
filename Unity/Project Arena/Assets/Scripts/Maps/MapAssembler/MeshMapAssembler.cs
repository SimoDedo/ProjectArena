using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Maps.MapAssembler
{
    /// <summary>
    ///     MeshMapAssembler is an implementation of MapAssembler that assembles the maps using meshes.
    /// </summary>
    public class MeshMapAssembler : MapAssembler
    {
        private const int TOP_LEFT_WALL = 8;
        private const int TOP_RIGHT_WALL = 4;
        private const int BOTTOM_RIGHT_WALL = 2;
        private const int BOTTOM_LEFT_WALL = 1;
        [SerializeField] private bool isSkyVisibile;

        [Header("Mesh materials")] [SerializeField]
        private Material topMaterial;

        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material floorMaterial;

        // We use this so that if we have checked a vertex we won't check it again;
        private readonly HashSet<int> checkedVertices = new HashSet<int>();

        // We can have multiple outlines, each one is a list of vertices.
        private readonly List<List<int>> outlines = new List<List<int>>();

        // A dictionary contains key-value pairs. We use the vertex index as a key and as value the list 
        // off all triangles that own that vertex.
        private readonly Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
        private MeshCollider floorCollider;
        private MeshFilter floorMeshFilter;
        private GameObject navMeshObject;

        private SquareGrid squareGrid;
        private MeshCollider topCollider;
        private MeshFilter topMeshFilter;
        private List<int> triangles;

        private List<Vector3> vertices;
        private MeshCollider wallsCollider;
        private MeshFilter wallsMeshFilter;

        private void Start()
        {
            GameObject childObject;

            childObject = new GameObject("Top - Mesh Filter");
            childObject.transform.parent = transform;
            childObject.transform.localPosition = Vector3.zero;
            topMeshFilter = childObject.AddComponent<MeshFilter>();
            childObject.AddComponent<MeshRenderer>();
            childObject.GetComponent<MeshRenderer>().material = topMaterial;

            childObject = new GameObject("Walls");
            childObject.transform.parent = transform;
            childObject.transform.localPosition = Vector3.zero;
            wallsMeshFilter = childObject.AddComponent<MeshFilter>();
            childObject.AddComponent<MeshRenderer>();
            childObject.GetComponent<MeshRenderer>().material = wallMaterial;
            wallsCollider = childObject.AddComponent<MeshCollider>();
            var wallSurfaceModifier = childObject.AddComponent<NavMeshModifier>();
            wallSurfaceModifier.overrideArea = true;
            wallSurfaceModifier.area = NavMesh.GetAreaFromName("Not Walkable");
            childObject.isStatic = true;


            if (!isSkyVisibile)
            {
                childObject = new GameObject("Top - Collider");
                childObject.transform.parent = transform;
                childObject.transform.localPosition = Vector3.zero;
                topCollider = childObject.AddComponent<MeshCollider>();
            }

            childObject = new GameObject("Floor");
            childObject.transform.parent = transform;
            childObject.transform.localPosition = Vector3.zero;
            floorCollider = childObject.AddComponent<MeshCollider>();
            floorMeshFilter = childObject.AddComponent<MeshFilter>();
            childObject.AddComponent<MeshRenderer>();
            childObject.GetComponent<MeshRenderer>().material = floorMaterial;
            childObject.isStatic = true;
            SetReady(true);

            navMeshObject = new GameObject("NavMeshSurface");
            navMeshObject.transform.parent = transform;
            navMeshObject.transform.localPosition = Vector3.zero;
        }

        // Generates the Mesh.
        public override void AssembleMap(char[,] map, char wallChar, char roomChar)
        {
            outlines.Clear();
            checkedVertices.Clear();
            triangleDictionary.Clear();

            squareGrid = new SquareGrid(map, wallChar, mapScale, wallHeight);

            vertices = new List<Vector3>();
            triangles = new List<int>();

            var gridRows = squareGrid.squares.GetLength(0);
            var gridColumns = squareGrid.squares.GetLength(1);

            for (var r = 0; r < gridRows; r++)
            for (var c = 0; c < gridColumns; c++)
                TriangulateSquare(squareGrid.squares[r, c]);

            CreateTopMesh(map.GetLength(0), map.GetLength(1));

            CreateWallMesh();

            CreateFloorMesh(map.GetLength(0), map.GetLength(1));

            var navMesh = navMeshObject.AddComponent<NavMeshSurface>();
            navMesh.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMesh.BuildNavMesh();
        }

        public override void AssembleMap(
            List<char[,]> maps,
            char wallChar,
            char roomChar,
            char voidChar
        )
        {
        }

        // Creates the top mesh.
        private void CreateTopMesh(int rows, int columns)
        {
            if (isSkyVisibile)
            {
                var topMesh = new Mesh {vertices = vertices.ToArray(), triangles = triangles.ToArray()};
                topMesh.RecalculateNormals();
                topMeshFilter.mesh = topMesh;
            }
            else
            {
                var topMesh = CreateRectangularMesh(rows, columns, mapScale, wallHeight, isSkyVisibile);
                topMeshFilter.mesh = topMesh;
                topCollider.sharedMesh = topMesh;
            }
        }

        // Creates the wall mesh.
        private void CreateWallMesh()
        {
            CalculateMeshOutilnes();

            var wallVertices = new List<Vector3>();
            var wallTriangles = new List<int>();

            var wallsMesh = new Mesh();

            foreach (var outline in outlines)
                for (var i = 0; i < outline.Count - 1; i++)
                {
                    var startIndex = wallVertices.Count;
                    // Left vertex of the wall panel.
                    wallVertices.Add(vertices[outline[i]]);
                    // Rigth vertex of the wall panel.
                    wallVertices.Add(vertices[outline[i + 1]]);
                    // Bottom left vertex of the wall panel.
                    wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);
                    // Bottom rigth vertex of the wall panel.
                    wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight);

                    // The wall will be seen from inside so we wind them anticlockwise.
                    wallTriangles.Add(startIndex + 0);
                    wallTriangles.Add(startIndex + 2);
                    wallTriangles.Add(startIndex + 3);

                    wallTriangles.Add(startIndex + 3);
                    wallTriangles.Add(startIndex + 1);
                    wallTriangles.Add(startIndex + 0);
                }

            wallsMesh.vertices = wallVertices.ToArray();
            wallsMesh.triangles = wallTriangles.ToArray();
            wallsMesh.RecalculateNormals();

            wallsMeshFilter.mesh = wallsMesh;

            wallsCollider.sharedMesh = wallsMesh;
        }

        // Creates the floor mesh.
        private void CreateFloorMesh(int rows, int columns)
        {
            var floorMesh = CreateRectangularMesh(rows, columns, mapScale, 0, true);

            floorMeshFilter.mesh = floorMesh;
            floorCollider.sharedMesh = floorMesh;
        }

        // Creates a rectangular mesh.
        private Mesh CreateRectangularMesh(
            int rectangleHeight,
            int rectangleWidth,
            float squareSize,
            float height,
            bool facingUpwards
        )
        {
            var mesh = new Mesh();

            var vertices = new Vector3[4];

            const int leftX = 0;
            var topZ = rectangleHeight * squareSize;
            var rightX = rectangleWidth * squareSize;
            const int bottomZ = 0;

            vertices[0] = new Vector3(leftX, height, topZ); //Top Left
            vertices[1] = new Vector3(rightX, height, topZ); //Top Right
            vertices[2] = new Vector3(rightX, height, bottomZ); //Bottom Right
            vertices[3] = new Vector3(leftX, height, bottomZ); //Bottom Left

            var triangles = !facingUpwards ? new[] {0, 3, 2, 2, 1, 0} : new[] {0, 2, 3, 1, 2, 0};

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        // Depending on the configuration of a Square I create the rigth mesh.
        private void TriangulateSquare(Square square)
        {
            switch (square.configuration)
            {
                case 0:
                    break;
                // 1 point cases.
                case /*1*/ BOTTOM_LEFT_WALL:
                    MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                    break;
                case /*2*/ BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                    break;
                case /*4*/ TOP_RIGHT_WALL:
                    MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                    break;
                case /*8*/ TOP_LEFT_WALL:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                    break;
                // 2 points cases.
                case /*3*/ BOTTOM_LEFT_WALL | BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft,
                        square.centreLeft);
                    break;
                case /*6*/ BOTTOM_RIGHT_WALL | TOP_RIGHT_WALL:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight,
                        square.centreBottom);
                    break;
                case /*9*/ TOP_LEFT_WALL | BOTTOM_LEFT_WALL:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom,
                        square.bottomLeft);
                    break;
                case /*12*/ TOP_LEFT_WALL | TOP_RIGHT_WALL:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight,
                        square.centreLeft);
                    break;
                case /*5*/ TOP_RIGHT_WALL | BOTTOM_LEFT_WALL:
                    MeshFromPoints(square.centreTop, square.topRight, square.centreRight,
                        square.centreBottom, square.bottomLeft, square.centreLeft);
                    break;
                case /*10*/ TOP_LEFT_WALL | BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight,
                        square.bottomRight, square.centreBottom, square.centreLeft);
                    break;
                // 3 points cases.
                case /*7*/ TOP_RIGHT_WALL | BOTTOM_LEFT_WALL | BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight,
                        square.bottomLeft, square.centreLeft);
                    break;
                case /*11*/ TOP_LEFT_WALL | BOTTOM_LEFT_WALL | BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight,
                        square.bottomRight, square.bottomLeft);
                    break;
                case /*13*/ TOP_LEFT_WALL | TOP_RIGHT_WALL | BOTTOM_LEFT_WALL:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight,
                        square.centreBottom, square.bottomLeft);
                    break;
                case /*14*/ TOP_LEFT_WALL | TOP_RIGHT_WALL | BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight,
                        square.centreBottom, square.centreLeft);
                    break;
                // 4 point case.
                case /*15*/ TOP_LEFT_WALL | TOP_RIGHT_WALL | BOTTOM_LEFT_WALL | BOTTOM_RIGHT_WALL:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight,
                        square.bottomLeft);
                    // All sorrounding Nodes are walls, so none of this vertices can belong to an 
                    // outline.
                    checkedVertices.Add(square.topLeft.vertexIndex);
                    checkedVertices.Add(square.bottomLeft.vertexIndex);
                    checkedVertices.Add(square.bottomRight.vertexIndex);
                    checkedVertices.Add(square.centreBottom.vertexIndex);
                    break;
            }
        }

        // Create a mesh from the Nodes passed as parameters.
        private void MeshFromPoints(params Node[] points)
        {
            AssignVertices(points);

            if (points.Length >= 3) CreateTriangle(points[0], points[1], points[2]);

            if (points.Length >= 4) CreateTriangle(points[0], points[2], points[3]);

            if (points.Length >= 5) CreateTriangle(points[0], points[3], points[4]);

            if (points.Length >= 6) CreateTriangle(points[0], points[4], points[5]);
        }

        // I add the Nodes to the vertices list after assigning them an incremental ID.
        private void AssignVertices(Node[] points)
        {
            for (var i = 0; i < points.Length; i++)
                // vertexIndex default value is -1, if the value is still the same the vertix has not
                // been assigned.
                if (points[i].vertexIndex == -1)
                {
                    points[i].vertexIndex = vertices.Count;
                    vertices.Add(points[i].position);
                }
        }

        // Create a new triangle by adding its vertices to the triangle list.
        private void CreateTriangle(Node a, Node b, Node c)
        {
            triangles.Add(a.vertexIndex);
            triangles.Add(b.vertexIndex);
            triangles.Add(c.vertexIndex);

            var triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
            AddTriangleToDictionary(triangle.vertexIndexA, triangle);
            AddTriangleToDictionary(triangle.vertexIndexB, triangle);
            AddTriangleToDictionary(triangle.vertexIndexC, triangle);
        }

        // Goes trough every single vertex in the map, it checks if it is an outline and if it follows
        // it until it meets up with itself. Then it adds it to the outline list.
        private void CalculateMeshOutilnes()
        {
            for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
                if (!checkedVertices.Contains(vertexIndex))
                {
                    var newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);

                    if (newOutlineVertex != -1)
                    {
                        checkedVertices.Add(vertexIndex);

                        var newOutline = new List<int>();
                        newOutline.Add(vertexIndex);
                        outlines.Add(newOutline);
                        FollowOutline(newOutlineVertex, outlines.Count - 1);
                        outlines[outlines.Count - 1].Add(vertexIndex);
                    }
                }
        }

        // Starting from a vertex it scans the outline the vertex belongs to.
        private void FollowOutline(int vertexIndex, int outlineIndex)
        {
            outlines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            var nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

            if (nextVertexIndex != -1) FollowOutline(nextVertexIndex, outlineIndex);
        }

        // Returns a connected vertex, if any, which forms an outline edge with the one passed as 
        // parameter.
        private int GetConnectedOutlineVertex(int vertexIndex)
        {
            // List of all the triangles containing the vertex index.
            var trianglesContainingVertex = triangleDictionary[vertexIndex];

            for (var i = 0; i < trianglesContainingVertex.Count; i++)
            {
                var triangle = trianglesContainingVertex[i];

                for (var j = 0; j < 3; j++)
                {
                    var vertexB = triangle[j];

                    if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                        if (IsOutlineEdge(vertexIndex, vertexB))
                            return vertexB;
                }
            }

            return -1;
        }

        // Given two vertex indeces tells if they define an edge, this happens if the share only a 
        // triangle.
        private bool IsOutlineEdge(int vertexA, int vertexB)
        {
            var trianglesContainingVertexA = triangleDictionary[vertexA];
            var sharedTriangleCount = 0;

            for (var i = 0; i < trianglesContainingVertexA.Count; i++)
                if (trianglesContainingVertexA[i].Contains(vertexB))
                {
                    sharedTriangleCount++;

                    if (sharedTriangleCount > 1) break;
                }

            return sharedTriangleCount == 1;
        }

        // Adds a triangle to the dictionary. 
        private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
        {
            if (triangleDictionary.ContainsKey(vertexIndexKey))
            {
                triangleDictionary[vertexIndexKey].Add(triangle);
            }
            else
            {
                var triangleList = new List<Triangle>();
                triangleList.Add(triangle);
                triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        public override void ClearMap()
        {
            topMeshFilter.mesh.Clear();
            wallsMeshFilter.mesh.Clear();
            floorMeshFilter.mesh.Clear();
            wallsCollider.sharedMesh.Clear();
            floorCollider.sharedMesh.Clear();
            if (!isSkyVisibile) topCollider.sharedMesh.Clear();

            var navMeshes = navMeshObject.GetComponents<NavMeshSurface>();
            foreach (var navMesh in navMeshes) Destroy(navMesh);
        }

        // A triangle is generated by three vertices.
        public struct Triangle
        {
            public int vertexIndexA;
            public int vertexIndexB;
            public int vertexIndexC;
            private readonly int[] vertices;

            public Triangle(int a, int b, int c)
            {
                vertexIndexA = a;
                vertexIndexB = b;
                vertexIndexC = c;

                vertices = new int[3];
                vertices[0] = a;
                vertices[1] = b;
                vertices[2] = c;
            }

            // An indexer allows to get elements of a struct as an array.
            public int this[int i] => vertices[i];

            public bool Contains(int vertexIndex)
            {
                return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB ||
                       vertexIndex == vertexIndexC;
            }
        }

        // Generates the grid of Squares used to generate the Mesh.
        public class SquareGrid
        {
            public Square[,] squares;

            public SquareGrid(char[,] map, char charWall, float squareSize, float wallHeight)
            {
                var rows = map.GetLength(0);
                var columns = map.GetLength(1);

                // We create a grid of Control Nodes.
                var controlNodes = new ControlNode[rows, columns];

                var halfSquareSize = 0.5f;
                for (var r = 0; r < rows; r++)
                for (var c = 0; c < columns; c++)
                {
                    var pos = new Vector3(halfSquareSize + c * squareSize, wallHeight,
                        halfSquareSize + (rows - r - 1) * squareSize);
                    controlNodes[r, c] = new ControlNode(pos, map[r, c] == charWall, squareSize);
                }

                // We create a grid of Squares out of the Control Nodes.
                squares = new Square[rows - 1, columns - 1];

                for (var r = 0; r < rows - 1; r++)
                for (var c = 0; c < columns - 1; c++)
                    squares[r, c] = new Square(controlNodes[r, c], controlNodes[r, c + 1],
                        controlNodes[r + 1, c + 1], controlNodes[r + 1, c]);
            }
        }

        // a   1   b    a, b, c, d are Control Nodes, each of them owns two Nodes (1, 2, 3, 4). 
        // 2       3    e.g.: c owns 2 and 4. A square contains 4 Control Nodes, 4 Nodes and a 
        // c   4   d    configuration,which depends on which Control Nodes are active.
        public class Square
        {
            public Node centreTop, centreRight, centreBottom, centreLeft;
            public int configuration;
            public ControlNode topLeft, topRight, bottomRight, bottomLeft;

            public Square(ControlNode tl, ControlNode tr, ControlNode br, ControlNode bl)
            {
                topLeft = tl;
                topRight = tr;
                bottomRight = br;
                bottomLeft = bl;

                centreTop = topLeft.right;
                centreRight = bottomRight.above;
                centreBottom = bottomLeft.right;
                centreLeft = bottomLeft.above;

                if (topLeft.active) configuration |= TOP_LEFT_WALL;

                if (topRight.active) configuration |= TOP_RIGHT_WALL;

                if (bottomRight.active) configuration |= BOTTOM_RIGHT_WALL;

                if (bottomLeft.active) configuration |= BOTTOM_LEFT_WALL;
            }
        }

        // A NodeProperties is placed between two Control Nodes and belongs to a single Control NodeProperties.
        public class Node
        {
            public Vector3 position;
            public int vertexIndex = -1;

            public Node(Vector3 p)
            {
                position = p;
            }
        }

        // An active Control NodeProperties is a wall.
        public class ControlNode : Node
        {
            public Node above, right;
            public bool active;

            public ControlNode(Vector3 p, bool a, float squareSize) : base(p)
            {
                active = a;
                above = new Node(position + Vector3.forward * squareSize / 2f);
                right = new Node(position + Vector3.right * squareSize / 2f);
            }
        }
    }
}