using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Outbreak
{
    //Shows how the outbreak is going and how much work the spatial partition saves,
    //and handles the controls: toggle the partition, restart, and drop zombies with the mouse
    public class OutbreakUI : MonoBehaviour
    {
        public OutbreakController controller;
        public Camera mainCamera;
        public TMP_Text statsText;
        public Toggle spatialPartitionToggle;
        public Button restartButton;

        //The timings are averaged over this many seconds so the numbers are readable
        [SerializeField, Min(0.1f)]
        float refreshInterval = 0.5f;

        //Timings collected since the text was last refreshed
        double sumMilliseconds;
        double minMilliseconds;
        double maxMilliseconds;
        int sampleCount;
        float timeSinceRefresh;

        //The latest average of each mode, so they can be compared. Negative means not measured yet
        double averageWithPartition = -1;
        double averageWithoutPartition = -1;


        void Start()
        {
            spatialPartitionToggle.SetIsOnWithoutNotify(controller.UseSpatialPartition);
            spatialPartitionToggle.onValueChanged.AddListener(OnSpatialPartitionToggled);

            restartButton.onClick.AddListener(Restart);

            ResetSamples();

            statsText.text = "Measuring...";
        }


        void OnDestroy()
        {
            spatialPartitionToggle.onValueChanged.RemoveListener(OnSpatialPartitionToggled);
            restartButton.onClick.RemoveListener(Restart);
        }


        void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                //Goes through the toggle so the UI always shows the current mode
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    spatialPartitionToggle.isOn = !spatialPartitionToggle.isOn;
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    Restart();
                }
            }

            Mouse mouse = Mouse.current;

            //Click on the ground to drop a zombie, unless the click is on the UI
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

                Plane ground = new Plane(Vector3.up, Vector3.zero);

                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);

                    if (point.x >= 0f && point.x <= controller.MapSize && point.z >= 0f && point.z <= controller.MapSize)
                    {
                        controller.SpawnZombie(point);
                    }
                }
            }
        }


        //LateUpdate runs after all Updates, so the controller has measured this frame
        void LateUpdate()
        {
            //The mode can also be changed in the Inspector
            if (spatialPartitionToggle.isOn != controller.UseSpatialPartition)
            {
                spatialPartitionToggle.SetIsOnWithoutNotify(controller.UseSpatialPartition);
            }

            //Ignore a frame that was measured before the mode was switched, so the two modes are never mixed
            if (controller.LastUpdateUsedSpatialPartition == controller.UseSpatialPartition)
            {
                double milliseconds = controller.LastUpdateMilliseconds;

                sumMilliseconds += milliseconds;
                minMilliseconds = System.Math.Min(minMilliseconds, milliseconds);
                maxMilliseconds = System.Math.Max(maxMilliseconds, milliseconds);
                sampleCount += 1;
            }

            timeSinceRefresh += Time.unscaledDeltaTime;

            if (timeSinceRefresh >= refreshInterval && sampleCount > 0)
            {
                RefreshText();

                ResetSamples();
            }
        }


        void OnSpatialPartitionToggled(bool isOn)
        {
            controller.UseSpatialPartition = isOn;

            //Start a new measurement for the new mode
            ResetSamples();
        }


        void Restart()
        {
            controller.Restart();

            //Spawning everyone takes a while, so don't count that frame
            ResetSamples();
        }


        void RefreshText()
        {
            double average = sumMilliseconds / sampleCount;

            bool isPartitionOn = controller.UseSpatialPartition;

            if (isPartitionOn)
            {
                averageWithPartition = average;
            }
            else
            {
                averageWithoutPartition = average;
            }

            string status = controller.IsOutbreakComplete
                ? string.Format("<color=#8BE36B>Everyone turned in {0:0.0} s</color>", controller.OutbreakSeconds)
                : string.Format("{0:0.0} s", controller.OutbreakSeconds);

            long bruteForceChecks = controller.LastBruteForceChecks;

            //How much of the brute force work the grid actually does
            string share = isPartitionOn && bruteForceChecks > 0
                ? string.Format(" <size=80%>({0:0.#}%)</size>", 100.0 * controller.LastDistanceChecks / bruteForceChecks)
                : "";

            statsText.text = string.Format(
                "Humans <b>{0}</b>   <color=#8BE36B>Zombies <b>{1}</b></color>   {2}\n" +
                "\n" +
                "Spatial partition: {3}\n" +
                "Update: <b>{4:0.000} ms</b>  <size=80%>min {5:0.000}  max {6:0.000}</size>\n" +
                "Distance checks: <b>{7:N0}</b>{8}\n" +
                "Checking everyone: {9:N0}\n" +
                "\n" +
                "Average Update time\n" +
                "  With partition: {10}\n" +
                "  Without partition: {11}\n" +
                "\n" +
                "<size=80%>Click: drop a zombie    R: restart    Space: partition</size>",
                controller.HumanCount,
                controller.ZombieCount,
                status,
                isPartitionOn ? "<color=#7CFC7C>ON</color>" : "<color=#FF7070>OFF</color>",
                average,
                minMilliseconds,
                maxMilliseconds,
                controller.LastDistanceChecks,
                share,
                bruteForceChecks,
                FormatAverage(averageWithPartition),
                FormatAverage(averageWithoutPartition));
        }


        static string FormatAverage(double milliseconds)
        {
            return milliseconds < 0 ? "-" : milliseconds.ToString("0.000") + " ms";
        }


        static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }


        void ResetSamples()
        {
            sumMilliseconds = 0;
            minMilliseconds = double.MaxValue;
            maxMilliseconds = 0;
            sampleCount = 0;
            timeSinceRefresh = 0;
        }
    }
}
