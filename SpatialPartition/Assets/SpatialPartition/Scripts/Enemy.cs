using UnityEngine;

namespace SpatialPartitionPattern
{
    //The enemy cube being chased by the spheres
    //Keeping the grid up to date is done by the GameController, so the enemy doesn't
    //have to know whether the spatial partition is switched on or off
    public class Enemy : Soldier
    {
        //The position the soldier is heading for when moving
        Vector3 currentTarget;
        //The width of the map to generate random coordinated within the map
        float mapWidth;


        //Init enemy
        public Enemy(GameObject soldierObj, float mapWidth)
        {
            //Save what we need to save
            this.soldierTrans = soldierObj.transform;

            this.soldierMeshRenderer = soldierObj.GetComponent<MeshRenderer>();

            this.mapWidth = mapWidth;

            this.walkSpeed = 5f;

            //Give it a random coordinate to move towards
            GetNewTarget();
        }


        //Move the cube randomly across the map
        public override void Move()
        {
            //Move towards the target
            soldierTrans.Translate(Vector3.forward * Time.deltaTime * walkSpeed);

            //If the soldier has reached the target, find a new target
            if ((soldierTrans.position - currentTarget).magnitude < 1f)
            {
                GetNewTarget();
            }
        }


        //Give the enemy a new target to move towards and rotate towards that target
        void GetNewTarget()
        {
            currentTarget = new Vector3(Random.Range(0f, mapWidth), 0.5f, Random.Range(0f, mapWidth));

            //Rotate towards the target
            soldierTrans.rotation = Quaternion.LookRotation(currentTarget - soldierTrans.position);
        }
    }
}
