using System;
using UnityEngine;

namespace MarbleSort.Data
{
    [Serializable]
    public sealed class LevelCatalogData
    {
        public int version = 1;
        public ConveyorSettingsData conveyor = new ConveyorSettingsData();
        public LevelData[] levels = new LevelData[0];
    }

    [Serializable]
    public sealed class ConveyorSettingsData
    {
        public int slotCount = 24;
        public float unitsPerSecond = 4f;
        public float straightLength = 7f;
        public float turnRadius = 0.75f;
    }

    [Serializable]
    public sealed class LevelData
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public TopGridData topGrid = new TopGridData();
        public ReceiverLaneData[] receiverLanes = new ReceiverLaneData[0];
    }

    [Serializable]
    public sealed class TopGridData
    {
        public int columns = 4;
        public int rows = 4;
        public float cellSpacing = 1f;
        public TopBoxData[] boxes = new TopBoxData[0];
    }

    [Serializable]
    public sealed class TopBoxData
    {
        public string id = string.Empty;
        public string color = string.Empty;
        public int column;
        public int row;
        public bool mystery;
    }

    [Serializable]
    public sealed class ReceiverLaneData
    {
        public string id = string.Empty;
        public SerializableVector3 position = new SerializableVector3();
        public float verticalSpacing = 0.7f;
        public BottomBoxData[] boxes = new BottomBoxData[0];
    }

    [Serializable]
    public sealed class BottomBoxData
    {
        public string id = string.Empty;
        public string color = string.Empty;
    }

    [Serializable]
    public sealed class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
