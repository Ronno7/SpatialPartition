using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SpatialPartitionPattern
{
    //Shows how long the GameController's Update takes, and lets the user switch the spatial partition on and off
    public class SpatialPartitionUI : MonoBehaviour
    {
        public GameController gameController;
        public TMP_Text statsText;
        public Toggle spatialPartitionToggle;

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
            spatialPartitionToggle.SetIsOnWithoutNotify(gameController.UseSpatialPartition);
            spatialPartitionToggle.onValueChanged.AddListener(OnSpatialPartitionToggled);

            ResetSamples();

            statsText.text = "Measuring...";
        }


        void OnDestroy()
        {
            spatialPartitionToggle.onValueChanged.RemoveListener(OnSpatialPartitionToggled);
        }


        void Update()
        {
            //Keyboard shortcut. Goes through the toggle so the UI always shows the current mode
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                spatialPartitionToggle.isOn = !spatialPartitionToggle.isOn;
            }
        }


        //LateUpdate runs after all Updates, so the GameController has measured this frame
        void LateUpdate()
        {
            //The mode can also be changed in the Inspector
            if (spatialPartitionToggle.isOn != gameController.UseSpatialPartition)
            {
                spatialPartitionToggle.SetIsOnWithoutNotify(gameController.UseSpatialPartition);
            }

            //Ignore a frame that was measured before the mode was switched, so the two modes are never mixed
            if (gameController.LastUpdateUsedSpatialPartition == gameController.UseSpatialPartition)
            {
                double milliseconds = gameController.LastUpdateMilliseconds;

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
            gameController.UseSpatialPartition = isOn;

            //Start a new measurement for the new mode
            ResetSamples();
        }


        void RefreshText()
        {
            double average = sumMilliseconds / sampleCount;

            bool isPartitionOn = gameController.UseSpatialPartition;

            if (isPartitionOn)
            {
                averageWithPartition = average;
            }
            else
            {
                averageWithoutPartition = average;
            }

            statsText.text = string.Format(
                "Spatial partition: {0}\n" +
                "GameController.Update: <b>{1:0.000} ms</b>\n" +
                "<size=80%>min {2:0.000}  max {3:0.000}  ({4} {8})</size>\n" +
                "\n" +
                "Average Update time\n" +
                "  With partition: {5}\n" +
                "  Without partition: {6}\n" +
                "\n" +
                "<size=80%>{7} enemies, {7} friendlies</size>",
                isPartitionOn ? "<color=#7CFC7C>ON</color>" : "<color=#FF7070>OFF</color>",
                average,
                minMilliseconds,
                maxMilliseconds,
                sampleCount,
                FormatAverage(averageWithPartition),
                FormatAverage(averageWithoutPartition),
                gameController.NumberOfSoldiers,
                sampleCount == 1 ? "frame" : "frames");
        }


        static string FormatAverage(double milliseconds)
        {
            return milliseconds < 0 ? "-" : milliseconds.ToString("0.000") + " ms";
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
