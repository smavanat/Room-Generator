using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;



    

    namespace please_work
    {

        public class Pathfinding
        {
            Node[,] grid;
            Grid map;
            int gridSizeX;
            int gridSizeY;
            public List<Node> returnedPath;
            public Pathfinding(int x, int y, Node[,] _grid, Grid _map)
            {
                this.grid = _grid;
                this.gridSizeX = x;
                this.gridSizeY = y;
                this.map = _map;
            }
            public void FindPath(Node startNode, Node targetNode)
            {

                Heap<Node> openSet = new Heap<Node>(map.MaxSize);
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    Node currentNode = openSet.RemoveFirst();
                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        returnedPath = RetracePath(startNode, targetNode);
                        return;
                    }

                    foreach (Node neighbour in map.GetNeighbours(currentNode))
                    {
                        if (!neighbour.isWalkable || closedSet.Contains(neighbour))
                        {
                            continue;
                        }

                        int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                        if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                        {
                            neighbour.gCost = newMovementCostToNeighbour;
                            neighbour.hCost = GetDistance(neighbour, targetNode);
                            neighbour.parent = currentNode;

                            if (!openSet.Contains(neighbour))
                                openSet.Add(neighbour);
                            else
                            {
                                //openSet.UpdateItem(neighbour);
                            }
                        }
                    }
                }
            }

            List<Node> RetracePath(Node startNode, Node endNode)
            {
                List<Node> path = new List<Node>();
                Node currentNode = endNode;

                while (currentNode != startNode)
                {
                    path.Add(currentNode);
                    currentNode = currentNode.parent;
                }
                path.Reverse();
                return path;
            }

            int GetDistance(Node nodeA, Node nodeB)
            {
                int dstX = (int)MathF.Abs(nodeA.xPos - nodeB.xPos);
                int dstY = (int)MathF.Abs(nodeA.yPos - nodeB.yPos);

                if (dstX > dstY)
                    return 14 * dstY + 10 * (dstX - dstY);
                return 14 * dstX + 10 * (dstY - dstX);
            }

            
        }
        public class Heap<T> where T : IHeapItem<T>
        {

            T[] items;
            int currentItemCount;

            public Heap(int maxHeapSize)
            {
                items = new T[maxHeapSize];
            }
            public void Add(T item)
            {
                item.HeapIndex = currentItemCount;
                items[currentItemCount] = item;
                SortUp(item);
                currentItemCount++;
            }
            public T RemoveFirst()
            {
                T firstItem = items[0];
                currentItemCount--;
                items[0] = items[currentItemCount];
                items[0].HeapIndex = 0;
                SortDown(items[0]);
                return firstItem;
            }
            public void UpdateItem(T item)
            {
                SortUp(item);
            }
            public int Count
            {
                get
                {
                    return currentItemCount;
                }
            }
            public bool Contains(T item)
            {
                return Equals(items[item.HeapIndex], item);
            }

            void SortDown(T item)
            {
                while (true)
                {
                    int childIndexLeft = item.HeapIndex * 2 + 1;
                    int childIndexRight = item.HeapIndex * 2 + 2;
                    int swapIndex = 0;

                    if (childIndexLeft < currentItemCount)
                    {
                        swapIndex = childIndexLeft;

                        if (childIndexRight < currentItemCount)
                        {
                            if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                            {
                                swapIndex = childIndexRight;
                            }
                        }

                        if (item.CompareTo(items[swapIndex]) < 0)
                        {
                            Swap(item, items[swapIndex]);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            void SortUp(T item)
            {
                int parentIndex = (item.HeapIndex - 1) / 2;

                while (true)
                {
                    T parentItem = items[parentIndex];
                    if (item.CompareTo(parentItem) > 0)
                    {
                        Swap(item, parentItem);
                    }
                    else
                    {
                        break;
                    }

                    parentIndex = (item.HeapIndex - 1) / 2;
                }
            }

            void Swap(T itemA, T itemB)
            {
                items[itemA.HeapIndex] = itemB;
                items[itemB.HeapIndex] = itemA;
                int itemAIndex = itemA.HeapIndex;
                itemA.HeapIndex = itemB.HeapIndex;
                itemB.HeapIndex = itemAIndex;
            }
        }

        public interface IHeapItem<T> : IComparable<T>
        {
            int HeapIndex
            {
                get;
                set;
            }
        }
    }