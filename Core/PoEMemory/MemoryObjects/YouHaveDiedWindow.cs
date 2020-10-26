namespace ExileCore.PoEMemory.MemoryObjects
{
    public class YouHaveDiedWindow : Element
    {
        // Currently four options in this window ( only two visible at least in sc)
        private Element DialogOptions => GetChildAtIndex(1);
        public Element ResurrectInTownOption => DialogOptions?.GetChildAtIndex(0);
        public Element ResurrectAtCheckPointOption => DialogOptions?.GetChildAtIndex(2);

    }
}
