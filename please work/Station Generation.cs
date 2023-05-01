using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace please_work
{
    //This is the node class it serves as a base for the grid.
    //Each stores its own relative position in the grid as well
    //as its world position and tile bitvalue.
    public class Node : IHeapItem<Node>
    {
        public int xPos, yPos;
        public Vector2 worldPosition;
        public Texture2D image;
        public int gCost, hCost;
        public bool isWalkable;
        public Node parent;
        public int bitValue;
        int heapIndex;
        public Node(int x, int y, int id, Texture2D _image, bool walkable, Vector2 position)
        {
            this.xPos = x;
            this.yPos = y;
            this.image = _image;
            this.isWalkable = walkable;
            this.worldPosition = position;
            this.bitValue = id;
            if (isWalkable)
                bitValue = 0;
            else
                bitValue = 1;
        }
        //For aStar pathfinding
        public int fCost
        {
            get
            {
                return gCost + hCost;
            }
        }
        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }
    }
    //A grid of nodes - 2D array. Also stores its world position.
    public class Grid : Game
    {
        int gridSizeX;
        int gridSizeY;
        public int gridWorldSize;
        public Node[,] grid;
        public int nodeRadius;
        int nodeDiameter;
        public Vector2 position;
        public Grid(int _gridWorldSize, int _nodeSize, Vector2 _position, Texture2D _sprite)
        {
            this.gridWorldSize = _gridWorldSize;
            this.nodeRadius = _nodeSize;
            this.position = _position;
            gridSizeX = gridWorldSize / nodeRadius;
            gridSizeY = gridWorldSize / nodeRadius;
            grid = new Node[gridSizeX, gridSizeY];
            nodeDiameter = nodeRadius * 2;
            Vector2 worldBottomLeft = position - new Vector2(_gridWorldSize / 2, 0) - new Vector2(0, gridWorldSize / 2);


            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector2 worldpoint = worldBottomLeft + new Vector2(1, 0) * (x * nodeDiameter + nodeRadius) + new Vector2(0, 1) * (y * nodeDiameter + nodeRadius);
                    grid[x, y] = new Node(x, y, 0, _sprite, true, worldpoint);
                }
            }
            //WriteToXML.WriteGridToXML(grid);
            //WriteToXML.ChangeXMLValueinGrid("5", 5 ,5);
        }
        //Given a node, get the 8 neighbours surrounding it.
        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.xPos + x;
                    int checkY = node.yPos + y;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }
            return neighbours;
        }
        //Gets the north, south, east and west neighbours of a node in a grid.
        public List<Node> GetCardinalNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();
            int[] dRow = { 0, 1, 0, -1 };
            int[] dCol = { -1, 0, 1, 0 };
            for (int i = 0; i < 4; i++)
            {
                int adjx = node.xPos + dRow[i];
                int adjy = node.yPos + dCol[i];
                if (adjx >= 0 && adjx < gridSizeX && adjy >= 0 && adjy < gridSizeY)
                    neighbours.Add(grid[adjx, adjy]);
            }
            return neighbours;
        }
        public int MaxSize
        {
            get
            {
                return gridSizeX * gridSizeY;
            }
        }
    }
    
    //Station class - each is made up of a number of floors represented by a node grid. 
    //The node grid can be used to generate procedural room layouts.
    public class Station : Game
    {
        public Grid station;
        List<Room> rooms = new List<Room>();
        Node[,] testGrid;
        RoomStruct stairs = new RoomStruct(1, 2, 2, 1, true);
        RoomStruct mediumStorage = new RoomStruct(3, 2, 3, 1, false);
        RoomStruct office = new RoomStruct(2, 3, 3, 1, false);
        RoomStruct storage = new RoomStruct(3, 4, 4, 1, false);
        RoomStruct bedroom = new RoomStruct(3, 4, 2, 1, false);
        RoomStruct bar = new RoomStruct(2, 5, 5, 1, false);
        RoomStruct largeStorage = new RoomStruct(3, 3, 4, 1, false);
        List<RoomStruct> roomTemplates = new List<RoomStruct>();
        bool roomPresent = false;
        int roomCount = 4;

        public Station(int _gridWorldSize, int _nodeSize, Vector2 _position, Texture2D _sprite, int _max)
        {
            station = new Grid(_gridWorldSize, _nodeSize, _position, _sprite);
            testGrid = station.grid;
            roomTemplates.Add(office);
            roomTemplates.Add(stairs);
            roomTemplates.Add(storage);
            roomTemplates.Add(bedroom);
            roomTemplates.Add(largeStorage);
            roomTemplates.Add(bar);
            //Some weird bug means that a bay isn't generated so this makes sure one is before proceding;
            while (!roomPresent)
            {
                CreateBay();
                foreach (Node n in testGrid)
                {
                    if (n.bitValue != 0)
                    {
                        roomPresent = true;
                        break;
                    }
                }
            }
            Room stairs2 = new Room(8, 9, 8, 9);
            rooms.Add(stairs2);
            roomCount = 7;
            CreateRooms(roomCount);
            CreateCorridors();
            CreateDoors();
            CleanUp();
        }
        //Checks if a propositioned room is out of bounds.
        bool IsOutOfBounds(Room room)
        {
            if (room.x1 > 0 && room.x2 < testGrid.GetLength(0) - 1 && room.y1 > 0 && room.y2 < testGrid.GetLength(1) - 1)
            {
                return false;
            }
            return true;
        }
        bool IsOutOfBounds(Node node)
        {
            if (node.xPos > 0 && node.xPos < testGrid.GetLength(0) - 1 && node.yPos > 0 && node.yPos < testGrid.GetLength(1) - 1)
            {
                return false;
            }
            return true;
        }
        //Creates a starship docking bay - all bays must be directly on the side of the grid.
        //This simply randomly determines which side the bay will be on and then generates it accordingly.
        void CreateBay()
        {
            Random rand = new Random();
            Room bay;
            int input = rand.Next(0, 4);
            int leftX = rand.Next(2, 4);
            int rightX = testGrid.GetLength(0) - (leftX + 1);
            int width = 6;
            switch (input)
            {
                case 0:
                    bay = new Room(0, width, leftX, rightX);
                    rooms.Add(bay);
                    for (int x = bay.GetLeft(); x <= bay.GetRight(); x++)
                    {
                        for (int y = bay.GetTop(); y <= bay.GetBottom(); y++)
                        {
                            if (x < testGrid.GetLength(0) && y < testGrid.GetLength(1))
                                testGrid[x, y].bitValue = 1;
                        }
                    }
                    break;
                case 1:
                    bay = new Room(testGrid.GetLength(0) - (width), testGrid.GetLength(0) - 1, leftX, rightX);
                    rooms.Add(bay);
                    for (int x = bay.GetLeft(); x <= bay.GetRight(); x++)
                    {
                        for (int y = bay.GetTop(); y <= bay.GetBottom(); y++)
                        {
                            if (x < testGrid.GetLength(0) && y < testGrid.GetLength(1))
                                testGrid[x, y].bitValue = 1;
                        }
                    }
                    break;
                case 2:
                    bay = new Room(leftX, rightX, 0, width);
                    rooms.Add(bay);
                    for (int x = bay.GetLeft(); x <= bay.GetRight(); x++)
                    {
                        for (int y = bay.GetTop(); y <= bay.GetBottom(); y++)
                        {
                            if (x < testGrid.GetLength(0) && y < testGrid.GetLength(1))
                                testGrid[x, y].bitValue = 1;
                        }
                    }
                    break;
                case 3:
                    bay = new Room(leftX, rightX, testGrid.GetLength(1) - 1, testGrid.GetLength(1) - (width));
                    rooms.Add(bay);
                    for (int x = bay.GetLeft(); x <= bay.GetRight(); x++)
                    {
                        for (int y = bay.GetTop(); y <= bay.GetBottom(); y++)
                        {
                            if (x < testGrid.GetLength(0) && y < testGrid.GetLength(1))
                                testGrid[x, y].bitValue = 1;
                        }
                    }
                    break;
            }
        }
        //Creates rooms by iterating over each node in the grid for a specific room template
        //and seeing if it will fit in the grid with that node as the room's top left node.
        void CreateRooms(int count)
        {   
            for(int i = 0; i < count; i++)
            {
                foreach(Node node in testGrid)
                {
                    if (node.bitValue != 0 || IsOnEdge(node))
                    {
                        continue;
                    }
                    else
                    {
                        //Uses the wieght given by each template to determine which room shall be chosen.
                        int totalWeight = 0;
                        foreach (RoomStruct r in roomTemplates)
                        {
                            totalWeight += r.weight;
                        }
                        var temp = UsefulFunctions.GetRoom(roomTemplates, totalWeight);
                        Room newRoom = new Room(node.xPos, node.xPos + temp.width - 1, node.yPos, node.yPos + temp.height - 1);
                        bool failed = false;
                        if (IsOutOfBounds(newRoom))
                        {
                            failed = true;
                        }
                        else
                        {
                            foreach (Room otherRoom in rooms)
                            {
                                if (newRoom.Intersects(otherRoom))
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }
                        if (!failed)
                        {
                            rooms.Add(newRoom);
                            roomCount -= temp.cost;
                            break;
                        }
                    }
                }
            }
            //Sets the bitvalues for the room tiles to 1.
            foreach (Room room in rooms)
            {
                for (int x = room.GetLeft(); x <= room.GetRight(); x++)
                {
                    for (int y = room.GetTop(); y <= room.GetBottom(); y++)
                    {
                        if (x < testGrid.GetLength(0) - 1 && y < testGrid.GetLength(1) - 1)
                            testGrid[x, y].bitValue = 1;
                    }
                }
            }
        }
        //Checks if a node is on the edge of the graph
        bool IsOnEdge(Node node)
        {
            if (node.xPos == 0 || node.yPos == 0 || node.xPos == testGrid.GetLength(0) - 1 || node.yPos == testGrid.GetLength(1) - 1)
            {
                return true;
            }
            return false;
        }
        //Creates corridors - see this link https://www.tomstephensondeveloper.co.uk/post/creating-simple-procedural-dungeon-generation
        //for more good info. It works by picking all points on the grid that have a bitValue of 0
        //And are surrounded by nodes with a bitValue of 0 and adding them to a stack of possible
        //startpoints. Each object in the stack is popped, checked if it is still a valid startpoint,
        //and then DFS is run on it to generate a corridor.
        void CreateCorridors()
        {
            Random rand = new Random();
            Stack<Node> possibleStarts = new Stack<Node>();
            foreach (Node node in testGrid)
            {
                bool isStartPos = true;
                if (node.bitValue != 0 || IsOnEdge(node))
                {
                    continue;
                }
                else
                {
                    foreach (Node neighbour in station.GetNeighbours(node))
                    {
                        if (neighbour.bitValue != 0)
                        {
                            isStartPos = false;
                            break;
                        }
                    }
                    if (isStartPos)
                    {
                        possibleStarts.Push(node);
                    }
                }
            }
            while (possibleStarts.Count > 0)
            {
                Node startNode = possibleStarts.Pop();
                if (startNode.bitValue == 0)
                {
                    DFS(startNode.xPos, startNode.yPos);
                }
                else
                    continue;
            }
        }
        //An implementation of the DFS algorithm
        void DFS(int row, int col)
        {
            int[] dRow = { 0, 1, 0, -1 };
            int[] dCol = { -1, 0, 1, 0 };
            bool[,] vis = new bool[testGrid.GetLength(0), testGrid.GetLength(1)];
            for (int i = 0; i < testGrid.GetLength(0); i++)
            {
                for (int j = 0; j < testGrid.GetLength(1); j++)
                {
                    vis[i, j] = false;
                }
            }

            // Initialize a stack of pairs and
            // push the starting cell into it
            Stack st = new Stack();
            st.Push(testGrid[row, col]);

            // Iterate until the
            // stack is not empty
            while (st.Count > 0)
            {

                // Pop the top pair
                Node curr = (Node)st.Peek();
                st.Pop();

                row = curr.xPos;
                col = curr.yPos;

                // Check if the current popped
                // cell is a valid cell or not
                if (!isValid(vis, row, col))
                    continue;

                // Mark the current
                // cell as visited
                vis[row, col] = true;

                // Print the element at
                // the current top cell
                testGrid[row, col].bitValue = 2;

                // Push all the adjacent cells
                for (int i = 0; i < 4; i++)
                {
                    int adjx = row + dRow[i];
                    int adjy = col + dCol[i];
                    foreach (Node n in station.GetNeighbours(testGrid[row, col]))
                    {
                        if (n.bitValue != 0)
                        {
                            break;
                        }
                        st.Push(testGrid[adjx, adjy]);
                    }
                }
            }
        }

        bool isValid(bool[,] vis, int row, int col)
        {

            // If cell is out of bounds
            if (row <= 0 || col <= 0 ||
                row >= testGrid.GetLength(0) - 1 || col >= testGrid.GetLength(1) - 1)
                return false;

            // If the cell is already visited
            if (vis[row, col])
                return false;

            //If not an empty cell
            if (testGrid[row, col].bitValue != 0)
                return false;

            //Checks if its next to an occupied node 
            foreach (Node n in station.GetNeighbours(testGrid[row, col]))
            {
                if (n.bitValue == 1)
                {
                    return false;
                }
            }
            //Checks if a node is surrounded by more than three corridor nodes on either North, South, east or west
            //This is to stop the painting of large empty areas as corridors instead of just 
            //creating corridors through them.
            int corridorIndex = 0;
            foreach (Node n in station.GetCardinalNeighbours(testGrid[row, col]))
            {
                if (n.bitValue == 2)
                {
                    corridorIndex++;
                    if (corridorIndex >= 2)
                    {
                        return false;
                    }
                }
            }
            // Otherwise, it can be visited
            return true;
        }

        void CreateDoors()
        {
            List<Node> doors = new List<Node>();
            Random randDoor = new Random();
            int[] dDir = { -1, 1 };
            bool[,] isDoor = new bool[testGrid.GetLength(0), testGrid.GetLength(1)];
            for(int i = 1;  i < testGrid.GetLength(0); i++)
            {
                for(int j = 1;  j < testGrid.GetLength(1); j++)
                {
                    if (testGrid[i,j].bitValue != 0)
                    {
                        //isDoor[i, j] = false;
                        continue;
                    }
                    else
                    {
                        int neighbourCount = 0;
                        foreach(Node node in station.GetCardinalNeighbours(testGrid[i,j]))
                        {
                            if(node.bitValue != 0) 
                            { 
                                neighbourCount++;
                            }
                            if(neighbourCount >= 2)
                            {
                                doors.Add(testGrid[i, j]);
                                //isDoor[i, j] = true;
                            }
                            //else 
                              // isDoor[i, j] = false;                                
                        }
                    }
                }
            }
            List<Node> horizontalDoors = doors;
            List<Node> verticalDoors = doors;
            for (int i = 0; i < doors.Count; i++)
            {
                Node n = doors[i];
                List<Node> temp = new List<Node>();
                Stack st = new Stack();
                st.Push(n);

                // Iterate until the
                // stack is empty
                while (st.Count > 0)
                {

                    // Pop the top pair
                    Node curr = (Node)st.Peek();
                    st.Pop();

                    int row = curr.xPos;
                    int col = curr.yPos;

                    // Check if the current popped
                    // cell is a valid cell or not
                    if (!horizontalDoors.Contains(n))
                        continue;

                    // Mark the current
                    // cell as visited
                    isDoor[row, col] = true;

                    // Push all the adjacent cells
                    for (int j = 0; i < 2; i++)
                    {
                        int adjx = row + dDir[j];
                        int adjy = col;
                        if (doors.Contains(testGrid[adjx, adjy]))
                            st.Push(testGrid[adjx, adjy]);
                    }
                }
                foreach (Node node in doors)
                {
                    if (isDoor[node.xPos, node.yPos])
                    {
                        temp.Add(node);
                    }
                }
                temp.Remove(temp[randDoor.Next(0, temp.Count)]);
                foreach(Node node in temp)
                {
                    if (horizontalDoors.Contains(node))
                    {
                        horizontalDoors.Remove(node);
                    }
                }
            }
            foreach (Node n in horizontalDoors)
            {
                n.bitValue = 3;
            }
            for (int i = 0; i < doors.Count; i++)
            {
                Node n = doors[i];
                List<Node> temp = new List<Node>();
                Stack st = new Stack();
                st.Push(n);

                // Iterate until the
                // stack is empty
                while (st.Count > 0)
                {

                    // Pop the top pair
                    Node curr = (Node)st.Peek();
                    st.Pop();

                    int row = curr.xPos;
                    int col = curr.yPos;

                    // Check if the current popped
                    // cell is a valid cell or not
                    if (!verticalDoors.Contains(n) || n.bitValue == 3)
                        continue;

                    // Mark the current
                    // cell as visited
                    isDoor[row, col] = true;

                    // Push all the adjacent cells
                    for (int j = 0; i < 2; i++)
                    {
                        int adjx = row + dDir[j];
                        int adjy = col;
                        if (doors.Contains(testGrid[adjx, adjy]))
                            st.Push(testGrid[adjx, adjy]);
                    }
                }
                foreach (Node node in doors)
                {
                    if (isDoor[node.xPos, node.yPos])
                    {
                        temp.Add(node);
                    }
                }
                temp.Remove(temp[randDoor.Next(0, temp.Count)]);
                foreach (Node node in temp)
                {
                    if (verticalDoors.Contains(node))
                    {
                        verticalDoors.Remove(node);
                    }
                }
            }
            foreach (Node n in verticalDoors)
            {
                n.bitValue = 3;
            }
        }
        
        void CleanUp()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (testGrid[i, j].bitValue != 3)
                    {
                        int neighbours = 0;
                        foreach(Node n in station.GetCardinalNeighbours(testGrid[i, j]))
                        {
                            if(n.bitValue != 0)
                            {
                                neighbours++;
                            }
                            if(neighbours <= 1)
                            {
                                testGrid[i, j].bitValue = 0;
                            }
                        }
                    }
                    else
                    {
                        if ((testGrid[i, j++].bitValue == 3) && (testGrid[i, j--].bitValue == 3))
                        {
                            testGrid[i, j].bitValue = 0;
                        }
                        else if ((testGrid[i++, j].bitValue == 3) && (testGrid[i--, j].bitValue == 3))
                        {
                            testGrid[i, j].bitValue = 0;
                        }
                    }
                }
            }
        }

        //Draw method called in Game - should add this instead to be called through an entity manager
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Node node in testGrid)
            {
                int width = node.image.Width;
                int height = node.image.Height;

                Rectangle sourceRectangle = new Rectangle(width, height, width, height); //Image within the texture we want to draw
                Rectangle destinationRectangle = new Rectangle((int)node.worldPosition.X, (int)node.worldPosition.Y, width, height); //Where we want to draw the texture within the game

                spriteBatch.Begin(sortMode: SpriteSortMode.FrontToBack);

                if (node.bitValue == 1)
                {
                    spriteBatch.Draw(node.image, destinationRectangle, sourceRectangle, Color.White);
                }
                else if (node.bitValue == 2)
                {
                    spriteBatch.Draw(node.image, destinationRectangle, sourceRectangle, Color.Green);
                }
                else if (node.bitValue == 3)
                {
                    spriteBatch.Draw(node.image, destinationRectangle, sourceRectangle, Color.Blue);
                }
                else
                {
                    spriteBatch.Draw(node.image, destinationRectangle, sourceRectangle, Color.Black);
                }
                spriteBatch.End();
            }
        }
    }
    //Class for creating rooms
    public class Room
    {
        public int x1, x2, y1, y2, w, h;
        public int invalidPos;

        public Room(int _x1, int _x2, int _y1, int _y2)
        {
            this.x1 = _x1;
            this.x2 = _x2;
            this.y1 = _y1;
            this.y2 = _y2;
            this.w = x2 - x1;
            this.h = y2 - y1;
        }


        public int GetLeft() { return x1; }
        public int GetRight() { return x2; }
        public int GetTop() { return y1; }
        public int GetBottom() { return y2; }

        public bool Intersects(Room other)
        {
            return (x1 <= other.x2 + 1 && x2 >= other.x1 - 1 &&
            y1 <= other.y2 + 1 && other.y2 >= other.y1 - 1);
        }
    }
    //Room template class - this needs to be moved to an xml file
    public class RoomStruct
    {
        public int weight;
        public int width;
        public int height;
        public int cost;
        public bool isStairs;

        public RoomStruct(int _weight, int _width, int _height, int _cost, bool _isStairs)
        {
            this.weight = _weight;
            this.width = _width;
            this.height = _height;
            this.cost = _cost;
            this.isStairs = _isStairs;
        }
    }
    //This gives back an object from a weighted list.
    public static class UsefulFunctions
    {
        private static Random _rnd = new Random();
        public static RoomStruct GetRoom(List<RoomStruct> rooms, int totalWeight)
        {
            // totalWeight is the sum of all rooms' weight

            int randomNumber = _rnd.Next(0, totalWeight);

            RoomStruct selectedRoom = null;
            foreach (RoomStruct room in rooms)
            {
                if (randomNumber < room.weight)
                {
                    selectedRoom = room;
                    break;
                }

                randomNumber = randomNumber - room.weight;
            }
            return selectedRoom;
        }
    }
}