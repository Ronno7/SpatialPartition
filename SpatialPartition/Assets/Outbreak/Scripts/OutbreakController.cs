using System.Collections.Generic;
using UnityEngine;

namespace Outbreak
{
    //Zombie tag. Humans run from the closest zombie they can see, zombies chase the closest
    //human they can see, and a bitten human turns into a zombie
    //Every agent asks "who is the closest one within my sight radius?" every frame,
    //which is the question the spatial partition answers quickly
    public class OutbreakController : MonoBehaviour
    {
        public GameObject personPrefab;
        public Transform agentParent;

        public Material humanMaterial;
        public Material panickingHumanMaterial;
        public Material zombieMaterial;

        //On: search the grid cells around the agent (fast)
        //Off: check every agent on the other team (slow), and don't use the grids at all
        [SerializeField]
        bool useSpatialPartition = true;

        [SerializeField, Min(1)]
        int numberOfHumans = 1500;

        [SerializeField, Min(0)]
        int startingZombies = 1;

        [SerializeField, Min(10f)]
        float mapSize = 72f;

        //Also the size of the grid cells, so a search only has to look in the neighboring cells
        [SerializeField, Min(1f)]
        float sightRadius = 6f;

        [SerializeField, Min(0.1f)]
        float biteRadius = 0.6f;

        //Zombies are faster than panicking humans, so a chase doesn't last long
        [SerializeField]
        float humanWalkSpeed = 1.2f;
        [SerializeField]
        float humanRunSpeed = 2.2f;
        [SerializeField]
        float zombieWanderSpeed = 1f;
        [SerializeField]
        float zombieChaseSpeed = 3.2f;

        List<Agent> humans = new List<Agent>();
        List<Agent> zombies = new List<Agent>();

        //Humans bitten this frame
        List<Agent> bitten = new List<Agent>();

        //One grid per team, because humans only look for zombies and zombies only look for humans
        AgentGrid humanGrid;
        AgentGrid zombieGrid;

        //The grids aren't kept up to date while the spatial partition is off,
        //so they have to be rebuilt before they can be used again
        bool areGridsUpToDate;

        //Keep agents this far from the edge of the map
        const float EdgeMargin = 0.5f;

        //Height of the agents' pivot above the ground
        float agentHeight;

        float outbreakStartTime;
        float outbreakDuration;

        int distanceChecks;

        System.Diagnostics.Stopwatch updateStopwatch = new System.Diagnostics.Stopwatch();

        static System.Predicate<Agent> isZombie = agent => agent.isZombie;

        public bool UseSpatialPartition
        {
            get { return useSpatialPartition; }
            set { useSpatialPartition = value; }
        }

        public float MapSize { get { return mapSize; } }
        public float CellSize { get { return sightRadius; } }
        public int HumanCount { get { return humans.Count; } }
        public int ZombieCount { get { return zombies.Count; } }

        public bool IsOutbreakComplete { get; private set; }

        public float OutbreakSeconds
        {
            get { return IsOutbreakComplete ? outbreakDuration : Time.time - outbreakStartTime; }
        }

        //How long the most recent Update took, in milliseconds
        public double LastUpdateMilliseconds { get; private set; }

        //If the most recent Update used the spatial partition
        public bool LastUpdateUsedSpatialPartition { get; private set; }

        //How many distances the most recent Update compared
        public int LastDistanceChecks { get; private set; }

        //How many distances the most recent Update would have compared by checking everyone on the other team
        public long LastBruteForceChecks { get; private set; }


        void Start()
        {
            humanGrid = new AgentGrid(mapSize, sightRadius);
            zombieGrid = new AgentGrid(mapSize, sightRadius);

            //Half the height of the mesh, so the agents stand on the ground
            MeshFilter meshFilter = personPrefab.GetComponent<MeshFilter>();
            agentHeight = meshFilter.sharedMesh.bounds.extents.y * personPrefab.transform.localScale.y;

            SpawnPopulation();
        }


