using System;
using System.Collections.Generic;


namespace MieMieFrameTools
{
    [Serializable]
    public class SlotsIndexData
    {
        public string currentSlotId;
        public List<SlotData> slots { get; set; } = new();

        public SlotsIndexData()
        {
            slots = new List<SlotData>();
        }
    }
}