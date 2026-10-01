using System.Collections.Generic;
using UnityEngine;

namespace SpatialPartitionPattern
{
    public class GameController : MonoBehaviour
    {
        public GameObject friendlyObj;
        public GameObject enemyObj;

        //Change materials to detect which enemy is the closest
        public Material enemyMaterial;
        public Material closestEnemyMaterial;

        //To get a cleaner workspace, parent all soldiers to these empty gameobjects
        public Transform enemyParent;
        public Transform friendlyParent;

        //On: find the closest enemy with the grid (fast)
        //Off: find the closest enemy by checking every enemy (slow), and don't use the grid at all
        [SerializeField]
        bool useSpatialPartition = true;

        //Number of soldiers on each team
        //Increase it to make the difference between the fast and the slow version easier to see
        [SerializeField, Min(1)]
        int numberOfSoldiers = 100;

        //Store all soldiers in these lists
        List<Soldier> enemySoldiers = new List<Soldier>();
        List<Soldier> friendlySoldiers = new List<Soldier>();

        //Save the closest enemies to easier change back its material
        List<Soldier> closestEnemies = new List<Soldier>();

        //Grid data
        float mapWidth = 50f;
        int cellSize = 10;

        //The Spatial Partition grid
        Grid grid;

        //The grid isn't kept up to date while the spatial partition is off,
        //so it has to be rebuilt before it can be used again
        bool isGridUpToDate;

        //Measures how long Update takes
        System.Diagnostics.Stopwatch updateStopwatch = new System.Diagnostics.Stopwatch();

        public bool UseSpatialPartition
        {
            get { return useSpatialPartition; }
            set { useSpatialPartition = value; }
        }

        public int NumberOfSoldiers
        {
            get { return numberOfSoldiers; }
        }

        //How long the most recent Update took, in milliseconds
        public double LastUpdateMilliseconds { get; private set; }

        //If the most recent Update used the spatial partition
        public bool LastUpdateUsedSpatialPartition { get; private set; }


        void Start()
        {
            //Create a new grid
            grid = new Grid((int)mapWidth, cellSize);

            //Add random enemies and friendly and store them in a list
            for (int i = 0; i < numberOfSoldiers; i++)
            {
                //Give the enemy a random position
                Vector3 randomPos = new Vector3(Random.Range(0f, mapWidth), 0.5f, Random.Range(0f, mapWidth));

                //Create a new enemy
                GameObject newEnemy = Instantiate(enemyObj, randomPos, Quaternion.identity);

                //Add the enemy to a list
                enemySoldiers.Add(new Enemy(newEnemy, mapWidth));

                //Parent it
                newEnemy.transform.parent = enemyParent;


                //Give the friendly a random position
                randomPos = new Vector3(Random.Range(0f, mapWidth), 0.5f, Random.Range(0f, mapWidth));

                //Create a new friendly
                GameObject newFriendly = Instantiate(friendlyObj, randomPos, Quaternion.identity);

                //Add the friendly to a list
                friendlySoldiers.Add(new Friendly(newFriendly, mapWidth));

                //Parent it
                newFriendly.transform.parent = friendlyParent;
            }

            //Add all enemies to the grid
            RebuildGrid();
        }


        void Update()
        {
            updateStopwatch.Restart();

            //Read the setting once so the whole frame uses the same mode
            bool usePartition = useSpatialPartition;

            if (usePartition && !isGridUpToDate)
            {
                RebuildGrid();
            }

            //Move the enemies
            for (int i = 0; i < enemySoldiers.Count; i++)
            {
                Soldier enemy = enemySoldiers[i];

                Vector3 oldPos = enemy.soldierTrans.position;

                enemy.Move();

                //See if the the cube has moved to another cell
                if (usePartition)
                {
                    grid.Move(enemy, oldPos);
                }
            }

            isGridUpToDate = usePartition;

            //Reset material of the closest enemies
            for (int i = 0; i < closestEnemies.Count; i++)
            {
                closestEnemies[i].soldierMeshRenderer.material = enemyMaterial;
            }

            //Reset the list with closest enemies
            closestEnemies.Clear();

            //For each friendly, find the closest enemy and change its color and chase it
            for (int i = 0; i < friendlySoldiers.Count; i++)
            {
                Soldier closestEnemy;

                if (usePartition)
                {
                    //The fast version with spatial partition
                    closestEnemy = grid.FindClosestEnemy(friendlySoldiers[i]);
                }
                else
                {
                    //The slow version which checks every enemy
                    closestEnemy = FindClosestEnemySlow(friendlySoldiers[i]);
                }

                //If we found an enemy
                if (closestEnemy != null)
                {
                    //Change material
                    closestEnemy.soldierMeshRenderer.material = closestEnemyMaterial;

                    closestEnemies.Add(closestEnemy);

                    //Move the friendly in the direction of the enemy
                    friendlySoldiers[i].Move(closestEnemy);
                }
            }

            updateStopwatch.Stop();

            LastUpdateMilliseconds = updateStopwatch.Elapsed.TotalMilliseconds;
            LastUpdateUsedSpatialPartition = usePartition;
        }


        //Put every enemy in the cell it's standing in
        void RebuildGrid()
        {
            grid.Clear();

            for (int i = 0; i < enemySoldiers.Count; i++)
            {
                grid.Add(enemySoldiers[i]);
            }

            isGridUpToDate = true;
        }


        //Find the closest enemy - slow version
        Soldier FindClosestEnemySlow(Soldier soldier)
        {
            Soldier closestEnemy = null;

            float bestDistSqr = Mathf.Infinity;

            //Loop thorugh all enemies
            for (int i = 0; i < enemySoldiers.Count; i++)
            {
                //The distance sqr between the soldier and this enemy
                float distSqr = (soldier.soldierTrans.position - enemySoldiers[i].soldierTrans.position).sqrMagnitude;

                //If this distance is better than the previous best distance, then we have found an enemy that's closer
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;

                    closestEnemy = enemySoldiers[i];
                }
            }

            return closestEnemy;
        }
    }
}
