using UnityEngine;

namespace SpatialPartitionPattern
{
    public class Grid
    {
        //Need this to convert from world coordinate position to cell position
        int cellSize;

        //How many cells there are along each side of the map
        int numberOfCells;

        //This is the actual grid, where a soldier is in each cell
        //Each individual soldier links to other soldiers in the same cell
        Soldier[,] cells;


        //Init the grid
        public Grid(int mapWidth, int cellSize)
        {
            this.cellSize = cellSize;

            numberOfCells = mapWidth / cellSize;

            cells = new Soldier[numberOfCells, numberOfCells];
        }


        //Remove all soldiers from the grid
        public void Clear()
        {
            System.Array.Clear(cells, 0, cells.Length);
        }


        //Add a unity to the grid
        public void Add(Soldier soldier)
        {
            //Determine which grid cell the soldier is in
            int cellX = GetCellIndex(soldier.soldierTrans.position.x);
            int cellZ = GetCellIndex(soldier.soldierTrans.position.z);

            //Add the soldier to the front of the list for the cell it's in
            soldier.previousSoldier = null;
            soldier.nextSoldier = cells[cellX, cellZ];

            //Associate this cell with this soldier
            cells[cellX, cellZ] = soldier;

            if (soldier.nextSoldier != null)
            {
                //Set this soldier to be the previous soldier of the next soldier of this soldier (linked lists ftw)
                soldier.nextSoldier.previousSoldier = soldier;
            }
        }


        //Get the closest enemy from the grid
        public Soldier FindClosestEnemy(Soldier friendlySoldier)
        {
            //Determine which grid cell the friendly soldier is in
            int cellX = GetCellIndex(friendlySoldier.soldierTrans.position.x);
            int cellZ = GetCellIndex(friendlySoldier.soldierTrans.position.z);

            //Get the first enemy in grid
            Soldier enemy = cells[cellX, cellZ];

            //Find the closest soldier of all in the linked list
            Soldier closestSoldier = null;

            float bestDistSqr = Mathf.Infinity;

            //Loop through the linked list
            while (enemy != null)
            {
                //The distance sqr between the soldier and this enemy
                float distSqr = (enemy.soldierTrans.position - friendlySoldier.soldierTrans.position).sqrMagnitude;

                //If this distance is better than the previous best distance, then we have found an enemy that's closer
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;

                    closestSoldier = enemy;
                }

                //Get the next enemy in the list
                enemy = enemy.nextSoldier;
            }

            return closestSoldier;
        }


        //A soldier in the grid has moved, so see if we need to update in which grid the soldier is
        public void Move(Soldier soldier, Vector3 oldPos)
        {
            //See which cell it was in
            int oldCellX = GetCellIndex(oldPos.x);
            int oldCellZ = GetCellIndex(oldPos.z);

            //See which cell it is in now
            int cellX = GetCellIndex(soldier.soldierTrans.position.x);
            int cellZ = GetCellIndex(soldier.soldierTrans.position.z);

            //If it didn't change cell, we are done
            if (oldCellX == cellX && oldCellZ == cellZ)
            {
                return;
            }

            //Unlink it from the list of its old cell
            if (soldier.previousSoldier != null)
            {
                soldier.previousSoldier.nextSoldier = soldier.nextSoldier;
            }

            if (soldier.nextSoldier != null)
            {
                soldier.nextSoldier.previousSoldier = soldier.previousSoldier;
            }

            //If it's the head of a list, remove it
            if (cells[oldCellX, oldCellZ] == soldier)
            {
                cells[oldCellX, oldCellZ] = soldier.nextSoldier;
            }

            //Add it back to the grid at its new cell
            Add(soldier);
        }


        //Convert a world coordinate to a cell index
        //Clamped so a soldier standing exactly on (or slightly past) the edge of the map still maps to a valid cell
        int GetCellIndex(float coordinate)
        {
            return Mathf.Clamp((int)(coordinate / cellSize), 0, numberOfCells - 1);
        }
    }
}
