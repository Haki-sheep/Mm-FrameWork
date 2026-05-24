using System;


namespace MieMieFrameTools
{
    [Serializable]
    public class SlotData : ISlot
    {
        public string SlotId { get; set; }
        public string DisplayName { get; set; }
        public long CreateTime { get; set; }
        public long LastSaveTime { get; set; }

        public SlotData()
        {
            SlotId = Guid.NewGuid().ToString();
            DisplayName = "New Slot";
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LastSaveTime = DateTime.Now.Ticks;
        }
        public SlotData(string displayName) : this() => this.DisplayName = displayName;
    }
}