        void Update()
        {
            updateStopwatch.Restart();

            //Read the setting once so the whole frame uses the same mode
            bool usePartition = useSpatialPartition;

            if (usePartition && !areGridsUpToDate)
            {
                RebuildGrids();
            }

            distanceChecks = 0;

            //Everyone on one team looks at everyone on the other team
            long bruteForceChecks = 2L * humans.Count * zombies.Count;

            float deltaTime = Time.deltaTime;

            //Humans run from the closest zombie they can see, or wander around
            for (int i = 0; i < humans.Count; i++)
            {
                Agent human = humans[i];

                Agent zombie = usePartition
                    ? zombieGrid.FindClosest(human.position, sightRadius, ref distanceChecks)
                    : FindClosestBruteForce(zombies, human.position, sightRadius);

                SetPanicking(human, zombie != null);

                if (zombie != null)
                {
                    Move(human, Direction(zombie.position, human.position), humanRunSpeed, deltaTime);
                }
                else
                {
                    Move(human, Wander(human, deltaTime), humanWalkSpeed, deltaTime);
                }

                if (usePartition)
                {
                    humanGrid.UpdateCell(human);
                }
            }

            //Zombies chase the closest human they can see, and bite when they catch up
            for (int i = 0; i < zombies.Count; i++)
            {
                Agent zombie = zombies[i];

                Agent human = usePartition
                    ? humanGrid.FindClosest(zombie.position, sightRadius, ref distanceChecks)
                    : FindClosestBruteForce(humans, zombie.position, sightRadius);

                if (human == null)
                {
                    Move(zombie, Wander(zombie, deltaTime), zombieWanderSpeed, deltaTime);
                }
                else if ((human.position - zombie.position).sqrMagnitude < biteRadius * biteRadius)
                {
                    if (!human.isBitten)
                    {
                        human.isBitten = true;

                        bitten.Add(human);
                    }
                }
                else
                {
                    Move(zombie, Direction(zombie.position, human.position), zombieChaseSpeed, deltaTime);
                }

                if (usePartition)
                {
                    zombieGrid.UpdateCell(zombie);
                }
            }

            TurnBittenIntoZombies(usePartition);

            areGridsUpToDate = usePartition;

            //Show the new positions
            for (int i = 0; i < humans.Count; i++)
            {
                humans[i].transform.position = humans[i].position;
            }

            for (int i = 0; i < zombies.Count; i++)
            {
                zombies[i].transform.position = zombies[i].position;
            }

            if (!IsOutbreakComplete && humans.Count == 0)
            {
                IsOutbreakComplete = true;

                outbreakDuration = Time.time - outbreakStartTime;
            }

            updateStopwatch.Stop();

            LastUpdateMilliseconds = updateStopwatch.Elapsed.TotalMilliseconds;
            LastUpdateUsedSpatialPartition = usePartition;
            LastDistanceChecks = distanceChecks;
            LastBruteForceChecks = bruteForceChecks;
        }


        //Remove everyone and start a new outbreak
        public void Restart()
        {
            for (int i = 0; i < humans.Count; i++)
            {
                Destroy(humans[i].transform.gameObject);
            }

            for (int i = 0; i < zombies.Count; i++)
            {
                Destroy(zombies[i].transform.gameObject);
            }

            humans.Clear();
            zombies.Clear();
            bitten.Clear();

            SpawnPopulation();
        }


        //Drop a new zombie into town
        public void SpawnZombie(Vector3 position)
        {
            Agent zombie = CreateAgent(ClampToMap(position), true);

            zombies.Add(zombie);

            //If the grids are out of date the zombie will be added when they are rebuilt
            if (useSpatialPartition && areGridsUpToDate)
            {
                zombieGrid.Add(zombie);
            }
        }


        void SpawnPopulation()
        {
            for (int i = 0; i < numberOfHumans; i++)
            {
                humans.Add(CreateAgent(RandomPosition(), false));
            }

            for (int i = 0; i < startingZombies; i++)
            {
                //Patient zero starts in the middle of town
                Vector3 position = i == 0 ? new Vector3(mapSize * 0.5f, agentHeight, mapSize * 0.5f) : RandomPosition();

                zombies.Add(CreateAgent(position, true));
            }

            areGridsUpToDate = false;

            IsOutbreakComplete = false;

            outbreakStartTime = Time.time;
        }


