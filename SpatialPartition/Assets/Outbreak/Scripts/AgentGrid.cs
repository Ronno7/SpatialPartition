using UnityEngine;

namespace Outbreak
{
    //A uniform grid where each cell holds a linked list of the agents standing in it
    //When the cell size is at least the search radius, a search only ever visits the
    //cells next to the searcher's cell, no matter how many agents there are in town
    public class AgentGrid
    {
        float cellSize;
        int cellsPerSide;
        Agent[,] cells;


        public AgentGrid(float mapSize, float cellSize)
        {
            this.cellSize = cellSize;

            cellsPerSide = Mathf.CeilToInt(mapSize / cellSize);

            cells = new Agent[cellsPerSide, cellsPerSide];
        }


        //Remove all agents from the grid
        public void Clear()
        {
            System.Array.Clear(cells, 0, cells.Length);
        }


        //Add the agent to the front of the list of the cell it's standing in
        public void Add(Agent agent)
        {
            int cellX = GetCellIndex(agent.position.x);
            int cellZ = GetCellIndex(agent.position.z);

            agent.cellX = cellX;
            agent.cellZ = cellZ;

            agent.previousInCell = null;
            agent.nextInCell = cells[cellX, cellZ];

            if (agent.nextInCell != null)
            {
                agent.nextInCell.previousInCell = agent;
            }

            cells[cellX, cellZ] = agent;
        }


        //Unlink the agent from the list of the cell it's stored in
        public void Remove(Agent agent)
        {
            if (agent.previousInCell != null)
            {
                agent.previousInCell.nextInCell = agent.nextInCell;
            }
            else
            {
                //It was the first agent in the cell
                cells[agent.cellX, agent.cellZ] = agent.nextInCell;
            }

            if (agent.nextInCell != null)
            {
                agent.nextInCell.previousInCell = agent.previousInCell;
            }

            agent.previousInCell = null;
            agent.nextInCell = null;
        }


        //The agent has moved, so see if it has to move to another cell
        public void UpdateCell(Agent agent)
        {
            int cellX = GetCellIndex(agent.position.x);
            int cellZ = GetCellIndex(agent.position.z);

            if (cellX == agent.cellX && cellZ == agent.cellZ)
            {
                return;
            }

            Remove(agent);

            Add(agent);
        }


        //Find the closest agent within the radius, or null if there's nobody that close
        //Only the cells the search circle overlaps are visited
        public Agent FindClosest(Vector3 position, float radius, ref int distanceChecks)
        {
            int minX = GetCellIndex(position.x - radius);
            int maxX = GetCellIndex(position.x + radius);
            int minZ = GetCellIndex(position.z - radius);
            int maxZ = GetCellIndex(position.z + radius);

            Agent closest = null;

            float bestDistSqr = radius * radius;

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    //Loop through the linked list of this cell
                    for (Agent other = cells[x, z]; other != null; other = other.nextInCell)
                    {
                        distanceChecks += 1;

                        float distSqr = (other.position - position).sqrMagnitude;

                        if (distSqr < bestDistSqr)
                        {
                            bestDistSqr = distSqr;

                            closest = other;
                        }
                    }
                }
            }

            return closest;
        }


        //Convert a world coordinate to a cell index, clamped so the edge of the map is still inside the grid
        int GetCellIndex(float coordinate)
        {
            return Mathf.Clamp((int)(coordinate / cellSize), 0, cellsPerSide - 1);
        }
    }
}
