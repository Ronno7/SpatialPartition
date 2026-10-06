using UnityEngine;

namespace Outbreak
{
    //A person in town. Humans and zombies share this class so a bitten human can turn into a zombie
    public class Agent
    {
        public Transform transform;
        public MeshRenderer meshRenderer;

        //The position is kept here and written to the transform once per frame,
        //so the distance checks don't have to ask the engine for it
        public Vector3 position;

        public bool isZombie;
        //A human who can see a zombie is running away
        public bool isPanicking;
        //Bitten this frame, turns into a zombie at the end of the frame
        public bool isBitten;

        //Where the agent is heading when it has nothing better to do
        public Vector3 heading;
        public float wanderTimer;

        //Like in the tutorial, each grid cell is a linked list of the agents standing in it
        public Agent previousInCell;
        public Agent nextInCell;
        //The cell the agent is stored in, so it can be unlinked without working it out again
        public int cellX;
        public int cellZ;
    }
}