        Agent CreateAgent(Vector3 position, bool isZombie)
        {
            GameObject agentObj = Instantiate(personPrefab, position, Quaternion.identity, agentParent);

            Agent agent = new Agent();

            agent.transform = agentObj.transform;
            agent.meshRenderer = agentObj.GetComponent<MeshRenderer>();
            agent.position = position;
            agent.isZombie = isZombie;

            agent.meshRenderer.sharedMaterial = isZombie ? zombieMaterial : humanMaterial;

            return agent;
        }


        //Put every agent in the cell it's standing in
        void RebuildGrids()
        {
            humanGrid.Clear();
            zombieGrid.Clear();

            for (int i = 0; i < humans.Count; i++)
            {
                humanGrid.Add(humans[i]);
            }

            for (int i = 0; i < zombies.Count; i++)
            {
                zombieGrid.Add(zombies[i]);
            }

            areGridsUpToDate = true;
        }


        void TurnBittenIntoZombies(bool usePartition)
        {
            if (bitten.Count == 0)
            {
                return;
            }

            for (int i = 0; i < bitten.Count; i++)
            {
                Agent agent = bitten[i];

                if (usePartition)
                {
                    humanGrid.Remove(agent);
                }

                agent.isBitten = false;
                agent.isPanicking = false;
                agent.isZombie = true;
                agent.wanderTimer = 0f;

                agent.meshRenderer.sharedMaterial = zombieMaterial;

                zombies.Add(agent);

                if (usePartition)
                {
                    zombieGrid.Add(agent);
                }
            }

            humans.RemoveAll(isZombie);

            bitten.Clear();
        }


        //Find the closest agent within the radius by checking every agent in the list - slow version
        Agent FindClosestBruteForce(List<Agent> candidates, Vector3 position, float radius)
        {
            Agent closest = null;

            float bestDistSqr = radius * radius;

            for (int i = 0; i < candidates.Count; i++)
            {
                distanceChecks += 1;

                float distSqr = (candidates[i].position - position).sqrMagnitude;

                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;

                    closest = candidates[i];
                }
            }

            return closest;
        }


        void SetPanicking(Agent human, bool isPanicking)
        {
            //Only change the material when the state changes
            if (human.isPanicking == isPanicking)
            {
                return;
            }

            human.isPanicking = isPanicking;

            human.meshRenderer.sharedMaterial = isPanicking ? panickingHumanMaterial : humanMaterial;
        }


        void Move(Agent agent, Vector3 direction, float speed, float deltaTime)
        {
            Vector3 newPos = agent.position + direction * (speed * deltaTime);

            Vector3 clampedPos = ClampToMap(newPos);

            //Walked into the edge of town, so pick a new direction next time it wanders
            if (clampedPos.x != newPos.x || clampedPos.z != newPos.z)
            {
                agent.wanderTimer = 0f;
            }

            agent.position = clampedPos;
        }


        //Keep walking in the same direction for a while, then pick a new one
        Vector3 Wander(Agent agent, float deltaTime)
        {
            agent.wanderTimer -= deltaTime;

            if (agent.wanderTimer <= 0f)
            {
                agent.wanderTimer = Random.Range(1f, 4f);

                Vector2 random = Random.insideUnitCircle.normalized;

                //Nudge the direction towards the middle of town so nobody hugs the edge
                Vector3 towardsMiddle = (new Vector3(mapSize * 0.5f, agent.position.y, mapSize * 0.5f) - agent.position) / mapSize;

                agent.heading = (new Vector3(random.x, 0f, random.y) + towardsMiddle).normalized;
            }

            return agent.heading;
        }


        static Vector3 Direction(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;

            direction.y = 0f;

            return direction.normalized;
        }


        Vector3 RandomPosition()
        {
            return new Vector3(Random.Range(EdgeMargin, mapSize - EdgeMargin), agentHeight, Random.Range(EdgeMargin, mapSize - EdgeMargin));
        }


        Vector3 ClampToMap(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, EdgeMargin, mapSize - EdgeMargin);
            position.y = agentHeight;
            position.z = Mathf.Clamp(position.z, EdgeMargin, mapSize - EdgeMargin);

            return position;
        }
    }
}
